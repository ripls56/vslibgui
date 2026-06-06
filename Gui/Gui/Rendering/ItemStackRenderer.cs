using System;
using System.Collections.Generic;
using System.Linq;
using Gui.Core.Basic;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace Gui.Rendering;

/// <summary>
///     Renders Vintage Story <see cref="ItemStack" /> objects into cached
///     <see cref="SKImage" /> instances via an offscreen GL framebuffer.
///     <para>
///         Cache identity combines the collectible, render size, and a composite
///         <em>visual bucket</em> (see <see cref="RegisterVisualBucket" />) that quantizes
///         continuously-varying visual attributes such as temperature glow and transition
///         (spoilage/curing) progress. Quantizing lets a cooling item, or food that slowly
///         perishes, reuse one cached render per visual step instead of re-rendering every
///         frame and briefly showing a stale fallback — which reads as flicker. The raw
///         backing attributes are excluded from exact identity via <see cref="_ignoredAttributes" />.
///     </para>
/// </summary>
public class ItemStackRenderer : IPreSkiaRenderer, IDisposable
{
    /// <summary>
    ///     Maps an item stack's continuously-varying visual state to a discrete bucket so that
    ///     micro-changes share one cached render while distinct visual stages still get their own.
    /// </summary>
    /// <param name="world">World accessor, for values derived from game time (e.g. temperature decay).</param>
    /// <param name="stack">The stack being cached.</param>
    /// <returns>A bucket id folded into the cache key together with every other provider.</returns>
    public delegate int VisualBucketProvider(IWorldAccessor world, ItemStack stack);

    private const int MaxCacheEntries = 8192;

    /// <summary>Per-bucket cap to stop spammy items from evicting unrelated cache entries via global LRU.</summary>
    private const int MaxEntriesPerKey = 512;

    // VS skips bucket-liquid rendering below ~24 px projected size; rendering at MinRenderSize and downscaling
    // via Skia preserves liquid for small slots (e.g. hotbar at 56 px display size).
    private const int MinRenderSize = 64;
    private const int NoAttributesHash = 17;

    // Frames between glow-refresh scans. Bucket crossings during cooling are seconds apart, so a coarse
    // scan keeps glowing slots current without per-frame cost.
    private const int GlowRefreshIntervalFrames = 6;

    private const uint GlRgba8 = 0x8058;
    private const uint GlTexture2D = 0x0DE1;

    private static string[] _ignoredAttributes =
    [
        "toolMode",
        "renderVariant"
    ];

    private static readonly HashSet<string> IgnoredAttributesSet =
        new(_ignoredAttributes, StringComparer.Ordinal);

    private static readonly Dictionary<string, VisualBucketProvider> BucketProvidersByAttribute =
        new(StringComparer.Ordinal);

    private static readonly List<VisualBucketProvider> VisualBucketProviders = new();

    private readonly Dictionary<ItemCacheKey, List<AttributedEntry>> _cache = new();
    private readonly ICoreClientAPI _capi;
    private readonly DummySlot _dummySlot = new();
    private readonly ClientMain _game;

    private readonly Dictionary<RenderItemStack, int> _glowingSlots = new();
    private readonly List<RenderItemStack> _glowScratch = [];
    private readonly Queue<(ItemCacheKey Key, AttributedEntry Entry)> _idleEvictionQueue = new();
    private readonly Dictionary<ItemCacheKey, List<RenderRequest>> _pendingByKey = new();
    private readonly List<RenderItemStack> _pendingListeners = [];

    private readonly List<(RenderRequest Req, List<RenderRequest> Bucket, int Index)>
        _pendingSnapshot = new();

    private FrameBufferRef? _fbo;
    private int _fboSize;
    private long _frameCounter;
    private long _idleEvictionQueueRebuiltFrame = -1;
    private int _pendingCount;
    private int _totalCacheCount;

    static ItemStackRenderer()
    {
        IgnoreAttribute("topCrustType");
        RegisterVisualBucket("transitionstate",
            static (_, stack) => ComputeTransitionBucket(stack));
        RegisterVisualBucket("temperature", ComputeTemperatureBucket);
    }

    public ItemStackRenderer(
        ICoreClientAPI capi
    )
    {
        _capi = capi;
        _game = (ClientMain)_capi.World;
    }

    /// <summary>
    ///     Debounce delay (ms): prevents GL render of cells that only briefly entered viewport during
    ///     fast scroll.
    /// </summary>
    public static int DebounceMs { get; set; } = 80;

    /// <summary>
    ///     Max FBO renders per <see cref="ProcessQueue" /> call; spreads scroll-burst work across
    ///     frames.
    /// </summary>
    public static int MaxRendersPerFrame { get; set; } = 7;

    /// <summary>
    ///     Drop queued requests older than this (ms); prevents stale queue from starving visible
    ///     cells.
    /// </summary>
    public static int StaleMs { get; set; } = 2000;

    /// <summary>Frames before an idle cache entry is evicted. Default = 30 s at 60 fps.</summary>
    public static int IdleEvictionFrames { get; set; } = 30 * 60;

    /// <summary>
    ///     Max cache disposals per <see cref="ProcessQueue" /> call; spreads GL.DeleteTexture cost
    ///     across frames.
    /// </summary>
    public static int MaxEvictionsPerFrame { get; set; } = 20;

    /// <summary>How often (frames) the idle-eviction pass rebuilds its work queue.</summary>
    public static int CleanupIntervalFrames { get; set; } = 60;

    /// <summary>
    ///     Discrete steps per transition (Perish, Cure…). Quantizes progress so micro-changes share one
    ///     cache entry;
    ///     distinct visual stages still get separate renders. Default 16 → ~6 % per step.
    /// </summary>
    public static int TransitionBuckets { get; set; } = 16;

    /// <summary>
    ///     Discrete glow steps between <see cref="GlowStartTemperature" /> and
    ///     <see cref="GlowFullTemperature" />. Quantizes temperature so a cooling item reuses one
    ///     cached render per step instead of re-rendering every frame. Default 16.
    /// </summary>
    public static int TemperatureBuckets { get; set; } = 16;

    /// <summary>
    ///     Temperature (°C) at or above which an item starts to glow. Below this, all temperatures
    ///     share a single non-glowing cache entry (bucket 0).
    /// </summary>
    public static float GlowStartTemperature { get; set; } = 500f;

    /// <summary>Temperature (°C) at which glow is treated as saturated for cache bucketing.</summary>
    public static float GlowFullTemperature { get; set; } = 1500f;

    public void Dispose()
    {
        foreach (var entries in _cache.Values)
        foreach (var e in entries)
        {
            e.Image?.Dispose();
        }

        _cache.Clear();
        _totalCacheCount = 0;
        _glowingSlots.Clear();

        if (_fbo != null)
        {
            _game.Platform.DisposeFrameBuffer(_fbo);
            _fbo = null;
        }
    }

    public bool HasPendingRequests => _pendingCount > 0;

    /// <summary>
    ///     Processes queued render requests. Call before <see cref="SkiaRenderer.Begin" /> while GL
    ///     state is clean.
    /// </summary>
    public void ProcessQueue()
    {
        _frameCounter++;

        EvictIdleEntries();
        RefreshGlowingSlots();

        if (_pendingCount == 0)
        {
            _pendingListeners.Clear();
            return;
        }

        var now = Environment.TickCount;
        DropStaleRequests(now);

        var rendered = 0;
        SortPendingIntoSnapshot();
        foreach (var (req, bucket, index) in _pendingSnapshot)
        {
            if (rendered >= MaxRendersPerFrame)
            {
                break;
            }

            if (now - req.EnqueuedTickMs < DebounceMs)
            {
                continue;
            }

            RemovePendingAt(req.PrimaryKey, bucket, index);

            if (FindEntry(req.PrimaryKey, req.Snapshot) != null)
            {
                continue;
            }

            var image = RenderToImage(req.Snapshot, req.RenderSize);
            if (image == null)
            {
                continue;
            }

            EvictIfNeeded();
            if (!_cache.TryGetValue(req.PrimaryKey, out var list))
            {
                list = new List<AttributedEntry>();
                _cache[req.PrimaryKey] = list;
            }

            EvictBucketIfFull(list);
            list.Add(new AttributedEntry
            {
                Snapshot = req.Snapshot,
                Image = image,
                LastUsedFrame = _frameCounter,
                AttrHash = req.AttrHash
            });
            _totalCacheCount++;
            rendered++;
        }

        for (var i = 0; i < _pendingListeners.Count; i++)
        {
            _pendingListeners[i].OnBitmapReady();
        }

        _pendingListeners.Clear();
    }

    /// <summary>
    ///     Adds an extra attribute name to ignore when comparing item stacks for
    ///     cache identity. Use this for items whose visual appearance does not
    ///     depend on attributes that vary every frame (pies, transition timers,
    ///     creation timestamps, etc.) so they don't spawn new cache entries on
    ///     every render.
    /// </summary>
    public static void IgnoreAttribute(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
        {
            return;
        }

        if (!IgnoredAttributesSet.Add(attributeName))
        {
            return;
        }

        var arr = new string[IgnoredAttributesSet.Count];
        IgnoredAttributesSet.CopyTo(arr);
        _ignoredAttributes = arr;
    }

    /// <summary>
    ///     Registers a quantizer for a continuously-varying visual attribute. Stacks whose quantized
    ///     value matches share one cached render; the raw <paramref name="attributeName" /> is excluded
    ///     from exact cache identity (see <see cref="IgnoreAttribute" />) so only the quantized bucket
    ///     distinguishes renders. Use for any attribute that changes nearly every frame but maps to a
    ///     small set of distinct appearances (temperature, spoilage, charge, wetness…).
    /// </summary>
    /// <param name="attributeName">Backing attribute name to exclude from exact identity.</param>
    /// <param name="provider">Maps a stack to a discrete visual bucket.</param>
    public static void RegisterVisualBucket(string attributeName, VisualBucketProvider provider)
    {
        if (provider == null || string.IsNullOrEmpty(attributeName))
        {
            return;
        }

        IgnoreAttribute(attributeName);
        if (!BucketProvidersByAttribute.TryAdd(attributeName, provider))
        {
            return;
        }

        VisualBucketProviders.Add(provider);
    }

    // Returns a hash of quantized transition progress values so micro-changes (1.22→1.23 h) share a cache entry.
    private static int ComputeTransitionBucket(ItemStack stack)
    {
        if (stack.Attributes is not TreeAttribute attrs)
        {
            return 0;
        }

        var ts = attrs.GetTreeAttribute("transitionstate");
        if (ts == null)
        {
            return 0;
        }

        if (ts["transitionedHours"]?.GetValue() is not float[] transitioned)
        {
            return 0;
        }

        if (ts["transitionHours"]?.GetValue() is not float[] transHours)
        {
            return 0;
        }

        var fresh = ts["freshHours"]?.GetValue() as float[];

        var hash = 0;
        var n = Math.Min(transitioned.Length, transHours.Length);
        var buckets = Math.Max(1, TransitionBuckets);
        for (var i = 0; i < n; i++)
        {
            var freshH = fresh != null && i < fresh.Length ? fresh[i] : 0f;
            var progress = transHours[i] > 0f ? (transitioned[i] - freshH) / transHours[i] : 0f;
            var b = Math.Clamp((int)(progress * buckets), 0, buckets - 1);
            hash = hash * 31 + b;
        }

        return hash;
    }

    // Built-in provider: quantizes the effective (time-decayed) temperature so glow steps share entries.
    private static int ComputeTemperatureBucket(IWorldAccessor world, ItemStack stack)
    {
        if (stack?.Collectible == null)
        {
            return 0;
        }

        return TemperatureToBucket(stack.Collectible.GetTemperature(world, stack));
    }

    // Quantizes a temperature (C) into a glow step. Cold stacks share bucket 0; glowing stacks map to
    // 1..TemperatureBuckets so a cooling item reuses one render per step instead of re-rendering each frame.
    internal static int TemperatureToBucket(float temperatureC)
    {
        // Negated comparison so NaN (corrupt/modded temperature) also resolves to the non-glowing bucket.
        if (!(temperatureC >= GlowStartTemperature))
        {
            return 0;
        }

        var buckets = Math.Max(1, TemperatureBuckets);
        var span = Math.Max(1f, GlowFullTemperature - GlowStartTemperature);
        var normalized = Math.Clamp((temperatureC - GlowStartTemperature) / span, 0f, 1f);
        return 1 + Math.Clamp((int)(normalized * (buckets - 1)), 0, buckets - 1);
    }

    private static bool TryNormalize(ItemStack? stack, int renderSize, out int effectiveSize)
    {
        if (stack?.Collectible == null)
        {
            effectiveSize = 0;
            return false;
        }

        effectiveSize = Math.Max(renderSize, MinRenderSize);
        return true;
    }

    private static int ComputeAttrHash(ItemStack stack)
    {
        if (stack?.Attributes is not TreeAttribute attrs || attrs.Count == 0)
        {
            return NoAttributesHash;
        }

        var hash = GetTreeAttributeHash(attrs);
        return hash == 0 ? NoAttributesHash : hash;
    }

    private static int GetTreeAttributeHash(TreeAttribute tree)
    {
        var xor = 0;
        foreach (var key in tree.Keys)
        {
            if (IgnoredAttributesSet.Contains(key))
            {
                continue;
            }

            var kh = StringComparer.Ordinal.GetHashCode(key);
            var v = tree[key];
            if (v == null)
            {
                continue;
            }

            var vh = 0;

            switch (v)
            {
                case TreeAttribute subTree:
                    vh = GetTreeAttributeHash(subTree);
                    break;

                case ItemstackAttribute stackAttr:
                {
                    var innerStack = stackAttr.value;
                    if (innerStack != null)
                    {
                        var itemClass = (int)innerStack.Class;
                        var itemId = innerStack.Collectible?.Id ?? 0;

                        var isLiquid = innerStack.Collectible?.MatterState ==
                                       EnumMatterState.Liquid;
                        var stackSize = isLiquid ? innerStack.StackSize : 1;

                        // if (isLiquid)
                        // {
                        // itemId ^= 54321;
                        // }

                        var innerAttrHash = ComputeAttrHash(innerStack);

                        vh = itemClass ^ (itemId * 397) ^ (stackSize * 13) ^ innerAttrHash;
                    }

                    break;
                }

                default:
                    vh = StringComparer.Ordinal.GetHashCode(v.ToJsonToken() ?? "");
                    break;
            }

            xor ^= (kh * 397) ^ vh;
        }

        return xor;
    }

    // Snapshot is needed: RemovePendingAt mutates buckets during iteration.
    private void SortPendingIntoSnapshot()
    {
        _pendingSnapshot.Clear();
        foreach (var (_, bucket) in _pendingByKey)
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                _pendingSnapshot.Add((bucket[i], bucket, i));
            }
        }

        _pendingSnapshot.Sort(static (a, b) =>
            b.Req.EnqueuedTickMs.CompareTo(a.Req.EnqueuedTickMs));
    }

    private void RemovePendingAt(ItemCacheKey key, List<RenderRequest> bucket, int index)
    {
        if (index >= bucket.Count)
        {
            return;
        }

        bucket.RemoveAt(index);
        _pendingCount--;
        if (bucket.Count == 0)
        {
            _pendingByKey.Remove(key);
        }
    }

    private void EvictIdleEntries()
    {
        if (_frameCounter - _idleEvictionQueueRebuiltFrame >= CleanupIntervalFrames)
        {
            BuildIdleEvictionQueue();
            _idleEvictionQueueRebuiltFrame = _frameCounter;
        }

        var disposed = 0;
        while (disposed < MaxEvictionsPerFrame && _idleEvictionQueue.TryDequeue(out var pending))
        {
            if (!_cache.TryGetValue(pending.Key, out var bucket))
            {
                continue;
            }

            var idx = bucket.IndexOf(pending.Entry);
            if (idx < 0)
            {
                continue;
            }

            if (_frameCounter - pending.Entry.LastUsedFrame < IdleEvictionFrames)
            {
                continue;
            }

            pending.Entry.Image?.Dispose();
            bucket.RemoveAt(idx);
            if (bucket.Count == 0)
            {
                _cache.Remove(pending.Key);
            }

            _totalCacheCount--;
            disposed++;
        }
    }

    private void BuildIdleEvictionQueue()
    {
        _idleEvictionQueue.Clear();
        var cutoff = _frameCounter - IdleEvictionFrames;
        foreach (var (key, bucket) in _cache)
        foreach (var entry in bucket)
        {
            if (entry.LastUsedFrame < cutoff)
            {
                _idleEvictionQueue.Enqueue((key, entry));
            }
        }
    }

    /// <summary>
    ///     Registers a slot for glow refresh while its item is hot enough to glow. Called every paint;
    ///     the stored bucket tracks the visual state the slot is currently showing.
    /// </summary>
    internal void TrackGlowingIfHot(RenderItemStack slot)
    {
        var stack = slot.CurrentStack;
        if (stack?.Collectible == null || !IsGlowing(stack))
        {
            return;
        }

        _glowingSlots[slot] = ComputeVisualBucket(stack);
    }

    private bool IsGlowing(ItemStack stack) =>
        stack.Collectible.GetTemperature(_capi.World, stack) >= GlowStartTemperature;

    // Repaints glowing slots when cooling carries them into a new temperature bucket, so the glow steps
    // down on its own even while the slot is idle (not hovered) and never accrues a stale hot frame that
    // would pop on the next interaction. Detached or cooled slots are pruned here.
    private void RefreshGlowingSlots()
    {
        if (_frameCounter % GlowRefreshIntervalFrames != 0 || _glowingSlots.Count == 0)
        {
            return;
        }

        _glowScratch.Clear();
        _glowScratch.AddRange(_glowingSlots.Keys);
        foreach (var slot in _glowScratch)
        {
            var stack = slot.CurrentStack;
            if (slot.Parent == null || stack?.Collectible == null || !IsGlowing(stack))
            {
                _glowingSlots.Remove(slot);
                continue;
            }

            var bucket = ComputeVisualBucket(stack);
            if (_glowingSlots[slot] == bucket)
            {
                continue;
            }

            _glowingSlots[slot] = bucket;
            slot.InvalidateGlow();
        }
    }

    private void DropStaleRequests(int now)
    {
        var emptyKeys = (List<ItemCacheKey>?)null;
        foreach (var (key, bucket) in _pendingByKey)
        {
            for (var i = bucket.Count - 1; i >= 0; i--)
            {
                if (now - bucket[i].EnqueuedTickMs <= StaleMs)
                {
                    continue;
                }

                bucket.RemoveAt(i);
                _pendingCount--;
            }

            if (bucket.Count == 0)
            {
                (emptyKeys ??= new List<ItemCacheKey>()).Add(key);
            }
        }

        if (emptyKeys == null)
        {
            return;
        }

        foreach (var key in emptyKeys)
        {
            _pendingByKey.Remove(key);
        }
    }

    /// <summary>
    ///     Returns cached image for <paramref name="itemStack" />, or enqueues a render and returns
    ///     <c>null</c>.
    /// </summary>
    public SKImage? GetOrQueue(
        ItemStack itemStack,
        int renderSize = 48
    )
    {
        if (!TryNormalize(itemStack, renderSize, out var effectiveSize))
        {
            return null;
        }

        var key = BuildKey(itemStack, effectiveSize);
        var entry = FindEntry(key, itemStack);
        if (entry != null)
        {
            entry.LastUsedFrame = _frameCounter;
            return entry.Image;
        }

        if (IsPending(key, itemStack))
        {
            return null;
        }

        if (!_pendingByKey.TryGetValue(key, out var bucket))
        {
            bucket = new List<RenderRequest>();
            _pendingByKey[key] = bucket;
        }

        bucket.Add(new RenderRequest
        {
            Snapshot = itemStack.Clone(),
            RenderSize = effectiveSize,
            PrimaryKey = key,
            EnqueuedTickMs = Environment.TickCount,
            AttrHash = ComputeAttrHash(itemStack)
        });
        _pendingCount++;
        return null;
    }

    /// <summary>
    ///     Transition-bucket-agnostic lookup: returns the MRU image for this item ignoring bucket.
    ///     Used while a new-bucket render is pending so the slot shows the previous stage instead of going
    ///     blank.
    /// </summary>
    public SKImage? GetCachedAny(
        ItemStack itemStack,
        int renderSize = 48
    )
    {
        if (!TryNormalize(itemStack, renderSize, out var effectiveSize))
        {
            return null;
        }

        var cls = itemStack.Class;
        var id = itemStack.Collectible.Id;

        AttributedEntry? best = null;
        var bestFrame = long.MinValue;
        foreach (var (key, entries) in _cache)
        {
            if (key.Class != cls || key.Id != id || key.RenderSize != effectiveSize)
            {
                continue;
            }

            foreach (var e in entries)
            {
                if (e.Image == null)
                {
                    continue;
                }

                if (e.LastUsedFrame <= bestFrame)
                {
                    continue;
                }

                bestFrame = e.LastUsedFrame;
                best = e;
            }
        }

        if (best == null)
        {
            return null;
        }

        best.LastUsedFrame = _frameCounter;
        return best.Image;
    }

    /// <summary>Removes all cached images for the given item's collectible type.</summary>
    public void Invalidate(
        ItemStack? itemStack
    )
    {
        if (itemStack?.Collectible == null)
        {
            return;
        }

        var cls = itemStack.Class;
        var id = itemStack.Collectible.Id;

        foreach (var key in _cache.Keys.Where(k => k.Class == cls && k.Id == id).ToList())
        {
            _totalCacheCount -= _cache[key].Count;
            foreach (var e in _cache[key])
            {
                e.Image?.Dispose();
            }

            _cache.Remove(key);
        }
    }

    internal void RegisterPending(
        RenderItemStack renderObject
    ) =>
        _pendingListeners.Add(renderObject);

    private ItemCacheKey BuildKey(
        ItemStack stack,
        int renderSize
    ) =>
        new(stack.Class, stack.Collectible.Id, renderSize, ComputeVisualBucket(stack));

    // Folds every registered visual-bucket provider into one composite bucket for the cache key.
    private int ComputeVisualBucket(ItemStack stack)
    {
        var hash = 0;
        foreach (var provider in VisualBucketProviders)
        {
            hash = hash * 31 + provider(_capi.World, stack);
        }

        return hash;
    }

    private AttributedEntry? FindEntry(
        ItemCacheKey key,
        ItemStack stack
    )
    {
        if (!_cache.TryGetValue(key, out var entries))
        {
            return null;
        }

        var stackHash = ComputeAttrHash(stack);
        foreach (var e in entries)
        {
            if (e.AttrHash != stackHash)
            {
                continue;
            }

            if (e.Snapshot.Equals(_capi.World, stack, _ignoredAttributes))
            {
                return e;
            }
        }

        return null;
    }

    private bool IsPending(
        ItemCacheKey key,
        ItemStack stack
    )
    {
        if (!_pendingByKey.TryGetValue(key, out var bucket))
        {
            return false;
        }

        var stackHash = ComputeAttrHash(stack);
        foreach (var req in bucket)
        {
            if (req.AttrHash != stackHash)
            {
                continue;
            }

            if (req.Snapshot.Equals(_capi.World, stack, _ignoredAttributes))
            {
                return true;
            }
        }

        return false;
    }

    private SKImage? RenderToImage(
        ItemStack itemStack,
        int size
    )
    {
        EnsureFbo(size);
        if (_fbo == null)
        {
            return null;
        }

        var savedState = GlStateSnapshot.Capture();
        try
        {
            _game.Platform.LoadFrameBuffer(_fbo, _fbo.ColorTextureIds[0]);

            GL.Viewport(0, 0, size, size);

            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.ScissorTest);
            GL.Disable(EnableCap.SampleAlphaToCoverage);
            _game.Platform.GlEnableDepthTest();
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.ColorMask(true, true, true, true);
            _game.Platform.GlDisableCullFace();
            _game.Platform.GlToggleBlend(true);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha
            );
            _game.Platform.ClearFrameBuffer(
                _fbo,
                [0, 0, 0, 0],
                clearColorBuffers: true
            );
            GL.Clear(ClearBufferMask.DepthBufferBit);

            _game.OrthoMode(size, size, true);

            var halfSize = size / 2f;
            _dummySlot.Itemstack = itemStack;
            _capi.Render.RenderItemstackToGui(
                _dummySlot,
                halfSize,
                halfSize,
                90.0,
                halfSize * 0.85f,
                -1,
                true,
                false,
                false
            );

            return CaptureFramebufferAsImage(size);
        }
        catch (Exception ex)
        {
            _capi.Logger.Warning(
                $"[GUI] Failed to render ItemStack: {ex.Message}"
            );
            return null;
        }
        finally
        {
            _game.PerspectiveMode();
            _game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
            savedState.Restore();
        }
    }

    // Wraps FBO color buffer as SKImage via Skia GL backend — avoids GPU↔CPU↔GPU round-trip of GL.ReadPixels.
    private SKImage? CaptureFramebufferAsImage(int size)
    {
        var grContext = GuiModSystem.Instance?.SkiaRenderer?.GrContext;
        if (grContext == null)
        {
            return null;
        }

        var textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, 0, 0, size, size, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        grContext.ResetContext();

        var glInfo = new GRGlTextureInfo
        {
            Id = (uint)textureId, Target = GlTexture2D, Format = GlRgba8
        };
        var backendTexture = new GRBackendTexture(size, size, false, glInfo);

        var capturedTextureId = textureId;
        var image = SKImage.FromTexture(
            grContext,
            backendTexture,
            GRSurfaceOrigin.TopLeft,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            null,
            _ =>
            {
                GL.DeleteTexture(capturedTextureId);
                backendTexture.Dispose();
            },
            null);

        if (image == null)
        {
            backendTexture.Dispose();
            GL.DeleteTexture(capturedTextureId);
        }

        return image;
    }

    private void EnsureFbo(
        int size
    )
    {
        if (_fbo != null && _fboSize >= size)
        {
            return;
        }

        if (_fbo != null)
        {
            _game.Platform.DisposeFrameBuffer(_fbo);
        }

        _fboSize = size;
        var attrs = new FramebufferAttrs("gui-itemstack-fbo", size, size);
        attrs.Attachments =
        [
            new FramebufferAttrsAttachment
            {
                AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
                Texture = new RawTexture
                {
                    Width = size,
                    Height = size,
                    PixelFormat = EnumTexturePixelFormat.Rgba,
                    PixelInternalFormat = EnumTextureInternalFormat.Rgba8
                }
            },
            new FramebufferAttrsAttachment
            {
                AttachmentType = EnumFramebufferAttachment.DepthAttachment,
                Texture = new RawTexture
                {
                    Width = size,
                    Height = size,
                    PixelFormat = EnumTexturePixelFormat.DepthComponent,
                    PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
                }
            }
        ];
        _fbo = _game.Platform.CreateFramebuffer(attrs);
    }

    private void EvictBucketIfFull(List<AttributedEntry> bucket)
    {
        if (bucket.Count < MaxEntriesPerKey)
        {
            return;
        }

        var idx = FindLruIndex(bucket);
        bucket[idx].Image?.Dispose();
        bucket.RemoveAt(idx);
        _totalCacheCount--;
    }

    private static int FindLruIndex(List<AttributedEntry> bucket)
    {
        var idx = 0;
        var oldest = bucket[0].LastUsedFrame;
        for (var i = 1; i < bucket.Count; i++)
        {
            if (bucket[i].LastUsedFrame >= oldest)
            {
                continue;
            }

            oldest = bucket[i].LastUsedFrame;
            idx = i;
        }

        return idx;
    }

    private void EvictIfNeeded()
    {
        if (_totalCacheCount < MaxCacheEntries)
        {
            return;
        }

        AttributedEntry? oldest = null;
        ItemCacheKey oldestKey = default;
        var oldestFrame = long.MaxValue;

        foreach (var (key, entries) in _cache)
        foreach (var e in entries)
        {
            if (e.LastUsedFrame < oldestFrame)
            {
                oldestFrame = e.LastUsedFrame;
                oldest = e;
                oldestKey = key;
            }
        }

        if (oldest == null)
        {
            return;
        }

        oldest.Image?.Dispose();
        var list = _cache[oldestKey];
        list.Remove(oldest);
        if (list.Count == 0)
        {
            _cache.Remove(oldestKey);
        }

        _totalCacheCount--;
    }

    private sealed class AttributedEntry
    {
        public int AttrHash;
        public SKImage? Image;
        public long LastUsedFrame;
        public required ItemStack Snapshot;
    }

    private struct RenderRequest
    {
        public ItemStack Snapshot;
        public int RenderSize;
        public ItemCacheKey PrimaryKey;
        public int EnqueuedTickMs;
        public int AttrHash;
    }

    private readonly record struct ItemCacheKey(
        EnumItemClass Class,
        int Id,
        int RenderSize,
        int VisualBucket
    );
}

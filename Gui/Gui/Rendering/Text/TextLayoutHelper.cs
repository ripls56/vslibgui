using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Rendering.Text;

/// <summary>
///     Caches <see cref="SKFont" /> and <see cref="SKTypeface" /> instances keyed by
///     family+size+weight.
///     Font entries follow an LRU eviction policy bounded by <see cref="MaxFontCacheSize" />;
///     typefaces
///     are shared across font sizes and intentionally live as long as the process (or until
///     <see cref="ClearCache" />).
/// </summary>
public static class TextLayoutHelper
{
    private static readonly Dictionary<FontKey, LinkedListNode<CacheEntry>> FontCache = new();

    private static readonly LinkedList<CacheEntry> LruOrder = new();
    private static readonly Dictionary<string, SKTypeface> TypefaceCache = new();

    /// <summary>
    ///     Maximum number of <see cref="SKFont" /> entries kept in the LRU cache. When exceeded, the
    ///     least-recently-used entry is evicted and disposed. Defaults to 500.
    /// </summary>
    internal static int MaxFontCacheSize { get; set; } = 500;

    /// <summary>Current number of entries in the font LRU cache. Test/diagnostic only.</summary>
    internal static int FontCacheCount => FontCache.Count;

    /// <summary>
    ///     Returns true if a font entry for the given key is currently cached (without promoting it
    ///     in the LRU order). Test/diagnostic only.
    /// </summary>
    internal static bool IsCached(
        string fontFamily,
        float fontSize,
        FontWeight weight
    )
    {
        var resolvedFamily = FontRegistry.ResolveFontFamily(fontFamily);
        if (!TypefaceCache.TryGetValue(resolvedFamily + weight, out var typeface))
        {
            return false;
        }

        return FontCache.ContainsKey(new FontKey(typeface.Handle, RoundSize(fontSize)));
    }

    private static float RoundSize(
        float fontSize
    )
    {
        var rounded = (float)Math.Round(fontSize * 2) / 2f;
        return rounded < 1
            ? 1
            : rounded;
    }

    public static SKFont GetFont(
        SKTypeface typeface,
        float fontSize
    )
    {
        var roundedSize = RoundSize(fontSize);
        var key = new FontKey(typeface.Handle, roundedSize);

        if (FontCache.TryGetValue(key, out var node))
        {
            LruOrder.Remove(node);
            LruOrder.AddFirst(node);
            return node.Value.Font;
        }

        EvictIfFull();

        var font = new SKFont(typeface, fontSize)
        {
            Subpixel = true,
            Edging = SKFontEdging.Antialias,
            LinearMetrics = true,
            Hinting = SKFontHinting.None
        };

        var entry = new CacheEntry { Key = key, Font = font };
        var newNode = new LinkedListNode<CacheEntry>(entry);
        LruOrder.AddFirst(newNode);
        FontCache[key] = newNode;
        return font;
    }

    public static SKFont GetFont(
        string fontFamily,
        float fontSize,
        FontWeight weight
    )
    {
        var resolvedFamily = FontRegistry.ResolveFontFamily(fontFamily);

        if (!TypefaceCache.TryGetValue(
                resolvedFamily + weight,
                out var typeface
            ))
        {
            typeface = FontRegistry.GetCustomTypeface(
                resolvedFamily,
                weight
            );

            if (typeface == null)
            {
                var skWeight = weight switch
                {
                    FontWeight.Bold => SKFontStyleWeight.Bold,
                    FontWeight.SemiBold => SKFontStyleWeight.SemiBold,
                    _ => SKFontStyleWeight.Normal
                };
                var skSlant = weight == FontWeight.Italic
                    ? SKFontStyleSlant.Italic
                    : SKFontStyleSlant.Upright;
                typeface = SKTypeface.FromFamilyName(
                    resolvedFamily,
                    skWeight,
                    SKFontStyleWidth.Normal,
                    skSlant
                );
            }

            TypefaceCache[resolvedFamily + weight] = typeface;
        }

        return GetFont(typeface, fontSize);
    }

    private static void EvictIfFull()
    {
        while (FontCache.Count >= MaxFontCacheSize && LruOrder.Last is { } oldest)
        {
            LruOrder.RemoveLast();
            FontCache.Remove(oldest.Value.Key);
            oldest.Value.Font.Dispose();
        }
    }

    public static Vector2 MeasureText(
        string text,
        string fontFamily,
        float fontSize,
        FontWeight weight,
        float boldness = 0
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        var font = GetFont(fontFamily, fontSize, weight);
        var originalEmbolden = font.Embolden;
        font.Embolden = boldness > 0;

        var primaryMetrics = font.Metrics;
        var leading = primaryMetrics.Leading;
        var lineHeight = primaryMetrics.Descent - primaryMetrics.Ascent + leading;
        if (lineHeight <= 0)
        {
            lineHeight = fontSize * 1.2f;
        }

        var lines = text.Split('\n');
        float maxWidth = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var runs = TextShaper.Shape(line, font);
            float lineWidth = 0;
            foreach (var run in runs)
            {
                lineWidth += run.Advance;
                var runLineHeight = run.Descent - run.Ascent + leading;
                if (runLineHeight > lineHeight)
                {
                    lineHeight = runLineHeight;
                }
            }

            if (lineWidth > maxWidth)
            {
                maxWidth = lineWidth;
            }
        }

        font.Embolden = originalEmbolden;
        return new Vector2(maxWidth, lines.Length * lineHeight);
    }

    internal static List<string> BreakIntoLines(
        string text,
        SKFont font,
        float maxWidth
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return [""];
        }

        var result = new List<string>();
        var hardLines = text.Split('\n');

        foreach (var hardLine in hardLines)
        {
            if (string.IsNullOrEmpty(hardLine) || font.MeasureText(hardLine) <= maxWidth)
            {
                result.Add(hardLine);
                continue;
            }

            SoftWrapLine(
                hardLine,
                font,
                maxWidth,
                result
            );
        }

        return result;
    }

    /// <summary>
    ///     Returns true if <paramref name="c" /> belongs to a script where every
    ///     character boundary is a valid line-break opportunity (CJK ideographs, Hangul,
    ///     Hiragana, Katakana).
    /// </summary>
    internal static bool IsCjkBreakable(char c)
    {
        return c is >= '　' and <= '鿿'
            or >= '가' and <= '힯'
            or >= '豈' and <= '﫿';
    }

    private static void SoftWrapLine(
        string line,
        SKFont font,
        float maxWidth,
        List<string> result
    )
    {
        var pos = 0;
        while (pos < line.Length)
        {
            var available = line.Length - pos;
            var count = CountCharsFitting(line, pos, available, font, maxWidth);
            if (count <= 0)
            {
                count = 1;
            }

            if (count >= available)
            {
                result.Add(line.Substring(pos, available).TrimEnd());
                break;
            }

            var breakCount = LastBreakOpportunity(line, pos, count);
            if (breakCount > 0)
            {
                count = breakCount;
            }

            result.Add(line.Substring(pos, count).TrimEnd());
            pos += count;
            while (pos < line.Length && line[pos] == ' ')
            {
                pos++;
            }
        }
    }

    private static int CountCharsFitting(string line, int start, int maxChars, SKFont font,
        float maxWidth)
    {
        var lo = 0;
        var hi = maxChars;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (ShapedWidth(line, start, mid, font) <= maxWidth)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    private static float ShapedWidth(string line, int start, int count, SKFont font)
    {
        if (count <= 0)
        {
            return 0f;
        }

        var runs = TextShaper.Shape(line.Substring(start, count), font);
        var w = 0f;
        foreach (var run in runs)
        {
            w += run.Advance;
        }

        return w;
    }

    private static int LastBreakOpportunity(string line, int start, int count)
    {
        var last = start + count - 1;

        // Segment already ends at a natural break: CJK char or the next char is CJK.
        if (IsCjkBreakable(line[last]))
        {
            return count;
        }

        var nextIdx = start + count;
        if (nextIdx < line.Length && IsCjkBreakable(line[nextIdx]))
        {
            return count;
        }

        // Scan backward for a space or CJK/Latin transition.
        for (var i = last; i > start; i--)
        {
            if (line[i] == ' ')
            {
                return i - start + 1;
            }

            if (IsCjkBreakable(line[i]) || (i > start && IsCjkBreakable(line[i - 1])))
            {
                return i - start;
            }
        }

        return -1;
    }

    internal static float MeasureTextWidth(
        string text,
        SKFont font
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        return font.MeasureText(text);
    }

    public static float GetVerticalOffset(
        SKFontMetrics metrics,
        float containerHeight
    ) =>
        containerHeight / 2f - (metrics.Ascent + metrics.Descent) / 2f;

    /// <summary>
    ///     Disposes and clears all cached fonts and typefaces, and resets the <see cref="TextShaper" />
    ///     and <see cref="FontRunSplitter" /> caches. Intended for teardown (tests, shutdown),
    ///     not hot-path eviction — the per-cache LRU policies handle bounded-size maintenance.
    /// </summary>
    public static void ClearCache()
    {
        foreach (var node in LruOrder)
        {
            node.Font.Dispose();
        }

        foreach (var tf in TypefaceCache.Values)
        {
            tf.Dispose();
        }

        FontCache.Clear();
        LruOrder.Clear();
        TypefaceCache.Clear();
        TextShaper.ClearCache();
        FontRunSplitter.ClearCache();
    }

    private readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly IntPtr TypefaceHandle;
        public readonly float Size;

        public FontKey(
            IntPtr typefaceHandle,
            float size
        )
        {
            TypefaceHandle = typefaceHandle;
            Size = size;
        }

        public bool Equals(
            FontKey other
        ) =>
            TypefaceHandle == other.TypefaceHandle && Size.Equals(other.Size);

        public override bool Equals(
            object? obj
        ) =>
            obj is FontKey other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                TypefaceHandle,
                Size
            );
        }
    }

    private sealed class CacheEntry
    {
        public SKFont Font = null!;
        public FontKey Key;
    }
}

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Gui.Rendering.Text;

/// <summary>
///     Splits a string into <see cref="FontRun" />s, each covered by a single typeface.
///     Uses <see cref="SKFontManager.Default" /> to find a system fallback for codepoints the
///     primary typeface cannot render. Results are cached per (codepoint, primary, weight) up
///     to <see cref="MaxCacheSize" /> entries with LRU eviction.
/// </summary>
public static class FontRunSplitter
{
    internal const int MaxCacheSize = 2000;

    private static readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> Cache = new();
    private static readonly LinkedList<CacheEntry> LruOrder = new();

    /// <summary>Number of cached entries. Test/diagnostic only.</summary>
    internal static int CacheCount => Cache.Count;

    /// <summary>
    ///     Resolves the run breakdown for <paramref name="text" /> using
    ///     <paramref name="primary" />.
    /// </summary>
    public static IReadOnlyList<FontRun> Split(
        string text,
        SKTypeface primary,
        FontWeight weight
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var runs = new List<FontRun>();
        var i = 0;
        var runStart = 0;
        SKTypeface? runTypeface = null;

        while (i < text.Length)
        {
            var codepoint = char.ConvertToUtf32(text, i);
            var charLen = char.IsHighSurrogate(text[i]) ? 2 : 1;
            var typeface = ResolveTypeface(codepoint, primary, weight);

            if (runTypeface == null)
            {
                runTypeface = typeface;
            }
            else if (!ReferenceEquals(runTypeface, typeface))
            {
                runs.Add(new FontRun(runStart, i - runStart, runTypeface));
                runStart = i;
                runTypeface = typeface;
            }

            i += charLen;
        }

        if (runTypeface != null && runStart < text.Length)
        {
            runs.Add(new FontRun(runStart, text.Length - runStart, runTypeface));
        }

        return runs;
    }

    /// <summary>Clears the codepoint -&gt; typeface cache. Test/diagnostic only.</summary>
    public static void ClearCache()
    {
        Cache.Clear();
        LruOrder.Clear();
    }

    private static SKTypeface ResolveTypeface(
        int codepoint,
        SKTypeface primary,
        FontWeight weight
    )
    {
        var key = new CacheKey(codepoint, primary.Handle, weight);
        if (Cache.TryGetValue(key, out var node))
        {
            LruOrder.Remove(node);
            LruOrder.AddFirst(node);
            return node.Value.Typeface;
        }

        SKTypeface chosen;
        if (primary.GetGlyph(codepoint) != 0)
        {
            chosen = primary;
        }
        else
        {
            var (skWeight, skSlant) = WeightToSk(weight);
            chosen = SKFontManager.Default.MatchCharacter(
                null,
                skWeight,
                (int)SKFontStyleWidth.Normal,
                skSlant,
                null,
                codepoint
            ) ?? primary;
        }

        EvictIfFull();

        var entry = new CacheEntry(key, chosen);
        var newNode = new LinkedListNode<CacheEntry>(entry);
        LruOrder.AddFirst(newNode);
        Cache[key] = newNode;
        return chosen;
    }

    private static (int Weight, SKFontStyleSlant Slant) WeightToSk(
        FontWeight weight
    )
    {
        return weight switch
        {
            FontWeight.Bold => ((int)SKFontStyleWeight.Bold, SKFontStyleSlant.Upright),
            FontWeight.SemiBold => ((int)SKFontStyleWeight.SemiBold, SKFontStyleSlant.Upright),
            FontWeight.Italic => ((int)SKFontStyleWeight.Normal, SKFontStyleSlant.Italic),
            _ => ((int)SKFontStyleWeight.Normal, SKFontStyleSlant.Upright)
        };
    }

    private static void EvictIfFull()
    {
        while (Cache.Count >= MaxCacheSize && LruOrder.Last is { } oldest)
        {
            LruOrder.RemoveLast();
            Cache.Remove(oldest.Value.Key);
        }
    }

    private readonly record struct CacheKey(int Codepoint, IntPtr PrimaryHandle, FontWeight Weight);

    private sealed record CacheEntry(CacheKey Key, SKTypeface Typeface);
}

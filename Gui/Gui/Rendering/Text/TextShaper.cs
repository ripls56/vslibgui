using System;
using System.Collections.Generic;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Gui.Rendering.Text;

/// <summary>
///     Shapes text into positioned glyph runs using HarfBuzz via <see cref="SKShaper" />.
///     Calls <see cref="FontRunSplitter" /> to segment by typeface coverage, then shapes each
///     run with a per-typeface cached <see cref="SKShaper" />. Results are cached per
///     (text, primary typeface handle, rounded size, weight) up to <see cref="MaxCacheSize" />
///     entries.
/// </summary>
public static class TextShaper
{
    internal const int MaxCacheSize = 500;

    private static readonly Dictionary<IntPtr, LinkedListNode<ShaperEntry>> Shapers = new();
    private static readonly LinkedList<ShaperEntry> ShaperOrder = new();
    private static readonly Dictionary<ShapeKey, LinkedListNode<ShapedEntry>> Cache = new();
    private static readonly LinkedList<ShapedEntry> LruOrder = new();

    /// <summary>
    ///     Maximum number of cached <see cref="SKShaper" /> instances. Each one pins the
    ///     HarfBuzz face tables built from its typeface, so an unbounded map would retain
    ///     every system fallback typeface ever matched for the rest of the session.
    /// </summary>
    internal static int MaxShaperCacheSize { get; set; } = 64;

    /// <summary>Number of cached shapers. Test/diagnostic only.</summary>
    internal static int ShaperCacheCount => Shapers.Count;

    /// <summary>
    ///     False once HarfBuzz has been found unusable. Shaping then degrades to
    ///     advance-only glyph positioning for the rest of the session.
    /// </summary>
    internal static bool ShapingAvailable { get; private set; } = true;

    /// <summary>
    ///     Raised once when HarfBuzz turns out to be unloadable, so the host can log it.
    ///     Set by <see cref="GuiModSystem" /> at startup.
    /// </summary>
    internal static Action<Exception>? OnShapingUnavailable { get; set; }

    /// <summary>
    ///     Shapes <paramref name="text" /> using <paramref name="primaryFont" /> and the
    ///     system fallback chain.
    /// </summary>
    public static ShapedRun[] Shape
    (
        string text,
        SKFont primaryFont
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<ShapedRun>();
        }

        var roundedSize = RoundSize(primaryFont.Size);
        var weight = WeightFromTypeface(primaryFont.Typeface);
        var key = new ShapeKey
        (
            text, primaryFont.Typeface.Handle, roundedSize, weight, primaryFont.Embolden
        );

        if (Cache.TryGetValue(key, out var node))
        {
            LruOrder.Remove(node);
            LruOrder.AddFirst(node);
            return node.Value.Runs;
        }

        var runs = ShapeRuns(text, primaryFont, weight);

        EvictIfFull();
        var entry = new ShapedEntry(key, runs);
        var newNode = new LinkedListNode<ShapedEntry>(entry);
        LruOrder.AddFirst(newNode);
        Cache[key] = newNode;
        return runs;
    }

    /// <summary>
    ///     Disposes all cached <see cref="SKShaper" /> instances and clears the shaped-text LRU.
    ///     <para>
    ///         Each shaper wraps HarfBuzz objects built from its <see cref="SKTypeface" />, so
    ///         this must run <em>before</em> those typefaces are disposed —
    ///         <see cref="TextLayoutHelper.ClearCache" /> owns that ordering.
    ///     </para>
    /// </summary>
    public static void ClearCache()
    {
        foreach (var entry in ShaperOrder)
        {
            entry.Shaper.Dispose();
        }

        Shapers.Clear();
        ShaperOrder.Clear();
        Cache.Clear();
        LruOrder.Clear();
    }

    private static ShapedRun[] ShapeRuns
    (
        string text,
        SKFont primaryFont,
        FontWeight weight
    )
    {
        var fontRuns = FontRunSplitter.Split(text, primaryFont.Typeface, weight);
        var result = new ShapedRun[fontRuns.Count];
        var x = 0f;

        for (var i = 0; i < fontRuns.Count; i++)
        {
            var run = fontRuns[i];
            var font = ReferenceEquals(run.Typeface, primaryFont.Typeface)
                ? primaryFont
                : TextLayoutHelper.GetFont(run.Typeface, primaryFont.Size);

            var substring = text.Substring(run.Start, run.Length);
            var shaper = GetOrCreateShaper(run.Typeface);

            result[i] = shaper != null
                ? ShapeWithHarfBuzz(shaper, substring, font, x)
                : ShapeWithoutHarfBuzz(substring, font, x);
            x += result[i].Advance;
        }

        return result;
    }

    private static ShapedRun ShapeWithHarfBuzz
    (
        SKShaper shaper,
        string text,
        SKFont font,
        float x
    )
    {
        var shaped = shaper.Shape(text, x, 0, font);
        var metrics = font.Metrics;
        return new ShapedRun
        (
            font,
            CopyGlyphs(shaped.Codepoints),
            (SKPoint[])shaped.Points.Clone(),
            shaped.Width,
            metrics.Ascent,
            metrics.Descent
        );
    }

    /// <summary>
    ///     Positions glyphs by their horizontal advances only, without HarfBuzz. Used when the
    ///     native library is unavailable: ligatures, kerning, and complex-script reordering are
    ///     lost, but text still renders instead of the screen going blank.
    /// </summary>
    internal static ShapedRun ShapeWithoutHarfBuzz
    (
        string text,
        SKFont font,
        float x
    )
    {
        var glyphs = font.GetGlyphs(text);
        var widths = font.GetGlyphWidths(glyphs);
        var points = new SKPoint[glyphs.Length];
        var advance = 0f;

        for (var i = 0; i < glyphs.Length; i++)
        {
            points[i] = new SKPoint(x + advance, 0);
            advance += i < widths.Length ? widths[i] : 0;
        }

        var metrics = font.Metrics;
        return new ShapedRun(font, glyphs, points, advance, metrics.Ascent, metrics.Descent);
    }

    /// <summary>
    ///     Returns a cached shaper for <paramref name="typeface" />, or null when HarfBuzz
    ///     could not be loaded. The failure is logged once and shaping stays disabled for the
    ///     rest of the session.
    /// </summary>
    private static SKShaper? GetOrCreateShaper
    (
        SKTypeface typeface
    )
    {
        if (!ShapingAvailable)
        {
            return null;
        }

        if (Shapers.TryGetValue(typeface.Handle, out var node))
        {
            ShaperOrder.Remove(node);
            ShaperOrder.AddFirst(node);
            return node.Value.Shaper;
        }

        SKShaper shaper;
        try
        {
            shaper = new SKShaper(typeface);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException
                                     or TypeInitializationException)
        {
            ShapingAvailable = false;
            OnShapingUnavailable?.Invoke(e);
            return null;
        }

        EvictShapersIfFull();
        var newNode = new LinkedListNode<ShaperEntry>(new ShaperEntry(typeface.Handle, shaper));
        ShaperOrder.AddFirst(newNode);
        Shapers[typeface.Handle] = newNode;
        return shaper;
    }

    /// <summary>
    ///     Drops least-recently-used shapers once the cache is full. Safe to dispose here:
    ///     shapers are used transiently inside <see cref="ShapeRuns" /> and never retained by
    ///     a <see cref="ShapedRun" />, which holds only an <see cref="SKFont" />.
    /// </summary>
    private static void EvictShapersIfFull()
    {
        while (Shapers.Count >= MaxShaperCacheSize && ShaperOrder.Last is { } oldest)
        {
            ShaperOrder.RemoveLast();
            Shapers.Remove(oldest.Value.TypefaceHandle);
            oldest.Value.Shaper.Dispose();
        }
    }

    private static ushort[] CopyGlyphs
    (
        uint[] codepoints
    )
    {
        var result = new ushort[codepoints.Length];
        for (var i = 0; i < codepoints.Length; i++)
        {
            result[i] = (ushort)codepoints[i];
        }

        return result;
    }

    private static float RoundSize
    (
        float fontSize
    )
    {
        var rounded = (float)Math.Round(fontSize * 2) / 2f;
        return rounded < 1 ? 1 : rounded;
    }

    private static FontWeight WeightFromTypeface
    (
        SKTypeface tf
    )
    {
        if (tf.IsItalic)
        {
            return FontWeight.Italic;
        }

        if (tf.FontWeight >= (int)SKFontStyleWeight.Bold)
        {
            return FontWeight.Bold;
        }

        if (tf.FontWeight >= (int)SKFontStyleWeight.SemiBold)
        {
            return FontWeight.SemiBold;
        }

        return FontWeight.Normal;
    }

    private static void EvictIfFull()
    {
        while (Cache.Count >= MaxCacheSize && LruOrder.Last is { } oldest)
        {
            LruOrder.RemoveLast();
            Cache.Remove(oldest.Value.Key);
        }
    }

    private readonly record struct ShapeKey
    (
        string Text,
        IntPtr PrimaryHandle,
        float RoundedSize,
        FontWeight Weight,
        bool Embolden
    );

    private sealed record ShapedEntry
    (
        ShapeKey Key,
        ShapedRun[] Runs
    );

    private sealed record ShaperEntry
    (
        IntPtr TypefaceHandle,
        SKShaper Shaper
    );
}

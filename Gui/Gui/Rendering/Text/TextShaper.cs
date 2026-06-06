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

    private static readonly Dictionary<IntPtr, SKShaper> Shapers = new();
    private static readonly Dictionary<ShapeKey, LinkedListNode<ShapedEntry>> Cache = new();
    private static readonly LinkedList<ShapedEntry> LruOrder = new();

    /// <summary>
    ///     Shapes <paramref name="text" /> using <paramref name="primaryFont" /> and the
    ///     system fallback chain.
    /// </summary>
    public static ShapedRun[] Shape(
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
        var key = new ShapeKey(text, primaryFont.Typeface.Handle, roundedSize, weight);

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

    /// <summary>Disposes all cached <see cref="SKShaper" /> instances and clears the shaped-text LRU.</summary>
    public static void ClearCache()
    {
        foreach (var s in Shapers.Values)
        {
            s.Dispose();
        }

        Shapers.Clear();
        Cache.Clear();
        LruOrder.Clear();
    }

    private static ShapedRun[] ShapeRuns(
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

            var shaper = GetOrCreateShaper(run.Typeface);
            var substring = text.Substring(run.Start, run.Length);
            var shaped = shaper.Shape(substring, x, 0, font);

            var metrics = font.Metrics;
            result[i] = new ShapedRun(
                font,
                CopyGlyphs(shaped.Codepoints),
                (SKPoint[])shaped.Points.Clone(),
                shaped.Width,
                metrics.Ascent,
                metrics.Descent
            );
            x += shaped.Width;
        }

        return result;
    }

    private static SKShaper GetOrCreateShaper(
        SKTypeface typeface
    )
    {
        if (Shapers.TryGetValue(typeface.Handle, out var s))
        {
            return s;
        }

        s = new SKShaper(typeface);
        Shapers[typeface.Handle] = s;
        return s;
    }

    private static ushort[] CopyGlyphs(
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

    private static float RoundSize(
        float fontSize
    )
    {
        var rounded = (float)Math.Round(fontSize * 2) / 2f;
        return rounded < 1 ? 1 : rounded;
    }

    private static FontWeight WeightFromTypeface(
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

    private readonly record struct ShapeKey(
        string Text,
        IntPtr PrimaryHandle,
        float RoundedSize,
        FontWeight Weight);

    private sealed record ShapedEntry(ShapeKey Key, ShapedRun[] Runs);
}

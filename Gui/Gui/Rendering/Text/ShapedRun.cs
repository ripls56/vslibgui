using SkiaSharp;

namespace Gui.Rendering.Text;

/// <summary>
///     Result of HarfBuzz shaping for a single <see cref="FontRun" />: glyph IDs, their
///     absolute positions on the baseline, total advance, and per-run vertical metrics
///     required for mixed-script line-height computation.
/// </summary>
public readonly struct ShapedRun
{
    /// <summary>Font used to render the glyphs in this run (typeface + size).</summary>
    public SKFont Font { get; }

    /// <summary>Glyph IDs from <c>SKShaper.Result.Codepoints</c>.</summary>
    public ushort[] Glyphs { get; }

    /// <summary>Absolute glyph positions on the baseline.</summary>
    public SKPoint[] Points { get; }

    /// <summary>Total horizontal advance contributed by this run.</summary>
    public float Advance { get; }

    /// <summary>Run's ascent (negative per Skia convention).</summary>
    public float Ascent { get; }

    /// <summary>Run's descent (positive per Skia convention).</summary>
    public float Descent { get; }

    /// <summary>Creates a new shaped run.</summary>
    public ShapedRun(
        SKFont font,
        ushort[] glyphs,
        SKPoint[] points,
        float advance,
        float ascent,
        float descent
    )
    {
        Font = font;
        Glyphs = glyphs;
        Points = points;
        Advance = advance;
        Ascent = ascent;
        Descent = descent;
    }
}

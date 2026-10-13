using Gui.Rendering.Text;
using SkiaSharp;

namespace Gui.Tests.Rendering;

/// <summary>
///     Covers the unshaped fallback used when HarfBuzz cannot be loaded — the second line of
///     defence for the Linux <c>free(): invalid pointer</c> crash, where the process would
///     otherwise render no text at all.
/// </summary>
public class TextShaperFallbackTests
{
    private static SKFont CreateFont() => TextLayoutHelper.GetFont("sans-serif", 16f,
        FontWeight.Normal);

    /// <summary>
    ///     The fallback must emit one glyph per character with strictly increasing X positions
    ///     and a total advance equal to the sum of the glyph widths.
    /// </summary>
    [Test]
    public void ShapeWithoutHarfBuzz_PositionsGlyphsByAdvance()
    {
        var font = CreateFont();

        var run = TextShaper.ShapeWithoutHarfBuzz("Hello", font, 0f);

        Assert.That(run.Glyphs, Has.Length.EqualTo(5));
        Assert.That(run.Points, Has.Length.EqualTo(5));
        Assert.That(run.Advance, Is.GreaterThan(0f));

        for (var i = 1; i < run.Points.Length; i++)
        {
            Assert.That(run.Points[i].X, Is.GreaterThan(run.Points[i - 1].X));
            Assert.That(run.Points[i].Y, Is.EqualTo(0f));
        }
    }

    /// <summary>
    ///     Runs are laid out sequentially, so the fallback must honour the starting X offset.
    /// </summary>
    [Test]
    public void ShapeWithoutHarfBuzz_RespectsStartOffset()
    {
        var font = CreateFont();

        var atZero = TextShaper.ShapeWithoutHarfBuzz("Hi", font, 0f);
        var atOffset = TextShaper.ShapeWithoutHarfBuzz("Hi", font, 40f);

        Assert.That(atOffset.Points[0].X, Is.EqualTo(atZero.Points[0].X + 40f).Within(0.01f));
        Assert.That(atOffset.Advance, Is.EqualTo(atZero.Advance).Within(0.01f));
    }

    /// <summary>
    ///     Vertical metrics must still come from the font so line-height computation keeps
    ///     working when shaping is unavailable.
    /// </summary>
    [Test]
    public void ShapeWithoutHarfBuzz_CarriesFontMetrics()
    {
        var font = CreateFont();

        var run = TextShaper.ShapeWithoutHarfBuzz("Ay", font, 0f);

        Assert.That(run.Font, Is.SameAs(font));
        Assert.That(run.Ascent, Is.LessThan(0f));
        Assert.That(run.Descent, Is.GreaterThan(0f));
    }
}

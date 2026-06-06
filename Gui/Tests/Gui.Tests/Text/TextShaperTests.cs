using Gui.Rendering.Text;
using SkiaSharp;

namespace Gui.Tests.Text;

[TestFixture]
public class TextShaperTests
{
    private static SKFont LatinFont()
    {
        var tf = SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright
        );
        return TextLayoutHelper.GetFont(tf, 16f);
    }

    [Test]
    public void Shape_AsciiHello_ProducesGlyphsWithPositiveAdvance()
    {
        var runs = TextShaper.Shape("Hello", LatinFont());

        Assert.That(runs, Has.Length.EqualTo(1));
        Assert.That(runs[0].Glyphs.Length, Is.EqualTo(5));
        Assert.That(runs[0].Advance, Is.GreaterThan(0f));
    }

    [Test]
    public void Shape_MixedScript_RunsHaveContiguousPositions()
    {
        var runs = TextShaper.Shape("Hi你好", LatinFont());

        Assert.That(runs.Length, Is.GreaterThanOrEqualTo(2));
        var prevEnd = 0f;
        foreach (var r in runs)
        {
            Assert.That(r.Points[0].X, Is.GreaterThanOrEqualTo(prevEnd - 0.5f),
                "next run must start at or after previous run's end");
            prevEnd = r.Points[0].X + r.Advance;
        }
    }

    [Test]
    public void Shape_SameInputTwice_HitsCache()
    {
        TextShaper.ClearCache();
        var font = LatinFont();
        var first = TextShaper.Shape("CacheTest", font);
        var second = TextShaper.Shape("CacheTest", font);

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void ClearCache_OnEmptyState_DoesNotThrow()
    {
        TextShaper.ClearCache();
        Assert.DoesNotThrow(() => TextShaper.ClearCache());
    }
}

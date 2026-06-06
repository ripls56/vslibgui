using Gui.Rendering.Text;
using SkiaSharp;

namespace Gui.Tests.Text;

[TestFixture]
public class FontRunSplitterTests
{
    private static SKTypeface LatinTypeface()
    {
        return SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright
        );
    }

    [Test]
    public void Split_PureAscii_ReturnsSingleRun()
    {
        var primary = LatinTypeface();

        var runs = FontRunSplitter.Split("Hello", primary, FontWeight.Normal);

        Assert.That(runs, Has.Count.EqualTo(1));
        Assert.That(runs[0].Start, Is.EqualTo(0));
        Assert.That(runs[0].Length, Is.EqualTo(5));
        Assert.That(runs[0].Typeface, Is.EqualTo(primary));
    }

    [Test]
    public void Split_AllCjk_ReturnsSingleFallbackRun()
    {
        var primary = LatinTypeface();
        const string cjk = "你好";

        var runs = FontRunSplitter.Split(cjk, primary, FontWeight.Normal);

        Assert.That(runs, Has.Count.EqualTo(1));
        Assert.That(runs[0].Length, Is.EqualTo(2));
        Assert.That(runs[0].Typeface, Is.Not.EqualTo(primary),
            "fallback typeface must differ from the Latin primary");
    }

    [Test]
    public void Split_MixedLatinAndCjk_ReturnsTwoRuns()
    {
        var primary = LatinTypeface();
        const string mixed = "Hi你好";

        var runs = FontRunSplitter.Split(mixed, primary, FontWeight.Normal);

        Assert.That(runs, Has.Count.EqualTo(2));
        Assert.That(runs[0].Start, Is.EqualTo(0));
        Assert.That(runs[0].Length, Is.EqualTo(2));
        Assert.That(runs[0].Typeface, Is.EqualTo(primary));
        Assert.That(runs[1].Start, Is.EqualTo(2));
        Assert.That(runs[1].Length, Is.EqualTo(2));
        Assert.That(runs[1].Typeface, Is.Not.EqualTo(primary));
    }

    [Test]
    public void Split_EmptyString_ReturnsEmpty()
    {
        var primary = LatinTypeface();

        var runs = FontRunSplitter.Split(string.Empty, primary, FontWeight.Normal);

        Assert.That(runs, Is.Empty);
    }

    [Test]
    public void Split_SurrogatePairEmoji_ConsumedAsOneCodepoint()
    {
        var primary = LatinTypeface();
        const string text = "A😀";

        var runs = FontRunSplitter.Split(text, primary, FontWeight.Normal);

        var total = 0;
        foreach (var r in runs)
        {
            total += r.Length;
        }

        Assert.That(total, Is.EqualTo(text.Length));
    }

    [Test]
    public void Split_SameCodepointsAcrossCalls_PopulatesCacheOnce()
    {
        FontRunSplitter.ClearCache();
        var primary = LatinTypeface();
        const string text = "你好你";

        FontRunSplitter.Split(text, primary, FontWeight.Normal);
        var afterFirst = FontRunSplitter.CacheCount;

        FontRunSplitter.Split(text, primary, FontWeight.Normal);
        var afterSecond = FontRunSplitter.CacheCount;

        Assert.That(afterFirst, Is.EqualTo(2), "two unique codepoints cached on first call");
        Assert.That(afterSecond, Is.EqualTo(afterFirst), "cache size unchanged on repeat");
    }
}

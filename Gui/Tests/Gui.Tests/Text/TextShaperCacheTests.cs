using Gui.Rendering.Text;
using SkiaSharp;

namespace Gui.Tests.Text;

/// <summary>
///     Covers the <see cref="SKShaper" /> cache bounds and the teardown ordering between
///     <see cref="TextShaper" /> and <see cref="TextLayoutHelper" />. A shaper wraps HarfBuzz
///     objects built from its typeface, so releasing them in the wrong order is a
///     use-after-free in native code.
/// </summary>
[TestFixture]
public class TextShaperCacheTests
{
    private static readonly string[] DistinctFamilies =
    [
        "Arial",
        "Times New Roman",
        "Courier New",
        "Verdana",
        "Georgia"
    ];

    private int _savedShaperCap;

    [SetUp]
    public void SetUp()
    {
        _savedShaperCap = TextShaper.MaxShaperCacheSize;
        TextLayoutHelper.ClearCache();
    }

    [TearDown]
    public void TearDown()
    {
        TextShaper.MaxShaperCacheSize = _savedShaperCap;
        TextLayoutHelper.ClearCache();
    }

    private static SKFont FontFor
    (
        string family
    ) =>
        TextLayoutHelper.GetFont(family, 16f, FontWeight.Normal);

    /// <summary>
    ///     The shaper map used to grow without limit, pinning every system fallback typeface
    ///     matched during the session. It must now stay within its configured bound.
    /// </summary>
    [Test]
    public void ShaperCache_StaysWithinBound()
    {
        TextShaper.MaxShaperCacheSize = 2;

        foreach (var family in DistinctFamilies)
        {
            TextShaper.Shape("Shaper bound", FontFor(family));
        }

        Assert.That(TextShaper.ShaperCacheCount, Is.LessThanOrEqualTo(2));
    }

    /// <summary>
    ///     Evicting a shaper disposes it. Shaping through the same typeface afterwards must
    ///     transparently rebuild one instead of touching the disposed instance.
    /// </summary>
    [Test]
    public void ShapingAfterShaperEviction_StillProducesGlyphs()
    {
        TextShaper.MaxShaperCacheSize = 1;
        var arial = FontFor("Arial");

        TextShaper.Shape("first", arial);
        TextShaper.Shape("second", FontFor("Times New Roman"));

        // Arial's shaper has been evicted and disposed by now; force a fresh shape.
        var runs = TextShaper.Shape("third", arial);

        Assert.That(runs, Is.Not.Empty);
        Assert.That(runs[0].Glyphs, Is.Not.Empty);
        Assert.That(runs[0].Advance, Is.GreaterThan(0f));
    }

    /// <summary>
    ///     <see cref="TextLayoutHelper.ClearCache" /> disposes typefaces and fonts, so the
    ///     shapers and shaped runs holding them must be released first. Shaping again after a
    ///     clear must work rather than fault in native code.
    /// </summary>
    [Test]
    public void ClearCache_ThenShapeAgain_DoesNotFault()
    {
        TextShaper.Shape("before clear", FontFor("Arial"));

        TextLayoutHelper.ClearCache();

        Assert.That(TextShaper.ShaperCacheCount, Is.EqualTo(0));

        var runs = TextShaper.Shape("after clear", FontFor("Arial"));

        Assert.That(runs, Is.Not.Empty);
        Assert.That(runs[0].Advance, Is.GreaterThan(0f));
    }

    /// <summary>
    ///     Repeated teardown must stay safe: a second clear should not re-dispose anything.
    /// </summary>
    [Test]
    public void ClearCache_Twice_DoesNotThrow()
    {
        TextShaper.Shape("double clear", FontFor("Arial"));

        TextLayoutHelper.ClearCache();

        Assert.DoesNotThrow(() => TextLayoutHelper.ClearCache());
    }

    /// <summary>
    ///     A cached shaper must be reused across calls that share a typeface, otherwise every
    ///     shaped string would rebuild the HarfBuzz face tables.
    /// </summary>
    [Test]
    public void ShaperCache_ReusesInstancePerTypeface()
    {
        var arial = FontFor("Arial");

        TextShaper.Shape("one", arial);
        var afterFirst = TextShaper.ShaperCacheCount;
        TextShaper.Shape("two", arial);

        Assert.That(TextShaper.ShaperCacheCount, Is.EqualTo(afterFirst));
    }

    /// <summary>
    ///     Guards the assumption the eviction tests rest on: the families above must resolve
    ///     to different typefaces, or the cache would never fill and those tests would pass
    ///     vacuously.
    /// </summary>
    [Test]
    public void TestFamilies_ResolveToDistinctTypefaces()
    {
        var handles = DistinctFamilies
            .Select(f => TextLayoutHelper.GetFont(f, 16f, FontWeight.Normal).Typeface.Handle)
            .Distinct()
            .Count();

        Assert.That
        (
            handles,
            Is.GreaterThanOrEqualTo(3),
            "shaper eviction tests need at least 3 distinct typefaces to be meaningful"
        );
    }
}

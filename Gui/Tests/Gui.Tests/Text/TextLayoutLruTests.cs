using Gui.Rendering.Text;

namespace Gui.Tests.Text;

[TestFixture]
[NonParallelizable]
public class TextLayoutLruTests
{
    [SetUp]
    public void SetUp()
    {
        _savedCap = TextLayoutHelper.MaxFontCacheSize;
        TextLayoutHelper.ClearCache();
    }

    [TearDown]
    public void TearDown()
    {
        TextLayoutHelper.MaxFontCacheSize = _savedCap;
        TextLayoutHelper.ClearCache();
    }

    private const string Family = "sans-serif";
    private int _savedCap;

    [Test]
    public void FontCache_EvictsOldestEntryWhenExceedingCapacity()
    {
        TextLayoutHelper.MaxFontCacheSize = 3;

        TextLayoutHelper.GetFont(Family, 10f, FontWeight.Normal);
        TextLayoutHelper.GetFont(Family, 11f, FontWeight.Normal);
        TextLayoutHelper.GetFont(Family, 12f, FontWeight.Normal);

        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(3));

        TextLayoutHelper.GetFont(Family, 13f, FontWeight.Normal);

        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(3));
        Assert.That(TextLayoutHelper.IsCached(Family, 10f, FontWeight.Normal), Is.False,
            "Oldest entry (10f) should have been evicted");
        Assert.That(TextLayoutHelper.IsCached(Family, 11f, FontWeight.Normal), Is.True);
        Assert.That(TextLayoutHelper.IsCached(Family, 12f, FontWeight.Normal), Is.True);
        Assert.That(TextLayoutHelper.IsCached(Family, 13f, FontWeight.Normal), Is.True);
    }

    [Test]
    public void FontCache_AccessingEntryPromotesItToMostRecent()
    {
        TextLayoutHelper.MaxFontCacheSize = 3;

        TextLayoutHelper.GetFont(Family, 10f, FontWeight.Normal);
        TextLayoutHelper.GetFont(Family, 11f, FontWeight.Normal);
        TextLayoutHelper.GetFont(Family, 12f, FontWeight.Normal);

        // Touch 10f — it should now be most-recently-used.
        TextLayoutHelper.GetFont(Family, 10f, FontWeight.Normal);

        // Insert a 4th key — 11f is now the LRU victim, not 10f.
        TextLayoutHelper.GetFont(Family, 13f, FontWeight.Normal);

        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(3));
        Assert.That(TextLayoutHelper.IsCached(Family, 10f, FontWeight.Normal), Is.True,
            "Promoted entry (10f) must survive eviction");
        Assert.That(TextLayoutHelper.IsCached(Family, 11f, FontWeight.Normal), Is.False,
            "11f should be evicted as the new LRU victim");
        Assert.That(TextLayoutHelper.IsCached(Family, 12f, FontWeight.Normal), Is.True);
        Assert.That(TextLayoutHelper.IsCached(Family, 13f, FontWeight.Normal), Is.True);
    }

    [Test]
    public void FontCache_EvictionIsIncremental_NotFullFlush()
    {
        TextLayoutHelper.MaxFontCacheSize = 5;

        for (var i = 0; i < 6; i++)
        {
            TextLayoutHelper.GetFont(Family, 10f + i, FontWeight.Normal);
        }

        // Previously: at cap+1 the whole cache was cleared. Now: exactly one eviction.
        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(5),
            "Exceeding cap by 1 should evict exactly 1 entry, not flush the whole cache");
        Assert.That(TextLayoutHelper.IsCached(Family, 10f, FontWeight.Normal), Is.False);
        Assert.That(TextLayoutHelper.IsCached(Family, 15f, FontWeight.Normal), Is.True);
    }

    [Test]
    public void FontCache_RepeatedGetSameKey_DoesNotGrowCache()
    {
        TextLayoutHelper.MaxFontCacheSize = 10;

        for (var i = 0; i < 50; i++)
        {
            TextLayoutHelper.GetFont(Family, 12f, FontWeight.Normal);
        }

        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearCache_ResetsLruStateEntirely()
    {
        TextLayoutHelper.MaxFontCacheSize = 3;
        TextLayoutHelper.GetFont(Family, 10f, FontWeight.Normal);
        TextLayoutHelper.GetFont(Family, 11f, FontWeight.Normal);

        TextLayoutHelper.ClearCache();

        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(0));
        Assert.That(TextLayoutHelper.IsCached(Family, 10f, FontWeight.Normal), Is.False);

        // And a fresh GetFont should still work post-clear.
        var font = TextLayoutHelper.GetFont(Family, 10f, FontWeight.Normal);
        Assert.That(font, Is.Not.Null);
        Assert.That(TextLayoutHelper.FontCacheCount, Is.EqualTo(1));
    }
}

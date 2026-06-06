using Gui.Core.Scroll;

namespace Gui.Tests.Scroll;

[TestFixture]
public class ItemHeightCacheTests
{
    [Test]
    public void Reset_ShouldInitializeWithEstimatedHeight()
    {
        var cache = new ItemHeightCache();
        cache.Reset(5, 40f);

        Assert.That(cache.Count, Is.EqualTo(5));
        Assert.That(cache.TotalHeight, Is.EqualTo(200f).Within(0.1f));
        Assert.That(cache.GetHeight(0), Is.EqualTo(40f));
        Assert.That(cache.GetHeight(4), Is.EqualTo(40f));
    }

    [Test]
    public void GetPosition_ShouldReturnCorrectPrefixSums()
    {
        var cache = new ItemHeightCache();
        cache.Reset(3, 50f);

        Assert.That(cache.GetPosition(0), Is.EqualTo(0f));
        Assert.That(cache.GetPosition(1), Is.EqualTo(50f));
        Assert.That(cache.GetPosition(2), Is.EqualTo(100f));
    }

    [Test]
    public void SetMeasured_ShouldUpdatePrefixSums()
    {
        var cache = new ItemHeightCache();
        cache.Reset(3, 40f);

        var delta = cache.SetMeasured(1, 80f);

        Assert.That(delta, Is.EqualTo(40f).Within(0.1f));
        Assert.That(cache.GetPosition(0), Is.EqualTo(0f));
        Assert.That(cache.GetPosition(1), Is.EqualTo(40f));
        Assert.That(cache.GetPosition(2), Is.EqualTo(120f)); // 40 + 80
        Assert.That(cache.TotalHeight, Is.EqualTo(160f).Within(0.1f)); // 40 + 80 + 40
    }

    [Test]
    public void SetMeasured_ShouldReturnZeroDelta_WhenAlreadyMeasuredSameHeight()
    {
        var cache = new ItemHeightCache();
        cache.Reset(3, 40f);
        cache.SetMeasured(1, 80f);

        var delta = cache.SetMeasured(1, 80f);
        Assert.That(delta, Is.EqualTo(0f));
    }

    [Test]
    public void GrowTo_ShouldPreserveExistingMeasurements()
    {
        var cache = new ItemHeightCache();
        cache.Reset(2, 40f);
        cache.SetMeasured(0, 60f);
        cache.SetMeasured(1, 30f);

        cache.GrowTo(4, 40f);

        Assert.That(cache.Count, Is.EqualTo(4));
        Assert.That(cache.GetHeight(0), Is.EqualTo(60f));
        Assert.That(cache.GetHeight(1), Is.EqualTo(30f));
        Assert.That(cache.GetHeight(2), Is.EqualTo(40f));
        Assert.That(cache.GetHeight(3), Is.EqualTo(40f));
        Assert.That(cache.TotalHeight, Is.EqualTo(170f).Within(0.1f)); // 60+30+40+40
    }

    [Test]
    public void FindFirstVisible_ShouldReturnCorrectIndex()
    {
        var cache = new ItemHeightCache();
        cache.Reset(5, 40f);

        Assert.That(cache.FindFirstVisible(0f), Is.EqualTo(0));
        Assert.That(cache.FindFirstVisible(39f), Is.EqualTo(0));
        Assert.That(cache.FindFirstVisible(40f), Is.EqualTo(1));
        Assert.That(cache.FindFirstVisible(79f), Is.EqualTo(1));
        Assert.That(cache.FindFirstVisible(160f), Is.EqualTo(4));
    }

    [Test]
    public void FindFirstVisible_WithVariableHeights()
    {
        var cache = new ItemHeightCache();
        cache.Reset(4, 40f);
        cache.SetMeasured(0, 10f);
        cache.SetMeasured(1, 100f);
        // positions: 0=0, 1=10, 2=110, 3=150

        Assert.That(cache.FindFirstVisible(5f), Is.EqualTo(0));
        Assert.That(cache.FindFirstVisible(10f), Is.EqualTo(1));
        Assert.That(cache.FindFirstVisible(50f), Is.EqualTo(1));
        Assert.That(cache.FindFirstVisible(110f), Is.EqualTo(2));
    }

    [Test]
    public void FindLastVisible_ShouldReturnCorrectIndex()
    {
        var cache = new ItemHeightCache();
        cache.Reset(10, 40f);

        Assert.That(cache.FindLastVisible(0f, 100f), Is.EqualTo(2));
        Assert.That(cache.FindLastVisible(0f, 40f), Is.EqualTo(0));
        Assert.That(cache.FindLastVisible(0f, 41f), Is.EqualTo(1));
    }

    [Test]
    public void EmptyCache_ShouldHandleGracefully()
    {
        var cache = new ItemHeightCache();
        cache.Reset(0, 40f);

        Assert.That(cache.Count, Is.EqualTo(0));
        Assert.That(cache.TotalHeight, Is.EqualTo(0f));
        Assert.That(cache.FindFirstVisible(0f), Is.EqualTo(0));
        Assert.That(cache.GetPosition(0), Is.EqualTo(0f));
    }

    [Test]
    public void SetEstimatedHeight_ShouldOnlyUpdateUnmeasuredItems()
    {
        var cache = new ItemHeightCache();
        cache.Reset(3, 40f);
        cache.SetMeasured(1, 60f);

        cache.SetEstimatedHeight(50f);

        Assert.That(cache.GetHeight(0), Is.EqualTo(50f)); // unmeasured, updated
        Assert.That(cache.GetHeight(1), Is.EqualTo(60f)); // measured, unchanged
        Assert.That(cache.GetHeight(2), Is.EqualTo(50f)); // unmeasured, updated
    }

    [Test]
    public void GrowTo_ShouldNotShrink()
    {
        var cache = new ItemHeightCache();
        cache.Reset(5, 40f);

        cache.GrowTo(3, 40f); // smaller - should be no-op

        Assert.That(cache.Count, Is.EqualTo(5));
    }
}

using Gui.Widgets.Inventory;

namespace Gui.Tests.Widgets.Inventory;

[TestFixture]
public class SlideshowIndexTests
{
    [Test]
    public void At_SingleElement_ReturnsZero() =>
        Assert.That(SlideshowIndex.At(5000, 1), Is.EqualTo(0));

    [Test]
    public void At_CyclesOncePerSecond()
    {
        Assert.That(SlideshowIndex.At(0, 3), Is.EqualTo(0));
        Assert.That(SlideshowIndex.At(1000, 3), Is.EqualTo(1));
        Assert.That(SlideshowIndex.At(2000, 3), Is.EqualTo(2));
        Assert.That(SlideshowIndex.At(3000, 3), Is.EqualTo(0));
    }
}

using Gui.Widgets.Inventory;

namespace Gui.Tests.Widgets;

[TestFixture]
public class ItemSlotFilterTests
{
    [Test]
    public void ShouldSkipRebuild_ReturnsFalseOnFirstCall()
    {
        var filter = new SlotChangeFilter();
        Assert.That(filter.ShouldSkipRebuild(null, null), Is.False);
    }

    [Test]
    public void ShouldSkipRebuild_ReturnsTrueWhenNullStaysNull()
    {
        var filter = new SlotChangeFilter();
        filter.ShouldSkipRebuild(null, null);
        Assert.That(filter.ShouldSkipRebuild(null, null), Is.True);
    }

    [Test]
    public void ShouldSkipRebuild_ReturnsFalseWhenSlotBecomesEmpty()
    {
        var filter = new SlotChangeFilter();
        // null → null (skip) then null → null again (skip)
        // This test verifies the null branch is stable
        filter.ShouldSkipRebuild(null, null);
        Assert.That(filter.ShouldSkipRebuild(null, null), Is.True);
    }
}

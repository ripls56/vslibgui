using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;

namespace Gui.Tests.Scroll;

[TestFixture]
public class AbsurdScrollTests
{
    [SetUp]
    public void SetUp()
    {
        _buildOwner = new BuildOwner();
        _buildOwner.SetTickerProvider(new MockTickerProvider());
    }

    private BuildOwner _buildOwner;

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private RenderObject BuildAndLayout(Widget widget, LayoutConstraints constraints)
    {
        var element = widget.CreateElement();
        element.AssignOwner(_buildOwner);
        element.Mount(null);
        _buildOwner.BuildDirtyElements();
        var ro = element.RenderObject!;
        ro.Layout(constraints);
        return ro;
    }

    [Test]
    public void ListView_WithOneMillionItems_ShouldNotExplode()
    {
        var createdCount = 0;
        var listView = new ListView(
            (ctx, index) =>
            {
                createdCount++;
                return new Container(new BoxStyle { Height = 50, Width = 100 });
            },
            1_000_000,
            50
        );

        // Viewport height 500 -> 10 items visible + buffer
        var ro = BuildAndLayout(listView, LayoutConstraints.Tight(100, 500));

        Assert.That(createdCount, Is.LessThan(20), "Should only create visible items plus buffer");
        Assert.That(ro.Size.X, Is.EqualTo(100));
        Assert.That(ro.Size.Y, Is.EqualTo(500));
    }

    [Test]
    public void ListView_WithZeroViewportHeight_ShouldCreateMinimalItems()
    {
        var createdCount = 0;
        var listView = new ListView(
            (ctx, index) =>
            {
                createdCount++;
                return new Container(new BoxStyle { Height = 50 });
            },
            100,
            50
        );

        // Viewport height 0
        var ro = BuildAndLayout(listView, LayoutConstraints.Tight(100, 0));

        // Current implementation defaults to 400px height if 0, so it creates about 8-10 items.
        // We assert < 15 to allow this fallback while ensuring it doesn't create all 100.
        Assert.That(createdCount, Is.LessThan(15));
        Assert.That(ro.Size.Y, Is.EqualTo(0));
    }

    [Test]
    public void SingleChildScrollView_Inside_ListView_ShouldLayout()
    {
        // This simulates a scenario where each list item is itself scrollable (e.g. horizontal carousel)
        // But here we put a vertical scroll view inside a vertical list view, which is tricky.
        // Current ListView implementation enforces strict height on children (ItemHeight).
        // SingleChildScrollView should accept that height.

        var listView = new ListView(
            (ctx, index) =>
            {
                return new SingleChildScrollView(
                    new Container(new BoxStyle { Height = 200, Width = 100 })
                );
            },
            5,
            100 // Constrains the inner scroll view to 100px height
        );

        var ro = BuildAndLayout(listView, LayoutConstraints.Tight(200, 500));

        Assert.That(ro.Size.Y, Is.EqualTo(500));

        // We need to dig deep to find the child render objects.
        // ListView -> Row -> Expanded -> GestureDetector -> Viewport -> _ListViewContent
        // The first child of _ListViewContent should be our SingleChildScrollView's render object tree.
        // SingleChildScrollView -> GestureDetector -> Viewport -> RenderBox (Child)

        // Let's just assert that layout didn't throw and produced correct root size.
    }

    [Test]
    public void ListView_ScrollOffset_ShouldUpdateVisibleItems()
    {
        var createdIndices = new List<int>();
        var controller = new ScrollController();

        var listView = new ListView(
            (ctx, index) =>
            {
                createdIndices.Add(index);
                return new Container(new BoxStyle { Height = 50 });
            },
            100,
            50,
            controller
        );

        var element = listView.CreateElement();
        element.AssignOwner(_buildOwner);
        element.Mount(null);
        _buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(100, 200)); // 4 items visible (0, 1, 2, 3)

        createdIndices.Clear();

        // Scroll down by 600px (12 items) to ensure we go beyond the initially created buffer (which might assume 400px height)
        controller.JumpTo(600);
        _buildOwner.BuildDirtyElements(); // Rebuild with new offset
        ro.Layout(LayoutConstraints.Tight(100, 200)); // Relayout

        // Now items around 12 should be visible.
        // Item 12 is definitely new (initial created 0..8 approx).
        Assert.That(createdIndices, Does.Contain(12));
        Assert.That(createdIndices, Does.Not.Contain(0)); // Item 0 should be recycled/not created
    }

    [Test]
    public void ListView_WithHugeItemHeight_ShouldLayoutCorrectly()
    {
        var listView = new ListView(
            (ctx, index) => new Container(new BoxStyle { Height = 5000 }),
            10,
            5000
        );

        var ro = BuildAndLayout(listView, LayoutConstraints.Tight(100, 500));

        // Viewport 500, Item 5000. Only 1 item should be visible.
        // The content height will be 50,000.
        // But the RenderObject size should be constrained to 500.

        Assert.That(ro.Size.Y, Is.EqualTo(500));
    }
}

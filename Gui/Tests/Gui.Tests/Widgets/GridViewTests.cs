using Gui.Core.Scroll;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;

namespace Gui.Tests.Widgets;

[TestFixture]
public class GridViewTests
{
    [Test]
    public void SliverGridGeometry_CellOrigin_IsCorrectForRowCol()
    {
        var g = new SliverGridGeometry(
            3, 100, 50,
            4, 4);

        Assert.That(g.CellOrigin(0), Is.EqualTo((0f, 0f)));
        Assert.That(g.CellOrigin(1), Is.EqualTo((104f, 0f)));
        Assert.That(g.CellOrigin(2), Is.EqualTo((208f, 0f)));
        Assert.That(g.CellOrigin(3), Is.EqualTo((0f, 54f)));
        Assert.That(g.CellOrigin(4), Is.EqualTo((104f, 54f)));
    }

    [Test]
    public void SliverGridGeometry_TotalContentHeight_IsCorrect()
    {
        var g = new SliverGridGeometry(3, 100, 50,
            4, 0);

        // 9 items -> 3 rows. height = 3*50 + 2*4 = 158
        Assert.That(g.TotalContentHeight(9), Is.EqualTo(158f));
        // 10 items -> 4 rows. height = 4*50 + 3*4 = 212
        Assert.That(g.TotalContentHeight(10), Is.EqualTo(212f));
        Assert.That(g.TotalContentHeight(0), Is.EqualTo(0f));
    }

    [Test]
    public void SliverGridGeometry_VisibleRange_AtScrollZero()
    {
        var g = new SliverGridGeometry(3, 100, 50,
            0, 0);

        // viewport height 120 -> rows 0..3 visible (with buffer)
        var (first, last) = g.ComputeVisibleRange(0, 120, 9);
        Assert.That(first, Is.EqualTo(0));
        Assert.That(last, Is.EqualTo(8));
    }

    [Test]
    public void SliverGridGeometry_VisibleRange_AfterScroll()
    {
        var g = new SliverGridGeometry(3, 100, 50,
            0, 0);

        // Scroll to y=150 (row 3 start). Viewport 100px -> rows 2..4 with buffer.
        var (first, last) = g.ComputeVisibleRange(150, 100, 15);
        Assert.That(first, Is.LessThanOrEqualTo(6)); // row 2
        Assert.That(last, Is.GreaterThanOrEqualTo(14)); // row 4
    }

    [Test]
    public void SliverGridGeometry_VisibleRange_EmptyReturnsNegative()
    {
        var g = new SliverGridGeometry(3, 100, 50, 0, 0);
        var (first, last) = g.ComputeVisibleRange(0, 400, 0);
        Assert.That(last, Is.LessThan(first));
    }


    [Test]
    public void FixedCrossAxisCount_ComputesCellDimensions()
    {
        var d = new SliverGridDelegateWithFixedCrossAxisCount(
            4, crossAxisSpacing: 8, childAspectRatio: 2.0f);

        // viewport 400: spacing = 3*8=24, cellWidth = (400-24)/4=94, cellHeight = 94/2=47
        var g = d.GetGeometry(400);
        Assert.That(g.CrossAxisCount, Is.EqualTo(4));
        Assert.That(g.CellWidth, Is.EqualTo(94f).Within(0.01f));
        Assert.That(g.CellHeight, Is.EqualTo(47f).Within(0.01f));
    }

    [Test]
    public void FixedCrossAxisCount_FixedItemHeight_OverridesAspectRatio()
    {
        var d = new SliverGridDelegateWithFixedCrossAxisCount(
            4, fixedItemHeight: 60);

        var g = d.GetGeometry(400);
        Assert.That(g.CellHeight, Is.EqualTo(60f));
    }

    [Test]
    public void MaxExtent_ComputesCrossAxisCount()
    {
        var d = new SliverGridDelegateWithMaxCrossAxisExtent(
            100);

        Assert.That(d.GetGeometry(400).CrossAxisCount, Is.EqualTo(4));
        Assert.That(d.GetGeometry(350).CrossAxisCount, Is.EqualTo(3));
        Assert.That(d.GetGeometry(50).CrossAxisCount, Is.EqualTo(1));
    }


    [Test]
    public void GridView_Static_MountsWithoutError()
    {
        var gridView = new GridView(
            [
                new Container(new BoxStyle { Width = 50, Height = 50 }),
                new Container(new BoxStyle { Width = 50, Height = 50 }),
                new Container(new BoxStyle { Width = 50, Height = 50 })
            ],
            new SliverGridDelegateWithFixedCrossAxisCount(3)
        );

        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        var el = gridView.CreateElement();
        el.AssignOwner(owner);
        Assert.DoesNotThrow(() =>
        {
            el.Mount(null);
            owner.BuildDirtyElements();
        });
    }

    [Test]
    public void GridView_Builder_OnlyCreatesVisibleCells()
    {
        var createdCount = 0;
        var gridView = GridView.Builder(
            (ctx, index) =>
            {
                createdCount++;
                return new Container(new BoxStyle { Width = 50, Height = 50 });
            },
            1000,
            new SliverGridDelegateWithFixedCrossAxisCount(
                4, fixedItemHeight: 60)
        );

        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        var el = gridView.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();

        // At 400px fallback, 4 cols, 60px rows -> ~7 visible rows + buffer ≈ 36 cells
        Assert.That(createdCount, Is.LessThan(50));
        Assert.That(createdCount, Is.GreaterThan(0));
    }

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick)
            => new(onTick);
    }
}

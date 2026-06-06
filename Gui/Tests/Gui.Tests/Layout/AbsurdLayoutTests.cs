using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;

namespace Gui.Tests.Layout;

[TestFixture]
public class AbsurdLayoutTests
{
    [SetUp]
    public void SetUp() => _buildOwner = new BuildOwner();

    private BuildOwner _buildOwner;

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
    public void DeeplyNestedFlex_ShouldLayoutWithoutStackOverflow()
    {
        // 50 levels of nested columns/rows
        Widget current = new Container(new BoxStyle { Width = 10, Height = 10 });
        for (var i = 0; i < 50; i++)
        {
            if (i % 2 == 0)
            {
                current = new Column(children: new List<Widget> { current });
            }
            else
            {
                current = new Row(children: new List<Widget> { current });
            }
        }

        var ro = BuildAndLayout(current, LayoutConstraints.Tight(500, 500));

        Assert.That(ro.Size.X, Is.EqualTo(500));
        Assert.That(ro.Size.Y, Is.EqualTo(500));

        // Walk down to find the Container's _BoxWidget (the painting element)
        // Container structure: SizedBox -> _BoxWidget -> SizedBox
        // We want to find _BoxWidget which should have the explicit size
        var deepest = ro;
        while (deepest.Children.Count > 0)
        {
            var child = deepest.Children[0];
            // Stop at _BoxWidget which has the explicit size
            if (deepest is RenderConstrainedBox && child is RenderBox)
            {
                break;
            }

            deepest = child;
        }

        Assert.That(deepest.Size.X, Is.EqualTo(10));
        Assert.That(deepest.Size.Y, Is.EqualTo(10));
    }

    [Test]
    public void ZeroSizeConstraints_ShouldForceEverythingToZero()
    {
        var widget = new Column(children:
        [
            new Container(new BoxStyle { Width = 100, Height = 100 }),
            new Expanded(new Container())
        ]);

        var ro = BuildAndLayout(widget, LayoutConstraints.Tight(0, 0));

        Assert.That(ro.Size.X, Is.EqualTo(0));
        Assert.That(ro.Size.Y, Is.EqualTo(0));

        Assert.That(ro.Children[0].Size.X, Is.EqualTo(0));
        Assert.That(ro.Children[0].Size.Y, Is.EqualTo(0));
        Assert.That(ro.Children[1].Size.X, Is.EqualTo(0));
        Assert.That(ro.Children[1].Size.Y, Is.EqualTo(0));
    }

    [Test]
    public void UnboundedFlex_ShouldThrowException()
    {
        var widget = new Column(children:
        [
            new Expanded(new Container())
        ]);

        var element = widget.CreateElement();
        element.AssignOwner(_buildOwner);
        element.Mount(null);
        _buildOwner.BuildDirtyElements();
        var ro = element.RenderObject!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            ro.Layout(new LayoutConstraints(0, 500));
        });
    }

    [Test]
    public void SpaceBetween_WithSingleChild_ShouldPositionAtStart()
    {
        var widget = new Column(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            children:
            [
                new Container(new BoxStyle { Width = 50, Height = 50 })
            ]
        );

        var ro = BuildAndLayout(widget, LayoutConstraints.Tight(100, 200));

        Assert.That(ro.Children[0].Y, Is.EqualTo(0));
    }

    [Test]
    public void SpaceEvenly_WithZeroChildren_ShouldNotCrash()
    {
        var widget = new Column(
            mainAxisAlignment: MainAxisAlignment.SpaceEvenly,
            children: []
        );

        Assert.DoesNotThrow(() =>
        {
            var ro = BuildAndLayout(widget, LayoutConstraints.Tight(100, 200));
            Assert.That(ro.Size.Y, Is.EqualTo(200));
        });
    }

    [Test]
    public void Stack_WithNegativePositioning_ShouldAllowOverflow()
    {
        var widget = new Stack([
            new Positioned(
                -10,
                -10,
                child: new Container(new BoxStyle { Width = 50, Height = 50 })
            ),
            new Container(new BoxStyle { Width = 100, Height = 100 })
        ]);

        var ro = BuildAndLayout(widget, LayoutConstraints.Tight(100, 100));

        Assert.That(ro.Children[0].X, Is.EqualTo(-10));
        Assert.That(ro.Children[0].Y, Is.EqualTo(-10));
        Assert.That(ro.Children[0].Size.X, Is.EqualTo(50));
        Assert.That(ro.Children[0].Size.Y, Is.EqualTo(50));
    }

    [Test]
    public void Padding_LargerThanConstraints_ShouldBeClamped()
    {
        // Parent allows max 100x100. Padding wants 60 on all sides (total 120x120).
        var widget = new Padding(
            EdgeInsets.All(60),
            new Container(new BoxStyle { Width = 10, Height = 10 })
        );

        var ro = BuildAndLayout(widget, LayoutConstraints.Loose(100, 100));

        // Final size should be 100x100 (clamped by parent)
        Assert.That(ro.Size.X, Is.EqualTo(100));
        Assert.That(ro.Size.Y, Is.EqualTo(100));

        var childRo = ro.Children[0];
        // Child should have 0 size because padding took all space.
        Assert.That(childRo.Size.X, Is.EqualTo(0));
        Assert.That(childRo.Size.Y, Is.EqualTo(0));
    }

    [Test]
    public void NestedExpanded_InDifferentOrientations()
    {
        // Row (200x300 tight)
        //   Expanded (flex 1) -> Column
        //     Expanded (flex 2) -> Container
        //     Expanded (flex 1) -> Container
        //   Expanded (flex 1) -> Container

        var widget = new Row(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Expanded(
                    flex: 1,
                    child: new Column(children:
                    [
                        new Expanded(flex: 2, child: new Container()),
                        new Expanded(flex: 1, child: new Container())
                    ])
                ),
                new Expanded(
                    flex: 1,
                    child: new Container()
                )
            ]);

        var ro = BuildAndLayout(widget, LayoutConstraints.Tight(200, 300));

        // ro is Row RenderObject
        // ro.Children[0] is Expanded ProxyBox
        // ro.Children[0].Children[0] is Column RenderObject
        // ro.Children[0].Children[0].Children[0] is Column's first Expanded ProxyBox

        var columnExpandedRo = ro.Children[0];
        Assert.That(columnExpandedRo.Size.X, Is.EqualTo(100));
        Assert.That(columnExpandedRo.Size.Y, Is.EqualTo(300));

        var colRo = columnExpandedRo.Children[0];
        Assert.That(colRo.Children[0].Size.Y, Is.EqualTo(200));
        Assert.That(colRo.Children[1].Size.Y, Is.EqualTo(100));

        Assert.That(ro.Children[1].Size.X, Is.EqualTo(100));
        Assert.That(ro.Children[1].Size.Y, Is.EqualTo(300));
    }

    [Test]
    public void MixedBag_ComplexLayout()
    {
        // Column (200x400 tight)
        //   Row (mainAxis: Center)
        //     Container (50x50)
        //     Expanded (flex 1) -> Container
        //   SizedBox (height: 50)
        //   Expanded (flex 1) -> Container

        var widget = new Column(children:
        [
            new Row(
                mainAxisAlignment: MainAxisAlignment.Center,
                children:
                [
                    new Container(new BoxStyle { Width = 50, Height = 50 }),
                    new Expanded(flex: 1, child: new Container())
                ]
            ),
            new SizedBox(height: 50),
            new Expanded(flex: 1, child: new Container())
        ]);

        var ro = BuildAndLayout(widget, LayoutConstraints.Tight(200, 400));

        // Column height = 400.
        // Fixed children: 
        //   Row: height is max of its children. Row has 1 fixed (50x50) and 1 flexible.
        //   Row's RenderFlex will take all available width (200) because it has flexible children.
        //   Row's height will be 50 (max of 50 and Container's height).
        //   SizedBox: height 50.
        // Total fixed height = 50 + 50 = 100.
        // Remaining height = 400 - 100 = 300.
        // Expanded (flex 1) gets 300.

        Assert.That(ro.Children[0].Size.Y, Is.EqualTo(50)); // Row
        Assert.That(ro.Children[1].Size.Y, Is.EqualTo(50)); // SizedBox
        Assert.That(ro.Children[2].Size.Y, Is.EqualTo(300)); // Expanded

        // Inside Row:
        // Width 200.
        // Fixed child: Container 50px.
        // Remaining width = 200 - 50 = 150.
        // Expanded (flex 1) gets 150.
        var rowRo = ro.Children[0];
        Assert.That(rowRo.Children[0].Size.X, Is.EqualTo(50));
        Assert.That(rowRo.Children[1].Size.X, Is.EqualTo(150));
    }
}

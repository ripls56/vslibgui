using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;

namespace Gui.Tests.Scroll;

[TestFixture]
public class ListViewAxisTests
{
    [Test]
    public void RenderViewport_Vertical_ConstrainsWidthTight()
    {
        var child = new RenderBox();
        var viewport = new RenderViewport { ScrollDirection = Axis.Vertical };
        viewport.AddChild(child);

        viewport.Layout(new LayoutConstraints(200, 200, 100, 100));

        Assert.That(child.Constraints.MinWidth, Is.EqualTo(200).Within(0.01f));
        Assert.That(child.Constraints.MaxWidth, Is.EqualTo(200).Within(0.01f));
        Assert.That(child.Constraints.MaxHeight, Is.EqualTo(float.MaxValue));
    }

    [Test]
    public void RenderViewport_Horizontal_ConstrainsHeightTight()
    {
        var child = new RenderBox();
        var viewport = new RenderViewport { ScrollDirection = Axis.Horizontal };
        viewport.AddChild(child);

        viewport.Layout(new LayoutConstraints(200, 200, 100, 100));

        Assert.That(child.Constraints.MinHeight, Is.EqualTo(100).Within(0.01f));
        Assert.That(child.Constraints.MaxHeight, Is.EqualTo(100).Within(0.01f));
        Assert.That(child.Constraints.MaxWidth, Is.EqualTo(float.MaxValue));
    }


    [Test]
    public void ListView_Horizontal_ShouldOnlyCreateVisibleElements()
    {
        var createdCount = 0;
        var listView = new ListView(
            (_, _) =>
            {
                createdCount++;
                return new Container(new BoxStyle { Width = 50 });
            },
            1000,
            50,
            scrollDirection: Axis.Horizontal
        );

        var root = listView.CreateElement();
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        root.AssignOwner(owner);
        root.Mount(null);

        Assert.That(createdCount, Is.LessThan(15));
    }


    [Test]
    public void Axis_DefaultScrollDirectionIsVertical()
    {
        var listView = new ListView(
            new[] { new Container(new BoxStyle { Height = 40 }) },
            40
        );

        Assert.That(listView.ScrollDirection, Is.EqualTo(Axis.Vertical));
    }

    [Test]
    public void Axis_ScrollDirectionIsPreserved()
    {
        var listView = new ListView(
            new[] { new Container(new BoxStyle { Width = 40 }) },
            40,
            scrollDirection: Axis.Horizontal
        );

        Assert.That(listView.ScrollDirection, Is.EqualTo(Axis.Horizontal));
    }


    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }
}

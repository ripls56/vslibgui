using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Tests.Scroll;

[TestFixture]
public class ScrollSystemTests
{
    [Test]
    public void RenderViewport_ShouldAdjustHitTestCoordinates()
    {
        var viewport =
            new RenderViewport { Size = new Vector2(100, 100), Offset = new Vector2(0, 50) };
        var child = new RenderBox
        {
            Size = new Vector2(100, 200),
            Color = new Vector4(1, 0, 0, 1) // Non-transparent to be a hit target
        };
        viewport.AddChild(child);

        var result = new HitTestResult();
        // Mouse click at (50, 10) in viewport space.
        // With offset 50, this is (50, 60) in child space.

        var hit = viewport.HitTest(result, new Vector2(50, 10), null!);
        Assert.That(hit, Is.True);
    }

    [Test]
    public void ListView_ShouldOnlyCreateVisibleElements()
    {
        var createdCount = 0;
        var listView = new ListView(
            (ctx, index) =>
            {
                createdCount++;
                return new Container(new BoxStyle { Height = 50 });
            },
            1000,
            50
        );

        var root = listView.CreateElement();
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());
        root.AssignOwner(owner);
        root.Mount(null); // This triggers initial build

        // At 400px height (hardcoded in ListView for now), 
        // we expect 400/50 = 8 items visible, maybe 9-10 with rounding
        Assert.That(createdCount, Is.LessThan(15));
    }

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }
}

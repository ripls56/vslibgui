using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;

namespace Gui.Tests.Widgets;

[TestFixture]
public class TooltipTests
{
    private class MockTickerProvider : ITickerProvider
    {
        public readonly List<Ticker> Tickers = [];

        public Ticker CreateTicker(Action<TimeSpan> onTick)
        {
            var t = new Ticker(onTick);
            Tickers.Add(t);
            return t;
        }

        public void Advance(TimeSpan elapsed)
        {
            var count = Tickers.Count;
            for (var i = 0; i < count; i++)
            {
                Tickers[i].Tick(elapsed);
            }
        }
    }

    private static (Element root, BuildOwner owner, MockTickerProvider vsync) Mount(
        Widget widget)
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);

        var rootWidget = new Overlay([new OverlayEntry(widget)]);
        var el = rootWidget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        el.RenderObject!.Layout(LayoutConstraints.Loose(800, 600));
        return (el, owner, vsync);
    }

    private static int OverlayEntryCount(Element root)
    {
        var overlayState = Overlay.Of(
            new BuildContext(new Overlay(), root))!;
        var stack = (Stack)overlayState.Build(
            new BuildContext(new Overlay(), root));
        return stack.Children.Count;
    }

    [Test]
    public void Tooltip_DoesNotShow_BeforeWaitDuration()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("hello"),
                TimeSpan.FromMilliseconds(500)));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));

        vsync.Advance(TimeSpan.FromMilliseconds(400));
        owner.BuildDirtyElements();

        Assert.That(OverlayEntryCount(root), Is.EqualTo(1),
            "Overlay should have only the root entry before WaitDuration elapses");
    }

    [Test]
    public void Tooltip_Shows_AfterWaitDuration()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("hello"),
                TimeSpan.FromMilliseconds(200)));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        vsync.Advance(TimeSpan.FromMilliseconds(150));
        owner.BuildDirtyElements();

        Assert.That(OverlayEntryCount(root), Is.EqualTo(2),
            "Overlay should have a second entry for the tooltip panel");
    }

    [Test]
    public void Tooltip_Hides_OnPointerExit()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("bye"),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(50)));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));
        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();

        // Move pointer away to trigger exit → fade-out
        dispatcher.DispatchPointerMove(root, new PointerEvent(500, 500));
        // Advance enough for fade-out to complete
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        Assert.That(OverlayEntryCount(root), Is.EqualTo(1),
            "Overlay should return to 1 entry after tooltip fades out");
    }

    [Test]
    public void Tooltip_DelayResets_OnEarlyExit()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("reset"),
                TimeSpan.FromMilliseconds(500)));

        var dispatcher = new EventDispatcher();

        // Enter and advance partially
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));
        vsync.Advance(TimeSpan.FromMilliseconds(300));

        // Exit before threshold
        dispatcher.DispatchPointerMove(root, new PointerEvent(500, 500));

        // Advance past what would have been the original threshold
        vsync.Advance(TimeSpan.FromMilliseconds(300));
        owner.BuildDirtyElements();

        Assert.That(OverlayEntryCount(root), Is.EqualTo(1),
            "Tooltip should not appear when pointer left before WaitDuration elapsed");
    }

    [Test]
    public void Tooltip_ReEnterDuringFadeOut_KeepsTooltipVisible()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("re-enter"),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100)));

        var dispatcher = new EventDispatcher();

        // Show tooltip
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));
        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();
        Assert.That(OverlayEntryCount(root), Is.EqualTo(2));

        // Start fade-out
        dispatcher.DispatchPointerMove(root, new PointerEvent(500, 500));
        vsync.Advance(TimeSpan.FromMilliseconds(30)); // partial fade-out

        // Re-enter during fade-out
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));
        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();

        Assert.That(OverlayEntryCount(root), Is.EqualTo(2),
            "Tooltip should remain visible when re-entering during fade-out");
    }

    [Test]
    public void Tooltip_BuildReturnsMouseRegionWrappingChild()
    {
        var child = new SizedBox(100, 40);
        var tooltip = new Tooltip(child, new Gui.Widgets.Basic.Text("x"));

        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var el = tooltip.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();

        Assert.That(el.RenderObject, Is.Not.Null,
            "Tooltip element should have a non-null RenderObject via MouseRegion");
    }

    [Test]
    public void Tooltip_Dispose_DoesNotThrow_WhenVisible()
    {
        var (root, owner, vsync) = Mount(
            new Tooltip(
                new SizedBox(100, 40),
                new Gui.Widgets.Basic.Text("dispose me"),
                TimeSpan.FromMilliseconds(100)));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 20));
        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();

        Assert.DoesNotThrow(() => root.Unmount(),
            "Unmounting while tooltip is visible must not throw");
    }
}

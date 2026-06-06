using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Tests.Helpers;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Tests.Gestures;

[TestFixture]
public class GestureTests
{
    private class ClickableWidget : RenderObjectWidget, IPointerClickHandler
    {
        public bool Clicked { get; private set; }
        public void OnPointerClick(PointerEvent args) => Clicked = true;
        public override RenderObject CreateRenderObject() => new RenderProxyBox();
    }

    [Test]
    public void EventDispatcher_ShouldDispatchClickToCorrectElement()
    {
        var dispatcher = new EventDispatcher();
        var widget = new ClickableWidget();
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        element.RenderObject!.Size = new Vector2(100, 100);

        var args = new PointerEvent(50, 50);
        dispatcher.DispatchPointerDown(element, args);
        dispatcher.DispatchPointerUp(element, args);

        Assert.That(widget.Clicked, Is.True);
    }

    [Test]
    public void GestureDetector_WithOnlyOnTap_ShouldReceiveClickEvents()
    {
        var tapped = false;
        var widget = new GestureDetector(
            new SizedBox(100, 100),
            _ => tapped = true
        );
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        var e = new PointerEvent(50, 50);
        dispatcher.DispatchPointerDown(element, e);
        dispatcher.DispatchPointerUp(element, e);

        Assert.That(tapped, Is.True);
    }

    [Test]
    public void GestureDetector_WithOnlyOnTap_ShouldNotBlockWheelEvents()
    {
        var wheelFired = false;
        // Inner: wheel handler. Outer: tap-only (should not block wheel).
        var inner = new GestureDetector(
            new SizedBox(100, 100),
            onWheel: _ => wheelFired = true
        );
        var outer = new GestureDetector(
            inner,
            _ => { }
        );

        var buildOwner = new BuildOwner();
        var element = outer.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchMouseWheel(element, new PointerEvent(50, 50, delta: 1));

        Assert.That(wheelFired, Is.True);
    }

    [Test]
    public void GestureDetector_WithOnWheel_ShouldReceiveWheelEvents()
    {
        var wheelFired = false;
        var widget = new GestureDetector(
            new SizedBox(100, 100),
            onWheel: _ => wheelFired = true
        );
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchMouseWheel(element, new PointerEvent(50, 50, delta: 1));

        Assert.That(wheelFired, Is.True);
    }

    [Test]
    public void GestureDetector_WithNoCallbacks_ShouldNotBeClickTarget()
    {
        var tapped = false;
        // Outer: no callbacks. Inner: onTap. Click should reach inner.
        var inner = new GestureDetector(
            new SizedBox(100, 100),
            _ => tapped = true
        );
        var outer = new GestureDetector(inner);

        var buildOwner = new BuildOwner();
        var element = outer.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        var e = new PointerEvent(50, 50);
        dispatcher.DispatchPointerDown(element, e);
        dispatcher.DispatchPointerUp(element, e);

        Assert.That(tapped, Is.True);
    }

    /// <summary>
    ///     Reproduces the ChatWidget structure:
    ///     Overlay > Stack > Container > Column > [Expanded > ListenableBuilder > ListView]
    ///     Wheel event at center should reach ListView's GestureDetector.
    /// </summary>
    [Test]
    public void WheelEvent_ShouldReachListView_InsideOverlayColumnExpanded()
    {
        var controller = new ScrollController();
        var listView = new ListView(
            (ctx, index) => new Container(
                new BoxStyle { Height = 50 },
                new Gui.Widgets.Basic.Text($"Item {index}")
            ),
            100,
            50,
            controller
        );

        // Mimic ChatWidget: Container > Column > Expanded > ListView
        var chatLike = new Container(
            new BoxStyle { Color = new Vector4(0, 0, 0, 0.4f), CornerRadius = new Vector4(8) },
            new Column(
                children:
                [
                    new Expanded(listView),
                    new SizedBox(height: 36) // input field placeholder
                ]
            )
        );

        // Wrap in Overlay > Stack like GuiBase does
        var overlay = new Overlay([new OverlayEntry(chatLike)]);

        var buildOwner = new BuildOwner();
        buildOwner.SetTickerProvider(new MockTickerProvider());
        var root = overlay.CreateElement();
        root.AssignOwner(buildOwner);
        root.Mount(null);
        buildOwner.BuildDirtyElements();

        // Layout with tight constraints like GuiBase does (450x350)
        root.RenderObject!.Layout(LayoutConstraints.Tight(450, 350));

        // Dump sizes for debugging
        var hitResult = new HitTestResult();
        var hit = root.HitTest(hitResult, new Vector2(225, 150));

        // Print path for debugging
        var pathItems = new List<string>();
        for (var i = 0; i < hitResult.Path.Count; i++)
        {
            var el = hitResult.Path[i].Element;
            var ro = el.RenderObject;
            pathItems.Add($"{el.Widget.GetType().Name}[{ro?.Size.X:F0}x{ro?.Size.Y:F0}]");
        }

        var path = string.Join(" > ", pathItems);
        Console.WriteLine($"Hit path: {path}");

        Assert.That(hit, Is.True, "Hit test should find something at center");
        Assert.That(hitResult.Path.Count, Is.GreaterThan(0), "Path should not be empty");

        // Now dispatch wheel event
        var dispatcher = new EventDispatcher();
        var wheelEvent = new PointerEvent(225, 150, delta: -1);
        dispatcher.DispatchMouseWheel(root, wheelEvent);

        Assert.That(wheelEvent.Handled, Is.True,
            $"Wheel event should be handled. Hit path: {path}");
    }

    /// <summary>
    ///     Verifies that clicking on a VtmlText link invokes OnLinkClick.
    /// </summary>
    [Test]
    public void VtmlText_ClickOnLink_ShouldInvokeOnLinkClick()
    {
        string? clickedHref = null;
        var vtmlWidget = new VtmlText(
            "<a href=\"handbook://test\">Click me</a>",
            onLinkClick: href => clickedHref = href
        );

        var buildOwner = new BuildOwner();
        buildOwner.SetTickerProvider(new MockTickerProvider());
        var element = vtmlWidget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        // Use Loose so Size reflects actual text height
        element.RenderObject!.Layout(LayoutConstraints.Loose(300, 500));

        var ro = TestHelpers.FindRenderRichText(element);
        Assert.That(ro, Is.Not.Null, "Should find RenderRichText in tree");
        Console.WriteLine($"RO.Size={ro!.Size}");

        var clickY = ro.Size.Y / 2;

        // Full event dispatch
        var hitResult = new HitTestResult();
        var hit = element.HitTest(hitResult, new Vector2(20, clickY));
        for (var i = 0; i < hitResult.Path.Count; i++)
        {
            var el = hitResult.Path[i].Element;
            Console.WriteLine(
                $"  [{i}] {el.GetType().Name}({el.Widget.GetType().Name})");
        }

        Assert.That(hit, Is.True, "Hit test should find VtmlText");

        var dispatcher = new EventDispatcher();
        var down = new PointerEvent(20, clickY);
        dispatcher.DispatchPointerDown(element, down);
        var up = new PointerEvent(20, clickY);
        dispatcher.DispatchPointerUp(element, up);

        Assert.That(clickedHref, Is.EqualTo("handbook://test"),
            "EventDispatcher click should invoke OnLinkClick");
    }

    /// <summary>
    ///     Mimics ChatMessageBlock structure:
    ///     Padding > MouseRegion > Container > Stack > [Padding > Column > ... > VtmlText]
    ///     Verifies click reaches VtmlText OnLinkClick through the full hierarchy.
    /// </summary>
    [Test]
    public void VtmlText_ClickInChatMessageStructure_ShouldInvokeOnLinkClick()
    {
        string? clickedHref = null;
        var vtml = new VtmlText(
            "<a href=\"handbook://test\">Click me</a>",
            onLinkClick: href => clickedHref = href
        );

        // Mimic ChatMessageBlock widget tree
        Widget content = new Padding(
            EdgeInsets.All(6),
            new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Gui.Widgets.Basic.Text("12:00", new TextStyle { FontSize = 11 }),
                    new SizedBox(height: 4),
                    new Padding(
                        EdgeInsets.Only(1),
                        vtml
                    )
                ]
            )
        );

        var tree = new Padding(
            EdgeInsets.Only(4, right: 4),
            new MouseRegion(
                onEnter: _ => { },
                onExit: _ => { },
                child: new Container(
                    new BoxStyle { Color = Vector4.Zero, CornerRadius = new Vector4(3) },
                    new Stack([content])
                )
            )
        );

        var buildOwner = new BuildOwner();
        buildOwner.SetTickerProvider(new MockTickerProvider());
        var element = tree.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Loose(400, 500));

        // Find VtmlText's RenderRichText size for click coords
        var ro = TestHelpers.FindRenderRichText(element);
        Assert.That(ro, Is.Not.Null, "Should find RenderRichText in tree");
        Console.WriteLine($"RenderRichText Size={ro!.Size}");

        // Click in the middle of the VtmlText area (global coords)
        var vtmlGlobal = ro.LocalToGlobal(
            new Vector2(20, ro.Size.Y / 2));
        Console.WriteLine($"Click global: {vtmlGlobal}");

        // Hit test
        var hitResult = new HitTestResult();
        var hit = element.HitTest(hitResult, vtmlGlobal);
        for (var i = 0; i < hitResult.Path.Count; i++)
        {
            var el = hitResult.Path[i].Element;
            Console.WriteLine(
                $"  [{i}] {el.GetType().Name}({el.Widget.GetType().Name})");
        }

        Assert.That(hit, Is.True, "Hit test should succeed");

        // Dispatch click
        var dispatcher = new EventDispatcher();
        var down = new PointerEvent(vtmlGlobal.X, vtmlGlobal.Y);
        dispatcher.DispatchPointerDown(element, down);
        var up = new PointerEvent(vtmlGlobal.X, vtmlGlobal.Y);
        dispatcher.DispatchPointerUp(element, up);

        Assert.That(clickedHref, Is.EqualTo("handbook://test"),
            "Link click should fire through full ChatMessage-like hierarchy");
    }

    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }
}

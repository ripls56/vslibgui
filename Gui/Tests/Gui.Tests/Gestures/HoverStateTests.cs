using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using NUnit.Framework;

namespace Gui.Tests.Gestures;

/// <summary>
///     Covers the hover and capture state that <c>GuiBase</c> reads before it cancels hover
///     from its per-frame pointer watchdog. Cancelling must fire the pending exit, and must
///     never happen while a widget holds capture.
/// </summary>
[TestFixture]
public class HoverStateTests
{
    private static (Element root, BuildOwner owner) Mount(Widget widget)
    {
        var owner = new BuildOwner();
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        el.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));
        return (el, owner);
    }

    [Test]
    public void HasHoverState_FalseBeforeAnyPointerEvent()
    {
        var (root, _) = Mount(new MouseRegion(new SizedBox(100, 100), onEnter: _ => { }));
        var dispatcher = new EventDispatcher();

        Assert.That(dispatcher.HasHoverState, Is.False);
        Assert.That(root, Is.Not.Null);
    }

    [Test]
    public void HasHoverState_TrueAfterEnter_FalseAfterOffscreenMove()
    {
        var exited = 0;
        var (root, _) = Mount(new MouseRegion(
            new SizedBox(100, 100),
            onEnter: _ => { },
            onExit: _ => exited++));
        var dispatcher = new EventDispatcher();

        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 50));
        Assert.That(dispatcher.HasHoverState, Is.True, "Hover is latched after an enter");

        // The same call GuiBase makes when the pointer belongs to another window.
        dispatcher.DispatchPointerMove(root, new PointerEvent(-1, -1));

        Assert.That(dispatcher.HasHoverState, Is.False);
        Assert.That(exited, Is.EqualTo(1), "Cancelling hover must fire the pending exit");
    }

    [Test]
    public void IsPointerCaptured_TrueBetweenDownAndUp()
    {
        var (root, _) = Mount(new GestureDetector(
            new SizedBox(100, 100),
            onPress: _ => { },
            onRelease: _ => { }));
        var dispatcher = new EventDispatcher();

        Assert.That(dispatcher.IsPointerCaptured, Is.False);

        dispatcher.DispatchPointerDown(root, new PointerEvent(50, 50, PointerButton.Left));
        Assert.That(dispatcher.IsPointerCaptured, Is.True, "A press captures the pointer");

        dispatcher.DispatchPointerUp(root, new PointerEvent(50, 50, PointerButton.Left));
        Assert.That(dispatcher.IsPointerCaptured, Is.False);
    }
}

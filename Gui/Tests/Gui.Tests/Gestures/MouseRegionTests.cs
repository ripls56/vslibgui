using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;

namespace Gui.Tests.Gestures;

[TestFixture]
public class MouseRegionTests
{
    private static (Element root, BuildOwner owner) Mount(Widget widget)
    {
        var owner = new BuildOwner();
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        return (el, owner);
    }

    [Test]
    public void OnEnter_FiredWhenPointerEntersRegion()
    {
        var entered = false;
        var widget = new MouseRegion(
            new SizedBox(100, 100),
            _ => entered = true);
        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 50));

        Assert.That(entered, Is.True);
    }

    [Test]
    public void OnExit_FiredWhenPointerLeavesRegion()
    {
        var exited = false;
        var widget = new MouseRegion(
            new SizedBox(100, 100),
            onExit: _ => exited = true);
        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 50));
        dispatcher.DispatchPointerMove(root, new PointerEvent(150, 150));

        Assert.That(exited, Is.True);
    }

    [Test]
    public void OnHover_FiredOnEveryMoveInsideRegion()
    {
        var moveCount = 0;
        var widget = new MouseRegion(
            new SizedBox(100, 100),
            onHover: _ => moveCount++);
        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(10, 10));
        dispatcher.DispatchPointerMove(root, new PointerEvent(20, 20));
        dispatcher.DispatchPointerMove(root, new PointerEvent(30, 30));

        Assert.That(moveCount, Is.EqualTo(3));
    }

    [Test]
    public void ResolveHoveredCursor_ReturnsInnermostMouseRegionCursor()
    {
        var inner = new MouseRegion(
            new SizedBox(50, 50),
            cursor: MouseCursor.LinkSelect);
        var outer = new MouseRegion(
            inner,
            cursor: MouseCursor.TextSelect);

        var (root, _) = Mount(outer);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(25, 25));

        var resolved = dispatcher.ResolveHoveredCursor();
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Name, Is.EqualTo("linkselect"));
    }

    [Test]
    public void ResolveHoveredCursor_FallsBackToOuterWhenInnerHasNoCursor()
    {
        var inner = new MouseRegion(
            new SizedBox(50, 50),
            _ => { });
        var outer = new MouseRegion(
            inner,
            cursor: MouseCursor.TextSelect);

        var (root, _) = Mount(outer);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(25, 25));

        var resolved = dispatcher.ResolveHoveredCursor();
        Assert.That(resolved?.Name, Is.EqualTo("textselect"));
    }

    [Test]
    public void ResolveHoveredCursor_ReturnsNullWhenNoMouseRegionHovered()
    {
        var widget = new SizedBox(100, 100);
        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        var resolved = dispatcher.ResolveHoveredCursor();

        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void MouseRegion_WithCursorOnly_IsHitTestable()
    {
        var widget = new MouseRegion(
            new SizedBox(100, 100),
            cursor: MouseCursor.LinkSelect);

        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        dispatcher.DispatchPointerMove(root, new PointerEvent(50, 50));

        var resolved = dispatcher.ResolveHoveredCursor();
        Assert.That(resolved?.Name, Is.EqualTo("linkselect"));
    }

    [Test]
    public void MouseRegion_DoesNotBlockClickEvents()
    {
        var tapped = false;
        var widget = new MouseRegion(
            cursor: MouseCursor.LinkSelect,
            child: new GestureDetector(
                onTap: _ => tapped = true,
                child: new SizedBox(100, 100)));

        var (root, _) = Mount(widget);
        root.RenderObject!.Layout(LayoutConstraints.Tight(100, 100));

        var dispatcher = new EventDispatcher();
        var args = new PointerEvent(50, 50);
        dispatcher.DispatchPointerDown(root, args);
        dispatcher.DispatchPointerUp(root, args);

        Assert.That(tapped, Is.True);
    }

    [Test]
    public void MouseCursor_Custom_CreatesWithGivenName()
    {
        var cursor = MouseCursor.Custom("mycursor");
        Assert.That(cursor.Name, Is.EqualTo("mycursor"));
    }
}

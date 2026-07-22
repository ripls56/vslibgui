using Gui.Core.Windowing;
using NUnit.Framework;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gui.Tests.Windowing;

[TestFixture]
public class WindowInteractionStateMachineTests
{
    private static WindowInteractionStateMachine Make(
        bool draggable = true, bool resizable = true,
        float minW = 100, float minH = 100, float maxW = 1000, float maxH = 1000,
        float dragH = 30, float resizeHandle = 8) =>
        new(new WindowConfig
        {
            Draggable = draggable,
            DragHandleHeight = dragH,
            Resizable = resizable,
            ResizeHandleSize = resizeHandle,
            MinSize = new Vector2(minW, minH),
            MaxSize = new Vector2(maxW, maxH),
        });

    [Test]
    public void Drag_InDragZone_MovesWindow()
    {
        var sm = Make();
        var initialPos = new Vector2(100, 100);
        sm.BeginDrag(new Vector2(150, 115), initialPos);
        var moved = sm.OnMove(new Vector2(200, 145), 0.1, out var newPos, out _);
        Assert.That(moved, Is.True);
        Assert.That(newPos, Is.EqualTo(initialPos + new Vector2(50, 30)));
    }

    [Test]
    public void Resize_RightEdge_GrowsWidth()
    {
        var sm = Make();
        sm.BeginResize(ResizeEdge.Right, new Vector2(400, 150), new Vector2(100, 100), new Vector2(300, 200));
        sm.OnMove(new Vector2(450, 150), 0.1, out _, out var newSize);
        Assert.That(newSize.X, Is.EqualTo(350f));
    }

    [Test]
    public void Resize_LeftEdge_ShrinksWidthAndMovesPos()
    {
        var sm = Make();
        sm.BeginResize(ResizeEdge.Left, new Vector2(100, 150), new Vector2(100, 100), new Vector2(300, 200));
        sm.OnMove(new Vector2(130, 150), 0.1, out var newPos, out var newSize);
        Assert.That(newSize.X, Is.EqualTo(270f));
        Assert.That(newPos.X, Is.EqualTo(130f));
    }

    [Test]
    public void Resize_RightEdge_ClampsToMinWidth()
    {
        var sm = Make(minW: 100);
        sm.BeginResize(ResizeEdge.Right, new Vector2(400, 150), new Vector2(100, 100), new Vector2(300, 200));
        sm.OnMove(new Vector2(150, 150), 0.1, out _, out var newSize);
        Assert.That(newSize.X, Is.EqualTo(100f));
    }

    [Test]
    public void Resize_LeftEdge_ClampAdjustsPosition()
    {
        var sm = Make(minW: 100);
        sm.BeginResize(ResizeEdge.Left, new Vector2(100, 150), new Vector2(100, 100), new Vector2(300, 200));
        sm.OnMove(new Vector2(350, 150), 0.1, out var newPos, out var newSize);
        Assert.That(newSize.X, Is.EqualTo(100f));
        Assert.That(newPos.X, Is.EqualTo(300f));
    }

    [Test]
    public void SettleThrottle_CommitsAfterThreshold()
    {
        var sm = Make();
        sm.BeginResize(ResizeEdge.Right, new Vector2(400, 150), new Vector2(100, 100), new Vector2(300, 200));
        sm.OnMove(new Vector2(450, 150), 0.05, out _, out _);
        Assert.That(sm.ShouldCommitLayout(0.10), Is.False);
        Assert.That(sm.ShouldCommitLayout(0.25), Is.True);
        Assert.That(sm.ShouldCommitLayout(0.50), Is.False);
    }

    [Test]
    public void HoverCursor_TopEdge_ReturnsResizeNS()
    {
        var sm = Make();
        var cursor = sm.HoverCursorFor(new Vector2(150, 4), new Vector2(300, 200));
        Assert.That(cursor, Is.EqualTo((CursorShape?)CursorShape.ResizeNS));
    }

    [Test]
    public void HoverCursor_TopRightCorner_ReturnsResizeNESW()
    {
        var sm = Make();
        var cursor = sm.HoverCursorFor(new Vector2(296, 4), new Vector2(300, 200));
        Assert.That(cursor, Is.EqualTo((CursorShape?)CursorShape.ResizeNESW));
    }

    [Test]
    public void HoverCursor_DuringDrag_ReturnsNull()
    {
        var sm = Make();
        sm.BeginDrag(new Vector2(150, 15), new Vector2(100, 100));
        Assert.That(sm.HoverCursorFor(new Vector2(150, 4), new Vector2(300, 200)), Is.Null);
    }

    [Test]
    public void Drag_NotDraggable_IsInDragZoneReturnsFalse()
    {
        var sm = Make(draggable: false);
        Assert.That(sm.IsInDragZone(new Vector2(150, 10), new Vector2(300, 200)), Is.False);
    }

    [Test]
    public void OnUp_AfterDrag_ReturnedWasDraggingTrue()
    {
        var sm = Make();
        sm.BeginDrag(new Vector2(150, 115), new Vector2(100, 100));
        var (wasDrag, wasResize) = sm.OnUp();
        Assert.That(wasDrag, Is.True);
        Assert.That(wasResize, Is.False);
        Assert.That(sm.IsDragging, Is.False);
    }

    [Test]
    public void OnMove_WhenNotInteracting_ReturnsFalse()
    {
        var sm = Make();
        var moved = sm.OnMove(new Vector2(200, 200), 0.1, out _, out _);
        Assert.That(moved, Is.False);
    }
}

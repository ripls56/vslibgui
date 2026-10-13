using Gui.Core.Windowing;
using NUnit.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Windowing;

/// <summary>
///     Covers the hover watchdog that keeps a window from holding hover state after the
///     pointer moved onto a window in front of it. The engine routes a mouse move to one
///     dialog only, so the starved window has to notice on its own.
/// </summary>
[TestFixture]
public class PointerWatchdogTests
{
    private static readonly Vector2 WindowPos = new(100, 200);
    private static readonly Vector2 WindowSize = new(300, 150);

    [Test]
    public void NeedsResync_TrueBeforeFirstSync()
    {
        var watchdog = new PointerWatchdog();

        Assert.That(watchdog.NeedsResync(new Vector2(10, 10), WindowPos), Is.True,
            "A fresh watchdog has never synced hover, so the first frame must resync");
    }

    [Test]
    public void NeedsResync_FalseAfterMarkSynced_WithSameValues()
    {
        var watchdog = new PointerWatchdog();
        var cursor = new Vector2(120, 220);
        watchdog.MarkSynced(cursor, WindowPos);

        Assert.That(watchdog.NeedsResync(cursor, WindowPos), Is.False,
            "Nothing moved, so hover still describes what is under the pointer");
    }

    [Test]
    public void NeedsResync_TrueWhenCursorMoved()
    {
        var watchdog = new PointerWatchdog();
        watchdog.MarkSynced(new Vector2(120, 220), WindowPos);

        Assert.That(watchdog.NeedsResync(new Vector2(121, 220), WindowPos), Is.True);
    }

    [Test]
    public void NeedsResync_TrueWhenWindowMovedUnderStaticCursor()
    {
        var watchdog = new PointerWatchdog();
        var cursor = new Vector2(120, 220);
        watchdog.MarkSynced(cursor, WindowPos);

        Assert.That(watchdog.NeedsResync(cursor, WindowPos + new Vector2(0, 40)), Is.True,
            "Dragging the window away from a static cursor also invalidates hover");
    }

    [Test]
    public void ShouldCancelHover_TrueWhenBlockedByFrontWindow()
    {
        var cursorInside = WindowPos + new Vector2(10, 10);

        Assert.That(
            PointerWatchdog.ShouldCancelHover(cursorInside, WindowPos, WindowSize, true),
            Is.True,
            "A focused window in front owns the cursor even when it is over our own rect");
    }

    [Test]
    public void ShouldCancelHover_TrueWhenCursorOutsideWindow()
    {
        var outside = WindowPos + new Vector2(WindowSize.X + 1, 10);

        Assert.That(
            PointerWatchdog.ShouldCancelHover(outside, WindowPos, WindowSize, false),
            Is.True);
    }

    [Test]
    public void ShouldCancelHover_FalseWhenCursorInsideAndUnblocked()
    {
        var inside = WindowPos + new Vector2(WindowSize.X, WindowSize.Y);

        Assert.That(
            PointerWatchdog.ShouldCancelHover(inside, WindowPos, WindowSize, false),
            Is.False,
            "Edges count as inside, matching GuiBase.IsInsideWindow");
    }

    [Test]
    public void ShouldCancelHover_FalseForCursorAtWindowOrigin()
    {
        Assert.That(
            PointerWatchdog.ShouldCancelHover(WindowPos, WindowPos, WindowSize, false),
            Is.False);
    }

    [Test]
    public void ShouldCancelHover_TrueForNegativeLocalCursor()
    {
        var above = WindowPos - new Vector2(0, 1);

        Assert.That(
            PointerWatchdog.ShouldCancelHover(above, WindowPos, WindowSize, false),
            Is.True);
    }
}

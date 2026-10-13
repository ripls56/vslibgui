using OpenTK.Mathematics;

namespace Gui.Core.Windowing;

/// <summary>
///     Decides when a window must drop its latched hover state because the pointer moved
///     without the window being told.
///     <para>
///         The engine routes a mouse move to one dialog only (the first in <c>LoadedGuis</c>
///         that marks it handled), so a window in front starves the windows behind it. Their
///         hovered element — an item slot, a tooltip trigger — keeps its hover and never gets
///         the exit event that clears it. <see cref="GuiBase" /> compares the global cursor
///         against the position it last synced hover for, once per frame, and cancels hover
///         when the pointer left the window or moved onto a window in front.
///     </para>
///     <para>Pure value logic: no VS or GLFW calls, so it is unit-testable.</para>
/// </summary>
internal sealed class PointerWatchdog
{
    private static readonly Vector2 Unset = new(float.NaN, float.NaN);

    private Vector2 _syncedCursor = Unset;
    private Vector2 _syncedWindowPos = Unset;

    /// <summary>
    ///     True when the cursor or the window moved since the last hover sync, so the latched
    ///     hover state may no longer describe what is under the pointer. NaN compares unequal
    ///     to everything, so the first call after construction always reports true.
    /// </summary>
    internal bool NeedsResync(Vector2 cursor, Vector2 windowPos) =>
        cursor != _syncedCursor || windowPos != _syncedWindowPos;

    /// <summary>
    ///     Records that hover is in sync with the pointer for this cursor and window position.
    ///     Call after every dispatch, including a cancelled one, so the next frame is a no-op.
    /// </summary>
    internal void MarkSynced(Vector2 cursor, Vector2 windowPos)
    {
        _syncedCursor = cursor;
        _syncedWindowPos = windowPos;
    }

    /// <summary>
    ///     True when hover must be cancelled: a window in front owns the cursor position, or
    ///     the cursor is outside this window's rect.
    /// </summary>
    internal static bool ShouldCancelHover(
        Vector2 cursor,
        Vector2 windowPos,
        Vector2 windowSize,
        bool blockedByFrontWindow)
    {
        if (blockedByFrontWindow)
        {
            return true;
        }

        var local = cursor - windowPos;
        return local.X < 0f || local.X > windowSize.X ||
               local.Y < 0f || local.Y > windowSize.Y;
    }
}

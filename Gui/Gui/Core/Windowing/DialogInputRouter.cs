using System;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;

namespace Gui.Core.Windowing;

/// <summary>
///     Thin adapter that translates raw input coordinates and key codes into
///     <see cref="PointerEvent" /> / <see cref="KeyboardEvent" /> objects and
///     forwards them to <see cref="EventDispatcher" /> and <see cref="FocusManager" />.
///     Also handles focus-on-click by invoking <c>onFocusAcquired</c> at the start
///     of every <see cref="PointerDown" /> call.
///     Keeps all event-object construction out of <c>GuiBase</c>.
/// </summary>
internal sealed class DialogInputRouter
{
    private readonly EventDispatcher _dispatcher;
    private readonly FocusManager _focusManager;
    private readonly Action? _onFocusAcquired;

    /// <summary>
    ///     Creates a router bound to the given dispatcher and focus manager.
    ///     <paramref name="onFocusAcquired" /> is invoked at the start of every
    ///     <see cref="PointerDown" /> to implement focus-on-click (e.g. setting the
    ///     active window reference in the mod system).
    /// </summary>
    internal DialogInputRouter
    (
        EventDispatcher dispatcher,
        FocusManager focusManager,
        Action? onFocusAcquired = null
    )
    {
        _dispatcher = dispatcher;
        _focusManager = focusManager;
        _onFocusAcquired = onFocusAcquired;
    }

    /// <summary>
    ///     Fires the focus-on-click callback then dispatches a pointer-down event
    ///     at <paramref name="x" />, <paramref name="y" />.
    ///     Returns <c>true</c> when a widget captured the event.
    /// </summary>
    internal bool PointerDown
    (
        Element root,
        float x,
        float y,
        PointerButton button
    )
    {
        _onFocusAcquired?.Invoke();
        var e = new PointerEvent(x, y, button);
        return _dispatcher.DispatchPointerDown(root, e);
    }

    /// <summary>
    ///     Dispatches a pointer-move event and returns the event so the caller
    ///     can inspect <see cref="PointerEvent.Handled" />.
    /// </summary>
    internal PointerEvent PointerMove
    (
        Element root,
        float x,
        float y
    )
    {
        var e = new PointerEvent(x, y);
        _dispatcher.DispatchPointerMove(root, e);
        return e;
    }

    /// <summary>
    ///     True while an element is latched as hovered and still owes a pointer-exit call.
    /// </summary>
    internal bool HasHoverState => _dispatcher.HasHoverState;

    /// <summary>
    ///     True while a widget holds pointer capture; hover must not be cancelled then.
    /// </summary>
    internal bool IsPointerCaptured => _dispatcher.IsPointerCaptured;

    /// <summary>
    ///     Sends an off-screen move at (−1, −1) to trigger pointer-leave events
    ///     on any currently-hovered elements.
    /// </summary>
    internal void PointerLeave
    (
        Element root
    ) => _dispatcher.DispatchPointerMove(root, new PointerEvent(-1, -1));

    /// <summary>
    ///     Fires pointer-exit on the currently hovered hierarchy and clears hover state
    ///     without a hit test. Used on dialog close, where the engine warps the cursor and
    ///     no further move event would ever reach the tree.
    /// </summary>
    internal void CancelHover() => _dispatcher.CancelHover();

    /// <summary>
    ///     Dispatches a pointer-up event and returns the event so the caller
    ///     can inspect <see cref="PointerEvent.Handled" />.
    /// </summary>
    internal PointerEvent PointerUp
    (
        Element root,
        float x,
        float y,
        PointerButton button
    )
    {
        var e = new PointerEvent(x, y, button);
        _dispatcher.DispatchPointerUp(root, e);
        return e;
    }

    /// <summary>
    ///     Dispatches a mouse-wheel event.
    ///     Returns <c>true</c> when a widget handled the scroll.
    /// </summary>
    internal bool MouseWheel
    (
        Element root,
        float x,
        float y,
        float delta
    )
    {
        var e = new PointerEvent(x, y, delta: delta);
        _dispatcher.DispatchMouseWheel(root, e);
        return e.Handled;
    }

    /// <summary>
    ///     Dispatches a key-down event.
    ///     Returns <c>true</c> when the event was handled.
    /// </summary>
    internal bool KeyDown
    (
        int keyCode,
        bool shift,
        bool ctrl,
        bool alt
    )
    {
        var e = new KeyboardEvent
            (KeyEventType.KeyDown, keyCode, shift: shift, ctrl: ctrl, alt: alt);
        _dispatcher.DispatchKeyDown(_focusManager, e);
        return e.Handled;
    }

    /// <summary>
    ///     Dispatches a key-up event.
    ///     Returns <c>true</c> when the event was handled.
    /// </summary>
    internal bool KeyUp
    (
        int keyCode,
        bool shift,
        bool ctrl,
        bool alt
    )
    {
        var e = new KeyboardEvent(KeyEventType.KeyUp, keyCode, shift: shift, ctrl: ctrl, alt: alt);
        _dispatcher.DispatchKeyUp(_focusManager, e);
        return e.Handled;
    }

    /// <summary>
    ///     Dispatches a key-char event.
    ///     Returns <c>true</c> when the event was handled.
    /// </summary>
    internal bool KeyChar
    (
        char keyChar,
        bool shift,
        bool ctrl,
        bool alt
    )
    {
        var e = new KeyboardEvent
            (KeyEventType.KeyChar, keyChar: keyChar, shift: shift, ctrl: ctrl, alt: alt);
        _dispatcher.DispatchKeyChar(_focusManager, e);
        return e.Handled;
    }

    /// <summary>
    ///     Returns the <see cref="MouseCursor" /> resolved from the last pointer-move
    ///     hit-test path, or <c>null</c> if no <c>MouseRegion</c> with a cursor was hit.
    /// </summary>
    internal MouseCursor? ResolvedCursor() =>
        _dispatcher.ResolveHoveredCursor();
}

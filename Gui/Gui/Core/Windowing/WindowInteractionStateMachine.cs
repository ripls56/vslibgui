using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gui.Core.Windowing;

/// <summary>
/// Pure state machine for window drag and resize interactions.
/// Holds start-of-interaction snapshots and processes move/up events to compute
/// desired position and size. Contains no VS or GLFW API dependencies beyond types.
/// </summary>
internal sealed class WindowInteractionStateMachine
{
    private const double SettleMs = 150.0;

    private readonly WindowConfig _config;

    private Vector2 _dragStartScreen;
    private Vector2 _dragStartPos;

    private ResizeEdge _resizeEdge;
    private Vector2 _resizeStartScreen;
    private Vector2 _resizeStartPos;
    private Vector2 _resizeStartSize;

    private double _lastResizeTimeSeconds;
    private bool _resizeLayoutPending;

    /// <summary>True while a drag interaction is active.</summary>
    public bool IsDragging { get; private set; }

    /// <summary>True while a resize interaction is active.</summary>
    public bool IsResizing { get; private set; }

    /// <summary>True while any window interaction is active.</summary>
    public bool IsInteracting => IsDragging || IsResizing;

    /// <summary>Creates a new state machine bound to the given window configuration.</summary>
    public WindowInteractionStateMachine(WindowConfig config) => _config = config;

    /// <summary>
    /// Detects which resize edge a window-local point falls on.
    /// Returns <see cref="ResizeEdge.None"/> when the window is not resizable or
    /// the point is outside a grip zone.
    /// </summary>
    public ResizeEdge DetectResizeEdge(Vector2 local, Vector2 windowSize)
    {
        if (!_config.Resizable || !IsInsideWindow(local, windowSize))
            return ResizeEdge.None;

        var grip = _config.ResizeHandleSize;
        var edge = ResizeEdge.None;

        if (local.X <= grip)
            edge |= ResizeEdge.Left;
        else if (local.X >= windowSize.X - grip)
            edge |= ResizeEdge.Right;

        if (local.Y <= grip)
            edge |= ResizeEdge.Top;
        else if (local.Y >= windowSize.Y - grip)
            edge |= ResizeEdge.Bottom;

        return edge;
    }

    /// <summary>Returns true when the point falls inside the draggable title-bar zone.</summary>
    public bool IsInDragZone(Vector2 local, Vector2 windowSize)
    {
        return _config.Draggable &&
               local.Y >= 0 && local.Y <= _config.DragHandleHeight &&
               local.X >= 0 && local.X <= windowSize.X;
    }

    /// <summary>
    /// Returns the cursor shape to display when hovering at a window-local position,
    /// or <c>null</c> when an interaction is active and the cursor should not change.
    /// </summary>
    public CursorShape? HoverCursorFor(Vector2 local, Vector2 windowSize)
    {
        if (IsDragging || IsResizing)
            return null;
        var edge = DetectResizeEdge(local, windowSize);
        return edge != ResizeEdge.None ? CursorForEdge(edge) : CursorShape.Arrow;
    }

    /// <summary>Maps a <see cref="ResizeEdge"/> combination to the appropriate cursor shape.</summary>
    public static CursorShape CursorForEdge(ResizeEdge edge)
    {
        var left = (edge & ResizeEdge.Left) != 0;
        var right = (edge & ResizeEdge.Right) != 0;
        var top = (edge & ResizeEdge.Top) != 0;
        var bottom = (edge & ResizeEdge.Bottom) != 0;

        if ((left && top) || (right && bottom))
            return CursorShape.ResizeNWSE;
        if ((right && top) || (left && bottom))
            return CursorShape.ResizeNESW;
        if (left || right)
            return CursorShape.ResizeEW;
        if (top || bottom)
            return CursorShape.ResizeNS;
        return CursorShape.Arrow;
    }

    /// <summary>Starts a drag interaction. Call when <c>OnMouseDown</c> confirms the drag zone.</summary>
    public void BeginDrag(Vector2 screen, Vector2 currentPos)
    {
        IsDragging = true;
        _dragStartScreen = screen;
        _dragStartPos = currentPos;
    }

    /// <summary>Starts a resize interaction. Call when <c>OnMouseDown</c> detects a resize edge.</summary>
    public void BeginResize(ResizeEdge edge, Vector2 screen, Vector2 currentPos, Vector2 currentSize)
    {
        IsResizing = true;
        _resizeEdge = edge;
        _resizeStartScreen = screen;
        _resizeStartPos = currentPos;
        _resizeStartSize = currentSize;
    }

    /// <summary>
    /// Processes a mouse-move event during an active interaction.
    /// Returns <c>true</c> when drag or resize is active; <paramref name="newPos"/> and
    /// <paramref name="newSize"/> hold the desired values (newSize is default for drag).
    /// Also advances the settle-throttle timer when resizing.
    /// </summary>
    public bool OnMove(Vector2 screen, double elapsedSeconds, out Vector2 newPos, out Vector2 newSize)
    {
        newPos = default;
        newSize = default;

        if (IsDragging)
        {
            newPos = _dragStartPos + (screen - _dragStartScreen);
            return true;
        }

        if (IsResizing)
        {
            var delta = screen - _resizeStartScreen;
            var pos = _resizeStartPos;
            var size = _resizeStartSize;

            if ((_resizeEdge & ResizeEdge.Left) != 0)
            {
                pos.X = _resizeStartPos.X + delta.X;
                size.X = _resizeStartSize.X - delta.X;
            }

            if ((_resizeEdge & ResizeEdge.Right) != 0)
                size.X = _resizeStartSize.X + delta.X;

            if ((_resizeEdge & ResizeEdge.Top) != 0)
            {
                pos.Y = _resizeStartPos.Y + delta.Y;
                size.Y = _resizeStartSize.Y - delta.Y;
            }

            if ((_resizeEdge & ResizeEdge.Bottom) != 0)
                size.Y = _resizeStartSize.Y + delta.Y;

            var clamped = ClampSize(size);

            if ((_resizeEdge & ResizeEdge.Left) != 0)
                pos.X += size.X - clamped.X;

            if ((_resizeEdge & ResizeEdge.Top) != 0)
                pos.Y += size.Y - clamped.Y;

            newPos = pos;
            newSize = clamped;

            _lastResizeTimeSeconds = elapsedSeconds;
            _resizeLayoutPending = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ends the current drag or resize interaction.
    /// Returns <c>(wasDragging, wasResizing)</c> so the caller can apply side effects
    /// (save drag position, commit resize layout).
    /// </summary>
    public (bool wasDragging, bool wasResizing) OnUp()
    {
        var d = IsDragging;
        var r = IsResizing;
        IsDragging = false;
        IsResizing = false;
        return (d, r);
    }

    /// <summary>
    /// Returns <c>true</c> once when the resize settle throttle fires (no resize move
    /// for 150 ms). Resets the pending flag on return — subsequent calls return
    /// <c>false</c> until the next resize move.
    /// Both <paramref name="elapsedSeconds"/> here and in <see cref="OnMove"/> must come
    /// from the same monotonically increasing game clock (accumulated frame time in seconds).
    /// </summary>
    public bool ShouldCommitLayout(double elapsedSeconds)
    {
        if (!_resizeLayoutPending)
            return false;
        if ((elapsedSeconds - _lastResizeTimeSeconds) * 1000.0 < SettleMs)
            return false;

        _resizeLayoutPending = false;
        return true;
    }

    private Vector2 ClampSize(Vector2 size) =>
        new(
            Math.Clamp(size.X, _config.MinSize.X, _config.MaxSize.X),
            Math.Clamp(size.Y, _config.MinSize.Y, _config.MaxSize.Y)
        );

    private static bool IsInsideWindow(Vector2 local, Vector2 windowSize) =>
        local.X >= 0 && local.X <= windowSize.X &&
        local.Y >= 0 && local.Y <= windowSize.Y;
}

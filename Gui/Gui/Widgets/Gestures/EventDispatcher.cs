using System;
using System.Linq;
using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Widgets.Gestures;

public class EventDispatcher
{
    private static readonly Func<object, bool> IsWheelTarget = IsActiveTarget<IPointerWheelHandler>;

    private static readonly Func<object, bool> IsDragEnterTarget =
        IsActiveTarget<IPointerDragEnterHandler>;

    private readonly HitTestResult _dragHitTestResult = new();
    private readonly HitTestResult _hitTestResult = new();
    private Element? _capturedElement;
    private Element? _dragHoveredElement;
    private Element? _hoveredElement;
    private Element? _pressedElement;

    private void InvokeHandlers<T>(
        Element el,
        Action<T> action
    ) where T : class
    {
        if (el.Widget is T w)
        {
            action(w);
        }

        if (el is StatefulElement se && se.State is T s)
        {
            action(s);
        }
    }

    private void DispatchToHierarchy<T>(
        Element? start,
        Action<T> action,
        bool stopOnHandled = false,
        PointerEvent? pe = null,
        KeyboardEvent? ke = null
    ) where T : class
    {
        var curr = start;
        while (curr != null)
        {
            InvokeHandlers(
                curr,
                action
            );
            if (stopOnHandled && ((pe?.Handled ?? false) || (ke?.Handled ?? false)))
            {
                break;
            }

            curr = curr.Parent;
        }
    }

    private Element? FindTarget(
        HitTestResult result,
        Func<object, bool>? filter = null
    )
    {
        // IPointerMoveHandler is intentionally excluded here. Move events are dispatched
        // to _hoveredElement, which must first be established via one of the interfaces
        // below. A widget that only handles move events must also implement
        // IPointerEnterHandler (or another interface in this list) to receive move events.
        Element? target = null;
        for (var i = 0; i < result.Path.Count; i++)
        {
            var el = result.Path[i].Element;
            var isTarget = filter != null
                ? filter(el.Widget) || (el is StatefulElement se2 && filter(se2.State))
                : IsAnyActiveTarget(el);

            if (isTarget)
            {
                target = el;
                break;
            }
        }

        DebugPainter.IsPointerOverInteractive = target != null;
        return target;
    }

    private static bool IsAnyActiveTarget(
        Element el
    )
    {
        return EventCheckHelper.HandlesAnyPointerEvent(el.Widget) ||
               (el is StatefulElement se &&
                EventCheckHelper.HandlesAnyPointerEvent(se.State));
    }

    private static bool IsActiveTarget<THandler>(
        object source
    ) where THandler : class
    {
        if (source is not THandler)
        {
            return false;
        }

        if (source is ISelectiveEventHandler sel)
        {
            return sel.HandlesEvent(typeof(THandler));
        }

        return true;
    }

    public void DispatchPointerMove(
        Element root,
        PointerEvent e
    )
    {
        DebugPainter.LastPointerPos = new Vector2(
            e.X,
            e.Y
        );

        if (_capturedElement != null)
        {
            _hitTestResult.Clear();
            e.Handled = true;
            DispatchToHierarchy<IPointerMoveHandler>(
                _capturedElement,
                h => h.OnPointerMove(e),
                true,
                e
            );

            // Continue normal enter/exit tracking during capture so
            // that hover-based drag-to-distribute works across slots.
            _dragHitTestResult.Clear();
            root.HitTest(
                _dragHitTestResult,
                new Vector2(
                    e.X,
                    e.Y
                )
            );
            var dragHit = FindTarget(_dragHitTestResult);

            if (dragHit != _dragHoveredElement)
            {
                if (_dragHoveredElement != null)
                {
                    DispatchToHierarchy<IPointerExitHandler>(
                        _dragHoveredElement,
                        h => h.OnPointerExit(e)
                    );
                }

                _dragHoveredElement = dragHit;

                if (_dragHoveredElement != null)
                {
                    DispatchToHierarchy<IPointerEnterHandler>(
                        _dragHoveredElement,
                        h => h.OnPointerEnter(e)
                    );
                }
            }

            return;
        }

        _hitTestResult.Clear();
        root.HitTest(
            _hitTestResult,
            new Vector2(
                e.X,
                e.Y
            )
        );
        if (_hitTestResult.Path.Count > 0)
        {
            e.Handled = true;
        }

        var hit = FindTarget(_hitTestResult);

        if (hit != _hoveredElement)
        {
            if (_hoveredElement != null)
            {
                DispatchToHierarchy<IPointerExitHandler>(
                    _hoveredElement,
                    h => h.OnPointerExit(e)
                );
            }

            _hoveredElement = hit;

            if (_hoveredElement != null)
            {
                DispatchToHierarchy<IPointerEnterHandler>(
                    _hoveredElement,
                    h => h.OnPointerEnter(e)
                );
            }
        }

        if (_hoveredElement != null)
        {
            DispatchToHierarchy<IPointerMoveHandler>(
                _hoveredElement,
                h => h.OnPointerMove(e),
                true,
                e
            );
        }
    }

    /// <summary>
    ///     Returns true when an interactive target was found and captured.
    /// </summary>
    public bool DispatchPointerDown(
        Element root,
        PointerEvent e
    )
    {
        DebugPainter.LastPointerPos = new Vector2(
            e.X,
            e.Y
        );
        _hitTestResult.Clear();
        root.HitTest(
            _hitTestResult,
            new Vector2(
                e.X,
                e.Y
            )
        );
        if (_hitTestResult.Path.Count > 0)
        {
            e.Handled = true;
        }

        // Clear focus whenever the clicked path contains no focusable element.
        var hasFocusTarget = _hitTestResult.Path.Any(entry =>
            entry.Element.Widget is IFocusable ||
            (entry.Element is StatefulElement se2 && se2.State is IFocusable)
        );
        if (!hasFocusTarget)
        {
            root.Owner?.FocusManager?.RequestFocus(null);
        }

        var hit = FindTarget(_hitTestResult);
        if (hit != null)
        {
            _pressedElement = hit;
            _capturedElement = hit;
            DispatchToHierarchy<IPointerDownHandler>(
                hit,
                h => h.OnPointerDown(e),
                true,
                e
            );
            return true;
        }

        return false;
    }

    public void DispatchPointerUp(
        Element root,
        PointerEvent e
    )
    {
        _hitTestResult.Clear();
        root.HitTest(
            _hitTestResult,
            new Vector2(
                e.X,
                e.Y
            )
        );
        if (_hitTestResult.Path.Count > 0)
        {
            e.Handled = true;
        }

        var target = _capturedElement ?? _pressedElement;
        if (target != null)
        {
            DispatchToHierarchy<IPointerUpHandler>(
                target,
                h => h.OnPointerUp(e),
                true,
                e
            );

            var hit = FindTarget(_hitTestResult);
            if (hit == target)
            {
                DispatchToHierarchy<IPointerClickHandler>(
                    target,
                    h => h.OnPointerClick(e),
                    true,
                    e
                );
            }

            _capturedElement = null;
            _pressedElement = null;

            if (_dragHoveredElement != null)
            {
                // Promote drag-hovered element to normal hover so
                // that exit fires naturally on the next move.
                _hoveredElement = _dragHoveredElement;
                _dragHoveredElement = null;
            }
        }
    }

    /// <summary>
    ///     Returns the innermost <see cref="MouseCursor" /> declared by a
    ///     <see cref="MouseRegion" /> in the last pointer-move hit-test path.
    ///     Returns null if no MouseRegion with a cursor was hit.
    ///     Call immediately after <see cref="DispatchPointerMove" />.
    /// </summary>
    public MouseCursor? ResolveHoveredCursor()
    {
        for (var i = 0; i < _hitTestResult.Path.Count; i++)
        {
            var el = _hitTestResult.Path[i].Element;
            if (el.Widget is MouseRegion mr && mr.Cursor != null)
            {
                return mr.Cursor;
            }
        }

        return null;
    }

    public void DispatchMouseWheel(
        Element root,
        PointerEvent e
    )
    {
        _hitTestResult.Clear();
        root.HitTest(
            _hitTestResult,
            new Vector2(
                e.X,
                e.Y
            )
        );
        var anyHit = _hitTestResult.Path.Count > 0;

        var hit = FindTarget(
            _hitTestResult,
            IsWheelTarget
        );
        if (hit != null)
        {
            DispatchToHierarchy<IPointerWheelHandler>(
                hit,
                h => h.OnMouseWheel(e),
                true,
                e
            );
        }

        if (anyHit)
        {
            e.Handled = true;
        }
    }

    public void DispatchKeyDown(
        FocusManager? focusManager,
        KeyboardEvent e
    )
    {
        var focus = focusManager?.PrimaryFocus;
        if (focus?.Owner != null)
        {
            DispatchToHierarchy<IKeyDownHandler>(
                focus.Owner,
                h => h.OnKeyDown(e),
                true,
                ke: e
            );
        }
    }

    public void DispatchKeyUp(
        FocusManager? focusManager,
        KeyboardEvent e
    )
    {
        var focus = focusManager?.PrimaryFocus;
        if (focus?.Owner != null)
        {
            DispatchToHierarchy<IKeyUpHandler>(
                focus.Owner,
                h => h.OnKeyUp(e),
                true,
                ke: e
            );
        }
    }

    public void DispatchKeyChar(
        FocusManager? focusManager,
        KeyboardEvent e
    )
    {
        var focus = focusManager?.PrimaryFocus;
        if (focus?.Owner != null)
        {
            DispatchToHierarchy<IKeyCharHandler>(
                focus.Owner,
                h => h.OnKeyChar(e),
                true,
                ke: e
            );
        }
    }
}

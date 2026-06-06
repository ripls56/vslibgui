using System;
using Gui.Core.Framework;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

public class GestureDetector : SingleChildWidget, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerMoveHandler,
    IPointerWheelHandler, IPointerDragEnterHandler, IPointerDragExitHandler,
    ISelectiveEventHandler
{
    public GestureDetector(
        Widget? child = null,
        Action<PointerEvent>? onTap = null,
        Action<PointerEvent>? onEnter = null,
        Action<PointerEvent>? onExit = null,
        Action<PointerEvent>? onPress = null,
        Action<PointerEvent>? onRelease = null,
        Action<PointerEvent>? onMove = null,
        Action<PointerEvent>? onWheel = null,
        Action<PointerEvent>? onDragEnter = null,
        Action<PointerEvent>? onDragExit = null
    ) : base(child)
    {
        OnTap = onTap;
        OnEnter = onEnter;
        OnExit = onExit;
        OnPress = onPress;
        OnRelease = onRelease;
        OnMove = onMove;
        OnWheel = onWheel;
        OnDragEnter = onDragEnter;
        OnDragExit = onDragExit;
    }

    public Action<PointerEvent>? OnTap { get; }
    public Action<PointerEvent>? OnEnter { get; }
    public Action<PointerEvent>? OnExit { get; }
    public Action<PointerEvent>? OnPress { get; }
    public Action<PointerEvent>? OnRelease { get; }
    public Action<PointerEvent>? OnMove { get; }
    public Action<PointerEvent>? OnWheel { get; }
    public Action<PointerEvent>? OnDragEnter { get; }
    public Action<PointerEvent>? OnDragExit { get; }

    public void OnPointerClick(
        PointerEvent args
    )
    {
        if (OnTap != null)
        {
            OnTap.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerDown(
        PointerEvent args
    )
    {
        if (OnPress != null)
        {
            OnPress.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerDragEnter(
        PointerEvent args
    )
    {
        if (OnDragEnter != null)
        {
            OnDragEnter.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerDragExit(
        PointerEvent args
    )
    {
        if (OnDragExit != null)
        {
            OnDragExit.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerEnter(
        PointerEvent args
    )
    {
        if (OnEnter != null)
        {
            OnEnter.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerExit(
        PointerEvent args
    )
    {
        if (OnExit != null)
        {
            OnExit.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerMove(
        PointerEvent args
    )
    {
        if (OnMove != null)
        {
            OnMove.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnPointerUp(
        PointerEvent args
    )
    {
        if (OnRelease != null)
        {
            OnRelease.Invoke(args);
            args.Handled = true;
        }
    }

    public void OnMouseWheel(
        PointerEvent args
    ) =>
        OnWheel?.Invoke(args);

    public bool HandlesEvent(
        Type eventInterface
    )
    {
        if (eventInterface == typeof(IPointerClickHandler))
        {
            return OnTap != null;
        }

        if (eventInterface == typeof(IPointerDownHandler))
        {
            return OnPress != null;
        }

        if (eventInterface == typeof(IPointerUpHandler))
        {
            return OnRelease != null;
        }

        if (eventInterface == typeof(IPointerEnterHandler))
        {
            return OnEnter != null;
        }

        if (eventInterface == typeof(IPointerExitHandler))
        {
            return OnExit != null;
        }

        if (eventInterface == typeof(IPointerMoveHandler))
        {
            return OnMove != null;
        }

        if (eventInterface == typeof(IPointerWheelHandler))
        {
            return OnWheel != null;
        }

        if (eventInterface == typeof(IPointerDragEnterHandler))
        {
            return OnDragEnter != null;
        }

        if (eventInterface == typeof(IPointerDragExitHandler))
        {
            return OnDragExit != null;
        }

        return false;
    }

    public override RenderObject CreateRenderObject() => new RenderProxyBox();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
    }
}

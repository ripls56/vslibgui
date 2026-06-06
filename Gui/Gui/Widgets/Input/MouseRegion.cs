using System;
using Gui.Core.Framework;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

/// <summary>
///     A widget that detects when the pointer enters, moves within, or exits its bounds,
///     and optionally changes the cursor shape while the pointer is over it.
///     <para>
///         Nested MouseRegions are supported: the innermost MouseRegion with a non-null
///         <see cref="Cursor" /> determines the displayed cursor.
///     </para>
///     <code>
/// new MouseRegion(
///   cursor: MouseCursor.Hand,
///   onEnter: _ => SetState(() => _hovered = true),
///   onExit: _ => SetState(() => _hovered = false),
///   child: new Text("Hover me")
/// )
/// </code>
/// </summary>
public class MouseRegion : SingleChildWidget,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler,
    ISelectiveEventHandler
{
    public MouseRegion(
        Widget? child = null,
        Action<PointerEvent>? onEnter = null,
        Action<PointerEvent>? onExit = null,
        Action<PointerEvent>? onHover = null,
        MouseCursor? cursor = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        OnEnter = onEnter;
        OnExit = onExit;
        OnHover = onHover;
        Cursor = cursor;
    }

    /// <summary>Called when the pointer enters this region's hit area.</summary>
    public Action<PointerEvent>? OnEnter { get; }

    /// <summary>Called when the pointer leaves this region's hit area.</summary>
    public Action<PointerEvent>? OnExit { get; }

    /// <summary>Called on every pointer move while inside this region.</summary>
    public Action<PointerEvent>? OnHover { get; }

    /// <summary>
    ///     Cursor to display while the pointer is inside this region.
    ///     Null means "inherit from parent MouseRegion or default Arrow".
    /// </summary>
    public MouseCursor? Cursor { get; }

    public void OnPointerEnter(
        PointerEvent e
    )
    {
        if (OnEnter != null)
        {
            OnEnter.Invoke(e);
            e.Handled = true;
        }
    }

    public void OnPointerExit(
        PointerEvent e
    )
    {
        if (OnExit != null)
        {
            OnExit.Invoke(e);
            e.Handled = true;
        }
    }

    public void OnPointerMove(
        PointerEvent e
    )
    {
        if (OnHover != null)
        {
            OnHover.Invoke(e);
            e.Handled = true;
        }
    }

    public bool HandlesEvent(
        Type eventInterface
    )
    {
        var isActive = OnEnter != null || OnExit != null
                                       || OnHover != null || Cursor != null;
        if (eventInterface == typeof(IPointerEnterHandler))
        {
            return isActive;
        }

        if (eventInterface == typeof(IPointerExitHandler))
        {
            return isActive;
        }

        if (eventInterface == typeof(IPointerMoveHandler))
        {
            return OnHover != null;
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

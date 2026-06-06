using System;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Provides hover animation and highlight state to inventory slot descendants via the widget
///     tree.
/// </summary>
public class ItemSlotHoverData : InheritedWidget
{
    /// <summary>Creates a hover data provider wrapping <paramref name="child" />.</summary>
    public ItemSlotHoverData(
        CurvedAnimation hoverAnimation,
        Widget child,
        bool isHighlighted = false,
        float punchScale = 1f,
        Action? onPunchEnd = null
    ) : base(child)
    {
        HoverAnimation = hoverAnimation;
        IsHighlighted = isHighlighted;
        PunchScale = punchScale;
        OnPunchEnd = onPunchEnd;
    }

    /// <summary>
    ///     Notifier carrying the pointer position in global (screen) coordinates while the
    ///     slot is hovered. The spotlight border subscribes to it directly, so pointer
    ///     movement repaints the glow without rebuilding the slot widget subtree.
    /// </summary>
    public ValueNotifier<Vector2?>? PointerPosition { get; init; }

    /// <summary>The current hover animation driven by <see cref="ItemSlotGestureLayer" />.</summary>
    public CurvedAnimation HoverAnimation { get; }

    /// <summary>Whether this slot is programmatically highlighted, independent of pointer hover.</summary>
    public bool IsHighlighted { get; }

    /// <summary>Target scale for the item icon punch animation (1.0 = idle).</summary>
    public float PunchScale { get; }

    /// <summary>Called when the punch animation reaches its target, to trigger the return animation.</summary>
    public Action? OnPunchEnd { get; }

    /// <summary>Returns the nearest <see cref="ItemSlotHoverData" /> ancestor, or null.</summary>
    public static ItemSlotHoverData? Of(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<ItemSlotHoverData>();

    /// <inheritdoc />
    public override bool UpdateShouldNotify(InheritedWidget oldWidget) => true;
}

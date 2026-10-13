using System;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Rendering;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;

namespace Gui.Core.Overlay;

/// <summary>
///     A specialized <see cref="RenderFollower" /> that dynamically computes
///     its <see cref="RenderFollower.Offset" /> to flip between the top and
///     bottom of a trigger and clamp itself within the overlay bounds.
/// </summary>
internal class RenderAnchoredOverlay : RenderFollower
{
    public float AnchorX
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public float TriggerGlobalY
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public float TriggerHeight
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public float VerticalGap
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public OverlaySide PreferredSide
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public HorizontalAnchor Horizontal
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        if (Children.Count == 0)
        {
            base.PerformLayout();
            return;
        }

        var child = Children[0];
        child.Layout(Constraints);
        Size = Constraints.Constrain(child.Size);

        var targetPos = ComputeOffset();
        if (ParentData is StackParentData spd)
        {
            spd.Left = targetPos.X + Offset.X;
            spd.Top = targetPos.Y + Offset.Y;
        }
    }

    /// <summary>
    ///     Recomputes placement every frame so flip and clamp follow the live
    ///     trigger through scrolls instead of the layout-time snapshot.
    /// </summary>
    public override void Paint
    (
        PaintingContext context
    )
    {
        ComputeOffset();
        base.Paint(context);
    }

    /// <summary>
    ///     Resolves <see cref="RenderFollower.Offset" /> against the live
    ///     <see cref="LayerLink" /> target when one is present, so the overlay
    ///     tracks the trigger through scrolls. Falls back to the snapshot geometry
    ///     when no target is linked.
    /// </summary>
    private Vector2 ComputeOffset()
    {
        var targetPos = TargetInLocal();
        var target = Link?.Target;

        var childW = Size.X;
        var childH = Size.Y;
        var screenW = Constraints.MaxWidth;
        var screenH = Constraints.MaxHeight;
        var gap = VerticalGap;

        var tY = target != null ? targetPos.Y : TriggerGlobalY;
        var span = target != null ? TargetSpan(target) : Vector2.Zero;
        var tH = target != null ? span.Y : TriggerHeight;
        var aX = target != null
            ? Horizontal == HorizontalAnchor.Center
                ? targetPos.X + span.X * 0.5f
                : targetPos.X
            : AnchorX;

        var side = ResolveSide(tY, tH, screenH, childH, gap);

        var desiredGlobalY = side == OverlaySide.Top ? tY - gap - childH : tY + tH + gap;
        var finalGlobalY = Math.Clamp(desiredGlobalY, 0f, screenH - childH);

        var desiredGlobalX = Horizontal == HorizontalAnchor.Center ? aX - childW * 0.5f : aX;
        var finalGlobalX = Math.Clamp(desiredGlobalX, 0f, screenW - childW);

        Offset = new Vector2(finalGlobalX - targetPos.X, finalGlobalY - targetPos.Y);
        return targetPos;
    }

    private OverlaySide ResolveSide
    (
        float tY,
        float tH,
        float screenH,
        float childH,
        float gap
    )
    {
        var roomAbove = tY;
        var roomBelow = screenH - (tY + tH);
        var fitsAbove = roomAbove - gap - childH >= 0f;
        var fitsBelow = roomBelow - gap - childH >= 0f;

        if (PreferredSide == OverlaySide.Top && !fitsAbove && fitsBelow)
        {
            return OverlaySide.Bottom;
        }
        if (PreferredSide == OverlaySide.Bottom && !fitsBelow && fitsAbove)
        {
            return OverlaySide.Top;
        }
        if (!fitsAbove && !fitsBelow)
        {
            return roomAbove >= roomBelow ? OverlaySide.Top : OverlaySide.Bottom;
        }

        return PreferredSide;
    }
}

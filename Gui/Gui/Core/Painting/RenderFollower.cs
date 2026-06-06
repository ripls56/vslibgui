using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Painting;

/// <summary>
///     Render object that positions itself to follow a <see cref="LayerLink" /> target.
///     Position is recomputed every paint pass so it tracks targets inside scrollables.
/// </summary>
internal class RenderFollower : RenderProxyBox
{
    public override bool IsHitTestTarget => true;

    public LayerLink? Link { get; set; }
    public Vector2 Offset { get; set; }
    public bool ShowAbove { get; set; }

    /// <summary>
    ///     Eagerly writes <see cref="StackParentData" /> with <c>Left=0, Top=0</c>
    ///     so that <see cref="RenderStack" /> identifies this child as positioned
    ///     before the first layout pass.
    /// </summary>
    internal void SyncParentData()
    {
        if (ParentData is not StackParentData)
        {
            ParentData = new StackParentData { Left = 0, Top = 0 };
        }
    }

    /// <summary>
    ///     Computes the target position in the follower's local coordinate
    ///     space, accounting for the difference in window offsets when the
    ///     target lives in a different window.
    /// </summary>
    private Vector2 TargetInLocal()
    {
        var target = Link?.Target;
        if (target == null)
        {
            return Vector2.Zero;
        }

        var pos = target.LocalToGlobal(Vector2.Zero);
        pos += target.GetScreenOffset() - GetScreenOffset();
        return pos;
    }

    public override void Layout(
        LayoutConstraints constraints
    )
    {
        if (ParentData is not StackParentData spd)
        {
            spd = new StackParentData();
            ParentData = spd;
        }

        var targetPos = TargetInLocal();
        spd.Left = targetPos.X + Offset.X;
        spd.Top = targetPos.Y + Offset.Y;

        base.Layout(constraints);

        if (ShowAbove)
        {
            spd.Top = targetPos.Y + Offset.Y - Size.Y;
        }
    }

    public override void Paint(
        PaintingContext context
    )
    {
        var targetPos = TargetInLocal();
        var desiredX = targetPos.X + Offset.X;
        var desiredY = ShowAbove
            ? targetPos.Y + Offset.Y - Size.Y
            : targetPos.Y + Offset.Y;

        var dx = desiredX - X;
        var dy = desiredY - Y;

        if (dx != 0 || dy != 0)
        {
            X = desiredX;
            Y = desiredY;
            context.Canvas?.Translate(
                dx,
                dy
            );
        }

        base.Paint(context);
    }
}

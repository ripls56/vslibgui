using System;
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
    private bool _registeredWithLink;

    /// <summary>
    ///     A follower is a placement wrapper, not a surface: it must not claim a pointer
    ///     its own subtree refused. Claiming it would let an <c>IgnorePointer</c> overlay
    ///     (a hover card, a tooltip) swallow the pointer for whatever it happens to cover —
    ///     and since the overlay stack hit-tests its topmost entry first and stops at the
    ///     first hit, the widget underneath would never see the pointer at all.
    /// </summary>
    public override bool IsHitTestTarget => false;

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
            ParentData = new StackParentData
            {
                Left = 0, Top = 0
            };
        }
    }

    /// <summary>
    ///     Computes the target position in the follower's local coordinate
    ///     space, accounting for the difference in window offsets when the
    ///     target lives in a different window.
    /// </summary>
    protected Vector2 TargetInLocal()
    {
        var link = Link;
        var target = link?.Target;
        if (target == null)
        {
            return Vector2.Zero;
        }

        // Subscribe on first use; the target may mount after the follower.
        if (!_registeredWithLink)
        {
            _registeredWithLink = true;
            link!.AddListener(OnTargetMoved);
        }

        var pos = target.LocalToGlobal(Vector2.Zero);
        pos += target.GetScreenOffset() - GetScreenOffset();
        return pos;
    }

    // The target moved in another window; nudge our own window to repaint.
    private void OnTargetMoved() => MarkNeedsPaint();

    /// <summary>
    ///     The target's extent measured in the follower's own space. A target sitting
    ///     inside a scaled viewport paints larger (or smaller) than its layout
    ///     <see cref="RenderObject.Size" />, and placement happens in follower space — so
    ///     map the box through <see cref="RenderObject.LocalToGlobal" /> instead of reading
    ///     <c>Size</c> directly. Reading it raw makes an overlay anchored below a zoomed
    ///     trigger land on top of that trigger, by exactly the zoom factor.
    /// </summary>
    protected static Vector2 TargetSpan
    (
        RenderObject target
    )
    {
        var origin = target.LocalToGlobal(Vector2.Zero);
        var far = target.LocalToGlobal(target.Size);
        return new Vector2
        (
            Math.Abs(far.X - origin.X),
            Math.Abs(far.Y - origin.Y)
        );
    }

    public override void Dispose()
    {
        if (_registeredWithLink)
        {
            Link?.RemoveListener(OnTargetMoved);
        }

        base.Dispose();
    }

    public override void Layout
    (
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

    public override void Paint
    (
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
            context.Canvas?.Translate
            (
                dx,
                dy
            );
        }

        base.Paint(context);
    }
}

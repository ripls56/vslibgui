using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Overlay;

/// <summary>
///     Render object that horizontally clamps its child so a tooltip stays
///     within the window bounds while keeping its tail anchored to <see cref="AnchorX" />.
/// </summary>
internal class RenderClampedTranslation : RenderProxyBox
{
    public float AnchorX
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    public float FractionalY
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (Children.Count == 0)
        {
            return;
        }

        var child = Children[0];
        var childW = child.Size.X;

        var desiredX = -childW * 0.5f;
        var minX = -AnchorX;
        var maxX = Constraints.MaxWidth - AnchorX - childW;
        child.X = Math.Clamp(
            desiredX,
            minX,
            maxX
        );
        child.Y = child.Size.Y * FractionalY;
    }

    public override bool HitTest(
        HitTestResult result,
        Vector2 position,
        Element element
    )
    {
        if (Children.Count == 0)
        {
            return base.HitTest(
                result,
                position,
                element
            );
        }

        var child = Children[0];
        var offset = new Vector2(
            child.X,
            child.Y
        );
        return base.HitTest(
            result,
            position - offset,
            element
        );
    }
}

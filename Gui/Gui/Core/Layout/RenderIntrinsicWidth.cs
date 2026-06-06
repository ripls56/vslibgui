using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderIntrinsicWidth : RenderProxyBox
{
    protected override void PerformLayout()
    {
        if (Children.Count == 0)
        {
            Size = Constraints.Constrain(Vector2.Zero);
            return;
        }

        var child = Children[0];
        var intrinsicW = child.GetMinIntrinsicWidth(Constraints.MaxHeight);
        intrinsicW = Math.Clamp(
            intrinsicW,
            Constraints.MinWidth,
            Constraints.MaxWidth
        );

        var childConstraints = new LayoutConstraints(
            intrinsicW,
            intrinsicW,
            Constraints.MinHeight,
            Constraints.MaxHeight
        );

        child.X = 0;
        child.Y = 0;
        child.Layout(childConstraints);
        Size = Constraints.Constrain(child.Size);
    }
}

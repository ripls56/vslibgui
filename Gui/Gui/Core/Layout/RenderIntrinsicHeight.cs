using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderIntrinsicHeight : RenderProxyBox
{
    protected override void PerformLayout()
    {
        if (Children.Count == 0)
        {
            Size = Constraints.Constrain(Vector2.Zero);
            return;
        }

        var child = Children[0];
        var intrinsicH = child.GetMinIntrinsicHeight(Constraints.MaxWidth);
        intrinsicH = Math.Clamp(
            intrinsicH,
            Constraints.MinHeight,
            Constraints.MaxHeight
        );

        var childConstraints = new LayoutConstraints(
            Constraints.MinWidth,
            Constraints.MaxWidth,
            intrinsicH,
            intrinsicH
        );

        child.X = 0;
        child.Y = 0;
        child.Layout(childConstraints);
        Size = Constraints.Constrain(child.Size);
    }
}

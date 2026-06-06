using Gui.Core.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderPositionedBox : RenderBox
{
    public Alignment Alignment
    {
        get;
        set => SetProperty(ref field, value, relayout: true);
    } = Alignment.Center;

    protected override void PerformLayout()
    {
        var childSize = Vector2.Zero;
        if (Children.Count > 0)
        {
            var child = Children[0];
            child.Layout(Constraints.Loosen());
            childSize = child.Size;
        }

        var width = Constraints.MaxWidth;
        if (float.IsPositiveInfinity(width))
        {
            width = childSize.X;
        }

        var height = Constraints.MaxHeight;
        if (float.IsPositiveInfinity(height))
        {
            height = childSize.Y;
        }

        Size = Constraints.Constrain(
            new Vector2(
                width,
                height
            )
        );

        if (Children.Count > 0)
        {
            var child = Children[0];
            var offset = Alignment.CalculateOffset(
                Size,
                child.Size
            );
            child.X = offset.X;
            child.Y = offset.Y;
        }
    }
}

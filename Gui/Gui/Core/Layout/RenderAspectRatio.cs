using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderAspectRatio : RenderProxyBox
{
    private float _aspectRatio;

    public float AspectRatio
    {
        get => _aspectRatio;
        set => SetProperty(
            ref _aspectRatio,
            value,
            relayout: true
        );
    }

    protected override void PerformLayout()
    {
        Size = ApplyAspectRatio(Constraints);

        if (Children.Count > 0)
        {
            var child = Children[0];
            child.X = 0;
            child.Y = 0;
            child.Layout(LayoutConstraints.Tight(Size));
        }
    }

    private Vector2 ApplyAspectRatio(
        LayoutConstraints constraints
    )
    {
        if (constraints.IsTight)
        {
            return new Vector2(
                constraints.MinWidth,
                constraints.MinHeight
            );
        }

        var width = constraints.MaxWidth;
        float height;

        if (!float.IsInfinity(width))
        {
            height = width / _aspectRatio;
        }
        else
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width > constraints.MaxWidth)
        {
            width = constraints.MaxWidth;
            height = width / _aspectRatio;
        }

        if (height > constraints.MaxHeight)
        {
            height = constraints.MaxHeight;
            width = height * _aspectRatio;
        }

        if (width < constraints.MinWidth)
        {
            width = constraints.MinWidth;
            height = width / _aspectRatio;
        }

        if (height < constraints.MinHeight)
        {
            height = constraints.MinHeight;
            width = height * _aspectRatio;
        }

        return constraints.Constrain(
            new Vector2(
                width,
                height
            )
        );
    }
}

using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderPadding : RenderBox
{
    private EdgeInsets _padding;

    public EdgeInsets Padding
    {
        get => _padding;
        set => SetProperty(
            ref _padding,
            value,
            relayout: true
        );
    }

    protected override void PerformLayout()
    {
        var left = _padding.Left;
        var top = _padding.Top;
        var right = _padding.Right;
        var bottom = _padding.Bottom;

        if (Children.Count > 0)
        {
            var child = Children[0];

            // Calculate child constraints by subtracting padding
            var horizontalPadding = left + right;
            var verticalPadding = top + bottom;

            var childConstraints = new LayoutConstraints(
                Math.Max(
                    0,
                    Constraints.MinWidth - horizontalPadding
                ),
                Math.Max(
                    0,
                    Constraints.MaxWidth - horizontalPadding
                ),
                Math.Max(
                    0,
                    Constraints.MinHeight - verticalPadding
                ),
                Math.Max(
                    0,
                    Constraints.MaxHeight - verticalPadding
                )
            );

            child.Layout(childConstraints);
            child.X = left;
            child.Y = top;

            Size = Constraints.Constrain(
                new Vector2(
                    child.Size.X + horizontalPadding,
                    child.Size.Y + verticalPadding
                )
            );
        }
        else
        {
            Size = Constraints.Constrain(
                new Vector2(
                    left + right,
                    top + bottom
                )
            );
        }
    }

    public override Vector2 GlobalToChild(
        RenderObject child,
        Vector2 position
    )
    {
        return position - new Vector2(
            _padding.Left,
            _padding.Top
        );
    }

    public override float GetMinIntrinsicWidth(
        float height
    )
    {
        var hp = _padding.Left + _padding.Right;
        var vp = _padding.Top + _padding.Bottom;
        return hp + (Children.Count > 0
            ? Children[0]
                .GetMinIntrinsicWidth(
                    Math.Max(
                        0,
                        height - vp
                    )
                )
            : 0f);
    }

    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        var hp = _padding.Left + _padding.Right;
        var vp = _padding.Top + _padding.Bottom;
        return hp + (Children.Count > 0
            ? Children[0]
                .GetMaxIntrinsicWidth(
                    Math.Max(
                        0,
                        height - vp
                    )
                )
            : 0f);
    }

    public override float GetMinIntrinsicHeight(
        float width
    )
    {
        var hp = _padding.Left + _padding.Right;
        var vp = _padding.Top + _padding.Bottom;
        return vp + (Children.Count > 0
            ? Children[0]
                .GetMinIntrinsicHeight(
                    Math.Max(
                        0,
                        width - hp
                    )
                )
            : 0f);
    }

    public override float GetMaxIntrinsicHeight(
        float width
    )
    {
        var hp = _padding.Left + _padding.Right;
        var vp = _padding.Top + _padding.Bottom;
        return vp + (Children.Count > 0
            ? Children[0]
                .GetMaxIntrinsicHeight(
                    Math.Max(
                        0,
                        width - hp
                    )
                )
            : 0f);
    }
}

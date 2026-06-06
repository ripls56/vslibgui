using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public class RenderStack : RenderObject
{
    public override bool IsHitTestTarget => false;

    protected override void PerformLayout()
    {
        float maxW = 0;
        float maxH = 0;
        var hasUnpositioned = false;

        var looseConstraints = LayoutConstraints.Loose(
            Constraints.MaxWidth,
            Constraints.MaxHeight
        );

        foreach (var child in Children)
        {
            if (child.ParentData is StackParentData pd && pd.IsPositioned)
                // Positioned children are handled after the stack size is determined
            {
                continue;
            }

            hasUnpositioned = true;
            child.X = 0;
            child.Y = 0;
            child.Layout(looseConstraints);
            maxW = Math.Max(
                maxW,
                child.Size.X
            );
            maxH = Math.Max(
                maxH,
                child.Size.Y
            );
        }

        // When all children are Positioned, expand to fill available space so that
        // hit-testing and Right/Bottom positioning work correctly
        if (!hasUnpositioned)
        {
            maxW = Constraints.MaxWidth;
            maxH = Constraints.MaxHeight;
        }

        Size = Constraints.Constrain(
            new Vector2(
                maxW,
                maxH
            )
        );

        // Now position the positioned children
        foreach (var child in Children)
        {
            if (child.ParentData is StackParentData pd && pd.IsPositioned)
            {
                float minW = 0;
                var maxWChild = Size.X;
                float minH = 0;
                var maxHChild = Size.Y;

                if (pd.Left.HasValue && pd.Right.HasValue)
                {
                    minW = maxWChild = Size.X - pd.Left.Value - pd.Right.Value;
                }
                else if (pd.Width.HasValue)
                {
                    minW = maxWChild = pd.Width.Value;
                }

                if (pd.Top.HasValue && pd.Bottom.HasValue)
                {
                    minH = maxHChild = Size.Y - pd.Top.Value - pd.Bottom.Value;
                }
                else if (pd.Height.HasValue)
                {
                    minH = maxHChild = pd.Height.Value;
                }

                child.Layout(
                    new LayoutConstraints(
                        minW,
                        maxWChild,
                        minH,
                        maxHChild
                    )
                );

                if (pd.Left.HasValue)
                {
                    child.X = pd.Left.Value;
                }
                else if (pd.Right.HasValue)
                {
                    child.X = Size.X - pd.Right.Value - child.Size.X;
                }
                else
                {
                    child.X = 0;
                }

                if (pd.Top.HasValue)
                {
                    child.Y = pd.Top.Value;
                }
                else if (pd.Bottom.HasValue)
                {
                    child.Y = Size.Y - pd.Bottom.Value - child.Size.Y;
                }
                else
                {
                    child.Y = 0;
                }
            }
        }
    }
}

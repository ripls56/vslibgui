using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

public enum FlexDirection
{
    Horizontal,
    Vertical
}

public enum MainAxisSize
{
    /// <summary>Column/Row sizes to its content, like a min-intrinsic wrap.</summary>
    Min,

    /// <summary>Column/Row fills all available main-axis space (default).</summary>
    Max
}

public class RenderFlex : RenderBox
{
    public FlexDirection Direction
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    }

    public float Spacing
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    }

    public MainAxisAlignment MainAxisAlignment
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    } = MainAxisAlignment.Start;

    public CrossAxisAlignment CrossAxisAlignment
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    } = CrossAxisAlignment.Center;

    /// <summary>
    ///     Controls whether the flex container expands to fill available main-axis
    ///     space (<see cref="MainAxisSize.Max" />) or shrinks to its content
    ///     (<see cref="MainAxisSize.Min" />).
    /// </summary>
    public MainAxisSize MainAxisSize
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    } = MainAxisSize.Max;

    private int GetFlex(
        RenderObject child
    )
    {
        if (child.ParentData is FlexParentData flexData)
        {
            return flexData.Flex;
        }

        return 0;
    }

    private FlexFit GetFlexFit(
        RenderObject child
    )
    {
        if (child.ParentData is FlexParentData flexData)
        {
            return flexData.Fit;
        }

        return FlexFit.Tight;
    }

    protected override void PerformLayout()
    {
        var isHorizontal = Direction == FlexDirection.Horizontal;
        float totalFlex = 0;
        float totalFixedMainSize = 0;
        float maxCrossSize = 0;

        // 1. Identifying and laying out fixed children
        foreach (var child in Children)
        {
            var flex = GetFlex(child);
            if (flex > 0)
            {
                totalFlex += flex;
            }
            else
            {
                maxCrossSize = LayoutFixedChild(
                    child,
                    isHorizontal,
                    ref totalFixedMainSize,
                    maxCrossSize
                );
            }
        }

        var totalSpacing = Spacing * Math.Max(
            0,
            Children.Count - 1
        );
        var totalMainSizeUsed = totalFixedMainSize + totalSpacing;
        var availableMainSize = isHorizontal
            ? Constraints.MaxWidth
            : Constraints.MaxHeight;
        var remainingMainSize = Math.Max(
            0,
            availableMainSize - totalMainSizeUsed
        );

        // 2. Layout flexible children
        if (totalFlex > 0)
        {
            if (float.IsPositiveInfinity(availableMainSize))
            {
                throw new InvalidOperationException(
                    $"RenderFlex children have non-zero flex but incoming {(isHorizontal ? "width" : "height")} constraints are unbounded.");
            }

            var actualFlexMainSize = LayoutFlexibleChildren(
                isHorizontal,
                totalFlex,
                remainingMainSize,
                ref maxCrossSize
            );
            totalMainSizeUsed += actualFlexMainSize;
        }

        var containerMainSize = MainAxisSize == MainAxisSize.Max
                                && !float.IsPositiveInfinity(availableMainSize)
            ? availableMainSize
            : totalMainSizeUsed;
        var finalSize = isHorizontal
            ? new Vector2(
                containerMainSize,
                maxCrossSize
            )
            : new Vector2(
                maxCrossSize,
                containerMainSize
            );

        Size = Constraints.Constrain(finalSize);

        // Check for overflow
        if (totalMainSizeUsed > availableMainSize + 0.001f &&
            !float.IsPositiveInfinity(availableMainSize))
        {
            ReportLayoutViolation(
                $"RenderFlex overflowed by {totalMainSizeUsed - availableMainSize:F1}px " +
                $"on the {Direction} axis. " +
                $"Available: {availableMainSize:F1}px, Used: {totalMainSizeUsed:F1}px. " +
                $"Consider using Expanded, Flexible, or wrapping in a SingleChildScrollView."
            );
        }

        var actualRemainingMainSpace = (isHorizontal
            ? Size.X
            : Size.Y) - totalMainSizeUsed;
        if (actualRemainingMainSpace < 0)
        {
            actualRemainingMainSpace = 0;
        }

        // 3. Position children
        PositionChildren(
            isHorizontal,
            actualRemainingMainSpace
        );
    }

    private float LayoutFixedChild(
        RenderObject child,
        bool isHorizontal,
        ref float accumulatedMainSize,
        float maxCrossSize
    )
    {
        LayoutConstraints childConstraints;
        if (isHorizontal)
        {
            var minCross = CrossAxisAlignment == CrossAxisAlignment.Stretch
                ? Constraints.MaxHeight
                : 0;
            if (float.IsInfinity(minCross))
            {
                minCross = 0;
            }

            childConstraints = new LayoutConstraints(
                0,
                Constraints.MaxWidth,
                minCross,
                Constraints.MaxHeight
            );
        }
        else
        {
            var minCross = CrossAxisAlignment == CrossAxisAlignment.Stretch
                ? Constraints.MaxWidth
                : 0;
            if (float.IsInfinity(minCross))
            {
                minCross = 0;
            }

            childConstraints = new LayoutConstraints(
                minCross,
                Constraints.MaxWidth,
                0,
                Constraints.MaxHeight
            );
        }

        child.Layout(childConstraints);
        accumulatedMainSize += isHorizontal
            ? child.Size.X
            : child.Size.Y;
        return Math.Max(
            maxCrossSize,
            isHorizontal
                ? child.Size.Y
                : child.Size.X
        );
    }

    // Returns the actual main-axis size consumed by all flex children combined.
    // maxCrossSize is updated in place.
    private float LayoutFlexibleChildren(
        bool isHorizontal,
        float totalFlex,
        float remainingMainSize,
        ref float maxCrossSize
    )
    {
        float actualFlexMainSize = 0;
        foreach (var child in Children)
        {
            var flex = GetFlex(child);
            if (flex <= 0)
            {
                continue;
            }

            var flexPixels = flex / totalFlex * remainingMainSize;
            var fit = GetFlexFit(child);
            LayoutConstraints childConstraints;

            if (isHorizontal)
            {
                if (fit == FlexFit.Tight)
                {
                    childConstraints = CrossAxisAlignment == CrossAxisAlignment.Stretch
                        ? LayoutConstraints.Tight(
                            flexPixels,
                            Constraints.MaxHeight
                        )
                        : LayoutConstraints
                            .TightFor(flexPixels)
                            .Enforce(
                                LayoutConstraints.Loose(
                                    flexPixels,
                                    Constraints.MaxHeight
                                )
                            );
                }
                else
                {
                    var crossMin = CrossAxisAlignment == CrossAxisAlignment.Stretch
                        ? Constraints.MaxHeight
                        : 0;
                    if (float.IsInfinity(crossMin))
                    {
                        crossMin = 0;
                    }

                    childConstraints = new LayoutConstraints(
                        0,
                        flexPixels,
                        crossMin,
                        Constraints.MaxHeight
                    );
                }
            }
            else
            {
                if (fit == FlexFit.Tight)
                {
                    childConstraints = CrossAxisAlignment == CrossAxisAlignment.Stretch
                        ? LayoutConstraints.Tight(
                            Constraints.MaxWidth,
                            flexPixels
                        )
                        : LayoutConstraints
                            .TightFor(height: flexPixels)
                            .Enforce(
                                LayoutConstraints.Loose(
                                    Constraints.MaxWidth,
                                    flexPixels
                                )
                            );
                }
                else
                {
                    var crossMin = CrossAxisAlignment == CrossAxisAlignment.Stretch
                        ? Constraints.MaxWidth
                        : 0;
                    if (float.IsInfinity(crossMin))
                    {
                        crossMin = 0;
                    }

                    childConstraints = new LayoutConstraints(
                        crossMin,
                        Constraints.MaxWidth,
                        0,
                        flexPixels
                    );
                }
            }

            child.Layout(childConstraints);
            actualFlexMainSize += isHorizontal
                ? child.Size.X
                : child.Size.Y;
            maxCrossSize = Math.Max(
                maxCrossSize,
                isHorizontal
                    ? child.Size.Y
                    : child.Size.X
            );
        }

        return actualFlexMainSize;
    }

    public override float GetMinIntrinsicWidth(
        float height
    )
    {
        if (Direction == FlexDirection.Horizontal)
        {
            var total = Spacing * Math.Max(
                0,
                Children.Count - 1
            );
            foreach (var child in Children)
            {
                total += child.GetMinIntrinsicWidth(height);
            }

            return total;
        }

        float max = 0;
        foreach (var child in Children)
        {
            max = Math.Max(
                max,
                child.GetMinIntrinsicWidth(height)
            );
        }

        return max;
    }

    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        if (Direction == FlexDirection.Horizontal)
        {
            var total = Spacing * Math.Max(
                0,
                Children.Count - 1
            );
            foreach (var child in Children)
            {
                total += child.GetMaxIntrinsicWidth(height);
            }

            return total;
        }

        float max = 0;
        foreach (var child in Children)
        {
            max = Math.Max(
                max,
                child.GetMaxIntrinsicWidth(height)
            );
        }

        return max;
    }

    public override float GetMinIntrinsicHeight(
        float width
    )
    {
        if (Direction == FlexDirection.Vertical)
        {
            var total = Spacing * Math.Max(
                0,
                Children.Count - 1
            );
            foreach (var child in Children)
            {
                total += child.GetMinIntrinsicHeight(width);
            }

            return total;
        }

        float max = 0;
        foreach (var child in Children)
        {
            max = Math.Max(
                max,
                child.GetMinIntrinsicHeight(width)
            );
        }

        return max;
    }

    public override float GetMaxIntrinsicHeight(
        float width
    ) =>
        GetMinIntrinsicHeight(width);

    private void PositionChildren(
        bool isHorizontal,
        float actualRemainingMainSpace
    )
    {
        float currentPos = 0;
        var gap = Spacing;

        switch (MainAxisAlignment)
        {
            case MainAxisAlignment.Start: currentPos = 0; break;
            case MainAxisAlignment.End: currentPos = actualRemainingMainSpace; break;
            case MainAxisAlignment.Center: currentPos = actualRemainingMainSpace / 2f; break;
            case MainAxisAlignment.SpaceBetween:
                currentPos = 0;
                if (Children.Count > 1)
                {
                    gap = Spacing + actualRemainingMainSpace / (Children.Count - 1);
                }

                break;
            case MainAxisAlignment.SpaceAround:
                var perItemGap = actualRemainingMainSpace / Children.Count;
                currentPos = perItemGap / 2f;
                gap = Spacing + perItemGap;
                break;
            case MainAxisAlignment.SpaceEvenly:
                var evenlyGap = actualRemainingMainSpace / (Children.Count + 1);
                currentPos = evenlyGap;
                gap = Spacing + evenlyGap;
                break;
        }

        foreach (var child in Children)
        {
            var freeCrossSpace = (isHorizontal
                                     ? Size.Y
                                     : Size.X) -
                                 (isHorizontal
                                     ? child.Size.Y
                                     : child.Size.X);
            var crossOffset = CrossAxisAlignment switch
            {
                CrossAxisAlignment.End => freeCrossSpace,
                CrossAxisAlignment.Center => freeCrossSpace / 2f,
                _ => 0
            };

            if (isHorizontal)
            {
                child.X = currentPos;
                child.Y = crossOffset;
                currentPos += child.Size.X + gap;
            }
            else
            {
                child.X = crossOffset;
                child.Y = currentPos;
                currentPos += child.Size.Y + gap;
            }
        }
    }
}

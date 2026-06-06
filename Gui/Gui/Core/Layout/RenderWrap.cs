using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

/// <summary>
///     A render object that lays out its children in runs, wrapping to the next
///     line when a child would overflow the available main-axis space.
/// </summary>
public class RenderWrap : RenderBox
{
    private CrossAxisAlignment _crossAxisAlignment = CrossAxisAlignment.Start;
    private MainAxisAlignment _mainAxisAlignment = MainAxisAlignment.Start;
    private MainAxisAlignment _runAlignment = MainAxisAlignment.Start;
    private float _runSpacing;
    private float _spacing;

    /// <summary>Primary axis direction. Default: Horizontal.</summary>
    public FlexDirection Direction
    {
        get;
        set => SetProperty(
            ref field,
            value,
            relayout: true
        );
    }

    /// <summary>Spacing between children on the main axis within a run.</summary>
    public float Spacing
    {
        get => _spacing;
        set => SetProperty(
            ref _spacing,
            value,
            relayout: true
        );
    }

    /// <summary>Spacing between runs on the cross axis.</summary>
    public float RunSpacing
    {
        get => _runSpacing;
        set => SetProperty(
            ref _runSpacing,
            value,
            relayout: true
        );
    }

    /// <summary>How children are aligned on the main axis within each run.</summary>
    public MainAxisAlignment MainAxisAlignment
    {
        get => _mainAxisAlignment;
        set => SetProperty(
            ref _mainAxisAlignment,
            value,
            relayout: true
        );
    }

    /// <summary>How children are aligned on the cross axis within each run.</summary>
    public CrossAxisAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set => SetProperty(
            ref _crossAxisAlignment,
            value,
            relayout: true
        );
    }

    /// <summary>How runs are aligned on the cross axis of the container.</summary>
    public MainAxisAlignment RunAlignment
    {
        get => _runAlignment;
        set => SetProperty(
            ref _runAlignment,
            value,
            relayout: true
        );
    }

    protected override void PerformLayout()
    {
        if (Children.Count == 0)
        {
            Size = Constraints.Constrain(Vector2.Zero);
            return;
        }

        var isHorizontal = Direction == FlexDirection.Horizontal;
        var maxMainAxis = isHorizontal
            ? Constraints.MaxWidth
            : Constraints.MaxHeight;
        if (float.IsPositiveInfinity(maxMainAxis))
        {
            maxMainAxis = float.MaxValue;
        }

        // 1. Measure children with loose constraints
        var childConstraints = new LayoutConstraints(
            0,
            Constraints.MaxWidth,
            0,
            Constraints.MaxHeight
        );

        foreach (var child in Children)
        {
            child.Layout(childConstraints);
        }

        // 2. Build runs
        var runs = new List<RunMetrics>();
        float runMainExtent = 0;
        float runCrossExtent = 0;
        var runStart = 0;
        var runCount = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childMain = isHorizontal
                ? child.Size.X
                : child.Size.Y;
            var childCross = isHorizontal
                ? child.Size.Y
                : child.Size.X;
            var spacingNeeded = runCount > 0
                ? _spacing
                : 0;

            if (runCount > 0 && runMainExtent + spacingNeeded + childMain > maxMainAxis + 0.001f)
            {
                // Finalize current run
                runs.Add(
                    new RunMetrics
                    {
                        StartIndex = runStart,
                        Count = runCount,
                        MainAxisExtent = runMainExtent,
                        CrossAxisExtent = runCrossExtent
                    }
                );
                runStart = i;
                runCount = 1;
                runMainExtent = childMain;
                runCrossExtent = childCross;
            }
            else
            {
                runMainExtent += spacingNeeded + childMain;
                runCrossExtent = Math.Max(
                    runCrossExtent,
                    childCross
                );
                runCount++;
            }
        }

        // Final run
        if (runCount > 0)
        {
            runs.Add(
                new RunMetrics
                {
                    StartIndex = runStart,
                    Count = runCount,
                    MainAxisExtent = runMainExtent,
                    CrossAxisExtent = runCrossExtent
                }
            );
        }

        // 3. Compute total cross extent
        float totalCrossExtent = 0;
        for (var r = 0; r < runs.Count; r++)
        {
            totalCrossExtent += runs[r].CrossAxisExtent;
            if (r < runs.Count - 1)
            {
                totalCrossExtent += _runSpacing;
            }
        }

        // 4. Compute container size
        float maxRunMain = 0;
        foreach (var run in runs)
        {
            maxRunMain = Math.Max(
                maxRunMain,
                run.MainAxisExtent
            );
        }

        var containerSize = isHorizontal
            ? new Vector2(
                maxRunMain,
                totalCrossExtent
            )
            : new Vector2(
                totalCrossExtent,
                maxRunMain
            );
        Size = Constraints.Constrain(containerSize);

        // 5. Position runs (cross axis)
        var containerCross = isHorizontal
            ? Size.Y
            : Size.X;
        var freeCrossSpace = Math.Max(
            0,
            containerCross - totalCrossExtent
        );
        float crossStart = 0;
        var runGap = _runSpacing;

        switch (_runAlignment)
        {
            case MainAxisAlignment.Start:
                crossStart = 0;
                break;
            case MainAxisAlignment.End:
                crossStart = freeCrossSpace;
                break;
            case MainAxisAlignment.Center:
                crossStart = freeCrossSpace / 2f;
                break;
            case MainAxisAlignment.SpaceBetween:
                crossStart = 0;
                if (runs.Count > 1)
                {
                    runGap = _runSpacing + freeCrossSpace / (runs.Count - 1);
                }

                break;
            case MainAxisAlignment.SpaceAround:
                var perRunGap = runs.Count > 0
                    ? freeCrossSpace / runs.Count
                    : 0;
                crossStart = perRunGap / 2f;
                runGap = _runSpacing + perRunGap;
                break;
            case MainAxisAlignment.SpaceEvenly:
                var evenGap = freeCrossSpace / (runs.Count + 1);
                crossStart = evenGap;
                runGap = _runSpacing + evenGap;
                break;
        }

        // 6. Position children within each run
        var crossPos = crossStart;
        var containerMain = isHorizontal
            ? Size.X
            : Size.Y;

        foreach (var run in runs)
        {
            var freeMainSpace = Math.Max(
                0,
                containerMain - run.MainAxisExtent
            );
            float mainPos = 0;
            var gap = _spacing;

            switch (_mainAxisAlignment)
            {
                case MainAxisAlignment.Start:
                    mainPos = 0;
                    break;
                case MainAxisAlignment.End:
                    mainPos = freeMainSpace;
                    break;
                case MainAxisAlignment.Center:
                    mainPos = freeMainSpace / 2f;
                    break;
                case MainAxisAlignment.SpaceBetween:
                    mainPos = 0;
                    if (run.Count > 1)
                    {
                        gap = _spacing + freeMainSpace / (run.Count - 1);
                    }

                    break;
                case MainAxisAlignment.SpaceAround:
                    var perItem = run.Count > 0
                        ? freeMainSpace / run.Count
                        : 0;
                    mainPos = perItem / 2f;
                    gap = _spacing + perItem;
                    break;
                case MainAxisAlignment.SpaceEvenly:
                    var evenItem = freeMainSpace / (run.Count + 1);
                    mainPos = evenItem;
                    gap = _spacing + evenItem;
                    break;
            }

            for (var i = run.StartIndex; i < run.StartIndex + run.Count; i++)
            {
                var child = Children[i];
                var childCross = isHorizontal
                    ? child.Size.Y
                    : child.Size.X;

                var crossOffset = _crossAxisAlignment switch
                {
                    CrossAxisAlignment.End => run.CrossAxisExtent - childCross,
                    CrossAxisAlignment.Center => (run.CrossAxisExtent - childCross) / 2f,
                    CrossAxisAlignment.Stretch => 0,
                    _ => 0 // Start
                };

                if (isHorizontal)
                {
                    child.X = mainPos;
                    child.Y = crossPos + crossOffset;
                    mainPos += child.Size.X + gap;
                }
                else
                {
                    child.X = crossPos + crossOffset;
                    child.Y = mainPos;
                    mainPos += child.Size.Y + gap;
                }
            }

            crossPos += run.CrossAxisExtent + runGap;
        }
    }

    /// <inheritdoc />
    public override float GetMinIntrinsicWidth(
        float height
    )
    {
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

    /// <inheritdoc />
    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        if (Direction == FlexDirection.Horizontal)
        {
            var total = _spacing * Math.Max(
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

    /// <inheritdoc />
    public override float GetMinIntrinsicHeight(
        float width
    )
    {
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

    /// <inheritdoc />
    public override float GetMaxIntrinsicHeight(
        float width
    )
    {
        if (Direction == FlexDirection.Vertical)
        {
            var total = _spacing * Math.Max(
                0,
                Children.Count - 1
            );
            foreach (var child in Children)
            {
                total += child.GetMaxIntrinsicHeight(width);
            }

            return total;
        }

        float max = 0;
        foreach (var child in Children)
        {
            max = Math.Max(
                max,
                child.GetMaxIntrinsicHeight(width)
            );
        }

        return max;
    }

    private struct RunMetrics
    {
        public int StartIndex;
        public int Count;
        public float MainAxisExtent;
        public float CrossAxisExtent;
    }
}

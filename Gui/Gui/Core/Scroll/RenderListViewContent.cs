using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Scroll;

/// <summary>
///     Render object that lays out visible list items and reports scroll metrics.
///     Supports both fixed-height and variable-height (measured) item modes.
/// </summary>
internal class RenderListViewContent : RenderObject
{
    private readonly Action<float, float> _onLayout;

    public RenderListViewContent(
        Action<float, float> onLayout
    )
    {
        _onLayout = onLayout;
    }

    public float TotalHeight { get; set; }
    public float FixedItemHeight { get; set; }
    public float ViewportHeight { get; set; }
    public bool IsVariableHeight { get; set; }
    public ItemHeightCache? HeightCache { get; set; }
    public float ScrollOffset { get; set; }
    public Action<float>? OnOffsetCorrection { get; set; }
    public Axis ScrollDirection { get; set; }
    public Dictionary<RenderObject, int> ChildIndexMap { get; } = new();

    public override bool IsHitTestTarget => true;

    protected override void PerformLayout()
    {
        ViewportHeight = ScrollDirection switch
        {
            Axis.Horizontal => Parent?.Size.X ?? 0,
            _ => Parent?.Size.Y ?? 0
        };

        if (IsVariableHeight && HeightCache != null)
        {
            PerformVariableLayout();
        }
        else
        {
            PerformFixedLayout();
        }

        _onLayout(
            ViewportHeight,
            TotalHeight
        );
    }

    private void PerformFixedLayout()
    {
        Size = ScrollDirection switch
        {
            Axis.Horizontal => new Vector2(TotalHeight, Constraints.MaxHeight),
            _ => new Vector2(Constraints.MaxWidth, TotalHeight)
        };
        var itemConstraints = ScrollDirection switch
        {
            Axis.Horizontal => LayoutConstraints.Tight(FixedItemHeight, Constraints.MaxHeight),
            _ => LayoutConstraints.Tight(Constraints.MaxWidth, FixedItemHeight)
        };
        foreach (var child in Children)
        {
            child.Layout(itemConstraints);
        }
    }

    private void PerformVariableLayout()
    {
        var crossSize = ScrollDirection switch
        {
            Axis.Horizontal => Constraints.MaxHeight,
            _ => Constraints.MaxWidth
        };
        var itemConstraints = ScrollDirection switch
        {
            Axis.Horizontal => LayoutConstraints.TightFor(height: crossSize),
            _ => LayoutConstraints.TightFor(crossSize)
        };
        float correctionAmount = 0;

        foreach (var child in Children)
        {
            child.Layout(itemConstraints);

            if (!ChildIndexMap.TryGetValue(
                    child,
                    out var index
                ))
            {
                continue;
            }

            var oldPos = HeightCache!.GetPosition(index);
            var wasAboveViewport = oldPos + HeightCache.GetHeight(index) <= ScrollOffset;

            var measuredSize = ScrollDirection switch
            {
                Axis.Horizontal => child.Size.X,
                _ => child.Size.Y
            };
            var delta = HeightCache.SetMeasured(index, measuredSize);

            if (delta != 0 && wasAboveViewport)
            {
                correctionAmount += delta;
            }
        }

        TotalHeight = HeightCache!.TotalHeight;
        Size = ScrollDirection switch
        {
            Axis.Horizontal => new Vector2(TotalHeight, crossSize),
            _ => new Vector2(crossSize, TotalHeight)
        };

        foreach (var child in Children)
        {
            if (!ChildIndexMap.TryGetValue(child, out var index))
            {
                continue;
            }

            SetMainAxisPosition(child, HeightCache.GetPosition(index));
        }

        if (Math.Abs(correctionAmount) > 0.5f)
        {
            OnOffsetCorrection?.Invoke(correctionAmount);
        }
    }

    private void SetMainAxisPosition(RenderObject child, float pos)
    {
        _ = ScrollDirection switch
        {
            Axis.Horizontal => child.X = pos,
            _ => child.Y = pos
        };
    }
}

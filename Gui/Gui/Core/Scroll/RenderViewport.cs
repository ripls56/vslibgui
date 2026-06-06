using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Scroll;

public class RenderViewport : RenderProxyBox
{
    private Vector2 _offset = Vector2.Zero;

    public Vector2 Offset
    {
        get => _offset;
        set => SetProperty(
            ref _offset,
            value,
            true
        );
    }

    /// <summary>The axis along which this viewport scrolls.</summary>
    public Axis ScrollDirection { get; set; }

    protected override void PerformLayout()
    {
        var width = float.IsPositiveInfinity(Constraints.MaxWidth)
            ? Constraints.MinWidth
            : Constraints.MaxWidth;
        var height = float.IsPositiveInfinity(Constraints.MaxHeight)
            ? Constraints.MinHeight
            : Constraints.MaxHeight;

        Size = Constraints.Constrain(
            new Vector2(
                width,
                height
            )
        );

        if (Children.Count > 0)
        {
            var child = Children[0];
            // Constrain cross axis tightly; allow scroll axis to grow freely.
            // We use float.MaxValue instead of PositiveInfinity because downstream
            // layout helpers (e.g. Biggest) fall back to Min for infinite max,
            // which would produce zero rather than the intended unconstrained size.
            child.Layout(ScrollDirection switch
            {
                Axis.Horizontal => new LayoutConstraints(0, float.MaxValue, Size.Y, Size.Y),
                _ => new LayoutConstraints(Size.X, Size.X, 0, float.MaxValue)
            });
        }
    }

    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        context.Canvas.Save();
        // Clip to the viewport's bounds
        context.Canvas.ClipRect(
            new SKRect(
                0,
                0,
                Size.X,
                Size.Y
            )
        );

        // Translate by negative offset
        context.Canvas.Translate(
            -_offset.X,
            -_offset.Y
        );

        base.Paint(context);

        context.Canvas.Restore();
    }

    public override Vector2 GlobalToChild(
        RenderObject child,
        Vector2 position
    )
    {
        return position + _offset - new Vector2(
            child.X,
            child.Y
        );
    }

    public override Vector2 LocalToGlobal(
        Vector2 localPoint
    )
    {
        // Children are visually shifted by -_offset due to scroll,
        // so subtract the scroll offset when converting to global coords.
        var point = localPoint + new Vector2(
            X,
            Y
        ) - _offset;
        return Parent?.LocalToGlobal(point) ?? point;
    }

    public override Vector2 GlobalToLocal(
        Vector2 globalPoint
    )
    {
        var parentLocal = Parent?.GlobalToLocal(globalPoint) ?? globalPoint;
        // Reverse the scroll adjustment: add _offset back.
        return parentLocal - new Vector2(
            X,
            Y
        ) + _offset;
    }

    public override bool HitTest(
        HitTestResult result,
        Vector2 position,
        Element element
    )
    {
        // Only hit test if within viewport bounds
        if (position.X >= 0 && position.X <= Size.X &&
            position.Y >= 0 && position.Y <= Size.Y)
        {
            // Adjust position for child hit-testing
            var adjustedPos = position + _offset;

            // Check children in reverse order (top-most first)
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                var childPos = adjustedPos - new Vector2(
                    child.X,
                    child.Y
                );
                if (child.HitTest(
                        result,
                        childPos,
                        element
                    ))
                {
                    return true;
                }
            }

            return IsHitTestTarget;
        }

        return false;
    }
}

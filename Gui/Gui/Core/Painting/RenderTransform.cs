using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     Applies an <see cref="SKMatrix" /> transformation to its child during painting.
///     Layout is passed through unchanged — the transform only affects the visual output.
///     <para>
///         When an <c>alignment</c> is supplied, the matrix is applied relative to the
///         corresponding point within this render object's bounds. For example,
///         <see cref="Widgets.Layout.Alignment.Center" /> causes rotations and scales to pivot around
///         the
///         widget's centre rather than its top-left corner.
///     </para>
/// </summary>
public class RenderTransform : RenderProxyBox
{
    private Alignment? _alignment;
    private SKMatrix _effectiveMatrix = SKMatrix.Identity;
    private SKMatrix _matrix = SKMatrix.Identity;

    public SKMatrix Matrix
    {
        get => _matrix;
        set
        {
            _matrix = value;
            _UpdateEffectiveMatrix();
            MarkNeedsPaint();
        }
    }

    public Alignment? Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            _UpdateEffectiveMatrix();
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        _UpdateEffectiveMatrix();
    }

    private void _UpdateEffectiveMatrix()
    {
        if (_alignment == null)
        {
            _effectiveMatrix = _matrix;
            return;
        }

        // Compute pivot point in local pixel space from the alignment.
        // CalculateOffset(parentSize, Vector2.Zero) returns the position of a zero-size
        // item placed with this alignment — which is exactly the pivot we want.
        var pivot = _alignment.CalculateOffset(
            Size,
            Vector2.Zero
        );

        // In SkiaSharp, A.PostConcat(B) yields B * A (B is applied first in column-vector terms).
        // To produce T(+pivot) * M * T(-pivot):
        //   start                       = T(-pivot)
        //   .PostConcat(_matrix)        → M * T(-pivot)
        //   .PostConcat(T(+pivot))      → T(+pivot) * M * T(-pivot)
        _effectiveMatrix = SKMatrix.CreateTranslation(
                -pivot.X,
                -pivot.Y
            )
            .PostConcat(_matrix)
            .PostConcat(
                SKMatrix.CreateTranslation(
                    pivot.X,
                    pivot.Y
                )
            );
    }

    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            base.Paint(context);
            return;
        }

        context.Canvas.Save();
        var m = _effectiveMatrix;
        context.Canvas.Concat(in m);
        base.Paint(context);
        context.Canvas.Restore();
    }

    public override Vector2 GlobalToChild(
        RenderObject child,
        Vector2 position
    )
    {
        if (_effectiveMatrix.TryInvert(out var inverse))
        {
            var pt = inverse.MapPoint(
                position.X,
                position.Y
            );
            return new Vector2(
                pt.X,
                pt.Y
            ) - new Vector2(
                child.X,
                child.Y
            );
        }

        // Degenerate transform (e.g. scale to zero) — report no hit.
        return new Vector2(
            float.NegativeInfinity,
            float.NegativeInfinity
        );
    }
}

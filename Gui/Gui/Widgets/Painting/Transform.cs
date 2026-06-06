using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Painting;

/// <summary>
///     Applies a matrix transformation to its child during painting.
///     Layout constraints pass through unchanged — the transform is purely visual.
///     <para>
///         Use the static factory constructors for common cases:
///         <see cref="Rotate" />, <see cref="Scale" />, <see cref="Translate" />.
///     </para>
/// </summary>
public class Transform : SingleChildWidget
{
    public Transform(
        Widget? child,
        SKMatrix matrix,
        Alignment? alignment = null,
        Framework.Key? key = null
    )
        : base(
            child,
            key
        )
    {
        Matrix = matrix;
        Alignment = alignment;
    }

    /// <summary>The transformation matrix applied to the child.</summary>
    public SKMatrix Matrix { get; }

    /// <summary>
    ///     The alignment of the origin within this widget's bounds.
    ///     When non-null, the matrix is applied relative to the corresponding point
    ///     (e.g. <see cref="Alignment.Center" /> pivots rotations and scales around
    ///     the widget's centre). When null, the origin is the top-left corner (0, 0).
    /// </summary>
    public Alignment? Alignment { get; }

    /// <summary>Creates a Transform that rotates its child by <paramref name="radians" />.</summary>
    public static Transform Rotate(
        Widget? child,
        float radians,
        Alignment? alignment = null,
        Framework.Key? key = null
    )
    {
        return new Transform(
            child,
            SKMatrix.CreateRotation(radians),
            alignment,
            key
        );
    }

    /// <summary>
    ///     Creates a Transform that scales its child uniformly by <paramref name="scale" />.
    /// </summary>
    public static Transform Scale(
        Widget? child,
        float scale,
        Alignment? alignment = null,
        Framework.Key? key = null
    )
    {
        return new Transform(
            child,
            SKMatrix.CreateScale(
                scale,
                scale
            ),
            alignment,
            key
        );
    }

    /// <summary>
    ///     Creates a Transform that scales its child independently on each axis.
    /// </summary>
    public static Transform ScaleXy(
        Widget? child,
        float scaleX,
        float scaleY,
        Alignment? alignment = null,
        Framework.Key? key = null
    )
    {
        return new Transform(
            child,
            SKMatrix.CreateScale(
                scaleX,
                scaleY
            ),
            alignment,
            key
        );
    }

    /// <summary>Creates a Transform that translates its child by <paramref name="offset" />.</summary>
    public static Transform Translate(
        Widget? child,
        Vector2 offset,
        Framework.Key? key = null
    )
    {
        return new Transform(
            child,
            SKMatrix.CreateTranslation(
                offset.X,
                offset.Y
            ),
            null,
            key
        );
    }

    public override RenderObject CreateRenderObject() =>
        new RenderTransform { Matrix = Matrix, Alignment = Alignment };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        var ro = (RenderTransform)renderObject;
        ro.Matrix = Matrix;
        ro.Alignment = Alignment;
    }
}

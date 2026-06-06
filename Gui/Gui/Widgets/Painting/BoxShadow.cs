using System;
using Gui.Core.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Painting;

/// <summary>
///     Immutable description of a single box shadow (CSS box-shadow equivalent).
///     Use in <see cref="BoxStyle.BoxShadows" />.
/// </summary>
/// <param name="Color">RGBA shadow color. Alpha drives opacity.</param>
/// <param name="Offset">Shadow displacement in pixels (X=right, Y=down).</param>
/// <param name="BlurRadius">
///     Gaussian blur sigma. 0 = hard edge. Negative values are clamped to 0.
/// </param>
/// <param name="SpreadRadius">
///     Expands (positive) or contracts (negative) the shadow shape before blur.
/// </param>
/// <param name="Inset">
///     When true, renders as an inner shadow inside the box boundary.
///     When false (default), renders as an outer shadow behind the box.
/// </param>
public readonly record struct BoxShadow(
    Vector4 Color,
    Vector2 Offset,
    float BlurRadius = 0f,
    float SpreadRadius = 0f,
    bool Inset = false
)
{
    /// <summary>
    ///     The maximum pixel distance this shadow can paint outside the box rect,
    ///     accounting for blur spread and offset magnitude. Used by
    ///     <see cref="RenderRepaintBoundary" /> to inflate its recording bounds
    ///     so outer shadows are not clipped.
    ///     Returns 0 for inset shadows (they never paint outside the box).
    /// </summary>
    public float Extent
    {
        get
        {
            if (Inset)
            {
                return 0f;
            }

            var blurReach = MathF.Max(
                0f,
                BlurRadius
            ) * 3f;
            var spread = MathF.Max(
                0f,
                SpreadRadius
            );
            var offsetReach = MathF.Max(
                MathF.Abs(Offset.X),
                MathF.Abs(Offset.Y)
            );
            return spread + blurReach + offsetReach;
        }
    }

    /// <summary>
    ///     Returns an <see cref="SKColor" /> representation of <see cref="Color" />.
    /// </summary>
    public SKColor ToSkColor()
    {
        return new SKColor(
            (byte)Math.Clamp(
                Color.X * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                Color.Y * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                Color.Z * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                Color.W * 255f,
                0,
                255
            )
        );
    }
}

using System;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Rendering;

/// <summary>
///     Single source of truth for shared drawing calculations.
///     Used by both <see cref="PaintingContext" /> and
///     <see cref="CanvasDrawExtensions" />.
/// </summary>
public static class DrawingOperations
{
    /// <summary>
    ///     Reduces corner radii by <paramref name="inset" />, clamping each
    ///     component to zero.
    ///     Radii: X=TopRight, Y=BottomRight, Z=TopLeft, W=BottomLeft.
    /// </summary>
    public static Vector4 InsetRadii(
        Vector4 radii,
        float inset
    )
    {
        return new Vector4(
            Math.Max(
                0,
                radii.X - inset
            ),
            Math.Max(
                0,
                radii.Y - inset
            ),
            Math.Max(
                0,
                radii.Z - inset
            ),
            Math.Max(
                0,
                radii.W - inset
            )
        );
    }

    /// <summary>
    ///     Computes background inset to avoid opaque border overlap.
    ///     Returns half the border width when opaque, otherwise 0.
    /// </summary>
    public static float ComputeBgInset(
        float border,
        float borderAlpha
    )
    {
        var isBorderOpaque = border > 0 && borderAlpha >= 0.99f;
        return isBorderOpaque
            ? border / 2f
            : 0f;
    }

    /// <summary>
    ///     Sets 4 corner radii on <paramref name="roundRect" /> using
    ///     the provided buffer to avoid per-call allocations.
    ///     Order: tl=TopLeft, tr=TopRight, br=BottomRight, bl=BottomLeft.
    /// </summary>
    public static void SetRadii(
        SKRoundRect roundRect,
        SKRect rect,
        float tl,
        float tr,
        float br,
        float bl,
        SKPoint[] radiiBuffer
    )
    {
        radiiBuffer[0] = new SKPoint(
            tl,
            tl
        );
        radiiBuffer[1] = new SKPoint(
            tr,
            tr
        );
        radiiBuffer[2] = new SKPoint(
            br,
            br
        );
        radiiBuffer[3] = new SKPoint(
            bl,
            bl
        );
        roundRect.SetRectRadii(
            rect,
            radiiBuffer
        );
    }

    /// <summary>
    ///     Sets radii from a <see cref="Vector4" />: Z=TL, X=TR, Y=BR,
    ///     W=BL.
    /// </summary>
    public static void SetRadii(
        SKRoundRect roundRect,
        SKRect rect,
        Vector4 radii,
        SKPoint[] radiiBuffer
    )
    {
        SetRadii(
            roundRect,
            rect,
            radii.Z,
            radii.X,
            radii.Y,
            radii.W,
            radiiBuffer
        );
    }

    /// <summary>
    ///     Draws a border stroke with per-corner radii. No-op when
    ///     <paramref name="border" /> &lt;= 0.
    /// </summary>
    public static void DrawBorder(
        SKCanvas canvas,
        SKRect outerRect,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedBorderRoundRect,
        SKPoint[] radiiBuffer
    )
    {
        if (border <= 0)
        {
            return;
        }

        sharedPaint.Style = SKPaintStyle.Stroke;
        sharedPaint.StrokeWidth = border;
        sharedPaint.Shader = null;
        sharedPaint.Color = borderColor.ToSkColor();
        sharedPaint.IsAntialias = true;

        var inset = border / 2f;
        var borderRect = new SKRect(
            outerRect.Left + inset,
            outerRect.Top + inset,
            outerRect.Right - inset,
            outerRect.Bottom - inset
        );
        var insetRadii = InsetRadii(
            radii,
            inset
        );
        SetRadii(
            sharedBorderRoundRect,
            borderRect,
            insetRadii,
            radiiBuffer
        );
        canvas.DrawRoundRect(
            sharedBorderRoundRect,
            sharedPaint
        );
    }
}

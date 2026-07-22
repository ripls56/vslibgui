using System;
using Gui.Widgets.Painting;
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
    public static Vector4 InsetRadii
    (
        Vector4 radii,
        float inset
    )
    {
        return new Vector4
        (
            Math.Max
            (
                0,
                radii.X - inset
            ),
            Math.Max
            (
                0,
                radii.Y - inset
            ),
            Math.Max
            (
                0,
                radii.Z - inset
            ),
            Math.Max
            (
                0,
                radii.W - inset
            )
        );
    }

    /// <summary>
    ///     Computes background inset to avoid opaque border overlap.
    ///     Returns half the border width when opaque, otherwise 0.
    /// </summary>
    public static float ComputeBgInset
    (
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
    public static void SetRadii
    (
        SKRoundRect roundRect,
        SKRect rect,
        float tl,
        float tr,
        float br,
        float bl,
        SKPoint[] radiiBuffer
    )
    {
        radiiBuffer[0] = new SKPoint
        (
            tl,
            tl
        );
        radiiBuffer[1] = new SKPoint
        (
            tr,
            tr
        );
        radiiBuffer[2] = new SKPoint
        (
            br,
            br
        );
        radiiBuffer[3] = new SKPoint
        (
            bl,
            bl
        );
        roundRect.SetRectRadii
        (
            rect,
            radiiBuffer
        );
    }

    /// <summary>
    ///     Sets radii from a <see cref="Vector4" />: Z=TL, X=TR, Y=BR,
    ///     W=BL.
    /// </summary>
    public static void SetRadii
    (
        SKRoundRect roundRect,
        SKRect rect,
        Vector4 radii,
        SKPoint[] radiiBuffer
    )
    {
        SetRadii
        (
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
    public static void DrawBorder
    (
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
        var borderRect = new SKRect
        (
            outerRect.Left + inset,
            outerRect.Top + inset,
            outerRect.Right - inset,
            outerRect.Bottom - inset
        );
        var insetRadii = InsetRadii
        (
            radii,
            inset
        );
        SetRadii
        (
            sharedBorderRoundRect,
            borderRect,
            insetRadii,
            radiiBuffer
        );
        canvas.DrawRoundRect
        (
            sharedBorderRoundRect,
            sharedPaint
        );
    }

    /// <summary>
    ///     Draws a per-side border with up to four distinct widths and colors.
    ///     The band between the outer and inner rounded rects is clipped, then
    ///     each side's strip is filled with its own color. Rounded corners are
    ///     preserved: the outer edge follows <paramref name="radii" />, the inner
    ///     edge follows radii reduced by the smaller of the two widths meeting
    ///     at each corner. No-op when no side has a positive width.
    /// </summary>
    public static void DrawBorder
    (
        SKCanvas canvas,
        SKRect outerRect,
        Vector4 radii,
        Border border,
        SKPaint sharedPaint,
        SKRoundRect outerRoundRect,
        SKRoundRect innerRoundRect,
        SKPoint[] radiiBuffer
    )
    {
        if (!border.AnyWidth)
        {
            return;
        }

        var l = border.Left.Width;
        var t = border.Top.Width;
        var r = border.Right.Width;
        var b = border.Bottom.Width;

        SetRadii
        (
            outerRoundRect,
            outerRect,
            radii.Z,
            radii.X,
            radii.Y,
            radii.W,
            radiiBuffer
        );

        var innerRect = new SKRect
        (
            Math.Min(outerRect.Left + l, outerRect.Right),
            Math.Min(outerRect.Top + t, outerRect.Bottom),
            Math.Max(outerRect.Right - r, outerRect.Left),
            Math.Max(outerRect.Bottom - b, outerRect.Top)
        );
        var innerRadii = new Vector4
        (
            Math.Max(0f, radii.X - Math.Min(t, r)),
            Math.Max(0f, radii.Y - Math.Min(r, b)),
            Math.Max(0f, radii.Z - Math.Min(t, l)),
            Math.Max(0f, radii.W - Math.Min(l, b))
        );
        SetRadii
        (
            innerRoundRect,
            innerRect,
            innerRadii,
            radiiBuffer
        );

        sharedPaint.Style = SKPaintStyle.Fill;
        sharedPaint.Shader = null;
        sharedPaint.IsAntialias = true;
        sharedPaint.BlendMode = SKBlendMode.SrcOver;

        var saveCount = canvas.Save();
        canvas.ClipRoundRect
        (
            outerRoundRect,
            SKClipOperation.Intersect,
            true
        );
        canvas.ClipRoundRect
        (
            innerRoundRect,
            SKClipOperation.Difference,
            true
        );

        FillStrip
        (
            canvas,
            sharedPaint,
            border.Top.Color,
            outerRect.Left,
            outerRect.Top,
            outerRect.Right,
            outerRect.Top + t
        );
        FillStrip
        (
            canvas,
            sharedPaint,
            border.Right.Color,
            outerRect.Right - r,
            outerRect.Top,
            outerRect.Right,
            outerRect.Bottom
        );
        FillStrip
        (
            canvas,
            sharedPaint,
            border.Bottom.Color,
            outerRect.Left,
            outerRect.Bottom - b,
            outerRect.Right,
            outerRect.Bottom
        );
        FillStrip
        (
            canvas,
            sharedPaint,
            border.Left.Color,
            outerRect.Left,
            outerRect.Top,
            outerRect.Left + l,
            outerRect.Bottom
        );

        canvas.RestoreToCount(saveCount);
    }

    private static void FillStrip
    (
        SKCanvas canvas,
        SKPaint paint,
        Vector4 color,
        float left,
        float top,
        float right,
        float bottom
    )
    {
        if (right <= left || bottom <= top)
        {
            return;
        }

        paint.Color = color.ToSkColor();
        canvas.DrawRect
        (
            new SKRect(left, top, right, bottom),
            paint
        );
    }
}

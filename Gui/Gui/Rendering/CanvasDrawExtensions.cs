using System;
using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Rendering;

/// <summary>
///     Extension methods for SKCanvas that provide optimized drawing operations
///     using shared resources from PaintingContext.
/// </summary>
public static class CanvasDrawExtensions
{
    [ThreadStatic] private static SKPoint[]? _radiiBuffer;

    private static void SetRadii(
        SKRoundRect rr,
        SKRect rect,
        float tl,
        float tr,
        float br,
        float bl
    )
    {
        _radiiBuffer ??= new SKPoint[4];
        _radiiBuffer[0] = new SKPoint(
            tl,
            tl
        );
        _radiiBuffer[1] = new SKPoint(
            tr,
            tr
        );
        _radiiBuffer[2] = new SKPoint(
            br,
            br
        );
        _radiiBuffer[3] = new SKPoint(
            bl,
            bl
        );
        rr.SetRectRadii(
            rect,
            _radiiBuffer
        );
    }

    /// <param name="radii">
    ///     Corner radii mapped as: X=TopRight, Y=BottomRight, Z=TopLeft, W=BottomLeft.
    ///     This matches SkiaSharp's clockwise SetRectRadii order starting from top-left.
    ///     Pass <c>new Vector4(r)</c> for uniform radii on all corners.
    /// </param>
    public static void DrawBox(
        this SKCanvas canvas,
        Vector2 pos,
        Vector2 size,
        Vector4 color,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedRoundRect,
        SKRoundRect sharedBorderRoundRect,
        SKShader? shader = null,
        SKImageFilter? imageFilter = null
    )
    {
        _radiiBuffer ??= new SKPoint[4];
        var bgInset = DrawingOperations.ComputeBgInset(
            border,
            borderColor.W
        );

        sharedPaint.Color = shader != null
            ? SKColors.White
            : color.ToSkColor();
        sharedPaint.Style = SKPaintStyle.Fill;
        sharedPaint.Shader = shader;
        sharedPaint.ImageFilter = imageFilter;
        sharedPaint.StrokeWidth = 0;
        sharedPaint.BlendMode = SKBlendMode.SrcOver;
        sharedPaint.IsAntialias = true;

        var rect = size.ToSkRect(pos);
        var bgRect = new SKRect(
            rect.Left + bgInset,
            rect.Top + bgInset,
            rect.Right - bgInset,
            rect.Bottom - bgInset
        );
        var bgRadii = DrawingOperations.InsetRadii(
            radii,
            bgInset
        );
        DrawingOperations.SetRadii(
            sharedRoundRect,
            bgRect,
            bgRadii,
            _radiiBuffer
        );

        canvas.DrawRoundRect(
            sharedRoundRect,
            sharedPaint
        );

        DrawingOperations.DrawBorder(
            canvas,
            rect,
            radii,
            border,
            borderColor,
            sharedPaint,
            sharedBorderRoundRect,
            _radiiBuffer
        );

        sharedPaint.Shader = null;
        sharedPaint.ImageFilter = null;
        sharedPaint.IsAntialias = true;
    }

    /// <summary>
    ///     Draws an image using pre-computed source and destination rects,
    ///     clipped to the rounded-rect widget boundary, with an optional border.
    ///     Accepts both <see cref="SKBitmap" /> and <see cref="SKImage" />.
    /// </summary>
    public static void DrawImage(
        this SKCanvas canvas,
        SKBitmap bitmap,
        Vector2 pos,
        Vector2 size,
        SKRect srcRect,
        SKRect dstRect,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedRoundRect,
        SKRoundRect sharedBorderRoundRect
    )
    {
        DrawImageCore(
            canvas,
            bitmap,
            null,
            pos,
            size,
            srcRect,
            dstRect,
            radii,
            border,
            borderColor,
            sharedPaint,
            sharedRoundRect,
            sharedBorderRoundRect
        );
    }

    /// <summary>
    ///     Draws an <see cref="SKImage" /> (immutable, GPU-friendly) using
    ///     pre-computed source and destination rects.
    /// </summary>
    public static void DrawImage(
        this SKCanvas canvas,
        SKImage image,
        Vector2 pos,
        Vector2 size,
        SKRect srcRect,
        SKRect dstRect,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedRoundRect,
        SKRoundRect sharedBorderRoundRect
    )
    {
        DrawImageCore(
            canvas,
            null,
            image,
            pos,
            size,
            srcRect,
            dstRect,
            radii,
            border,
            borderColor,
            sharedPaint,
            sharedRoundRect,
            sharedBorderRoundRect
        );
    }

    private static void DrawImageCore(
        SKCanvas canvas,
        SKBitmap? bitmap,
        SKImage? image,
        Vector2 pos,
        Vector2 size,
        SKRect srcRect,
        SKRect dstRect,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedRoundRect,
        SKRoundRect sharedBorderRoundRect
    )
    {
        _radiiBuffer ??= new SKPoint[4];
        var bgInset = DrawingOperations.ComputeBgInset(
            border,
            borderColor.W
        );

        var clipRect = new SKRect(
            pos.X + bgInset,
            pos.Y + bgInset,
            pos.X + size.X - bgInset,
            pos.Y + size.Y - bgInset
        );
        var bgRadii = DrawingOperations.InsetRadii(
            radii,
            bgInset
        );
        DrawingOperations.SetRadii(
            sharedRoundRect,
            clipRect,
            bgRadii,
            _radiiBuffer
        );

        using (canvas.SaveScope())
        {
            canvas.ClipRoundRect(
                sharedRoundRect,
                SKClipOperation.Intersect,
                true
            );

            sharedPaint.Style = SKPaintStyle.Fill;
            sharedPaint.Color = SKColors.White;
            sharedPaint.BlendMode = SKBlendMode.SrcOver;
            sharedPaint.Shader = null;
            sharedPaint.ImageFilter = null;
            var adjustedDst = new SKRect(
                pos.X + dstRect.Left,
                pos.Y + dstRect.Top,
                pos.X + dstRect.Right,
                pos.Y + dstRect.Bottom
            );
            var bilinear = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Nearest);

            if (image != null)
            {
                canvas.DrawImage(
                    image,
                    srcRect,
                    adjustedDst,
                    bilinear,
                    sharedPaint
                );
            }
            else if (bitmap != null)
            {
#pragma warning disable CS0618
                sharedPaint.FilterQuality = SKFilterQuality.Medium;
                canvas.DrawBitmap(bitmap, srcRect, adjustedDst, sharedPaint);
                sharedPaint.FilterQuality = SKFilterQuality.None;
#pragma warning restore CS0618
            }
        }

        DrawingOperations.DrawBorder(
            canvas,
            clipRect,
            radii,
            border,
            borderColor,
            sharedPaint,
            sharedBorderRoundRect,
            _radiiBuffer
        );
        sharedPaint.Style = SKPaintStyle.Fill;
        sharedPaint.StrokeWidth = 0;
        sharedPaint.IsAntialias = true;
    }

    /// <summary>
    ///     Renders a bitmap using 9-slice (or 3-slice) scaling so corners stay
    ///     pixel-perfect while edges and the center stretch or tile.
    /// </summary>
    /// <param name="pos">Top-left origin in local canvas space (usually Vector2.Zero).</param>
    /// <param name="size">Widget render size.</param>
    /// <param name="bitmap">Source bitmap.</param>
    /// <param name="slice">Corner insets in source-texture pixels.</param>
    /// <param name="mode">Simple / Sliced / Tiled.</param>
    /// <param name="tint">Multiply tint; Vector4.One = no tint.</param>
    public static void DrawNineSlice(
        this SKCanvas canvas,
        Vector2 pos,
        Vector2 size,
        SKBitmap bitmap,
        EdgeInsets slice,
        float scale,
        ImageDrawMode mode,
        Vector4 tint,
        SKPaint sharedPaint
    )
    {
        if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return;
        }

        if (size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        sharedPaint.Style = SKPaintStyle.Fill;
        sharedPaint.Color = tint == Vector4.One
            ? SKColors.White
            : tint.ToSkColor();
        sharedPaint.BlendMode = SKBlendMode.SrcOver;
        sharedPaint.Shader = null;
        sharedPaint.ImageFilter = null;
        sharedPaint.ColorFilter = null;
        sharedPaint.StrokeWidth = 0;

        float bW = bitmap.Width;
        float bH = bitmap.Height;
        var dW = size.X;
        var dH = size.Y;

        if (mode == ImageDrawMode.Simple)
        {
            canvas.DrawBitmap(
                bitmap,
                SKRect.Create(
                    0,
                    0,
                    bW,
                    bH
                ),
                SKRect.Create(
                    pos.X,
                    pos.Y,
                    dW,
                    dH
                ),
                sharedPaint
            );
            return;
        }

        // Clamp source borders so they never exceed half the source dimension.
        var sL = Math.Min(
            slice.Left,
            bW / 2f
        );
        var sT = Math.Min(
            slice.Top,
            bH / 2f
        );
        var sR = Math.Min(
            slice.Right,
            bW / 2f
        );
        var sB = Math.Min(
            slice.Bottom,
            bH / 2f
        );

        // Calculate destination borders with scale factor.
        var dL = slice.Left * scale;
        var dT = slice.Top * scale;
        var dR = slice.Right * scale;
        var dB = slice.Bottom * scale;

        // Clamp destination borders proportionately so they never overlap.
        var sumX = dL + dR;
        if (sumX > dW)
        {
            dL = dL / sumX * dW;
            dR = dR / sumX * dW;
        }

        var sumY = dT + dB;
        if (sumY > dH)
        {
            dT = dT / sumY * dH;
            dB = dB / sumY * dH;
        }

        // Round all destination segments to integers to eliminate floating point 
        // gaps/overlaps that cause black lines or artifacting on edges.
        dL = MathF.Round(dL);
        dT = MathF.Round(dT);
        dR = MathF.Round(dR);
        dB = MathF.Round(dB);

        var originalAa = sharedPaint.IsAntialias;
        sharedPaint.IsAntialias =
            false; // Prevents Skia from blending adjacent edges with the background

        var srcTl = SKRect.Create(
            0,
            0,
            sL,
            sT
        );
        var srcTc = SKRect.Create(
            sL,
            0,
            bW - sL - sR,
            sT
        );
        var srcTr = SKRect.Create(
            bW - sR,
            0,
            sR,
            sT
        );

        var srcMl = SKRect.Create(
            0,
            sT,
            sL,
            bH - sT - sB
        );
        var srcMc = SKRect.Create(
            sL,
            sT,
            bW - sL - sR,
            bH - sT - sB
        );
        var srcMr = SKRect.Create(
            bW - sR,
            sT,
            sR,
            bH - sT - sB
        );

        var srcBl = SKRect.Create(
            0,
            bH - sB,
            sL,
            sB
        );
        var srcBc = SKRect.Create(
            sL,
            bH - sB,
            bW - sL - sR,
            sB
        );
        var srcBr = SKRect.Create(
            bW - sR,
            bH - sB,
            sR,
            sB
        );

        var dstTl = SKRect.Create(
            pos.X,
            pos.Y,
            dL,
            dT
        );
        var dstTc = SKRect.Create(
            pos.X + dL,
            pos.Y,
            dW - dL - dR,
            dT
        );
        var dstTr = SKRect.Create(
            pos.X + dW - dR,
            pos.Y,
            dR,
            dT
        );

        var dstMl = SKRect.Create(
            pos.X,
            pos.Y + dT,
            dL,
            dH - dT - dB
        );
        var dstMc = SKRect.Create(
            pos.X + dL,
            pos.Y + dT,
            dW - dL - dR,
            dH - dT - dB
        );
        var dstMr = SKRect.Create(
            pos.X + dW - dR,
            pos.Y + dT,
            dR,
            dH - dT - dB
        );

        var dstBl = SKRect.Create(
            pos.X,
            pos.Y + dH - dB,
            dL,
            dB
        );
        var dstBc = SKRect.Create(
            pos.X + dL,
            pos.Y + dH - dB,
            dW - dL - dR,
            dB
        );
        var dstBr = SKRect.Create(
            pos.X + dW - dR,
            pos.Y + dH - dB,
            dR,
            dB
        );

        switch (mode)
        {
            case ImageDrawMode.Sliced:
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTl,
                    dstTl,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTc,
                    dstTc,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTr,
                    dstTr,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMl,
                    dstMl,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMc,
                    dstMc,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMr,
                    dstMr,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBl,
                    dstBl,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBc,
                    dstBc,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBr,
                    dstBr,
                    false
                );
                break;

            case ImageDrawMode.Tiled:
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTl,
                    dstTl,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTc,
                    dstTc,
                    true
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcTr,
                    dstTr,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMl,
                    dstMl,
                    true
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMc,
                    dstMc,
                    true
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcMr,
                    dstMr,
                    true
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBl,
                    dstBl,
                    false
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBc,
                    dstBc,
                    true
                );
                DrawRegion(
                    canvas,
                    bitmap,
                    sharedPaint,
                    srcBr,
                    dstBr,
                    false
                );
                break;
        }

        sharedPaint.IsAntialias = originalAa;
    }

    private static void DrawRegion(
        SKCanvas canvas,
        SKBitmap bitmap,
        SKPaint sharedPaint,
        SKRect src,
        SKRect dst,
        bool tile
    )
    {
        if (dst.Width <= 0 || dst.Height <= 0 || src.Width <= 0 || src.Height <= 0)
        {
            return;
        }

        if (tile)
        {
            DrawTiledRegion(
                canvas,
                bitmap,
                sharedPaint,
                src,
                dst
            );
        }
        else
        {
            canvas.DrawBitmap(
                bitmap,
                src,
                dst,
                sharedPaint
            );
        }
    }

    private static void DrawTiledRegion(
        SKCanvas canvas,
        SKBitmap bitmap,
        SKPaint sharedPaint,
        SKRect srcRegion,
        SKRect dstRegion
    )
    {
        if (srcRegion.Width < 1.0f || srcRegion.Height < 1.0f)
        {
            return;
        }

        if (dstRegion.Width <= 0 || dstRegion.Height <= 0)
        {
            return;
        }

        using (canvas.SaveScope())
        {
            canvas.ClipRect(dstRegion);

            var tileW = srcRegion.Width;
            var tileH = srcRegion.Height;

            for (var ty = dstRegion.Top; ty < dstRegion.Bottom; ty += tileH)
            for (var tx = dstRegion.Left; tx < dstRegion.Right; tx += tileW)
            {
                var tileRect = new SKRect(
                    tx,
                    ty,
                    tx + tileW,
                    ty + tileH
                );
                canvas.DrawBitmap(
                    bitmap,
                    srcRegion,
                    tileRect,
                    sharedPaint
                );
            }
        }
    }

    public static void DrawMaskedBox(
        this SKCanvas canvas,
        Vector2 pos,
        Vector2 size,
        SKBitmap texture,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKPaint sharedPaint,
        SKRoundRect sharedRoundRect,
        SKRoundRect sharedBorderRoundRect
    )
    {
        _radiiBuffer ??= new SKPoint[4];
        var bgInset = DrawingOperations.ComputeBgInset(
            border,
            borderColor.W
        );

        var clipRect = new SKRect(
            pos.X + bgInset,
            pos.Y + bgInset,
            pos.X + size.X - bgInset,
            pos.Y + size.Y - bgInset
        );
        var bgRadii = DrawingOperations.InsetRadii(
            radii,
            bgInset
        );
        DrawingOperations.SetRadii(
            sharedRoundRect,
            clipRect,
            bgRadii,
            _radiiBuffer
        );

        var rect = size.ToSkRect(pos);

        using (canvas.SaveScope())
        {
            canvas.ClipRoundRect(
                sharedRoundRect,
                SKClipOperation.Intersect,
                true
            );
#pragma warning disable CS0618
            sharedPaint.FilterQuality = SKFilterQuality.Medium;
            canvas.DrawBitmap(texture, rect, sharedPaint);
            sharedPaint.FilterQuality = SKFilterQuality.None;
#pragma warning restore CS0618
        }

        DrawingOperations.DrawBorder(
            canvas,
            rect,
            radii,
            border,
            borderColor,
            sharedPaint,
            sharedBorderRoundRect,
            _radiiBuffer
        );
        sharedPaint.Style = SKPaintStyle.Fill;
        sharedPaint.StrokeWidth = 0;
        sharedPaint.IsAntialias = true;
    }

    /// <summary>
    ///     Draws <paramref name="text" /> at <paramref name="pos" /> using HarfBuzz shaping with
    ///     automatic per-character font fallback. Shapes once into <see cref="ShapedRun" />s,
    ///     then draws one <see cref="SKTextBlob" /> per run for each enabled pass
    ///     (glow → outline → fill).
    /// </summary>
    public static void DrawText(
        this SKCanvas canvas,
        string text,
        Vector2 pos,
        float fontSize,
        Vector4 color,
        string fontFamily,
        FontWeight weight = FontWeight.Normal,
        float boldness = 0,
        float outlineWidth = 0,
        Vector4? outlineColor = null,
        float glowWidth = 0,
        Vector4? glowColor = null,
        SKPaint? sharedPaint = null,
        Dictionary<float, SKImageFilter>? blurFilterCache = null
    )
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // GetFont returns a shared cached instance; save and restore all mutations.
        var font = TextLayoutHelper.GetFont(fontFamily, fontSize, weight);
        var originalSize = font.Size;
        font.Size = fontSize; // exact size for smooth animation (GetFont rounds for cache key)

        var paint = sharedPaint ?? new SKPaint { IsAntialias = true };
        paint.Style = SKPaintStyle.Fill;
        paint.StrokeWidth = 0;
        paint.ImageFilter = null;
        paint.BlendMode = SKBlendMode.SrcOver;

        var runs = TextShaper.Shape(text, font);
        var blobs = BuildBlobs(runs, boldness > 0);

        try
        {
            if (glowWidth > 0 && glowColor.HasValue)
            {
                paint.Color = glowColor.Value.ToSkColor();
                paint.ImageFilter = GetBlurFilter(blurFilterCache, glowWidth * fontSize * 0.5f);
                foreach (var blob in blobs)
                {
                    canvas.DrawText(blob, pos.X, pos.Y, paint);
                }

                paint.ImageFilter = null;
            }

            if (outlineWidth > 0 && outlineColor.HasValue)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = outlineWidth * fontSize;
                paint.Color = outlineColor.Value.ToSkColor();
                foreach (var blob in blobs)
                {
                    canvas.DrawText(blob, pos.X, pos.Y, paint);
                }

                paint.Style = SKPaintStyle.Fill;
                paint.StrokeWidth = 0;
            }

            paint.Color = color.ToSkColor();
            foreach (var blob in blobs)
            {
                canvas.DrawText(blob, pos.X, pos.Y, paint);
            }
        }
        finally
        {
            foreach (var blob in blobs)
            {
                blob.Dispose();
            }

            font.Size = originalSize;
        }
    }

    private static SKTextBlob[] BuildBlobs(ShapedRun[] runs, bool embolden)
    {
        var blobs = new SKTextBlob[runs.Length];
        for (var i = 0; i < runs.Length; i++)
        {
            var run = runs[i];
            var savedEmbolden = run.Font.Embolden;
            run.Font.Embolden = embolden;
            using var builder = new SKTextBlobBuilder();
            var buffer = builder.AllocatePositionedRun(run.Font, run.Glyphs.Length);
            run.Glyphs.CopyTo(buffer.Glyphs);
            run.Points.CopyTo(buffer.Positions);
            blobs[i] = builder.Build()!;
            run.Font.Embolden = savedEmbolden;
        }

        return blobs;
    }

    private static SKImageFilter GetBlurFilter(
        Dictionary<float, SKImageFilter>? cache,
        float sigma
    )
    {
        // Round to 2 dp so that near-identical animation frames reuse the same filter.
        var key = MathF.Round(
            sigma,
            2
        );

        if (cache != null && cache.TryGetValue(
                key,
                out var cached
            ))
        {
            return cached;
        }

        var filter = SKImageFilter.CreateBlur(
            sigma,
            sigma
        );

        if (cache != null)
        {
            const int maxCache = 32;
            if (cache.Count >= maxCache)
            {
                foreach (var f in cache.Values)
                {
                    f.Dispose();
                }

                cache.Clear();
            }

            cache[key] = filter;
        }

        return filter;
    }
}

using System;
using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Rendering;

public class PaintingContext
{
    // Cache for blur image filters keyed by sigma value (rounded to 2 decimal places).
    // Max 32 entries; evict all on overflow (blur radii rarely exceed a handful of values).
    private const int BlurCacheMax = 32;

    // Caches for shadow blur filters (mask for outer, image for inner).
    private const int ShadowCacheMax = 64;
    private readonly Dictionary<float, SKImageFilter> _blurImageCache = new();
    private readonly Dictionary<float, SKMaskFilter> _blurMaskCache = new();
    private readonly Stack<SKCanvas> _canvasStack = new();

    public PaintingContext(
        SKCanvas canvas,
        double currentTime
    )
    {
        _canvasStack.Push(canvas);
        CurrentTime = currentTime;
    }

    /// <summary>
    ///     Shared SKPaint instance for reuse across drawing operations.
    ///     IMPORTANT: Reset all modified properties after use to avoid state leakage.
    /// </summary>
    public SKPaint SharedPaint { get; } = new() { IsAntialias = true };

    /// <summary>
    ///     Shared SKFont instance for reuse across text operations.
    ///     IMPORTANT: Reset all modified properties after use to avoid state leakage.
    /// </summary>
    public SKFont SharedFont { get; } = new()
    {
        Subpixel = true,
        Edging = SKFontEdging.Antialias,
        LinearMetrics = true,
        Hinting = SKFontHinting.None
    };

    /// <summary>
    ///     Shared SKRoundRect for reuse in rounded-rect drawing operations.
    /// </summary>
    public SKRoundRect SharedRoundRect { get; } = new();

    /// <summary>
    ///     Shared SKRoundRect for border drawing operations.
    /// </summary>
    public SKRoundRect SharedBorderRoundRect { get; } = new();

    /// <summary>
    ///     Shared SKRoundRect for shadow shape operations, distinct from
    ///     SharedRoundRect which may be in use as a clip path at the same call site.
    /// </summary>
    public SKRoundRect SharedShadowRoundRect { get; } = new();

    /// <summary>
    ///     Shared SKPoint[4] buffer for SetRectRadii calls to avoid per-frame heap allocations.
    /// </summary>
    public SKPoint[] SharedRadiiBuffer { get; } = new SKPoint[4];

    /// <summary>
    ///     Cache for blur image filters used in text glow and other effects.
    ///     Shared across all drawing operations for efficiency.
    /// </summary>
    public Dictionary<float, SKImageFilter> BlurFilterCache { get; } = new();

    public SKCanvas? Canvas => _canvasStack.Count > 0
        ? _canvasStack.Peek()
        : null;

    public double CurrentTime { get; private set; }

    /// <summary>
    ///     Sets the 4 corner radii in <see cref="SharedRadiiBuffer" /> and applies them
    ///     to the given <paramref name="roundRect" />. Avoids allocating a new SKPoint[]
    ///     on every call.
    /// </summary>
    public void SetRadii(
        SKRoundRect roundRect,
        SKRect rect,
        float tl,
        float tr,
        float br,
        float bl
    )
    {
        SharedRadiiBuffer[0] = new SKPoint(
            tl,
            tl
        );
        SharedRadiiBuffer[1] = new SKPoint(
            tr,
            tr
        );
        SharedRadiiBuffer[2] = new SKPoint(
            br,
            br
        );
        SharedRadiiBuffer[3] = new SKPoint(
            bl,
            bl
        );
        roundRect.SetRectRadii(
            rect,
            SharedRadiiBuffer
        );
    }

    /// <summary>
    ///     Resets the context for a new frame, reusing all shared native Skia objects.
    /// </summary>
    public void Reset(
        SKCanvas canvas,
        double currentTime
    )
    {
        _canvasStack.Clear();
        _canvasStack.Push(canvas);
        CurrentTime = currentTime;
    }

    public void PushCanvas(
        SKCanvas canvas
    ) =>
        _canvasStack.Push(canvas);

    public void PopCanvas()
    {
        if (_canvasStack.Count > 1) // Keep the root canvas
        {
            _canvasStack.Pop();
        }
    }

    /// <param name="radii">
    ///     Corner radii mapped as: X=TopRight, Y=BottomRight, Z=TopLeft, W=BottomLeft.
    ///     This matches SkiaSharp's clockwise SetRectRadii order starting from top-left.
    ///     Pass <c>new Vector4(r)</c> for uniform radii on all corners.
    /// </param>
    public void DrawBox(
        Vector2 pos,
        Vector2 size,
        Vector4 color,
        Vector4 radii,
        float border,
        Vector4 borderColor,
        SKShader? shader = null,
        SKImageFilter? imageFilter = null
    )
    {
        if (Canvas == null)
        {
            return;
        }

        var bgInset = DrawingOperations.ComputeBgInset(
            border,
            borderColor.W
        );

        SharedPaint.Color = color.ToSkColor();
        SharedPaint.Style = SKPaintStyle.Fill;
        SharedPaint.Shader = shader;
        SharedPaint.ImageFilter = imageFilter;
        SharedPaint.StrokeWidth = 0;
        SharedPaint.BlendMode = SKBlendMode.SrcOver;
        SharedPaint.IsAntialias = true;

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
            SharedRoundRect,
            bgRect,
            bgRadii,
            SharedRadiiBuffer
        );

        Canvas.DrawRoundRect(
            SharedRoundRect,
            SharedPaint
        );

        DrawingOperations.DrawBorder(
            Canvas,
            rect,
            radii,
            border,
            borderColor,
            SharedPaint,
            SharedBorderRoundRect,
            SharedRadiiBuffer
        );

        SharedPaint.Shader = null;
        SharedPaint.ImageFilter = null;
        SharedPaint.IsAntialias = true;
    }

    /// <summary>
    ///     Draws a bitmap using pre-computed source and destination rects (from BoxFit logic),
    ///     clipped to the rounded-rect widget boundary defined by <paramref name="pos" /> and
    ///     <paramref name="size" />, with an optional border on top.
    /// </summary>
    public void DrawImage(
        SKBitmap bitmap,
        Vector2 pos,
        Vector2 size,
        SKRect srcRect,
        SKRect dstRect,
        Vector4 radii,
        float border,
        Vector4 borderColor
    )
    {
        if (Canvas == null)
        {
            return;
        }

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
            SharedRoundRect,
            clipRect,
            bgRadii,
            SharedRadiiBuffer
        );

        using (Canvas.SaveScope())
        {
            Canvas.ClipRoundRect(
                SharedRoundRect,
                SKClipOperation.Intersect,
                true
            );

            SharedPaint.Style = SKPaintStyle.Fill;
            SharedPaint.Color = SKColors.White;
            SharedPaint.BlendMode = SKBlendMode.SrcOver;
            SharedPaint.Shader = null;
            SharedPaint.ImageFilter = null;
            var adjustedDst = new SKRect(
                pos.X + dstRect.Left,
                pos.Y + dstRect.Top,
                pos.X + dstRect.Right,
                pos.Y + dstRect.Bottom
            );
#pragma warning disable CS0618
            SharedPaint.FilterQuality = SKFilterQuality.Medium;
            Canvas.DrawBitmap(bitmap, srcRect, adjustedDst, SharedPaint);
            SharedPaint.FilterQuality = SKFilterQuality.None;
#pragma warning restore CS0618
        }

        DrawingOperations.DrawBorder(
            Canvas,
            clipRect,
            radii,
            border,
            borderColor,
            SharedPaint,
            SharedBorderRoundRect,
            SharedRadiiBuffer
        );
        SharedPaint.Style = SKPaintStyle.Fill;
        SharedPaint.StrokeWidth = 0;
        SharedPaint.IsAntialias = true;
    }

    /// <summary>
    ///     Renders a bitmap using 9-slice (or 3-slice) scaling so corners stay
    ///     pixel-perfect while edges and the center stretch or tile.
    /// </summary>
    /// <param name="pos">Top-left origin in local canvas space (usually Vector2.Zero).</param>
    /// <param name="size">Widget render size.</param>
    /// <param name="bitmap">Source bitmap.</param>
    /// <param name="border">Corner insets in source-texture pixels.</param>
    /// <param name="mode">Simple / Sliced / Tiled.</param>
    /// <param name="tint">Multiply tint; Vector4.One = no tint.</param>
    public void DrawNineSlice(
        Vector2 pos,
        Vector2 size,
        SKBitmap bitmap,
        EdgeInsets slice,
        float scale,
        ImageDrawMode mode,
        Vector4 tint
    )
    {
        if (Canvas == null || bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return;
        }

        if (size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        SharedPaint.Style = SKPaintStyle.Fill;
        SharedPaint.Color = tint == Vector4.One
            ? SKColors.White
            : tint.ToSkColor();
        SharedPaint.BlendMode = SKBlendMode.SrcOver;
        SharedPaint.Shader = null;
        SharedPaint.ImageFilter = null;
        SharedPaint.ColorFilter = null;
        SharedPaint.StrokeWidth = 0;

        float bW = bitmap.Width;
        float bH = bitmap.Height;
        var dW = size.X;
        var dH = size.Y;

        if (mode == ImageDrawMode.Simple)
        {
            Canvas.DrawBitmap(
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
                SharedPaint
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

        var originalAa = SharedPaint.IsAntialias;
        SharedPaint.IsAntialias =
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
                    srcTl,
                    dstTl,
                    false
                );
                DrawRegion(
                    srcTc,
                    dstTc,
                    false
                );
                DrawRegion(
                    srcTr,
                    dstTr,
                    false
                );
                DrawRegion(
                    srcMl,
                    dstMl,
                    false
                );
                DrawRegion(
                    srcMc,
                    dstMc,
                    false
                );
                DrawRegion(
                    srcMr,
                    dstMr,
                    false
                );
                DrawRegion(
                    srcBl,
                    dstBl,
                    false
                );
                DrawRegion(
                    srcBc,
                    dstBc,
                    false
                );
                DrawRegion(
                    srcBr,
                    dstBr,
                    false
                );
                break;

            case ImageDrawMode.Tiled:
                DrawRegion(
                    srcTl,
                    dstTl,
                    false
                );
                DrawRegion(
                    srcTc,
                    dstTc,
                    true
                );
                DrawRegion(
                    srcTr,
                    dstTr,
                    false
                );
                DrawRegion(
                    srcMl,
                    dstMl,
                    true
                );
                DrawRegion(
                    srcMc,
                    dstMc,
                    true
                );
                DrawRegion(
                    srcMr,
                    dstMr,
                    true
                );
                DrawRegion(
                    srcBl,
                    dstBl,
                    false
                );
                DrawRegion(
                    srcBc,
                    dstBc,
                    true
                );
                DrawRegion(
                    srcBr,
                    dstBr,
                    false
                );
                break;
        }

        void DrawRegion(
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
                    bitmap,
                    src,
                    dst
                );
            }
            else
            {
                Canvas.DrawBitmap(
                    bitmap,
                    src,
                    dst,
                    SharedPaint
                );
            }
        }

        SharedPaint.IsAntialias = originalAa;
    }

    private void DrawTiledRegion(
        SKBitmap bitmap,
        SKRect srcRegion,
        SKRect dstRegion
    )
    {
        if (Canvas == null)
        {
            return;
        }

        if (srcRegion.Width < 1.0f || srcRegion.Height < 1.0f)
        {
            return;
        }

        if (dstRegion.Width <= 0 || dstRegion.Height <= 0)
        {
            return;
        }

        using (Canvas.SaveScope())
        {
            Canvas.ClipRect(dstRegion);

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
                Canvas.DrawBitmap(
                    bitmap,
                    srcRegion,
                    tileRect,
                    SharedPaint
                );
            }
        }
    }

    public void DrawMaskedBox(
        Vector2 pos,
        Vector2 size,
        SKBitmap texture,
        Vector4 radii,
        float border,
        Vector4 borderColor
    )
    {
        if (Canvas == null)
        {
            return;
        }

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
            SharedRoundRect,
            clipRect,
            bgRadii,
            SharedRadiiBuffer
        );

        var rect = size.ToSkRect(pos);

        using (Canvas.SaveScope())
        {
            Canvas.ClipRoundRect(
                SharedRoundRect,
                SKClipOperation.Intersect,
                true
            );
#pragma warning disable CS0618
            SharedPaint.FilterQuality = SKFilterQuality.Medium;
            Canvas.DrawBitmap(texture, rect, SharedPaint);
            SharedPaint.FilterQuality = SKFilterQuality.None;
#pragma warning restore CS0618
        }

        DrawingOperations.DrawBorder(
            Canvas,
            rect,
            radii,
            border,
            borderColor,
            SharedPaint,
            SharedBorderRoundRect,
            SharedRadiiBuffer
        );
        SharedPaint.Style = SKPaintStyle.Fill;
        SharedPaint.StrokeWidth = 0;
        SharedPaint.IsAntialias = true;
    }

    public void SaveLayer(
        SKRect? bounds = null,
        SKPaint? paint = null
    )
    {
        if (bounds.HasValue)
        {
            Canvas?.SaveLayer(
                bounds.Value,
                paint
            );
        }
        else
        {
            Canvas?.SaveLayer(paint);
        }
    }

    public void Restore() => Canvas?.Restore();

    public void DrawText(
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
        Vector4? glowColor = null
    )
    {
        if (Canvas == null)
        {
            return;
        }

        // IMPORTANT: GetFont returns a SHARED cached SKFont instance. All property mutations
        // (Size, Embolden) must be saved and restored before returning.
        var font = TextLayoutHelper.GetFont(
            fontFamily,
            fontSize,
            weight
        );
        font.Size =
            fontSize; // GetFont rounds size for cache hits; restore exact size for smooth animation
        SharedPaint.Style = SKPaintStyle.Fill;
        SharedPaint.Color = color.ToSkColor();
        SharedPaint.StrokeWidth = 0;
        SharedPaint.ImageFilter = null;
        SharedPaint.BlendMode = SKBlendMode.SrcOver;

        var originalEmbolden = font.Embolden;
        font.Embolden = boldness > 0;

        if (glowWidth > 0 && glowColor.HasValue)
        {
            SharedPaint.Color = glowColor.Value.ToSkColor();
            SharedPaint.ImageFilter = GetBlurFilter(glowWidth * fontSize * 0.5f);
            Canvas.DrawText(
                text,
                pos.X,
                pos.Y,
                font,
                SharedPaint
            );
            SharedPaint.ImageFilter = null;
        }

        if (outlineWidth > 0 && outlineColor.HasValue)
        {
            SharedPaint.Style = SKPaintStyle.Stroke;
            SharedPaint.StrokeWidth = outlineWidth * fontSize;
            SharedPaint.Color = outlineColor.Value.ToSkColor();

            Canvas.DrawText(
                text,
                pos.X,
                pos.Y,
                font,
                SharedPaint
            );
            SharedPaint.Style = SKPaintStyle.Fill;
        }

        SharedPaint.Color = color.ToSkColor();
        Canvas.DrawText(
            text,
            pos.X,
            pos.Y,
            font,
            SharedPaint
        );

        font.Embolden = originalEmbolden;
    }

    private SKImageFilter GetBlurFilter(
        float sigma
    )
    {
        // Round to 2 dp so that near-identical animation frames reuse the same filter.
        var key = MathF.Round(
            sigma,
            2
        );
        if (BlurFilterCache.TryGetValue(
                key,
                out var cached
            ))
        {
            return cached;
        }

        if (BlurFilterCache.Count >= BlurCacheMax)
        {
            foreach (var f in BlurFilterCache.Values)
            {
                f.Dispose();
            }

            BlurFilterCache.Clear();
        }

        var filter = SKImageFilter.CreateBlur(
            sigma,
            sigma
        );
        BlurFilterCache[key] = filter;
        return filter;
    }

    /// <summary>
    ///     Returns a cached <see cref="SKMaskFilter" /> blur for the given sigma.
    ///     Used for outer shadow blurring. Caller must NOT dispose the returned filter.
    /// </summary>
    internal SKMaskFilter GetOrCreateBlurMask(
        float sigma
    )
    {
        var key = MathF.Round(
            sigma,
            2
        );
        if (_blurMaskCache.TryGetValue(
                key,
                out var cached
            ))
        {
            return cached;
        }

        if (_blurMaskCache.Count >= ShadowCacheMax)
        {
            foreach (var f in _blurMaskCache.Values)
            {
                f.Dispose();
            }

            _blurMaskCache.Clear();
        }

        var filter = SKMaskFilter.CreateBlur(
            SKBlurStyle.Normal,
            sigma
        );
        _blurMaskCache[key] = filter;
        return filter;
    }

    /// <summary>
    ///     Returns a cached <see cref="SKImageFilter" /> blur for the given sigma.
    ///     Used for inner shadow layer blurring. Caller must NOT dispose.
    /// </summary>
    internal SKImageFilter GetOrCreateBlurImage(
        float sigma
    )
    {
        var key = MathF.Round(
            sigma,
            2
        );
        if (_blurImageCache.TryGetValue(
                key,
                out var cached
            ))
        {
            return cached;
        }

        if (_blurImageCache.Count >= ShadowCacheMax)
        {
            foreach (var f in _blurImageCache.Values)
            {
                f.Dispose();
            }

            _blurImageCache.Clear();
        }

        var filter = SKImageFilter.CreateBlur(
            sigma,
            sigma
        );
        _blurImageCache[key] = filter;
        return filter;
    }

    public void Dispose()
    {
        SharedPaint.Dispose();
        SharedFont.Dispose();
        SharedRoundRect.Dispose();
        SharedBorderRoundRect.Dispose();
        SharedShadowRoundRect.Dispose();
        foreach (var f in BlurFilterCache.Values)
        {
            f.Dispose();
        }

        BlurFilterCache.Clear();
        foreach (var f in _blurMaskCache.Values)
        {
            f.Dispose();
        }

        _blurMaskCache.Clear();
        foreach (var f in _blurImageCache.Values)
        {
            f.Dispose();
        }

        _blurImageCache.Clear();
    }
}

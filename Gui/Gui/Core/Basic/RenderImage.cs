using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Basic;

public class RenderImage : RenderBox
{
    private Alignment _alignment = Alignment.Center;
    private SKBitmap? _bitmap;
    private BoxFit _fit = BoxFit.Contain;
    private float? _height;
    private float? _width;

    // Tracks the asset path that produced the current bitmap so UpdateRenderObject
    // can skip reloading when the source hasn't changed.
    internal string? SourceDomain { get; set; }
    internal string? SourcePath { get; set; }

    public SKBitmap? Bitmap
    {
        get => _bitmap;
        set => SetProperty(
            ref _bitmap,
            value,
            relayout: true
        );
    }

    public BoxFit Fit
    {
        get => _fit;
        set => SetProperty(
            ref _fit,
            value,
            true
        );
    }

    public Alignment Alignment
    {
        get => _alignment;
        set => SetProperty(
            ref _alignment,
            value,
            true
        );
    }

    public float? Width
    {
        get => _width;
        set => SetProperty(
            ref _width,
            value,
            relayout: true
        );
    }

    public float? Height
    {
        get => _height;
        set => SetProperty(
            ref _height,
            value,
            relayout: true
        );
    }

    public override bool IsHitTestTarget =>
        HitTestBehavior == HitTestBehavior.Opaque ||
        (HitTestBehavior == HitTestBehavior.Defer && (_bitmap != null || BorderThickness > 0));

    protected override void PerformLayout()
    {
        var intrinsicW = _width ?? _bitmap?.Width ?? 0f;
        var intrinsicH = _height ?? _bitmap?.Height ?? 0f;
        Size = Constraints.Constrain(
            new Vector2(
                intrinsicW,
                intrinsicH
            )
        );
    }

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (_bitmap == null || context.Canvas == null)
        {
            return;
        }

        var (srcRect, dstRect) = ComputeRects();
        context.Canvas.DrawImage(
            _bitmap,
            Vector2.Zero,
            Size,
            srcRect,
            dstRect,
            CornerRadii,
            BorderThickness,
            BorderColor,
            context.SharedPaint,
            context.SharedRoundRect,
            context.SharedBorderRoundRect
        );
    }

    /// <summary>
    ///     Computes the source (bitmap) and destination (widget-local) rects for the
    ///     current BoxFit mode and alignment. The canvas is assumed to be already
    ///     translated to this widget's local origin.
    /// </summary>
    internal (SKRect src, SKRect dst) ComputeRects()
    {
        float srcW = _bitmap!.Width;
        float srcH = _bitmap!.Height;
        var dstW = Size.X;
        var dstH = Size.Y;

        var fullSrc = new SKRect(
            0,
            0,
            srcW,
            srcH
        );
        var fullDst = new SKRect(
            0,
            0,
            dstW,
            dstH
        );

        if (srcW <= 0 || srcH <= 0 || dstW <= 0 || dstH <= 0)
        {
            return (fullSrc, fullDst);
        }

        switch (_fit)
        {
            case BoxFit.Fill:
                return (fullSrc, fullDst);

            case BoxFit.Contain:
            {
                var scale = Math.Min(
                    dstW / srcW,
                    dstH / srcH
                );
                return ContainRects(
                    srcW,
                    srcH,
                    dstW,
                    dstH,
                    scale,
                    fullSrc
                );
            }

            case BoxFit.Cover:
            {
                var scale = Math.Max(
                    dstW / srcW,
                    dstH / srcH
                );
                var cropW = dstW / scale;
                var cropH = dstH / scale;
                // Alignment maps [-1,1] → [0, available crop offset].
                var srcOffX = (srcW - cropW) * (_alignment.X + 1f) / 2f;
                var srcOffY = (srcH - cropH) * (_alignment.Y + 1f) / 2f;
                var srcRect = new SKRect(
                    srcOffX,
                    srcOffY,
                    srcOffX + cropW,
                    srcOffY + cropH
                );
                return (srcRect, fullDst);
            }

            case BoxFit.FitWidth:
            {
                var scale = dstW / srcW;
                return ContainRects(
                    srcW,
                    srcH,
                    dstW,
                    dstH,
                    scale,
                    fullSrc
                );
            }

            case BoxFit.FitHeight:
            {
                var scale = dstH / srcH;
                return ContainRects(
                    srcW,
                    srcH,
                    dstW,
                    dstH,
                    scale,
                    fullSrc
                );
            }

            case BoxFit.None:
                return ContainRects(
                    srcW,
                    srcH,
                    dstW,
                    dstH,
                    1f,
                    fullSrc
                );

            case BoxFit.ScaleDown:
            {
                var scale = Math.Min(
                    1f,
                    Math.Min(
                        dstW / srcW,
                        dstH / srcH
                    )
                );
                return ContainRects(
                    srcW,
                    srcH,
                    dstW,
                    dstH,
                    scale,
                    fullSrc
                );
            }

            default:
                return (fullSrc, fullDst);
        }
    }

    // Scales the source bitmap by `scale`, then uses Alignment to position the
    // scaled image within the widget bounds.
    private (SKRect src, SKRect dst) ContainRects(
        float srcW,
        float srcH,
        float dstW,
        float dstH,
        float scale,
        SKRect fullSrc
    )
    {
        var renderW = srcW * scale;
        var renderH = srcH * scale;
        var offset = _alignment.CalculateOffset(
            new Vector2(
                dstW,
                dstH
            ),
            new Vector2(
                renderW,
                renderH
            )
        );
        var dst = new SKRect(
            offset.X,
            offset.Y,
            offset.X + renderW,
            offset.Y + renderH
        );
        return (fullSrc, dst);
    }
}

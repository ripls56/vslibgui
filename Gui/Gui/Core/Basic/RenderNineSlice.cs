using System;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Basic;

/// <summary>
///     A render object that paints a bitmap using 9-slice (or 3-slice) scaling so
///     corners remain pixel-perfect while edges and the center stretch or tile.
///     Extends <see cref="RenderConstrainedBox" /> to support optional explicit
///     width and height constraints.
/// </summary>
public class RenderNineSlice : RenderBox
{
    private SKBitmap? _bitmap;
    private Vector2 _cachedSize;
    private ImageDrawMode _drawMode = ImageDrawMode.Sliced;
    private float? _maxHeight;
    private float? _maxWidth;
    private float? _minHeight;

    private float? _minWidth;
    private float _scale = 1f;
    private EdgeInsets _slice = EdgeInsets.Zero;

    // Cache the 9-slice rendering as an SKPicture to avoid 9 individual
    // DrawBitmap GPU calls every frame.
    private SKPicture? _sliceCache;
    private Vector4 _tint = Vector4.One;

    public SKBitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            if (_bitmap != value)
            {
                _bitmap = value;
                InvalidateSliceCache();
                MarkNeedsLayout();
            }
        }
    }

    public EdgeInsets Slice
    {
        get => _slice;
        set
        {
            if (_slice != value)
            {
                _slice = value;
                InvalidateSliceCache();
                MarkNeedsPaint();
            }
        }
    }

    public float Scale
    {
        get => _scale;
        set
        {
            if (Math.Abs(_scale - value) > 0.001f)
            {
                _scale = value;
                InvalidateSliceCache();
                MarkNeedsPaint();
            }
        }
    }

    public ImageDrawMode DrawMode
    {
        get => _drawMode;
        set
        {
            if (_drawMode != value)
            {
                _drawMode = value;
                InvalidateSliceCache();
                MarkNeedsPaint();
            }
        }
    }

    public Vector4 Tint
    {
        get => _tint;
        set
        {
            if (_tint != value)
            {
                _tint = value;
                InvalidateSliceCache();
                MarkNeedsPaint();
            }
        }
    }

    public float? MinWidth
    {
        get => _minWidth;
        set => SetProperty(ref _minWidth, value, relayout: true);
    }

    public float? MaxWidth
    {
        get => _maxWidth;
        set => SetProperty(ref _maxWidth, value, relayout: true);
    }

    public float? MinHeight
    {
        get => _minHeight;
        set => SetProperty(ref _minHeight, value, relayout: true);
    }

    public float? MaxHeight
    {
        get => _maxHeight;
        set => SetProperty(ref _maxHeight, value, relayout: true);
    }

    public override bool IsHitTestTarget =>
        HitTestBehavior == HitTestBehavior.Opaque ||
        (HitTestBehavior == HitTestBehavior.Defer &&
         (_bitmap != null || Color.W > 0 || BorderThickness > 0));

    private void InvalidateSliceCache()
    {
        _sliceCache?.Dispose();
        _sliceCache = null;
    }

    protected override void PerformLayout()
    {
        // Apply explicit width/height constraints if provided
        var additional = new LayoutConstraints(
            _minWidth ?? 0,
            _maxWidth ?? float.PositiveInfinity,
            _minHeight ?? 0,
            _maxHeight ?? float.PositiveInfinity
        );
        var innerConstraints = additional.Enforce(Constraints);

        if (Children.Count > 0)
        {
            var child = Children[0];
            child.X = 0;
            child.Y = 0;
            child.Layout(innerConstraints);
            Size = Constraints.Constrain(child.Size);
        }
        else
        {
            var intrinsicW = _bitmap?.Width ?? 0f;
            var intrinsicH = _bitmap?.Height ?? 0f;
            Size = Constraints.Constrain(
                innerConstraints.Constrain(
                    new Vector2(
                        intrinsicW,
                        intrinsicH
                    )
                )
            );
        }
    }

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (Size.X <= 0 || Size.Y <= 0 || context.Canvas == null)
        {
            return;
        }

        if (_bitmap != null)
        {
            // Invalidate cache if size changed (e.g. after layout).
            if (_sliceCache != null && _cachedSize != Size)
            {
                InvalidateSliceCache();
            }

            if (_sliceCache == null)
            {
                using var recorder = new SKPictureRecorder();
                var canvas = recorder.BeginRecording(
                    new SKRect(
                        0,
                        0,
                        Size.X,
                        Size.Y
                    )
                );
                canvas.DrawNineSlice(
                    Vector2.Zero,
                    Size,
                    _bitmap,
                    _slice,
                    _scale,
                    _drawMode,
                    _tint,
                    context.SharedPaint
                );
                _sliceCache = recorder.EndRecording();
                _cachedSize = Size;
            }

            context.Canvas.DrawPicture(_sliceCache);
        }

        if (Color.W > 0 || BorderThickness > 0)
        {
            context.Canvas.DrawBox(
                Vector2.Zero,
                Size,
                Color,
                CornerRadii,
                BorderThickness,
                BorderColor,
                context.SharedPaint,
                context.SharedRoundRect,
                context.SharedBorderRoundRect
            );
        }
    }

    public override void Dispose()
    {
        InvalidateSliceCache();
        base.Dispose();
    }
}

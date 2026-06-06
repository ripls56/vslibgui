using System;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Framework;

public class RenderBox : RenderObject
{
    [ThreadStatic] private static SKPoint[]? _shadowRadiiBuffer;
    private Gradient? _gradient;
    private Vector2? _preferredSize;
    private BoxShadow[]? _shadows;

    public Vector4 Color
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = Vector4.Zero;

    public Gradient? Gradient
    {
        get => _gradient;
        set
        {
            if (!ReferenceEquals(
                    _gradient,
                    value
                ))
            {
                _gradient = value;
                MarkNeedsPaint();
            }
        }
    }

    public HitTestBehavior HitTestBehavior
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
            }
        }
    } = HitTestBehavior.Defer;

    public SKBitmap? Texture
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public Vector4 CornerRadii
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = Vector4.Zero;

    public float BorderThickness
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    public Vector4 BorderColor
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = Vector4.One;

    /// <summary>
    ///     Preferred size to use when there are no children. Falls back to this size
    ///     if the widget has no child content. Matches old CustomPaint behavior.
    /// </summary>
    public Vector2? PreferredSize
    {
        get => _preferredSize;
        set => SetProperty(
            ref _preferredSize,
            value,
            relayout: true
        );
    }

    /// <summary>
    ///     Ordered list of box shadows. Outer shadows are drawn before the fill;
    ///     inner shadows after the fill. Null or empty = no shadows.
    /// </summary>
    public BoxShadow[]? Shadows
    {
        get => _shadows;
        set
        {
            if (ReferenceEquals(
                    _shadows,
                    value
                ))
            {
                return;
            }

            _shadows = value;
            MarkNeedsPaint();
        }
    }

    public override bool IsHitTestTarget =>
        HitTestBehavior == HitTestBehavior.Opaque ||
        (HitTestBehavior == HitTestBehavior.Defer &&
         (Color.W > 0 || BorderThickness > 0 || Texture != null || Children.Count > 0));

    protected override void PerformLayout()
    {
        float maxW = 0;
        float maxH = 0;

        if (Children.Count > 0)
        {
            var childConstraints = LayoutConstraints.Loose(
                Constraints.MaxWidth,
                Constraints.MaxHeight
            );
            foreach (var child in Children)
            {
                child.X = 0;
                child.Y = 0;
                child.Layout(childConstraints);
                maxW = Math.Max(
                    maxW,
                    child.Size.X
                );
                maxH = Math.Max(
                    maxH,
                    child.Size.Y
                );
            }

            // If we have a preferred size and children are 0x0, use preferred size
            // This handles Container(Width, Height) where _BoxWidget has preferredSize set
            // but the child (empty SizedBox) returns 0
            if (_preferredSize.HasValue && maxW == 0 && maxH == 0)
            {
                maxW = _preferredSize.Value.X;
                maxH = _preferredSize.Value.Y;
            }
        }
        else if (_preferredSize.HasValue)
        {
            // Use preferred size when there are no children and preferredSize is set
            maxW = _preferredSize.Value.X;
            maxH = _preferredSize.Value.Y;
        }

        Size = Constraints.Constrain(
            new Vector2(
                maxW,
                maxH
            )
        );
    }

    /// <summary>
    ///     Custom paint flow that draws outer shadows BEFORE applying the optional
    ///     <see cref="RenderObject.ClipBehavior" /> clip, so shadows can extend outside
    ///     the box bounds even when children are clipped to the rounded box shape.
    ///     The clip respects <see cref="CornerRadii" /> (rounded-rect, not plain rect).
    /// </summary>
    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        MarkPainted();
        RecordPaintEvent(context);

        using (context.Canvas.SaveScope())
        {
            PaintOuterShadows(context);

            if (ClipBehavior != ClipBehavior.None)
            {
                ApplyRoundedContentClip(context);
            }

            PaintInternal(context);

            if (!IsRepaintBoundary)
            {
                NeedsPaint = false;
            }

            PaintChildren(context);
        }

        ChildNeedsPaint = false;
    }

    private void PaintOuterShadows(
        PaintingContext context
    )
    {
        if (_shadows == null)
        {
            return;
        }

        foreach (var shadow in _shadows)
        {
            if (!shadow.Inset)
            {
                PaintOuterShadow(
                    context,
                    shadow
                );
            }
        }
    }

    private void ApplyRoundedContentClip(
        PaintingContext context
    )
    {
        var canvas = context.Canvas!;
        var rect = new SKRect(
            0,
            0,
            Size.X,
            Size.Y
        );
        var rr = context.SharedRoundRect;
        rr.SetRectRadii(
            rect,
            GetShadowRadii(
                CornerRadii.Z,
                CornerRadii.X,
                CornerRadii.Y,
                CornerRadii.W
            )
        );
        canvas.ClipRoundRect(
            rr,
            SKClipOperation.Intersect,
            ClipBehavior == ClipBehavior.AntiAlias
        );
    }

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        // Outer shadows are painted in Paint(), BEFORE the clip is applied,
        // so they can extend outside the box bounds.

        // 2. Fill (existing logic)
        var shader = _gradient?.CreateShader(Size);
        try
        {
            if (Texture != null)
            {
                context.Canvas.DrawMaskedBox(
                    Vector2.Zero,
                    Size,
                    Texture,
                    CornerRadii,
                    BorderThickness,
                    BorderColor,
                    context.SharedPaint,
                    context.SharedRoundRect,
                    context.SharedBorderRoundRect
                );
            }
            else if (Color.W > 0 || BorderThickness > 0 || shader != null)
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
                    context.SharedBorderRoundRect,
                    shader
                );
            }
        }
        finally
        {
            shader?.Dispose();
        }

        // 3. Inner shadows (after fill, before children)
        if (_shadows != null)
        {
            foreach (var shadow in _shadows)
            {
                if (shadow.Inset)
                {
                    PaintInnerShadow(
                        context,
                        shadow
                    );
                }
            }
        }
    }

    private static SKPoint[] GetShadowRadii(
        float tl,
        float tr,
        float br,
        float bl
    )
    {
        _shadowRadiiBuffer ??= new SKPoint[4];
        _shadowRadiiBuffer[0] = new SKPoint(
            tl,
            tl
        );
        _shadowRadiiBuffer[1] = new SKPoint(
            tr,
            tr
        );
        _shadowRadiiBuffer[2] = new SKPoint(
            br,
            br
        );
        _shadowRadiiBuffer[3] = new SKPoint(
            bl,
            bl
        );
        return _shadowRadiiBuffer;
    }

    private void PaintOuterShadow(
        PaintingContext context,
        BoxShadow shadow
    )
    {
        var canvas = context.Canvas!;
        var spread = shadow.SpreadRadius;
        var sigma = MathF.Max(
            0f,
            shadow.BlurRadius
        );

        // Shadow shape: expanded by spread and shifted by offset.
        // The offset is baked into the rect so we don't need SKImageFilter
        // (ImageFilter + Dispose is unreliable on SKPictureRecorder canvases).
        var shadowRect = new SKRect(
            -spread + shadow.Offset.X,
            -spread + shadow.Offset.Y,
            Size.X + spread + shadow.Offset.X,
            Size.Y + spread + shadow.Offset.Y
        );

        // Corner radii grow with positive spread
        var tl = MathF.Max(
            0f,
            CornerRadii.Z + spread
        );
        var tr = MathF.Max(
            0f,
            CornerRadii.X + spread
        );
        var br = MathF.Max(
            0f,
            CornerRadii.Y + spread
        );
        var bl = MathF.Max(
            0f,
            CornerRadii.W + spread
        );

        var shadowRr = context.SharedShadowRoundRect;
        shadowRr.SetRectRadii(
            shadowRect,
            GetShadowRadii(
                tl,
                tr,
                br,
                bl
            )
        );

        // Draw the shadow shape directly with blur MaskFilter.
        // MaskFilter is cached in PaintingContext (not disposed per-frame),
        // so it survives SKPicture recording and replay.
        var paint = context.SharedPaint;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = shadow.ToSkColor();
        paint.Shader = null;
        paint.ImageFilter = null;
        paint.BlendMode = SKBlendMode.SrcOver;
        paint.IsAntialias = true;
        paint.MaskFilter = sigma > 0f
            ? context.GetOrCreateBlurMask(sigma)
            : null;

        canvas.DrawRoundRect(
            shadowRr,
            paint
        );

        paint.MaskFilter = null;
    }

    private void PaintInnerShadow(
        PaintingContext context,
        BoxShadow shadow
    )
    {
        var canvas = context.Canvas!;
        var spread = shadow.SpreadRadius;
        var sigma = MathF.Max(
            0f,
            shadow.BlurRadius
        );

        var boxRect = new SKRect(
            0,
            0,
            Size.X,
            Size.Y
        );

        using (canvas.SaveScope())
        {
            // Clip to box boundary
            var clipRr = context.SharedRoundRect;
            clipRr.SetRectRadii(
                boxRect,
                GetShadowRadii(
                    CornerRadii.Z,
                    CornerRadii.X,
                    CornerRadii.Y,
                    CornerRadii.W
                )
            );
            canvas.ClipRoundRect(
                clipRr,
                SKClipOperation.Intersect,
                true
            );

            // Inner rect (the "hole") — spread contracts it, offset shifts it
            var innerRect = new SKRect(
                spread + shadow.Offset.X,
                spread + shadow.Offset.Y,
                Size.X - spread + shadow.Offset.X,
                Size.Y - spread + shadow.Offset.Y
            );

            var tl = MathF.Max(
                0f,
                CornerRadii.Z - spread
            );
            var tr = MathF.Max(
                0f,
                CornerRadii.X - spread
            );
            var br = MathF.Max(
                0f,
                CornerRadii.Y - spread
            );
            var bl = MathF.Max(
                0f,
                CornerRadii.W - spread
            );

            // SaveLayer with blur filter applied on compositing.
            // SharedPaint is safe here: SaveLayer copies the paint state at call time.
            var fill = context.SharedPaint;
            fill.IsAntialias = true;
            fill.Shader = null;
            fill.MaskFilter = null;
            fill.Style = SKPaintStyle.Fill;
            fill.BlendMode = SKBlendMode.SrcOver;
            fill.ImageFilter = sigma > 0f ? context.GetOrCreateBlurImage(sigma) : null;
            canvas.SaveLayer(boxRect, fill);
            fill.ImageFilter = null;

            // Flood layer with shadow color
            fill.Style = SKPaintStyle.Fill;
            fill.Color = shadow.ToSkColor();
            fill.Shader = null;
            fill.MaskFilter = null;
            fill.ImageFilter = null;
            fill.BlendMode = SKBlendMode.SrcOver;
            fill.IsAntialias = false;
            canvas.DrawRect(
                boxRect,
                fill
            );

            // Punch transparent hole using DstOut
            fill.Color = SKColors.Black;
            fill.BlendMode = SKBlendMode.DstOut;
            fill.IsAntialias = true;

            var holeRr = context.SharedShadowRoundRect;
            holeRr.SetRectRadii(
                innerRect,
                GetShadowRadii(
                    tl,
                    tr,
                    br,
                    bl
                )
            );
            canvas.DrawRoundRect(
                holeRr,
                fill
            );

            canvas.Restore(); // layer

            fill.BlendMode = SKBlendMode.SrcOver;
            fill.IsAntialias = true;
        }
    }
}

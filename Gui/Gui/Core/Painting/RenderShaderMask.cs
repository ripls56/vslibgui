using System;
using Gui.Core.Framework;
using Gui.Rendering;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     Render object that paints a shader overlay on top of its child content.
///     Overrides <see cref="RenderObject.Paint" /> (not PaintInternal) so the
///     overlay is drawn after children.
/// </summary>
internal class RenderShaderMask : RenderProxyBox
{
    private SKBlendMode _blendMode = SKBlendMode.SrcOver;
    private float _offsetX;
    private float _offsetY;
    private SKShader? _shader;

    /// <summary>The shader to overlay. Not owned by this render object.</summary>
    public SKShader? Shader
    {
        get => _shader;
        set
        {
            if (_shader == value)
            {
                return;
            }

            _shader = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Blend mode for the shader overlay.</summary>
    public SKBlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode == value)
            {
                return;
            }

            _blendMode = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Horizontal shader offset in pixels.</summary>
    public float OffsetX
    {
        get => _offsetX;
        set
        {
            _offsetX = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Vertical shader offset in pixels.</summary>
    public float OffsetY
    {
        get => _offsetY;
        set
        {
            _offsetY = value;
            MarkNeedsPaint();
        }
    }

    /// <inheritdoc />
    public override void Paint(
        PaintingContext context
    )
    {
        base.Paint(context);

        if (_shader == null || context.Canvas == null)
        {
            return;
        }

        if (Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }

        context.Canvas.Save();
        context.Canvas.ClipRect(
            new SKRect(
                0,
                0,
                Size.X,
                Size.Y
            ),
            SKClipOperation.Intersect,
            true
        );
        context.Canvas.Translate(
            -_offsetX,
            -_offsetY
        );

        var paint = context.SharedPaint;
        paint.Shader = _shader;
        paint.BlendMode = _blendMode;
        paint.IsAntialias = true;
        context.Canvas.DrawRect(
            0,
            0,
            Size.X + MathF.Abs(_offsetX) + 200f,
            Size.Y + MathF.Abs(_offsetY) + 200f,
            paint
        );
        paint.Shader = null;
        paint.BlendMode = SKBlendMode.SrcOver;
        paint.Reset();

        context.Canvas.Restore();
    }
}

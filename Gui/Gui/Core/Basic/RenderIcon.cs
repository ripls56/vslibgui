using System;
using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;
using Svg.Skia;

namespace Gui.Core.Basic;

public class RenderIcon : RenderObject
{
    private SKBlendMode _blendMode = SKBlendMode.SrcIn;

    private SKColorFilter? _cachedTintFilter;
    private Vector4 _cachedTintKey = new(-1f);
    private float _iconSize = 24f;
    private SKSvg? _svgData;

    private Vector4 _tintColor = new(
        0f,
        0f,
        0f,
        1f
    );

    internal string? SourceDomain { get; set; }
    internal string? SourcePath { get; set; }

    public SKSvg? SvgData
    {
        get => _svgData;
        set => SetProperty(
            ref _svgData,
            value,
            relayout: true
        );
    }

    public float IconSize
    {
        get => _iconSize;
        set => SetProperty(
            ref _iconSize,
            value,
            relayout: true
        );
    }

    public Vector4 TintColor
    {
        get => _tintColor;
        set => SetProperty(
            ref _tintColor,
            value,
            true
        );
    }


    public SKBlendMode BlendMode
    {
        get => _blendMode;
        set => SetProperty(
            ref _blendMode,
            value,
            true
        );
    }

    public override bool IsHitTestTarget => false;

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(
            new Vector2(
                _iconSize,
                _iconSize
            )
        );
    }

    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (_svgData == null || context.Canvas == null)
        {
            return;
        }

        if (_svgData.Picture == null)
        {
            return;
        }

        var canvas = context.Canvas;
        var paint = context.SharedPaint;

        var canvasMin = Math.Min(
            Size.X,
            Size.Y
        );
        var svgMax = Math.Max(
            _svgData.Picture.CullRect.Width,
            _svgData.Picture.CullRect.Height
        );
        var scale = canvasMin / svgMax;
        var matrix = SKMatrix.CreateScale(
            scale,
            scale
        );
        if (_tintColor != _cachedTintKey)
        {
            _cachedTintFilter?.Dispose();
            _cachedTintFilter = SKColorFilter.CreateBlendMode(
                _tintColor.ToSkColor(),
                _blendMode
            );
            _cachedTintKey = _tintColor;
        }


        paint.Style = SKPaintStyle.Stroke;
        paint.ColorFilter = _cachedTintFilter;
        paint.Color = SKColors.White;
        try
        {
            using (canvas.SaveScope())
            {
                canvas.DrawPicture(
                    _svgData.Picture,
                    matrix,
                    paint
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            paint.ColorFilter = null;
        }
    }

    public override void Dispose()
    {
        _cachedTintFilter?.Dispose();
        _cachedTintFilter = null;
        base.Dispose();
    }
}

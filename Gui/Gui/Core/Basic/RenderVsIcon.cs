using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Basic;

/// <summary>
///     Leaf render object that paints a VS built-in icon obtained from
///     <see cref="VsIconTextureCache" /> as a GPU-resident <see cref="SKImage" />.
///     Supports tinting via <see cref="SKColorFilter.CreateBlendMode" /> identically
///     to <see cref="RenderIcon" />.
/// </summary>
public class RenderVsIcon : RenderObject
{
    private SKBlendMode _blendMode = SKBlendMode.SrcIn;
    private SKColorFilter? _cachedTintFilter;
    private Vector4 _cachedTintKey = new(-1f);
    private string? _iconName;
    private float _iconSize = 24f;
    private Vector4 _tintColor = new(0f, 0f, 0f, 1f);

    /// <summary>Gets or sets the VS icon name (key into <see cref="VsIconTextureCache" />).</summary>
    public string? IconName
    {
        get => _iconName;
        set => SetProperty(ref _iconName, value, relayout: true);
    }

    /// <summary>Gets or sets the rendered size in pixels.</summary>
    public float IconSize
    {
        get => _iconSize;
        set => SetProperty(ref _iconSize, value, relayout: true);
    }

    /// <summary>Gets or sets the tint color applied via <see cref="SKBlendMode.SrcIn" />.</summary>
    public Vector4 TintColor
    {
        get => _tintColor;
        set => SetProperty(ref _tintColor, value, true);
    }

    /// <summary>Gets or sets the blend mode used for tinting.</summary>
    public SKBlendMode BlendMode
    {
        get => _blendMode;
        set => SetProperty(ref _blendMode, value, true);
    }

    /// <inheritdoc />
    public override bool IsHitTestTarget => false;

    /// <inheritdoc />
    protected override void PerformLayout() =>
        Size = Constraints.Constrain(new Vector2(_iconSize, _iconSize));

    /// <inheritdoc />
    protected override void PaintInternal(PaintingContext context)
    {
        if (_iconName == null || context.Canvas == null)
        {
            return;
        }

        var modSystem = GuiModSystem.Instance;
        var grContext = modSystem?.SkiaRenderer?.GrContext;
        var cache = modSystem?.VsIconTextureCache;
        if (grContext == null || cache == null)
        {
            return;
        }

        var image = cache.Get(_iconName, (int)_iconSize, grContext);
        if (image == null)
        {
            return;
        }

        if (_tintColor != _cachedTintKey)
        {
            _cachedTintFilter?.Dispose();
            _cachedTintFilter = SKColorFilter.CreateBlendMode(
                _tintColor.ToSkColor(),
                _blendMode
            );
            _cachedTintKey = _tintColor;
        }

        var paint = context.SharedPaint;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColors.White;
        paint.ColorFilter = _cachedTintFilter;
        paint.Shader = null;
        paint.ImageFilter = null;

        var dst = new SKRect(0, 0, _iconSize, _iconSize);
        try
        {
            context.Canvas.DrawImage(image, dst, new SKSamplingOptions(SKFilterMode.Linear), paint);
        }
        finally
        {
            paint.ColorFilter = null;
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _cachedTintFilter?.Dispose();
        _cachedTintFilter = null;
        base.Dispose();
    }
}

using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     Overlay that draws a glowing rounded-rectangle border whose brightest point follows
///     the pointer. The glow is a radial gradient centred on the cursor's local position,
///     fading out toward the rest of the edge, producing a "spotlight" that tracks the
///     mouse. Renders nothing while <see cref="Intensity" /> is zero.
///     <para>
///         The pointer position is delivered through a <see cref="ValueNotifier{T}" /> so that
///         mouse movement only marks the render object for repaint, never rebuilding the
///         widget subtree. Use <see cref="SpotlightHover" /> for a self-contained wrapper.
///     </para>
/// </summary>
public sealed class SpotlightBorder : RenderObjectWidget
{
    /// <summary>Creates a spotlight border overlay.</summary>
    /// <param name="intensity">Glow strength, 0–1 (typically a hover animation value).</param>
    /// <param name="glowColor">Glow RGBA; its alpha scales the peak opacity.</param>
    /// <param name="pointerPosition">
    ///     Notifier carrying the pointer position in global coordinates, or null to centre
    ///     the glow.
    /// </param>
    /// <param name="cornerRadius">Corner radius of the border in pixels.</param>
    public SpotlightBorder(
        float intensity,
        Vector4 glowColor,
        ValueNotifier<Vector2?>? pointerPosition,
        float cornerRadius = 2f
    )
    {
        Intensity = intensity;
        GlowColor = glowColor;
        PointerPosition = pointerPosition;
        CornerRadius = cornerRadius;
    }

    /// <summary>Glow strength in the range 0–1.</summary>
    public float Intensity { get; }

    /// <summary>Glow color as RGBA (each channel 0–1); alpha scales peak opacity.</summary>
    public Vector4 GlowColor { get; }

    /// <summary>Notifier carrying the pointer position in global coordinates.</summary>
    public ValueNotifier<Vector2?>? PointerPosition { get; }

    /// <summary>Corner radius of the border in pixels.</summary>
    public float CornerRadius { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderSpotlightBorder
        {
            Intensity = Intensity,
            GlowColor = GlowColor,
            CornerRadius = CornerRadius,
            PointerPosition = PointerPosition
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(RenderObject renderObject)
    {
        var ro = (RenderSpotlightBorder)renderObject;
        ro.Intensity = Intensity;
        ro.GlowColor = GlowColor;
        ro.CornerRadius = CornerRadius;
        ro.PointerPosition = PointerPosition;
    }
}

/// <summary>
///     Render object backing <see cref="SpotlightBorder" />. Strokes a rounded rectangle and
///     fills it with a radial gradient shader anchored at the pointer's local position. The
///     shader is cached and rebuilt only when its inputs change, so a hovered-but-stationary
///     target allocates nothing.
/// </summary>
internal sealed class RenderSpotlightBorder : RenderBox
{
    private const float FillOpacityFactor = 0.1f;
    private const float GradientRadiusFactor = 1.25f;

    private readonly SKColor[] _gradientColors = new SKColor[3];
    private readonly float[] _gradientStops = [0f, 0.45f, 1f];

    private float _cornerRadius = 2f;
    private Vector4 _glowColor = Vector4.One;
    private float _intensity;
    private ValueNotifier<Vector2?>? _pointerPosition;

    private SKShader? _shader;
    private SKPoint _shaderCenter = new(float.NaN, float.NaN);
    private byte _shaderPeak;
    private float _shaderRadius = -1f;
    private int _shaderRgb = -1;

    /// <summary>Glow strength in the range 0–1. Zero paints nothing.</summary>
    public float Intensity
    {
        get => _intensity;
        set => SetProperty(ref _intensity, value, true);
    }

    /// <summary>Glow color as RGBA; alpha scales peak opacity.</summary>
    public Vector4 GlowColor
    {
        get => _glowColor;
        set => SetProperty(ref _glowColor, value, true);
    }

    /// <summary>Corner radius of the border in pixels.</summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set => SetProperty(ref _cornerRadius, value, true);
    }

    /// <summary>
    ///     Notifier carrying the pointer position in global coordinates. The render object
    ///     subscribes to it and repaints on change without a widget rebuild.
    /// </summary>
    public ValueNotifier<Vector2?>? PointerPosition
    {
        get => _pointerPosition;
        set
        {
            if (ReferenceEquals(_pointerPosition, value))
            {
                return;
            }

            _pointerPosition?.RemoveListener(OnPointerChanged);
            _pointerPosition = value;
            _pointerPosition?.AddListener(OnPointerChanged);
            MarkNeedsPaint();
        }
    }

    /// <inheritdoc />
    public override bool IsHitTestTarget => false;

    private void OnPointerChanged() => MarkNeedsPaint();

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(
            new Vector2(Constraints.MaxWidth, Constraints.MaxHeight)
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(PaintingContext context)
    {
        if (context.Canvas == null || _intensity <= 0f || Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        var strokeW = MathF.Max(1.5f, Size.X * 0.045f);
        var inset = strokeW * 0.5f;
        var rect = new SKRect(inset, inset, Size.X - inset, Size.Y - inset);

        var shader = ResolveShader();
        var paint = context.SharedPaint;
        paint.ImageFilter = null;
        paint.Shader = shader;

        paint.Style = SKPaintStyle.Fill;
        paint.Color = new SKColor(255, 255, 255, (byte)(FillOpacityFactor * 255f));
        context.Canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, paint);

        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = strokeW;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = SKColors.White;
        context.Canvas.DrawRoundRect(rect, _cornerRadius, _cornerRadius, paint);

        paint.Shader = null;
    }

    /// <summary>
    ///     Returns the cached radial gradient, rebuilding it only when the centre, peak
    ///     opacity, color, or radius have changed since the last paint.
    /// </summary>
    private SKShader ResolveShader()
    {
        var center = LocalGlowCenter();
        var radius = MathF.Max(1f, Size.X * GradientRadiusFactor);
        var peak = (byte)Math.Clamp(_glowColor.W * _intensity * 255f, 0f, 255f);
        var r = (byte)(_glowColor.X * 255f);
        var g = (byte)(_glowColor.Y * 255f);
        var b = (byte)(_glowColor.Z * 255f);
        var rgb = (r << 16) | (g << 8) | b;

        if (_shader != null
            && _shaderCenter == center
            && _shaderPeak == peak
            && _shaderRgb == rgb
            && _shaderRadius.Equals(radius))
        {
            return _shader;
        }

        _shader?.Dispose();

        _gradientColors[0] = new SKColor(r, g, b, peak);
        _gradientColors[1] = new SKColor(r, g, b, (byte)(peak * 0.3f));
        _gradientColors[2] = new SKColor(r, g, b, 0);
        _shader = SKShader.CreateRadialGradient(
            center,
            radius,
            _gradientColors,
            _gradientStops,
            SKShaderTileMode.Clamp
        );

        _shaderCenter = center;
        _shaderPeak = peak;
        _shaderRgb = rgb;
        _shaderRadius = radius;
        return _shader;
    }

    /// <summary>
    ///     Converts the pointer's global position to a local glow centre, quantised to whole
    ///     pixels and clamped to the bounds, falling back to the centre when no pointer
    ///     position is available.
    /// </summary>
    private SKPoint LocalGlowCenter()
    {
        if (_pointerPosition?.Value is not { } global)
        {
            return new SKPoint(Size.X * 0.5f, Size.Y * 0.5f);
        }

        var local = GlobalToLocal(global);
        return new SKPoint(
            MathF.Round(Math.Clamp(local.X, 0f, Size.X)),
            MathF.Round(Math.Clamp(local.Y, 0f, Size.Y))
        );
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _pointerPosition?.RemoveListener(OnPointerChanged);
        _shader?.Dispose();
        _shader = null;
        base.Dispose();
    }
}

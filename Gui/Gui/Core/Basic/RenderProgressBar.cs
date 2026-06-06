using System;
using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;

namespace Gui.Core.Basic;

/// <summary>
///     Render object for a progress bar. Draws a track background, a fill bar,
///     and an optional border.
/// </summary>
internal class RenderProgressBar : RenderProxyBox
{
    private float _cornerRadius = 5;
    private Vector4 _fillColor;
    private float _fillPadding;
    private Vector4 _trackColor;
    private float _trackHeight = 10;
    private float _value;

    /// <summary>Progress value (0..1). Clamped on set.</summary>
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(
                value,
                0f,
                1f
            );
            if (MathF.Abs(_value - clamped) > 0.0001f)
            {
                _value = clamped;
                MarkNeedsPaint();
            }
        }
    }

    /// <summary>Height of the track in pixels.</summary>
    public float TrackHeight
    {
        get => _trackHeight;
        set => SetProperty(
            ref _trackHeight,
            value,
            relayout: true
        );
    }

    /// <summary>Corner radius for track and fill.</summary>
    public float CornerRadius
    {
        get => _cornerRadius;
        set => SetProperty(
            ref _cornerRadius,
            value,
            true
        );
    }

    /// <summary>Inset in pixels applied inside the track around the fill bar.</summary>
    public float FillPadding
    {
        get => _fillPadding;
        set => SetProperty(
            ref _fillPadding,
            value,
            true
        );
    }

    /// <summary>Color of the filled portion.</summary>
    public Vector4 FillColor
    {
        get => _fillColor;
        set => SetProperty(
            ref _fillColor,
            value,
            true
        );
    }

    /// <summary>Background color of the track.</summary>
    public Vector4 TrackColor
    {
        get => _trackColor;
        set => SetProperty(
            ref _trackColor,
            value,
            true
        );
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        var width = Constraints.MaxWidth;
        var height = float.IsPositiveInfinity(Constraints.MaxHeight)
            ? _trackHeight
            : Math.Min(
                Constraints.MaxHeight,
                _trackHeight
            );
        Size = new Vector2(
            width,
            height
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(
        PaintingContext context
    )
    {
        var radii = new Vector4(_cornerRadius);

        context.DrawBox(
            Vector2.Zero,
            Size,
            _trackColor,
            radii,
            BorderThickness,
            BorderColor
        );

        var p = _fillPadding;
        var availWidth = Size.X - p * 2;
        var fillWidth = _value * availWidth;
        if (fillWidth > 0 && availWidth > 0)
        {
            var fillHeight = Math.Max(
                0,
                Size.Y - p * 2
            );
            var fillRadius = Math.Max(
                0,
                _cornerRadius - p
            );
            context.DrawBox(
                new Vector2(
                    p,
                    p
                ),
                new Vector2(
                    fillWidth,
                    fillHeight
                ),
                _fillColor,
                new Vector4(fillRadius),
                0,
                Vector4.Zero
            );
        }
    }
}

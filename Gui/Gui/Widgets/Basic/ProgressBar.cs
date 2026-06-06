using System;
using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     A horizontal progress bar that displays a value between 0 and 1.
///     The bar consists of a background track, a fill portion, and a border.
///     Colors default to the current <see cref="Theme" />.
/// </summary>
public class ProgressBar : StatelessWidget
{
    /// <summary>Creates a new progress bar.</summary>
    public ProgressBar(
        float value,
        ProgressBarStyle? style = null,
        Framework.Key? key = null
    )
        : base(key)
    {
        Value = Math.Clamp(
            value,
            0f,
            1f
        );
        Style = style;
    }

    /// <summary>Progress value, clamped to 0..1.</summary>
    public float Value { get; }

    /// <summary>
    ///     Optional style override. When null, the style is read from
    ///     <see cref="ThemeData.ProgressBarStyle" />.
    /// </summary>
    public ProgressBarStyle? Style { get; }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        var style = Style ?? Theme.Of(context).ProgressBarStyle;
        return new ProgressBarTrackWidget(
            Value,
            style.Height,
            style.CornerRadius,
            style.BorderThickness,
            style.FillColor,
            style.TrackColor,
            style.BorderColor,
            style.FillPadding
        );
    }
}

internal class ProgressBarTrackWidget : SingleChildWidget
{
    public ProgressBarTrackWidget(
        float value,
        float trackHeight,
        float cornerRadius,
        float borderThickness,
        Vector4 fillColor,
        Vector4 trackColor,
        Vector4 borderColor,
        float fillPadding = 0f
    )
    {
        Value = value;
        TrackHeight = trackHeight;
        CornerRadius = cornerRadius;
        BorderThickness = borderThickness;
        FillColor = fillColor;
        TrackColor = trackColor;
        BorderColor = borderColor;
        FillPadding = fillPadding;
    }

    public float Value { get; }
    public float TrackHeight { get; }
    public float CornerRadius { get; }
    public float BorderThickness { get; }
    public Vector4 FillColor { get; }
    public Vector4 TrackColor { get; }
    public Vector4 BorderColor { get; }
    public float FillPadding { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderProgressBar
        {
            Value = Value,
            TrackHeight = TrackHeight,
            CornerRadius = CornerRadius,
            BorderThickness = BorderThickness,
            FillColor = FillColor,
            TrackColor = TrackColor,
            BorderColor = BorderColor,
            FillPadding = FillPadding
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderProgressBar)renderObject;
        ro.Value = Value;
        ro.TrackHeight = TrackHeight;
        ro.CornerRadius = CornerRadius;
        ro.BorderThickness = BorderThickness;
        ro.FillColor = FillColor;
        ro.TrackColor = TrackColor;
        ro.BorderColor = BorderColor;
        ro.FillPadding = FillPadding;
    }
}

/// <summary>
///     Render object for a progress bar. Draws a track background, a fill bar,
///     and an optional border.

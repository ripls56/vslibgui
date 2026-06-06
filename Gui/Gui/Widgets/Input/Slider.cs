using System;
using Gui.Core.Framework;
using Gui.Core.Input;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;

namespace Gui.Widgets.Input;

/// <summary>
///     A horizontal slider widget that lets the user select a value from a range by
///     dragging a thumb along a track. Supports continuous and discrete (stepped) modes.
/// </summary>
/// <example>
///     Continuous:
///     <code>
/// new Slider(
///   value: _continuousValue,
///   onChanged: (v) => SetState(() => _continuousValue = v
/// )
/// </code>
///     Discrete:
///     <code>
/// new Slider(
///   value: _volume,
///   onChanged: v => SetState(() => _volume = v),
///   min: 0, max: 100, divisions: 10,
///   label: "Volume"
/// )
/// </code>
/// </example>
public class Slider : StatefulWidget
{
    /// <summary>Creates a new slider widget.</summary>
    public Slider(
        float value,
        Action<float>? onChanged = null,
        Action<float>? onChangeEnd = null,
        float min = 0,
        float max = 1,
        int? divisions = null,
        string? label = null,
        float? trackHeight = null,
        float? thumbWidth = null,
        float? thumbHeight = null,
        float? thumbRadius = null,
        Vector4? activeColor = null,
        Vector4? inactiveColor = null,
        Vector4? thumbColor = null,
        bool showValueLabel = true,
        Func<float, Widget>? valueLabelBuilder = null,
        SliderStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Value = Math.Clamp(
            value,
            min,
            max
        );
        OnChanged = onChanged;
        OnChangeEnd = onChangeEnd;
        Min = min;
        Max = max;
        Divisions = divisions;
        Label = label;
        TrackHeight = trackHeight;
        ThumbWidth = thumbWidth;
        ThumbHeight = thumbHeight;
        ThumbRadius = thumbRadius;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        ThumbColor = thumbColor;
        ShowValueLabel = showValueLabel;
        ValueLabelBuilder = valueLabelBuilder;
        Style = style;
    }

    /// <summary>Current value of the slider. Must be between <see cref="Min" /> and <see cref="Max" />.</summary>
    public float Value { get; }

    /// <summary>Called continuously as the user drags the thumb.</summary>
    public Action<float>? OnChanged { get; }

    /// <summary>Called once when the user releases the thumb after dragging.</summary>
    public Action<float>? OnChangeEnd { get; }

    /// <summary>Minimum value of the range. Defaults to 0.</summary>
    public float Min { get; }

    /// <summary>Maximum value of the range. Defaults to 1.</summary>
    public float Max { get; }

    /// <summary>
    ///     Number of discrete divisions. When set, the slider snaps to evenly spaced steps.
    ///     When null (default), the slider is continuous.
    /// </summary>
    public int? Divisions { get; }

    /// <summary>Optional text label displayed to the left of the slider.</summary>
    public string? Label { get; }

    /// <summary>
    ///     Height of the track in pixels. When null, falls back to
    ///     <see cref="SliderStyle.TrackHeight" />.
    /// </summary>
    public float? TrackHeight { get; }

    /// <summary>
    ///     Width of the thumb in pixels. When null, falls back to
    ///     <see cref="SliderStyle.ThumbWidth" />.
    /// </summary>
    public float? ThumbWidth { get; }

    /// <summary>
    ///     Height of the thumb in pixels. When null, falls back to
    ///     <see cref="SliderStyle.ThumbHeight" />.
    /// </summary>
    public float? ThumbHeight { get; }

    /// <summary>
    ///     Corner radius of the thumb. When null, falls back to
    ///     <see cref="SliderStyle.ThumbRadius" />.
    /// </summary>
    public float? ThumbRadius { get; }

    /// <summary>Color of the filled (active) portion of the track. Uses theme primary if null.</summary>
    public Vector4? ActiveColor { get; }

    /// <summary>Color of the unfilled (inactive) portion of the track. Uses theme surface if null.</summary>
    public Vector4? InactiveColor { get; }

    /// <summary>Color of the thumb. Uses theme primary if null.</summary>
    public Vector4? ThumbColor { get; }

    /// <summary>
    ///     Whether to show a tooltip with the current value on hover/drag.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool ShowValueLabel { get; }

    /// <summary>
    ///     Optional builder that returns a custom widget for the value tooltip content.
    ///     Receives the current slider value. When null, a default <see cref="Text" />
    ///     widget with the formatted value is used.
    /// </summary>
    public Func<float, Widget>? ValueLabelBuilder { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.SliderStyle" />.
    ///     Per-field color params (<see cref="ActiveColor" /> etc.) override the
    ///     style's corresponding field when non-null.
    /// </summary>
    public SliderStyle? Style { get; }

    /// <inheritdoc />
    public override State CreateState() => new SliderState();
}

internal class SliderState : State<Slider>
{
    private float _currentValue;
    private bool _isDragging;
    private bool _isHovered;
    private float _thumbWidth;
    private RenderObject? _trackRenderObject;
    private float _trackWidth;

    public override void InitState()
    {
        base.InitState();
        _currentValue = Widget.Value;
    }

    public override void UpdateWidget(
        Slider oldWidget
    )
    {
        base.UpdateWidget(oldWidget);
        if (!_isDragging)
        {
            _currentValue = Widget.Value;
        }
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var resolved = Widget.Style ?? Theme.Of(context).SliderStyle;
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var activeColor = Widget.ActiveColor ?? resolved.ActiveColor;
        var inactiveColor = Widget.InactiveColor ?? resolved.InactiveColor;
        var thumbColor = Widget.ThumbColor ?? resolved.ThumbColor;
        var trackHeight = Widget.TrackHeight ?? resolved.TrackHeight;
        var thumbWidth = Widget.ThumbWidth ?? resolved.ThumbWidth;
        var thumbHeight = Widget.ThumbHeight ?? resolved.ThumbHeight;
        var thumbRadius = Widget.ThumbRadius ?? resolved.ThumbRadius;
        _thumbWidth = thumbWidth;

        var ratio = Widget.Max > Widget.Min
            ? (_currentValue - Widget.Min) / (Widget.Max - Widget.Min)
            : 0f;

        Widget track = new SliderTrackWidget(
            ratio,
            activeColor,
            inactiveColor,
            _isHovered || _isDragging
                ? new Vector4(
                    thumbColor.X * 1.1f,
                    thumbColor.Y * 1.1f,
                    thumbColor.Z * 1.1f,
                    thumbColor.W
                )
                : thumbColor,
            trackHeight,
            thumbWidth,
            thumbHeight,
            thumbRadius,
            (
                width,
                ro
            ) =>
            {
                _trackWidth = width;
                _trackRenderObject = ro;
            },
            new GestureDetector(
                onPress: _OnPress,
                onMove: _OnMove,
                onRelease: _OnRelease,
                onEnter: _OnEnter,
                onExit: _OnExit,
                onWheel: _OnWheel
            )
        );

        if (Widget.ShowValueLabel)
        {
            var tooltipContent = Widget.ValueLabelBuilder?.Invoke(_currentValue)
                                 ?? new Padding(
                                     EdgeInsets.All(4),
                                     new Text(
                                         _FormatValue(_currentValue),
                                         new TextStyle
                                         {
                                             FontSize = theme.TextTheme.Label.FontSize,
                                             Color = colors.Primary
                                         }
                                     )
                                 );

            var usable = _trackWidth - thumbWidth;
            var thumbCenterX = usable * ratio + thumbWidth / 2f;

            track = new Tooltip(
                track,
                tooltipContent,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(100),
                anchorX: thumbCenterX
            );
        }

        if (Widget.Label != null)
        {
            return new Row(
                mainAxisAlignment: MainAxisAlignment.Start,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new Text(
                        Widget.Label,
                        new TextStyle
                        {
                            FontSize = theme.TextTheme.Body.FontSize, Color = colors.OnSurface
                        }
                    ),
                    new SizedBox(8),
                    new Expanded(track)
                ]
            );
        }

        return track;
    }

    private string _FormatValue(
        float value
    )
    {
        if (Widget.Divisions != null)
        {
            return value.ToString("F0");
        }

        return value.ToString("F2");
    }

    private float _ToLocalX(
        PointerEvent e
    )
    {
        if (_trackRenderObject == null)
        {
            return e.X;
        }

        return _trackRenderObject.GlobalToLocal(
                new Vector2(
                    e.X,
                    e.Y
                )
            )
            .X;
    }

    private float _ComputeValueFromX(
        float localX
    )
    {
        var usable = _trackWidth - _thumbWidth;
        if (usable <= 0)
        {
            return Widget.Min;
        }

        var ratio = Math.Clamp(
            (localX - _thumbWidth / 2f) / usable,
            0f,
            1f
        );
        var rawValue = Widget.Min + ratio * (Widget.Max - Widget.Min);
        return _SnapValue(rawValue);
    }

    private float _SnapValue(
        float rawValue
    )
    {
        rawValue = Math.Clamp(
            rawValue,
            Widget.Min,
            Widget.Max
        );
        if (Widget.Divisions != null && Widget.Divisions > 0)
        {
            var step = (Widget.Max - Widget.Min) / Widget.Divisions.Value;
            rawValue = MathF.Round((rawValue - Widget.Min) / step) * step
                       + Widget.Min;
            rawValue = Math.Clamp(
                rawValue,
                Widget.Min,
                Widget.Max
            );
        }

        return rawValue;
    }

    private void _OnPress(
        PointerEvent e
    )
    {
        _isDragging = true;
        var localX = _ToLocalX(e);
        var newValue = _ComputeValueFromX(localX);
        _UpdateValue(newValue);
    }

    private void _OnMove(
        PointerEvent e
    )
    {
        if (!_isDragging)
        {
            return;
        }

        var localX = _ToLocalX(e);
        var newValue = _ComputeValueFromX(localX);
        _UpdateValue(newValue);
    }

    private void _OnRelease(
        PointerEvent e
    )
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        Widget.OnChangeEnd?.Invoke(_currentValue);
        SetState(() => { });
    }

    private void _OnEnter(
        PointerEvent e
    ) =>
        SetState(() => _isHovered = true);

    private void _OnExit(
        PointerEvent e
    ) =>
        SetState(() => _isHovered = false);

    private void _OnWheel(
        PointerEvent e
    )
    {
        float step;
        if (Widget.Divisions != null && Widget.Divisions > 0)
        {
            step = (Widget.Max - Widget.Min) / Widget.Divisions.Value;
        }
        else
        {
            step = (Widget.Max - Widget.Min) * 0.05f;
        }

        var newValue = _SnapValue(_currentValue + e.Delta * step);
        _UpdateValue(newValue);
        e.Handled = true;
    }

    private void _UpdateValue(
        float newValue
    )
    {
        if (Math.Abs(newValue - _currentValue) < 0.0001f)
        {
            return;
        }

        SetState(() => _currentValue = newValue);
        Widget.OnChanged?.Invoke(_currentValue);
    }
}

internal class SliderTrackWidget : SingleChildWidget
{
    public SliderTrackWidget(
        float ratio,
        Vector4 activeColor,
        Vector4 inactiveColor,
        Vector4 thumbColor,
        float trackHeight,
        float thumbWidth,
        float thumbHeight,
        float thumbRadius,
        Action<float, RenderObject> onLayout,
        Widget? child = null
    ) : base(child)
    {
        Ratio = ratio;
        ActiveColor = activeColor;
        InactiveColor = inactiveColor;
        ThumbColor = thumbColor;
        TrackHeight = trackHeight;
        ThumbWidth = thumbWidth;
        ThumbHeight = thumbHeight;
        ThumbRadius = thumbRadius;
        OnLayout = onLayout;
    }

    public float Ratio { get; }
    public Vector4 ActiveColor { get; }
    public Vector4 InactiveColor { get; }
    public Vector4 ThumbColor { get; }
    public float TrackHeight { get; }
    public float ThumbWidth { get; }
    public float ThumbHeight { get; }
    public float ThumbRadius { get; }
    public Action<float, RenderObject> OnLayout { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderSliderTrack(OnLayout)
        {
            Ratio = Ratio,
            ActiveColor = ActiveColor,
            InactiveColor = InactiveColor,
            ThumbColor = ThumbColor,
            TrackHeight = TrackHeight,
            ThumbWidth = ThumbWidth,
            ThumbHeight = ThumbHeight,
            ThumbRadius = ThumbRadius
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderSliderTrack)renderObject;
        ro.Ratio = Ratio;
        ro.ActiveColor = ActiveColor;
        ro.InactiveColor = InactiveColor;
        ro.ThumbColor = ThumbColor;
        ro.TrackHeight = TrackHeight;
        ro.ThumbWidth = ThumbWidth;
        ro.ThumbHeight = ThumbHeight;
        ro.ThumbRadius = ThumbRadius;
    }
}

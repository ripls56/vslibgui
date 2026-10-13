using System;
using System.Globalization;
using Gui.Sound;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;

namespace Gui.Widgets.Input;

public class NumericField : StatefulWidget
{
    public NumericField
    (
        float initialValue = 0,
        float step = 1,
        Action<float>? onChanged = null,
        BoxStyle? style = null
    )
    {
        Value = initialValue;
        Step = step;
        OnChanged = onChanged;
        Style = style ?? new BoxStyle
        {
            Height = 40, Width = 100
        };
    }

    public float Value { get; }
    public float Step { get; }
    public Action<float>? OnChanged { get; }
    public BoxStyle Style { get; }

    public override State CreateState() => new NumericFieldState();
}

internal class NumericFieldState : FocusableState<NumericField>
{
    private TextEditingController _controller = null!;
    private float _currentValue;

    public override void InitState()
    {
        base.InitState();
        _currentValue = Widget.Value;
        _controller = new TextEditingController(_currentValue.ToString());
        _controller.AddListener(OnTextChanged);
    }

    private void OnTextChanged()
    {
        if (float.TryParse
            (
                _controller.Text,
                out var newValue
            ))
        {
            if (Math.Abs(_currentValue - newValue) > 0.0001f)
            {
                _currentValue = newValue;
                Widget.OnChanged?.Invoke(_currentValue);
            }
        }
        else if (_controller.Text != "" && _controller.Text != "-" && _controller.Text != "." &&
                 _controller.Text != "," && _controller.Text != " ")
        {
            _controller.Value = new TextEditingValue
            (
                _currentValue.ToString(CultureInfo.InvariantCulture),
                TextSelection.Collapsed(_currentValue.ToString(CultureInfo.InvariantCulture).Length)
            );
        }
    }

    private void Adjust
    (
        float delta
    )
    {
        FocusNode.RequestFocus();
        _currentValue += delta;
        _controller.Text = _currentValue.ToString(CultureInfo.InvariantCulture);
        Widget.OnChanged?.Invoke(_currentValue);
        SetState(() => { });
    }

    public override Widget Build
    (
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var fieldStyle = theme.TextFieldStyle;
        var fieldHeight = Widget.Style.Height ?? fieldStyle.Height;
        var spinnerWidth = fieldHeight / 2f;

        return new Container
        (
            Widget.Style,
            new Row
            (
                children:
                [
                    new Expanded
                    (
                        new TextField
                        (
                            _controller,
                            FocusNode,
                            fieldStyle with
                            {
                                Height = fieldHeight,
                                CornerRadius = fieldStyle.CornerRadius with
                                {
                                    X = 0, Y = 0
                                }
                            }
                        )
                    ),
                    new Column
                    (
                        children:
                        [
                            new StepperButton
                            (
                                "textures/icons/plus.svg",
                                _ => Adjust(Widget.Step),
                                spinnerWidth
                            ),
                            new StepperButton
                            (
                                "textures/icons/minus.svg",
                                _ => Adjust(-Widget.Step),
                                spinnerWidth
                            )
                        ]
                    )
                ]
            )
        );
    }

    public override void Dispose()
    {
        _controller.RemoveListener(OnTextChanged);
        _controller.Dispose();
        base.Dispose();
    }
}

internal class StepperButton
(
    string iconPath,
    Action<PointerEvent> onTap,
    float size
) : StatefulWidget
{
    public string IconPath { get; } = iconPath;
    public Action<PointerEvent> OnTap { get; } = onTap;
    public float Size { get; } = size;

    public override State CreateState() => new StepperButtonState();
}

internal class StepperButtonState : State<StepperButton>
{
    private bool _isHovered;
    private bool _isPressed;

    public override Widget Build
    (
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var variant = theme.ButtonStyle.Primary;

        var fill = _isPressed
            ? variant.PressBackgroundColor
            : _isHovered
                ? variant.HoverBackgroundColor
                : variant.BackgroundColor;

        return new MouseRegion
        (
            cursor: MouseCursor.LinkSelect,
            child: new GestureDetector
            (
                onTap: Widget.OnTap,
                onPress: _ => SetState
                (() =>
                    {
                        _isPressed = true;
                        this.Step();
                    }
                ),
                onRelease: _ => SetState(() => _isPressed = false),
                onEnter: _ => SetState(() => _isHovered = true),
                onExit: _ => SetState
                (() =>
                    {
                        _isHovered = false;
                        _isPressed = false;
                    }
                ),
                child: new Container
                (
                    new BoxStyle
                    {
                        Width = Widget.Size, Height = Widget.Size, Color = fill
                    },
                    new Center
                    (
                        new Icon
                        (
                            "gui",
                            Widget.IconPath,
                            Widget.Size * 0.6f,
                            theme.ColorScheme.OnPrimary
                        )
                    )
                )
            )
        );
    }
}

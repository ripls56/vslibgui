using System;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Input;

public class NumericField : StatefulWidget
{
    public NumericField(
        float initialValue = 0,
        float step = 1,
        Action<float>? onChanged = null,
        BoxStyle? style = null
    )
    {
        Value = initialValue;
        Step = step;
        OnChanged = onChanged;
        Style = style ?? new BoxStyle { Height = 40, Width = 100 };
    }

    public float Value { get; }
    public float Step { get; }
    public Action<float>? OnChanged { get; }
    public BoxStyle Style { get; }

    public override State CreateState() => new NumericFieldState();
}

internal class NumericFieldState : State<NumericField>
{
    private TextEditingController _controller = null!;
    private float _currentValue;
    private FocusNode _focusNode = null!;

    public override void InitState()
    {
        base.InitState();
        _currentValue = Widget.Value;
        _controller = new TextEditingController(_currentValue.ToString());
        _controller.AddListener(OnTextChanged);
        _focusNode = new FocusNode();
    }

    private void OnTextChanged()
    {
        if (float.TryParse(
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
            _controller.Value = new TextEditingValue(
                _currentValue.ToString(),
                TextSelection.Collapsed(_currentValue.ToString().Length)
            );
        }
    }

    private void Adjust(
        float delta
    )
    {
        _focusNode.RequestFocus(); // Ensure focus when using buttons
        _currentValue += delta;
        _controller.Text = _currentValue.ToString();
        Widget.OnChanged?.Invoke(_currentValue);
        SetState(() => { });
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var fieldHeight = Widget.Style.Height ?? 40;
        var buttonSize = fieldHeight / 2;

        Widget MakeButton(
            string label,
            Action<PointerEvent> onTap
        )
        {
            return new GestureDetector(
                onTap: onTap,
                child: new Container(
                    new BoxStyle
                    {
                        Width = buttonSize,
                        Height = buttonSize,
                        Color = colors.Primary,
                        CornerRadius = Vector4.Zero
                    },
                    new Center(
                        new Text(
                            label,
                            new TextStyle
                            {
                                FontSize = theme.TextTheme.Body.FontSize,
                                Color = colors.OnPrimary
                            }
                        )
                    )
                )
            );
        }

        return new Container(
            Widget.Style,
            new Row(
                children:
                [
                    new Expanded(
                        new TextField(
                            _controller,
                            _focusNode,
                            new BoxStyle
                            {
                                Height = fieldHeight,
                                Color = new Vector4(
                                    colors.Background.X,
                                    colors.Background.Y,
                                    colors.Background.Z,
                                    0.9f
                                ),
                                BorderThickness = 1,
                                BorderColor = colors.Border
                            }
                        )
                    ),
                    new Column(
                        children:
                        [
                            MakeButton(
                                "+",
                                e => Adjust(Widget.Step)
                            ),
                            MakeButton(
                                "-",
                                e => Adjust(-Widget.Step)
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
        _focusNode.Dispose();
        base.Dispose();
    }
}

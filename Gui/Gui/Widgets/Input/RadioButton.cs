using System;
using Gui.Rendering;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Input;

public class RadioButton<T> : StatefulWidget where T : IEquatable<T>
{
    public RadioButton
    (
        T value,
        T groupValue,
        Action<T>? onChanged = null,
        string? label = null,
        float size = 24,
        RadioButtonStyle? style = null
    )
    {
        Value = value;
        GroupValue = groupValue;
        OnChanged = onChanged;
        Label = label;
        Size = size;
        Style = style;
    }

    public T Value { get; }
    public T GroupValue { get; }
    public Action<T>? OnChanged { get; }
    public string? Label { get; }
    public float Size { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.RadioButtonStyle" />.
    /// </summary>
    public RadioButtonStyle? Style { get; }

    public override State CreateState() => new RadioButtonState<T>();
}

internal class RadioButtonState<T> : FocusableState<RadioButton<T>>, IActivatable
    where T : IEquatable<T>
{
    private SKBitmap? _backgroundTexture;
    private bool _hovered;
    private bool _pressed;

    /// <inheritdoc />
    public void Activate()
    {
        if (!Widget.Value.Equals(Widget.GroupValue))
        {
            this.ToggleOn();
            Widget.OnChanged?.Invoke(Widget.Value);
        }
    }

    public override void InitState()
    {
        base.InitState();
        _backgroundTexture = GuiModSystem.Instance?.SkiaAssetLoader?.LoadBitmap
        (
            "game",
            "textures/gui/gui-parchment.png"
        );
    }

    public override Widget Build
    (
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var style = Widget.Style ?? theme.RadioButtonStyle;
        var radius = Widget.Size / 2f;
        var focused = FocusNode.ShowsFocusIndicator;

        var isSelected = Widget.Value.Equals(Widget.GroupValue);

        var dotValue = (isSelected, _hovered, _pressed) switch
        {
            (true, _, _) => 1f,
            (false, true, false) => 0.75f,
            (false, _, true) => 0.9f,
            _ => 0
        };
        var dot = new AnimatedOpacity
        (
            dotValue,
            TimeSpan.FromMilliseconds(150),
            Curves.EaseOut,
            child:
            new AnimatedScale
            (
                dotValue,
                TimeSpan.FromMilliseconds(180),
                Curves.EaseOutBack,
                child: new Center
                (
                    new Container
                    (
                        new BoxStyle
                        {
                            Width = Widget.Size * 0.5f,
                            Height = Widget.Size * 0.5f,
                            Color = style.DotColor,
                            CornerRadius = new Vector4(radius)
                        }
                    )
                )
            )
        );

        var circle = new Container
        (
            new BoxStyle
            {
                Width = Widget.Size,
                Height = Widget.Size,
                Color = _backgroundTexture == null
                    ? style.BackgroundColor
                    : Vector4.One,
                Texture = _backgroundTexture,
                BorderThickness = focused
                    ? theme.FocusOutlineWidth
                    : style.BorderThickness,
                BorderColor = focused
                    ? theme.FocusOutlineColor
                    : style.BorderColor,
                CornerRadius = new Vector4(radius)
            },
            dot
        );

        var content = Widget.Label == null
            ? (Widget)circle
            : new Row
            (
                mainAxisAlignment: MainAxisAlignment.Start,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    circle,
                    new Padding
                    (
                        EdgeInsets.Only(8),
                        new Text(Widget.Label, style.LabelStyle)
                    )
                ]
            );

        var bodyScale = (_hovered, _pressed) switch
        {
            (true, true) => 1.005f,
            (true, false) => 1.01f,
            _ => 1f
        };
        var body = new AnimatedScale
        (
            bodyScale,
            TimeSpan.FromMilliseconds(140),
            Curves.EaseOut,
            child: content
        );

        return new GestureDetector
        (
            onTap: e =>
            {
                SetState(() => { });
                if (!Widget.Value.Equals(Widget.GroupValue))
                {
                    Widget.OnChanged?.Invoke(Widget.Value);
                }
            },
            onEnter: _ => SetState(() => _hovered = true),
            onExit: _ => SetState
            (() =>
                {
                    _hovered = false;
                    _pressed = false;
                }
            ),
            onPress: _ => SetState
            (() =>
                {
                    _pressed = true;
                    if (!Widget.Value.Equals(Widget.GroupValue))
                    {
                        this.ToggleOn();
                    }
                }
            ),
            onRelease: _ => SetState(() => _pressed = false),
            child: body
        );
    }

    public override void Dispose() => base.Dispose();
}

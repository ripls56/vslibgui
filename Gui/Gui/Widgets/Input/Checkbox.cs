using System;
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

public class Checkbox : StatefulWidget
{
    public Checkbox
    (
        bool value,
        Action<bool>? onChanged = null,
        string? label = null,
        float size = 24,
        CheckboxStyle? style = null
    )
    {
        Value = value;
        OnChanged = onChanged;
        Label = label;
        Size = size;
        Style = style;
    }

    public bool Value { get; }
    public Action<bool>? OnChanged { get; }
    public string? Label { get; }
    public float Size { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.CheckboxStyle" />.
    /// </summary>
    public CheckboxStyle? Style { get; }

    public override State CreateState() => new CheckboxState();
}

internal class CheckboxState : FocusableState<Checkbox>, IActivatable
{
    private SKBitmap? _backgroundTexture;
    private bool _hovered;
    private bool _pressed;

    /// <inheritdoc />
    public void Activate()
    {
        PlayToggleSound();
        Widget.OnChanged?.Invoke(!Widget.Value);
    }

    /// <summary>Rising tone when checking, falling when unchecking.</summary>
    private void PlayToggleSound()
    {
        if (Widget.Value)
        {
            this.ToggleOff();
        }
        else
        {
            this.ToggleOn();
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
        var style = Widget.Style ?? theme.CheckboxStyle;

        var checkMarkValue = (Widget.Value, _hovered, _pressed) switch
        {
            (true, _, _) => 1f,
            (false, true, false) => 0.75f,
            (false, _, true) => 0.9f,
            _ => 0
        };
        var checkMark = new AnimatedOpacity
        (
            checkMarkValue,
            TimeSpan.FromMilliseconds(150),
            Curves.EaseOut,
            child: new AnimatedScale
            (
                checkMarkValue,
                TimeSpan.FromMilliseconds(150),
                Curves.EaseOut,
                child: new Center
                (
                    new Container
                    (
                        new BoxStyle
                        {
                            Width = Widget.Size * 0.6f,
                            Height = Widget.Size * 0.6f,
                            Color = style.CheckColor,
                            CornerRadius = new Vector4(style.CornerRadius)
                        }
                    )
                )
            )
        );

        var focused = FocusNode.ShowsFocusIndicator;

        var box = new Container
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
                CornerRadius = new Vector4(style.CornerRadius)
            },
            checkMark
        );


        var content = Widget.Label == null
            ? (Widget)box
            : new Row
            (
                mainAxisAlignment: MainAxisAlignment.Start,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    box,
                    new SizedBox(8),
                    new Text(Widget.Label, style.LabelStyle)
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
            onTap: e => { Widget.OnChanged?.Invoke(!Widget.Value); },
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
                    PlayToggleSound();
                }
            ),
            onRelease: _ => SetState(() => _pressed = false),
            child: body
        );
    }

    public override void Dispose() => base.Dispose();
}

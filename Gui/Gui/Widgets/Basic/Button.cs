using System;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     A themeable button that supports four visual variants and displays an
///     arbitrary <see cref="Child" /> widget as its content.
/// </summary>
public class Button : StatefulWidget
{
    public Button(
        Widget child,
        ButtonVariant variant = ButtonVariant.Primary,
        Action<PointerEvent>? onTap = null,
        ButtonStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Child = child;
        Variant = variant;
        OnTap = onTap;
        Style = style;
    }

    /// <summary>Content displayed inside the button.</summary>
    public Widget Child { get; }

    /// <summary>Visual variant that controls colors and border.</summary>
    public ButtonVariant Variant { get; }

    /// <summary>Callback invoked when the button is tapped.</summary>
    public Action<PointerEvent>? OnTap { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="ThemeData.ButtonStyle" />.
    /// </summary>
    public ButtonStyle? Style { get; }

    public override State CreateState() => new ButtonState();
}

internal class ButtonState : State<Button>
{
    private bool _isHovered;
    private bool _isPressed;

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var style = Widget.Style ?? theme.ButtonStyle;
        var variantStyle = style[Widget.Variant];

        var bgColor = _isPressed
            ? variantStyle.PressBackgroundColor
            : _isHovered
                ? variantStyle.HoverBackgroundColor
                : variantStyle.BackgroundColor;

        return new MouseRegion(
            cursor: MouseCursor.LinkSelect,
            child: new GestureDetector(
                onTap: p =>
                {
                    context.GetSoundPlayer().Play("click", Pitch.Varied(.95f, .05f), .2f);
                    Widget.OnTap?.Invoke(p);
                },
                onPress: _ => SetState(() => _isPressed = true),
                onRelease: _ => SetState(() => _isPressed = false),
                onEnter: _ => SetState(() => _isHovered = true),
                onExit: _ => SetState(() =>
                {
                    _isHovered = false;
                    _isPressed = false;
                }),
                child: new AnimatedContainer(
                    duration: TimeSpan.FromMilliseconds(150),
                    curve: Curves.EaseOut,
                    style: new BoxStyle
                    {
                        Color = bgColor,
                        BorderColor = variantStyle.BorderColor,
                        BorderThickness = variantStyle.BorderThickness,
                        CornerRadius = new Vector4(variantStyle.CornerRadius)
                    },
                    child: new Padding(
                        style.Padding,
                        Widget.Child
                    )
                )
            )
        );
    }
}

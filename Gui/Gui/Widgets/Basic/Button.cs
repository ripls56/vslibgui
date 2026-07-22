using System;
using System.Collections.Generic;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic.Theming;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>Visual status of a <see cref="Button" />. Mutually exclusive — only one applies at a time.</summary>
public enum ButtonStatus
{
    /// <summary>Default resting state.</summary>
    Normal,

    /// <summary>Busy / in progress; the button is non-interactive and shows an indicator.</summary>
    Loading,

    /// <summary>An error occurred.</summary>
    Error,

    /// <summary>The action completed successfully.</summary>
    Success
}

public class Button : StatefulWidget
{
    public Button
    (
        Widget child,
        ButtonVariant variant = ButtonVariant.Primary,
        Action<PointerEvent>? onTap = null,
        bool enabled = true,
        ButtonStatus status = ButtonStatus.Normal,
        ButtonStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Child = child;
        Variant = variant;
        OnTap = onTap;
        Enabled = enabled;
        Status = status;
        Style = style;
    }

    public Widget Child { get; }
    public ButtonVariant Variant { get; }
    public Action<PointerEvent>? OnTap { get; }
    public bool Enabled { get; }
    public ButtonStatus Status { get; }
    public ButtonStyle? Style { get; }

    internal bool ForceHover { get; init; }
    internal bool ForceFocus { get; init; }
    internal bool ForceActive { get; init; }

    public override State CreateState() => new ButtonState();
}

internal class ButtonState : FocusableState<Button>, IActivatable
{
    private static readonly PointerEvent KeyboardActivation = new(0, 0);

    private bool _isHovered;
    private bool _isPressed;

    // Re-triggerable content-swap transition: crossfades Child <-> LoadingDots
    // (and any other status-driven content change) instead of snapping instantly.
    private AnimationController _statusTransition = null!;
    private CurvedAnimation _statusCurved = null!;
    private ButtonStatus _lastStatus;

    private bool IsInteractive => Widget.Enabled && Widget.Status != ButtonStatus.Loading;
    private bool EffectivePressed => (_isPressed || Widget.ForceActive) && IsInteractive;
    private bool EffectiveHovered => (_isHovered || Widget.ForceHover) && IsInteractive;
    private bool EffectiveFocused => FocusNode.ShowsFocusIndicator || Widget.ForceFocus;

    public override void InitState()
    {
        base.InitState();
        _lastStatus = Widget.Status;
        _statusTransition = new AnimationController
            (TimeSpan.FromMilliseconds(220), Element.Owner!.GetTickerProvider());
        _statusCurved = new CurvedAnimation(_statusTransition, Curves.EaseOutCubic);
        _statusTransition.Forward();
    }

    public override void Dispose()
    {
        _statusTransition.Dispose();
        base.Dispose();
    }

    public void Activate()
    {
        if (IsInteractive) HandleTap(KeyboardActivation);
    }

    private void PlaySound()
    {
        Element.Owner!.GetSoundPlayer().Play("click", Pitch.Varied(.95f, .05f), .2f);
    }

    private void HandleTap(PointerEvent e)
    {
        if (IsInteractive) Widget.OnTap?.Invoke(e);
    }

    public override Widget Build(BuildContext context)
    {
        if (Widget.Status != _lastStatus)
        {
            _lastStatus = Widget.Status;
            _statusTransition.Reset();
            _statusTransition.Forward();
        }

        var theme = Theme.Of(context);
        var style = Widget.Style ?? theme.ButtonStyle;
        var variantStyle = style[Widget.Variant];

        var bgColor = GetBackgroundColor(theme, variantStyle);
        var borderColor = GetBorderColor(theme, variantStyle);
        var fgColor = GetForegroundColor(theme, variantStyle);
        var shadows = BuildShadows(Widget.Variant, bgColor);
        var content = BuildContent(fgColor);
        var scale = EffectivePressed ? 0.96f : (EffectiveHovered ? 1.03f : 1f);

        return new MouseRegion
        (
            cursor: IsInteractive ? MouseCursor.LinkSelect : null,
            child: new GestureDetector
            (
                onTap: HandleTap,
                onPress: _ => SetState
                (() =>
                    {
                        _isPressed = true;
                        if (IsInteractive) PlaySound();
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
                child: new AnimatedScale
                (
                    scale: scale,
                    duration: TimeSpan.FromMilliseconds(140),
                    curve: Curves.EaseOut,
                    alignment: Alignment.Center,
                    child: new AnimatedContainer
                    (
                        duration: TimeSpan.FromMilliseconds(200),
                        curve: Curves.EaseOutCubic,
                        style: new BoxStyle
                        {
                            Color = bgColor,
                            BorderColor = borderColor,
                            BorderThickness = MathF.Max(1f, variantStyle.BorderThickness),
                            CornerRadius = new Vector4(variantStyle.CornerRadius),
                            BoxShadows = shadows
                        },
                        child: new Opacity
                        (
                            Widget.Enabled ? 1f : 0.45f,
                            new Padding
                            (
                                style.Padding,
                                new DefaultTextStyle
                                (
                                    new TextStyle
                                    {
                                        Color = fgColor, FontSize = 14, Weight = FontWeight.SemiBold
                                    },
                                    new IconTheme
                                    (
                                        new IconThemeData
                                        {
                                            Color = fgColor
                                        }, content
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );
    }

    private Vector4 GetForegroundColor(ThemeData theme, ButtonVariantStyle variant)
    {
        if (Widget.Status == ButtonStatus.Error) return theme.ColorScheme.OnError;
        if (Widget.Status == ButtonStatus.Success) return theme.ColorScheme.OnSecondary;
        if (EffectiveFocused)
            return Widget.Variant switch
            {
                ButtonVariant.Primary => theme.ColorScheme.OnPrimary,
                ButtonVariant.Danger => theme.ColorScheme.OnError,
                _ => theme.ColorScheme.OnSurface
            };
        ;

        return Widget.Variant switch
        {
            ButtonVariant.Primary => theme.ColorScheme.OnPrimary,
            ButtonVariant.Danger => theme.ColorScheme.OnError,
            _ => theme.ColorScheme.OnSurface
        };
    }

    private Vector4 GetBackgroundColor(ThemeData theme, ButtonVariantStyle variant)
    {
        if (Widget.Status == ButtonStatus.Error) return theme.ColorScheme.Error;
        if (Widget.Status == ButtonStatus.Success) return theme.ColorScheme.Secondary;
        if (EffectivePressed) return variant.PressBackgroundColor;
        if (EffectiveHovered) return variant.HoverBackgroundColor;
        return variant.BackgroundColor;
    }

    private Vector4 GetBorderColor(ThemeData theme, ButtonVariantStyle variant)
    {
        if (EffectiveFocused) return theme.FocusOutlineColor;

        var color = variant.BorderColor;

        if (Widget.Variant == ButtonVariant.Ghost && color.W < 0.01f)
        {
            return theme.ColorScheme.OnSurface.WithAlpha(0.15f);
        }

        return color;
    }

    private BoxShadow[] BuildShadows(ButtonVariant variant, Vector4 bgColor)
    {
        var shadows = new List<BoxShadow>();

        var shadow = new BoxShadow
        (
            bgColor.WithAlpha(0.35f),
            Vector2.Zero,
            BlurRadius: 6
        );

        var resolvedShadow = variant switch
        {
            ButtonVariant.Primary or ButtonVariant.Danger => shadow,
            ButtonVariant.Secondary or ButtonVariant.Ghost => shadow with
            {
                Color = bgColor.WithAlpha(0.17f)
            },
            _ => shadow
        };

        if (EffectiveFocused)
        {
            shadows.Add(resolvedShadow);
        }

        if (EffectiveHovered && !EffectivePressed)
        {
            shadows.Add
            (
                resolvedShadow with
                {
                    Offset = new Vector2(0, 3)
                }
            );
        }

        return shadows.ToArray();
    }

    private Widget BuildContent(Vector4 fgColor)
    {
        return new AnimatedBuilder
        (
            animation: _statusTransition,
            builder: _ =>
            {
                var opacity = (float)_statusCurved.Value;
                var child = Widget.Status == ButtonStatus.Loading
                    ? new Padding(edgeInsets: EdgeInsets.All(6), new LoadingDots(fgColor))
                    : Widget.Child;

                return new Opacity(opacity, child);
            }
        );
    }
}

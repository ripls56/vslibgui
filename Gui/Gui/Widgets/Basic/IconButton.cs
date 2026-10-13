using System;
using System.Collections.Generic;
using Gui.Rendering;
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

public class IconButton : StatefulWidget
{
    public IconButton
    (
        Icon icon,
        ButtonVariant variant = ButtonVariant.Primary,
        Action<PointerEvent>? onTap = null,
        bool enabled = true,
        ButtonStatus status = ButtonStatus.Normal,
        IconButtonStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Icon = icon;
        Variant = variant;
        OnTap = onTap;
        Enabled = enabled;
        Status = status;
        Style = style;
    }

    public Icon Icon { get; }
    public ButtonVariant Variant { get; }
    public Action<PointerEvent>? OnTap { get; }
    public bool Enabled { get; }
    public ButtonStatus Status { get; }
    public IconButtonStyle? Style { get; }

    internal bool ForceHover { get; init; }
    internal bool ForceFocus { get; init; }
    internal bool ForceActive { get; init; }

    public override State CreateState() => new IconButtonState();
}

internal class IconButtonState : FocusableState<IconButton>, IActivatable
{
    private static readonly PointerEvent KeyboardActivation = new(0, 0);

    private bool _isHovered;
    private bool _isPressed;
    private ButtonStatus _lastStatus;
    private CurvedAnimation _statusCurved = null!;
    private AnimationController _statusTransition = null!;

    private bool IsInteractive => Widget.Enabled && Widget.Status != ButtonStatus.Loading;
    private bool EffectivePressed => (_isPressed || Widget.ForceActive) && IsInteractive;
    private bool EffectiveHovered => (_isHovered || Widget.ForceHover) && IsInteractive;
    private bool EffectiveFocused => FocusNode.ShowsFocusIndicator || Widget.ForceFocus;

    public void Activate()
    {
        if (IsInteractive)
        {
            HandleTap(KeyboardActivation);
        }
    }

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

    private void HandleTap(PointerEvent e)
    {
        if (IsInteractive)
        {
            Widget.OnTap?.Invoke(e);
        }
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
        var style = Widget.Style ?? theme.IconButtonStyle;
        var variantStyle = style[Widget.Variant];
        var backgroundColor = GetBackgroundColor(theme, variantStyle);
        var borderColor = GetBorderColor(theme, variantStyle);
        var foregroundColor = GetForegroundColor(theme);
        var scale = EffectivePressed ? 0.96f : EffectiveHovered ? 1.03f : 1f;

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
                        if (IsInteractive)
                        {
                            this.Click();
                        }
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
                child: new RepaintBoundary
                (
                    new AnimatedScale
                    (
                        scale,
                        TimeSpan.FromMilliseconds(140),
                        Curves.EaseOut,
                        alignment: Alignment.Center,
                        child: new AnimatedContainer
                        (
                            duration: TimeSpan.FromMilliseconds(200),
                            curve: Curves.EaseOutCubic,
                            style: new BoxStyle
                            {
                                Color = backgroundColor,
                                Width = style.Size,
                                Height = style.Size,
                                BorderColor = borderColor,
                                BorderThickness = MathF.Max(1f, variantStyle.BorderThickness),
                                CornerRadius = new Vector4(variantStyle.CornerRadius),
                                BoxShadows = BuildShadows(Widget.Variant, backgroundColor)
                            },
                            child: new Opacity
                            (
                                Widget.Enabled ? 1f : 0.45f,
                                new IconTheme
                                (
                                    new IconThemeData
                                    {
                                        Color = foregroundColor
                                    },
                                    new Center(BuildContent(foregroundColor))
                                )
                            )
                        )
                    )
                )
            )
        );
    }

    private Vector4 GetForegroundColor(ThemeData theme)
    {
        if (Widget.Status == ButtonStatus.Error)
        {
            return theme.ColorScheme.OnError;
        }
        if (Widget.Status == ButtonStatus.Success)
        {
            return theme.ColorScheme.OnSecondary;
        }

        return Widget.Variant switch
        {
            ButtonVariant.Primary => theme.ColorScheme.OnPrimary,
            ButtonVariant.Danger => theme.ColorScheme.OnError,
            _ => theme.ColorScheme.OnSurface
        };
    }

    private Vector4 GetBackgroundColor(ThemeData theme, ButtonVariantStyle variant)
    {
        if (Widget.Status == ButtonStatus.Error)
        {
            return theme.ColorScheme.Error;
        }
        if (Widget.Status == ButtonStatus.Success)
        {
            return theme.ColorScheme.Secondary;
        }
        if (EffectivePressed)
        {
            return variant.PressBackgroundColor;
        }
        if (EffectiveHovered)
        {
            return variant.HoverBackgroundColor;
        }
        return variant.BackgroundColor;
    }

    private Vector4 GetBorderColor(ThemeData theme, ButtonVariantStyle variant)
    {
        if (EffectiveFocused)
        {
            return theme.FocusOutlineColor;
        }

        if (Widget.Variant == ButtonVariant.Ghost && variant.BorderColor.W < 0.01f)
        {
            return theme.ColorScheme.OnSurface.WithAlpha(0.15f);
        }

        return variant.BorderColor;
    }

    private BoxShadow[] BuildShadows(ButtonVariant variant, Vector4 backgroundColor)
    {
        var shadow = new BoxShadow
        (
            backgroundColor.WithAlpha
                (variant is ButtonVariant.Primary or ButtonVariant.Danger ? 0.35f : 0.17f),
            Vector2.Zero,
            6
        );
        var shadows = new List<BoxShadow>();

        if (EffectiveFocused)
        {
            shadows.Add(shadow);
        }

        if (EffectiveHovered && !EffectivePressed)
        {
            shadows.Add
            (
                shadow with
                {
                    Offset = new Vector2(0, 3)
                }
            );
        }

        return shadows.ToArray();
    }

    private Widget BuildContent(Vector4 foregroundColor)
    {
        return new AnimatedBuilder
        (
            _statusTransition,
            _ => new Opacity
            (
                (float)_statusCurved.Value,
                Widget.Status == ButtonStatus.Loading
                    ? new LoadingDots(foregroundColor)
                    : Widget.Icon
            )
        );
    }
}

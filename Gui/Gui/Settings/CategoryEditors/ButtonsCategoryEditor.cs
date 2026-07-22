using System;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Settings.CategoryEditors;

/// <summary>Editor for button styles (padding + 4 variants).</summary>
public sealed class ButtonsCategoryEditor : StatelessWidget
{
    private readonly ThemeData _baseTheme;
    private readonly ThemeOverrides _draft;
    private readonly Action _onChanged;

    /// <summary>Creates the buttons editor.</summary>
    public ButtonsCategoryEditor
    (
        ThemeOverrides draft,
        ThemeData baseTheme,
        Action onChanged
    )
    {
        _draft = draft;
        _baseTheme = baseTheme;
        _onChanged = onChanged;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var b = _baseTheme.ButtonStyle;
        _draft.Button ??= new ButtonOverrides();
        var o = _draft.Button;
        return new Column
        (
            spacing: 24,
            children:
            [
                new EdgeInsetsOverrideField
                (
                    "Padding", o.Padding, b.Padding, v =>
                    {
                        o.Padding = v;
                        _onChanged();
                    }
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                ButtonVariantSection("Primary", o.Primary, b.Primary),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                ButtonVariantSection("Secondary", o.Secondary, b.Secondary),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                ButtonVariantSection("Danger", o.Danger, b.Danger),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                ButtonVariantSection("Ghost", o.Ghost, b.Ghost)
            ]
        );
    }

    private Widget ButtonVariantSection
    (
        string name,
        ButtonVariantOverrides? variant,
        ButtonVariantStyle fallback
    )
    {
        variant ??= new ButtonVariantOverrides();
        SetVariant(name, variant);
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text(name, _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    $"{name} Background", variant.BackgroundColor, fallback.BackgroundColor, v =>
                    {
                        variant.BackgroundColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    $"{name} Hover Background", variant.HoverBackgroundColor,
                    fallback.HoverBackgroundColor, v =>
                    {
                        variant.HoverBackgroundColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    $"{name} Press Background", variant.PressBackgroundColor,
                    fallback.PressBackgroundColor, v =>
                    {
                        variant.PressBackgroundColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    $"{name} Border", variant.BorderColor, fallback.BorderColor, v =>
                    {
                        variant.BorderColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    $"{name} Border Thickness", variant.BorderThickness, 0, 20, 1,
                    fallback.BorderThickness, v =>
                    {
                        variant.BorderThickness = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    $"{name} Corner Radius", variant.CornerRadius, 0, 40, 1, fallback.CornerRadius,
                    v =>
                    {
                        variant.CornerRadius = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private void SetVariant(string name, ButtonVariantOverrides variant)
    {
        var o = _draft.Button!;
        switch (name)
        {
            case "Primary": o.Primary = variant; break;
            case "Secondary": o.Secondary = variant; break;
            case "Danger": o.Danger = variant; break;
            case "Ghost": o.Ghost = variant; break;
        }
    }
}

using System;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Settings.CategoryEditors;

/// <summary>Editor for input component styles (TextField, Checkbox, RadioButton, Slider, Dropdown).</summary>
public sealed class InputsCategoryEditor : StatelessWidget
{
    private readonly ThemeData _baseTheme;
    private readonly ThemeOverrides _draft;
    private readonly Action _onChanged;

    /// <summary>Creates the inputs editor.</summary>
    public InputsCategoryEditor
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
        return new Column
        (
            spacing: 24,
            children:
            [
                TextFieldSection(),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                CheckboxSection(),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                RadioButtonSection(),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                SliderSection(),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                DropdownSection()
            ]
        );
    }

    private Widget TextFieldSection()
    {
        _draft.TextField ??= new TextFieldOverrides();
        var o = _draft.TextField;
        var s = _baseTheme.TextFieldStyle;
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text("TextField", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Fill Color", o.FillColor, s.FillColor, v =>
                    {
                        o.FillColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Border Color", o.BorderColor, s.BorderColor, v =>
                    {
                        o.BorderColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Selection Color", o.SelectionColor, s.SelectionColor, v =>
                    {
                        o.SelectionColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Border Thickness", o.BorderThickness, 0, 20, 1, s.BorderThickness, v =>
                    {
                        o.BorderThickness = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Height", o.Height, 20, 100, 1, s.Height, v =>
                    {
                        o.Height = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Corner Radius", o.CornerRadius?.X, 0, 20, 1, s.CornerRadius.X, v =>
                    {
                        o.CornerRadius = v.HasValue ? new Vector4(v.Value) : null;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget CheckboxSection()
    {
        _draft.Checkbox ??= new CheckboxOverrides();
        var o = _draft.Checkbox;
        var s = _baseTheme.CheckboxStyle;
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text("Checkbox", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Check Color", o.CheckColor, s.CheckColor, v =>
                    {
                        o.CheckColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Background Color", o.BackgroundColor, s.BackgroundColor, v =>
                    {
                        o.BackgroundColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Border Color", o.BorderColor, s.BorderColor, v =>
                    {
                        o.BorderColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Border Thickness", o.BorderThickness, 0, 10, 1, s.BorderThickness, v =>
                    {
                        o.BorderThickness = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Corner Radius", o.CornerRadius, 0, 10, 1, s.CornerRadius, v =>
                    {
                        o.CornerRadius = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget RadioButtonSection()
    {
        _draft.RadioButton ??= new RadioButtonOverrides();
        var o = _draft.RadioButton;
        var s = _baseTheme.RadioButtonStyle;
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text("RadioButton", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Dot Color", o.DotColor, s.DotColor, v =>
                    {
                        o.DotColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Background Color", o.BackgroundColor, s.BackgroundColor, v =>
                    {
                        o.BackgroundColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Border Color", o.BorderColor, s.BorderColor, v =>
                    {
                        o.BorderColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Border Thickness", o.BorderThickness, 0, 10, 1, s.BorderThickness, v =>
                    {
                        o.BorderThickness = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget SliderSection()
    {
        _draft.Slider ??= new SliderOverrides();
        var o = _draft.Slider;
        var s = _baseTheme.SliderStyle;
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text("Slider", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Active Color", o.ActiveColor, s.ActiveColor, v =>
                    {
                        o.ActiveColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Inactive Color", o.InactiveColor, s.InactiveColor, v =>
                    {
                        o.InactiveColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Thumb Color", o.ThumbColor, s.ThumbColor, v =>
                    {
                        o.ThumbColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Track Height", o.TrackHeight, 0, 20, 1, s.TrackHeight, v =>
                    {
                        o.TrackHeight = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Thumb Width", o.ThumbWidth, 0, 40, 1, s.ThumbWidth, v =>
                    {
                        o.ThumbWidth = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Thumb Height", o.ThumbHeight, 0, 40, 1, s.ThumbHeight, v =>
                    {
                        o.ThumbHeight = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Thumb Radius", o.ThumbRadius, 0, 20, 1, s.ThumbRadius, v =>
                    {
                        o.ThumbRadius = v;
                        _onChanged();
                    }
                )
            ]
        );
    }

    private Widget DropdownSection()
    {
        _draft.Dropdown ??= new DropdownOverrides();
        var o = _draft.Dropdown;
        var s = _baseTheme.DropdownStyle;
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text("Dropdown", _baseTheme.TextTheme.Title),
                new ColorOverrideField
                (
                    "Button Color", o.ButtonColor, s.ButtonColor, v =>
                    {
                        o.ButtonColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Border Color", o.BorderColor, s.BorderColor, v =>
                    {
                        o.BorderColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Button Height", o.ButtonHeight, 20, 100, 1, s.ButtonHeight, v =>
                    {
                        o.ButtonHeight = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Border Thickness", o.BorderThickness, 0, 10, 1, s.BorderThickness, v =>
                    {
                        o.BorderThickness = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Button Corner Radius", o.ButtonCornerRadius, 0, 20, 1, s.ButtonCornerRadius,
                    v =>
                    {
                        o.ButtonCornerRadius = v;
                        _onChanged();
                    }
                ),
                new EdgeInsetsOverrideField
                (
                    "Button Padding", o.ButtonPadding, s.ButtonPadding, v =>
                    {
                        o.ButtonPadding = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Menu Min Width", o.MenuMinWidth, 50, 500, 5, s.MenuMinWidth, v =>
                    {
                        o.MenuMinWidth = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Visible Item Count", (float?)o.VisibleItemCount, 1, 20, 1, s.VisibleItemCount,
                    v =>
                    {
                        o.VisibleItemCount = (int?)v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Item Spacing", o.ItemSpacing, 0, 20, 1, s.ItemSpacing, v =>
                    {
                        o.ItemSpacing = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Menu Color", o.MenuColor, s.MenuColor, v =>
                    {
                        o.MenuColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Menu Corner Radius", o.MenuCornerRadius, 0, 20, 1, s.MenuCornerRadius, v =>
                    {
                        o.MenuCornerRadius = v;
                        _onChanged();
                    }
                ),
                new EdgeInsetsOverrideField
                (
                    "Menu Padding", o.MenuPadding, s.MenuPadding, v =>
                    {
                        o.MenuPadding = v;
                        _onChanged();
                    }
                ),
                new EdgeInsetsOverrideField
                (
                    "Item Padding", o.ItemPadding, s.ItemPadding, v =>
                    {
                        o.ItemPadding = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Item Corner Radius", o.ItemCornerRadius, 0, 20, 1, s.ItemCornerRadius, v =>
                    {
                        o.ItemCornerRadius = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Selection Color", o.SelectionColor, s.SelectionColor, v =>
                    {
                        o.SelectionColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Hover Color", o.HoverColor, s.HoverColor, v =>
                    {
                        o.HoverColor = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Chevron Color", o.ChevronColor, s.ChevronColor, v =>
                    {
                        o.ChevronColor = v;
                        _onChanged();
                    }
                ),
                new FloatOverrideField
                (
                    "Menu Gap", o.MenuGap, 0, 20, 1, s.MenuGap, v =>
                    {
                        o.MenuGap = v;
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    "Selection Accent Color", o.SelectionAccentColor, s.SelectionAccentColor, v =>
                    {
                        o.SelectionAccentColor = v;
                        _onChanged();
                    }
                )
            ]
        );
    }
}

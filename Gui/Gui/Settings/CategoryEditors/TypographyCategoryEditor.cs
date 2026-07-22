using System;
using Gui.Rendering.Text;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Settings.CategoryEditors;

/// <summary>Editor for typography styles (7 text styles).</summary>
public sealed class TypographyCategoryEditor : StatelessWidget
{
    private readonly ThemeData _baseTheme;
    private readonly ThemeOverrides _draft;
    private readonly Action _onChanged;

    /// <summary>Creates the typography editor.</summary>
    public TypographyCategoryEditor
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
        var t = _baseTheme.TextTheme;
        _draft.Typography ??= new TypographyOverrides();
        var o = _draft.Typography;
        return new Column
        (
            spacing: 24,
            children:
            [
                TextStyleSection
                (
                    "Display", o.DisplayFontSize, o.DisplayWeight, o.DisplayColor,
                    t.Display.FontSize, t.Display.Weight, t.Display.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Headline", o.HeadlineFontSize, o.HeadlineWeight, o.HeadlineColor,
                    t.Headline.FontSize, t.Headline.Weight, t.Headline.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Title", o.TitleFontSize, o.TitleWeight, o.TitleColor, t.Title.FontSize,
                    t.Title.Weight, t.Title.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Subtitle", o.SubtitleFontSize, o.SubtitleWeight, o.SubtitleColor,
                    t.Subtitle.FontSize, t.Subtitle.Weight, t.Subtitle.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Body", o.BodyFontSize, o.BodyWeight, o.BodyColor, t.Body.FontSize,
                    t.Body.Weight, t.Body.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Label", o.LabelFontSize, o.LabelWeight, o.LabelColor, t.Label.FontSize,
                    t.Label.Weight, t.Label.Color
                ),
                new Divider(color: _baseTheme.ColorScheme.OutlineVariant),
                TextStyleSection
                (
                    "Caption", o.CaptionFontSize, o.CaptionWeight, o.CaptionColor,
                    t.Caption.FontSize, t.Caption.Weight, t.Caption.Color
                )
            ]
        );
    }

    private Widget TextStyleSection
    (
        string name,
        float? fontSize,
        FontWeight? weight,
        Vector4? color,
        float fallbackSize,
        FontWeight fallbackWeight,
        Vector4 fallbackColor
    )
    {
        return new Column
        (
            spacing: 8,
            children:
            [
                new Text(name, _baseTheme.TextTheme.Title),
                new FloatOverrideField
                (
                    $"{name} Font Size", fontSize, 0, 200, 1, fallbackSize, v =>
                    {
                        SetFontSize(name, v);
                        _onChanged();
                    }
                ),
                new EnumOverrideField<FontWeight>
                (
                    $"{name} Weight", weight, fallbackWeight, v =>
                    {
                        SetWeight(name, v);
                        _onChanged();
                    }
                ),
                new ColorOverrideField
                (
                    $"{name} Color", color, fallbackColor, v =>
                    {
                        SetColor(name, v);
                        _onChanged();
                    }
                )
            ]
        );
    }

    private void SetFontSize(string name, float? value)
    {
        var o = _draft.Typography!;
        switch (name)
        {
            case "Display": o.DisplayFontSize = value; break;
            case "Headline": o.HeadlineFontSize = value; break;
            case "Title": o.TitleFontSize = value; break;
            case "Subtitle": o.SubtitleFontSize = value; break;
            case "Body": o.BodyFontSize = value; break;
            case "Label": o.LabelFontSize = value; break;
            case "Caption": o.CaptionFontSize = value; break;
        }
    }

    private void SetWeight(string name, FontWeight? value)
    {
        var o = _draft.Typography!;
        switch (name)
        {
            case "Display": o.DisplayWeight = value; break;
            case "Headline": o.HeadlineWeight = value; break;
            case "Title": o.TitleWeight = value; break;
            case "Subtitle": o.SubtitleWeight = value; break;
            case "Body": o.BodyWeight = value; break;
            case "Label": o.LabelWeight = value; break;
            case "Caption": o.CaptionWeight = value; break;
        }
    }

    private void SetColor(string name, Vector4? value)
    {
        var o = _draft.Typography!;
        switch (name)
        {
            case "Display": o.DisplayColor = value; break;
            case "Headline": o.HeadlineColor = value; break;
            case "Title": o.TitleColor = value; break;
            case "Subtitle": o.SubtitleColor = value; break;
            case "Body": o.BodyColor = value; break;
            case "Label": o.LabelColor = value; break;
            case "Caption": o.CaptionColor = value; break;
        }
    }
}

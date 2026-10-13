using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Theming;

/// <summary>Partial color overrides. Null fields mean "use the base value".</summary>
public sealed class ColorOverrides
{
    public Vector4? Primary { get; set; }
    public Vector4? OnPrimary { get; set; }
    public Vector4? Secondary { get; set; }
    public Vector4? OnSecondary { get; set; }
    public Vector4? Surface { get; set; }
    public Vector4? OnSurface { get; set; }
    public Vector4? OnSurfaceVariant { get; set; }
    public Vector4? Background { get; set; }
    public Vector4? OnBackground { get; set; }
    public Vector4? Border { get; set; }
    public Vector4? Error { get; set; }
    public Vector4? OnError { get; set; }
    public Vector4? SurfaceLow { get; set; }
    public Vector4? SurfaceHigh { get; set; }
    public Vector4? StateHover { get; set; }
    public Vector4? StateSelected { get; set; }
    public Vector4? OutlineVariant { get; set; }

    /// <summary>Returns a copy of <paramref name="scheme" /> with every non-null field replaced.</summary>
    public ColorScheme ApplyTo(ColorScheme scheme) => new()
    {
        Primary = Primary ?? scheme.Primary,
        OnPrimary = OnPrimary ?? scheme.OnPrimary,
        Secondary = Secondary ?? scheme.Secondary,
        OnSecondary = OnSecondary ?? scheme.OnSecondary,
        Surface = Surface ?? scheme.Surface,
        OnSurface = OnSurface ?? scheme.OnSurface,
        OnSurfaceVariant = OnSurfaceVariant ?? scheme.OnSurfaceVariant,
        Background = Background ?? scheme.Background,
        OnBackground = OnBackground ?? scheme.OnBackground,
        Border = Border ?? scheme.Border,
        Error = Error ?? scheme.Error,
        OnError = OnError ?? scheme.OnError,
        SurfaceLow = SurfaceLow ?? scheme.SurfaceLow,
        SurfaceHigh = SurfaceHigh ?? scheme.SurfaceHigh,
        StateHover = StateHover ?? scheme.StateHover,
        StateSelected = StateSelected ?? scheme.StateSelected,
        OutlineVariant = OutlineVariant ?? scheme.OutlineVariant
    };

    /// <summary>Deep copy.</summary>
    public ColorOverrides Copy() => (ColorOverrides)MemberwiseClone();
}

/// <summary>Partial typography overrides (size/weight/color per text style).</summary>
public sealed class TypographyOverrides
{
    public float? DisplayFontSize { get; set; }
    public FontWeight? DisplayWeight { get; set; }
    public Vector4? DisplayColor { get; set; }
    public float? HeadlineFontSize { get; set; }
    public FontWeight? HeadlineWeight { get; set; }
    public Vector4? HeadlineColor { get; set; }
    public float? TitleFontSize { get; set; }
    public FontWeight? TitleWeight { get; set; }
    public Vector4? TitleColor { get; set; }
    public float? SubtitleFontSize { get; set; }
    public FontWeight? SubtitleWeight { get; set; }
    public Vector4? SubtitleColor { get; set; }
    public float? BodyFontSize { get; set; }
    public FontWeight? BodyWeight { get; set; }
    public Vector4? BodyColor { get; set; }
    public float? LabelFontSize { get; set; }
    public FontWeight? LabelWeight { get; set; }
    public Vector4? LabelColor { get; set; }
    public float? CaptionFontSize { get; set; }
    public FontWeight? CaptionWeight { get; set; }
    public Vector4? CaptionColor { get; set; }

    /// <summary>Returns a copy of <paramref name="theme" /> with overridden text styles replaced.</summary>
    public TextTheme ApplyTo(TextTheme theme) => new()
    {
        Display = ApplyTextStyle(theme.Display, DisplayFontSize, DisplayWeight, DisplayColor),
        Headline = ApplyTextStyle(theme.Headline, HeadlineFontSize, HeadlineWeight, HeadlineColor),
        Title = ApplyTextStyle(theme.Title, TitleFontSize, TitleWeight, TitleColor),
        Subtitle = ApplyTextStyle(theme.Subtitle, SubtitleFontSize, SubtitleWeight, SubtitleColor),
        Body = ApplyTextStyle(theme.Body, BodyFontSize, BodyWeight, BodyColor),
        Label = ApplyTextStyle(theme.Label, LabelFontSize, LabelWeight, LabelColor),
        Caption = ApplyTextStyle(theme.Caption, CaptionFontSize, CaptionWeight, CaptionColor)
    };

    private static TextStyle ApplyTextStyle
    (
        TextStyle style,
        float? size,
        FontWeight? weight,
        Vector4? color
    )
    {
        var result = style;
        if (size.HasValue)
        {
            result.FontSize = size.Value;
        }
        if (weight.HasValue)
        {
            result.Weight = weight.Value;
        }
        if (color.HasValue)
        {
            result.Color = color.Value;
        }
        return result;
    }

    /// <summary>Deep copy.</summary>
    public TypographyOverrides Copy() => (TypographyOverrides)MemberwiseClone();
}

/// <summary>Overrides for one button variant.</summary>
public sealed class ButtonVariantOverrides
{
    public Vector4? BackgroundColor { get; set; }
    public Vector4? HoverBackgroundColor { get; set; }
    public Vector4? PressBackgroundColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public float? BorderThickness { get; set; }
    public float? CornerRadius { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public ButtonVariantStyle ApplyTo(ButtonVariantStyle style) => new()
    {
        BackgroundColor = BackgroundColor ?? style.BackgroundColor,
        HoverBackgroundColor = HoverBackgroundColor ?? style.HoverBackgroundColor,
        PressBackgroundColor = PressBackgroundColor ?? style.PressBackgroundColor,
        BorderColor = BorderColor ?? style.BorderColor,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        CornerRadius = CornerRadius ?? style.CornerRadius
    };

    /// <summary>Deep copy.</summary>
    public ButtonVariantOverrides Copy() => (ButtonVariantOverrides)MemberwiseClone();
}

/// <summary>Partial button overrides (padding + per-variant).</summary>
public sealed class ButtonOverrides
{
    public EdgeInsets? Padding { get; set; }
    public ButtonVariantOverrides? Primary { get; set; }
    public ButtonVariantOverrides? Secondary { get; set; }
    public ButtonVariantOverrides? Danger { get; set; }
    public ButtonVariantOverrides? Ghost { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public ButtonStyle ApplyTo(ButtonStyle style) => new()
    {
        Padding = Padding ?? style.Padding,
        Primary = (Primary ?? new ButtonVariantOverrides()).ApplyTo(style.Primary),
        Secondary = (Secondary ?? new ButtonVariantOverrides()).ApplyTo(style.Secondary),
        Danger = (Danger ?? new ButtonVariantOverrides()).ApplyTo(style.Danger),
        Ghost = (Ghost ?? new ButtonVariantOverrides()).ApplyTo(style.Ghost)
    };

    /// <summary>Deep copy.</summary>
    public ButtonOverrides Copy() => new()
    {
        Padding = Padding,
        Primary = Primary?.Copy(),
        Secondary = Secondary?.Copy(),
        Danger = Danger?.Copy(),
        Ghost = Ghost?.Copy()
    };
}

/// <summary>Partial text-field overrides.</summary>
public sealed class TextFieldOverrides
{
    public Vector4? FillColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public Vector4? SelectionColor { get; set; }
    public float? BorderThickness { get; set; }
    public float? Height { get; set; }
    public Vector4? CornerRadius { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public TextFieldStyle ApplyTo(TextFieldStyle style) => new()
    {
        FillColor = FillColor ?? style.FillColor,
        BorderColor = BorderColor ?? style.BorderColor,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        Height = Height ?? style.Height,
        CornerRadius = CornerRadius ?? style.CornerRadius,
        TextStyle = style.TextStyle,
        SelectionColor = SelectionColor ?? style.SelectionColor
    };

    /// <summary>Deep copy.</summary>
    public TextFieldOverrides Copy() => (TextFieldOverrides)MemberwiseClone();
}

/// <summary>Partial checkbox overrides.</summary>
public sealed class CheckboxOverrides
{
    public Vector4? CheckColor { get; set; }
    public Vector4? BackgroundColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public float? BorderThickness { get; set; }
    public float? CornerRadius { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public CheckboxStyle ApplyTo(CheckboxStyle style) => new()
    {
        CheckColor = CheckColor ?? style.CheckColor,
        BackgroundColor = BackgroundColor ?? style.BackgroundColor,
        BorderColor = BorderColor ?? style.BorderColor,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        CornerRadius = CornerRadius ?? style.CornerRadius,
        LabelStyle = style.LabelStyle
    };

    /// <summary>Deep copy.</summary>
    public CheckboxOverrides Copy() => (CheckboxOverrides)MemberwiseClone();
}

/// <summary>Partial radio-button overrides.</summary>
public sealed class RadioButtonOverrides
{
    public Vector4? DotColor { get; set; }
    public Vector4? BackgroundColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public float? BorderThickness { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public RadioButtonStyle ApplyTo(RadioButtonStyle style) => new()
    {
        DotColor = DotColor ?? style.DotColor,
        BackgroundColor = BackgroundColor ?? style.BackgroundColor,
        BorderColor = BorderColor ?? style.BorderColor,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        LabelStyle = style.LabelStyle
    };

    /// <summary>Deep copy.</summary>
    public RadioButtonOverrides Copy() => (RadioButtonOverrides)MemberwiseClone();
}

/// <summary>Partial slider overrides.</summary>
public sealed class SliderOverrides
{
    public Vector4? ActiveColor { get; set; }
    public Vector4? InactiveColor { get; set; }
    public Vector4? ThumbColor { get; set; }
    public float? TrackHeight { get; set; }
    public float? ThumbWidth { get; set; }
    public float? ThumbHeight { get; set; }
    public float? ThumbRadius { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public SliderStyle ApplyTo(SliderStyle style) => new()
    {
        ActiveColor = ActiveColor ?? style.ActiveColor,
        InactiveColor = InactiveColor ?? style.InactiveColor,
        ThumbColor = ThumbColor ?? style.ThumbColor,
        TrackHeight = TrackHeight ?? style.TrackHeight,
        ThumbWidth = ThumbWidth ?? style.ThumbWidth,
        ThumbHeight = ThumbHeight ?? style.ThumbHeight,
        ThumbRadius = ThumbRadius ?? style.ThumbRadius
    };

    /// <summary>Deep copy.</summary>
    public SliderOverrides Copy() => (SliderOverrides)MemberwiseClone();
}

/// <summary>Partial dropdown overrides.</summary>
public sealed class DropdownOverrides
{
    public float? ButtonHeight { get; set; }
    public Vector4? ButtonColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public float? BorderThickness { get; set; }
    public float? ButtonCornerRadius { get; set; }
    public EdgeInsets? ButtonPadding { get; set; }
    public float? MenuMinWidth { get; set; }
    public int? VisibleItemCount { get; set; }
    public float? ItemSpacing { get; set; }
    public Vector4? MenuColor { get; set; }
    public float? MenuCornerRadius { get; set; }
    public EdgeInsets? MenuPadding { get; set; }
    public EdgeInsets? ItemPadding { get; set; }
    public float? ItemCornerRadius { get; set; }
    public Vector4? SelectionColor { get; set; }
    public Vector4? HoverColor { get; set; }
    public Vector4? ChevronColor { get; set; }
    public float? MenuGap { get; set; }
    public Vector4? SelectionAccentColor { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public DropdownStyle ApplyTo(DropdownStyle style) => new()
    {
        ButtonHeight = ButtonHeight ?? style.ButtonHeight,
        ButtonColor = ButtonColor ?? style.ButtonColor,
        BorderColor = BorderColor ?? style.BorderColor,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        ButtonCornerRadius = ButtonCornerRadius ?? style.ButtonCornerRadius,
        ButtonPadding = ButtonPadding ?? style.ButtonPadding,
        MenuMinWidth = MenuMinWidth ?? style.MenuMinWidth,
        VisibleItemCount = VisibleItemCount ?? style.VisibleItemCount,
        ItemSpacing = ItemSpacing ?? style.ItemSpacing,
        MenuColor = MenuColor ?? style.MenuColor,
        MenuCornerRadius = MenuCornerRadius ?? style.MenuCornerRadius,
        MenuPadding = MenuPadding ?? style.MenuPadding,
        ItemPadding = ItemPadding ?? style.ItemPadding,
        ItemCornerRadius = ItemCornerRadius ?? style.ItemCornerRadius,
        SelectionColor = SelectionColor ?? style.SelectionColor,
        HoverColor = HoverColor ?? style.HoverColor,
        TextStyle = style.TextStyle,
        ChevronColor = ChevronColor ?? style.ChevronColor,
        MenuGap = MenuGap ?? style.MenuGap,
        SelectionAccentColor = SelectionAccentColor ?? style.SelectionAccentColor
    };

    /// <summary>Deep copy.</summary>
    public DropdownOverrides Copy() => (DropdownOverrides)MemberwiseClone();
}

/// <summary>Partial progress-bar overrides.</summary>
public sealed class ProgressBarOverrides
{
    public float? Height { get; set; }
    public float? CornerRadius { get; set; }
    public float? BorderThickness { get; set; }
    public Vector4? FillColor { get; set; }
    public Vector4? TrackColor { get; set; }
    public Vector4? BorderColor { get; set; }
    public float? FillPadding { get; set; }

    /// <summary>Returns a copy of <paramref name="style" /> with overridden fields replaced.</summary>
    public ProgressBarStyle ApplyTo(ProgressBarStyle style) => new()
    {
        Height = Height ?? style.Height,
        CornerRadius = CornerRadius ?? style.CornerRadius,
        BorderThickness = BorderThickness ?? style.BorderThickness,
        FillColor = FillColor ?? style.FillColor,
        TrackColor = TrackColor ?? style.TrackColor,
        BorderColor = BorderColor ?? style.BorderColor,
        FillPadding = FillPadding ?? style.FillPadding
    };

    /// <summary>Deep copy.</summary>
    public ProgressBarOverrides Copy() => (ProgressBarOverrides)MemberwiseClone();
}

/// <summary>Partial focus overrides.</summary>
public sealed class FocusOverrides
{
    public Vector4? OutlineColor { get; set; }
    public float? OutlineWidth { get; set; }

    /// <summary>Deep copy.</summary>
    public FocusOverrides Copy() => (FocusOverrides)MemberwiseClone();
}

/// <summary>
///     User-editable overrides layered on top of a preset's theme. Each nullable
///     category sub-object carries its own partial overrides; a null category means
///     "use the preset/derived default" for all of its fields.
/// </summary>
public sealed class ThemeOverrides
{
    /// <summary>Color overrides.</summary>
    public ColorOverrides? Colors { get; set; }
    /// <summary>Typography overrides.</summary>
    public TypographyOverrides? Typography { get; set; }
    /// <summary>Button overrides.</summary>
    public ButtonOverrides? Button { get; set; }
    /// <summary>Text-field overrides.</summary>
    public TextFieldOverrides? TextField { get; set; }
    /// <summary>Checkbox overrides.</summary>
    public CheckboxOverrides? Checkbox { get; set; }
    /// <summary>Radio-button overrides.</summary>
    public RadioButtonOverrides? RadioButton { get; set; }
    /// <summary>Slider overrides.</summary>
    public SliderOverrides? Slider { get; set; }
    /// <summary>Dropdown overrides.</summary>
    public DropdownOverrides? Dropdown { get; set; }
    /// <summary>Progress-bar overrides.</summary>
    public ProgressBarOverrides? ProgressBar { get; set; }
    /// <summary>Focus overrides.</summary>
    public FocusOverrides? Focus { get; set; }

    /// <summary>True when no category is set (a pure preset applies unchanged).</summary>
    public bool IsEmpty =>
        Colors is null && Typography is null && Button is null && TextField is null
        && Checkbox is null && RadioButton is null && Slider is null && Dropdown is null
        && ProgressBar is null && Focus is null;

    /// <summary>Returns a new <paramref name="baseTheme" /> with every set override applied.</summary>
    public ThemeData ApplyOnto(ThemeData baseTheme) => new
    (
        Colors?.ApplyTo(baseTheme.ColorScheme) ?? baseTheme.ColorScheme,
        Typography?.ApplyTo(baseTheme.TextTheme) ?? baseTheme.TextTheme,
        ProgressBar?.ApplyTo(baseTheme.ProgressBarStyle) ?? baseTheme.ProgressBarStyle,
        baseTheme.ItemSlotStyle,
        baseTheme.HotkeyBadgeStyle,
        Dropdown?.ApplyTo(baseTheme.DropdownStyle) ?? baseTheme.DropdownStyle,
        Checkbox?.ApplyTo(baseTheme.CheckboxStyle) ?? baseTheme.CheckboxStyle,
        RadioButton?.ApplyTo(baseTheme.RadioButtonStyle) ?? baseTheme.RadioButtonStyle,
        Slider?.ApplyTo(baseTheme.SliderStyle) ?? baseTheme.SliderStyle,
        Button?.ApplyTo(baseTheme.ButtonStyle) ?? baseTheme.ButtonStyle,
        TextField?.ApplyTo(baseTheme.TextFieldStyle) ?? baseTheme.TextFieldStyle
    )
    {
        FocusOutlineColor = Focus?.OutlineColor ?? baseTheme.FocusOutlineColor,
        FocusOutlineWidth = Focus?.OutlineWidth ?? baseTheme.FocusOutlineWidth
    };

    /// <summary>Deep, independent copy.</summary>
    public ThemeOverrides DeepCopy() => new()
    {
        Colors = Colors?.Copy(),
        Typography = Typography?.Copy(),
        Button = Button?.Copy(),
        TextField = TextField?.Copy(),
        Checkbox = Checkbox?.Copy(),
        RadioButton = RadioButton?.Copy(),
        Slider = Slider?.Copy(),
        Dropdown = Dropdown?.Copy(),
        ProgressBar = ProgressBar?.Copy(),
        Focus = Focus?.Copy()
    };
}

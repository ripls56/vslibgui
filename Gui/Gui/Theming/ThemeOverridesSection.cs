using System;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Theming;

/// <summary>Serializable color overrides (hex strings).</summary>
public sealed class ColorOverrideSection
{
    public string? Primary { get; set; }
    public string? OnPrimary { get; set; }
    public string? Secondary { get; set; }
    public string? OnSecondary { get; set; }
    public string? Surface { get; set; }
    public string? OnSurface { get; set; }
    public string? OnSurfaceVariant { get; set; }
    public string? Background { get; set; }
    public string? OnBackground { get; set; }
    public string? Border { get; set; }
    public string? Error { get; set; }
    public string? OnError { get; set; }
    public string? SurfaceLow { get; set; }
    public string? SurfaceHigh { get; set; }
    public string? StateHover { get; set; }
    public string? StateSelected { get; set; }
    public string? OutlineVariant { get; set; }

    internal ColorOverrides? ToOverrides() => AnySet() ? new ColorOverrides
    {
        Primary = Parse(Primary),
        OnPrimary = Parse(OnPrimary),
        Secondary = Parse(Secondary),
        OnSecondary = Parse(OnSecondary),
        Surface = Parse(Surface),
        OnSurface = Parse(OnSurface),
        OnSurfaceVariant = Parse(OnSurfaceVariant),
        Background = Parse(Background),
        OnBackground = Parse(OnBackground),
        Border = Parse(Border),
        Error = Parse(Error),
        OnError = Parse(OnError),
        SurfaceLow = Parse(SurfaceLow),
        SurfaceHigh = Parse(SurfaceHigh),
        StateHover = Parse(StateHover),
        StateSelected = Parse(StateSelected),
        OutlineVariant = Parse(OutlineVariant)
    } : null;

    internal static ColorOverrideSection? From(ColorOverrides? o)
    {
        if (o is null) return null;
        return new ColorOverrideSection
        {
            Primary = o.Primary?.ToHex(),
            OnPrimary = o.OnPrimary?.ToHex(),
            Secondary = o.Secondary?.ToHex(),
            OnSecondary = o.OnSecondary?.ToHex(),
            Surface = o.Surface?.ToHex(),
            OnSurface = o.OnSurface?.ToHex(),
            OnSurfaceVariant = o.OnSurfaceVariant?.ToHex(),
            Background = o.Background?.ToHex(),
            OnBackground = o.OnBackground?.ToHex(),
            Border = o.Border?.ToHex(),
            Error = o.Error?.ToHex(),
            OnError = o.OnError?.ToHex(),
            SurfaceLow = o.SurfaceLow?.ToHex(),
            SurfaceHigh = o.SurfaceHigh?.ToHex(),
            StateHover = o.StateHover?.ToHex(),
            StateSelected = o.StateSelected?.ToHex(),
            OutlineVariant = o.OutlineVariant?.ToHex()
        };
    }

    private bool AnySet() =>
        Primary is not null || OnPrimary is not null || Secondary is not null
        || OnSecondary is not null || Surface is not null || OnSurface is not null
        || OnSurfaceVariant is not null || Background is not null || OnBackground is not null
        || Border is not null || Error is not null || OnError is not null
        || SurfaceLow is not null || SurfaceHigh is not null || StateHover is not null
        || StateSelected is not null || OutlineVariant is not null;

    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable typography overrides.</summary>
public sealed class TypographyOverrideSection
{
    public double? DisplayFontSize { get; set; }
    public string? DisplayWeight { get; set; }
    public string? DisplayColor { get; set; }

    public double? HeadlineFontSize { get; set; }
    public string? HeadlineWeight { get; set; }
    public string? HeadlineColor { get; set; }

    public double? TitleFontSize { get; set; }
    public string? TitleWeight { get; set; }
    public string? TitleColor { get; set; }

    public double? SubtitleFontSize { get; set; }
    public string? SubtitleWeight { get; set; }
    public string? SubtitleColor { get; set; }

    public double? BodyFontSize { get; set; }
    public string? BodyWeight { get; set; }
    public string? BodyColor { get; set; }

    public double? LabelFontSize { get; set; }
    public string? LabelWeight { get; set; }
    public string? LabelColor { get; set; }

    public double? CaptionFontSize { get; set; }
    public string? CaptionWeight { get; set; }
    public string? CaptionColor { get; set; }

    internal TypographyOverrides? ToOverrides() => AnySet() ? new TypographyOverrides
    {
        DisplayFontSize = ToFloat(DisplayFontSize),
        DisplayWeight = ParseWeight(DisplayWeight),
        DisplayColor = Parse(DisplayColor),
        HeadlineFontSize = ToFloat(HeadlineFontSize),
        HeadlineWeight = ParseWeight(HeadlineWeight),
        HeadlineColor = Parse(HeadlineColor),
        TitleFontSize = ToFloat(TitleFontSize),
        TitleWeight = ParseWeight(TitleWeight),
        TitleColor = Parse(TitleColor),
        SubtitleFontSize = ToFloat(SubtitleFontSize),
        SubtitleWeight = ParseWeight(SubtitleWeight),
        SubtitleColor = Parse(SubtitleColor),
        BodyFontSize = ToFloat(BodyFontSize),
        BodyWeight = ParseWeight(BodyWeight),
        BodyColor = Parse(BodyColor),
        LabelFontSize = ToFloat(LabelFontSize),
        LabelWeight = ParseWeight(LabelWeight),
        LabelColor = Parse(LabelColor),
        CaptionFontSize = ToFloat(CaptionFontSize),
        CaptionWeight = ParseWeight(CaptionWeight),
        CaptionColor = Parse(CaptionColor)
    } : null;

    internal static TypographyOverrideSection? From(TypographyOverrides? o)
    {
        if (o is null) return null;
        return new TypographyOverrideSection
        {
            DisplayFontSize = ToDouble(o.DisplayFontSize),
            DisplayWeight = o.DisplayWeight?.ToString(),
            DisplayColor = o.DisplayColor?.ToHex(),
            HeadlineFontSize = ToDouble(o.HeadlineFontSize),
            HeadlineWeight = o.HeadlineWeight?.ToString(),
            HeadlineColor = o.HeadlineColor?.ToHex(),
            TitleFontSize = ToDouble(o.TitleFontSize),
            TitleWeight = o.TitleWeight?.ToString(),
            TitleColor = o.TitleColor?.ToHex(),
            SubtitleFontSize = ToDouble(o.SubtitleFontSize),
            SubtitleWeight = o.SubtitleWeight?.ToString(),
            SubtitleColor = o.SubtitleColor?.ToHex(),
            BodyFontSize = ToDouble(o.BodyFontSize),
            BodyWeight = o.BodyWeight?.ToString(),
            BodyColor = o.BodyColor?.ToHex(),
            LabelFontSize = ToDouble(o.LabelFontSize),
            LabelWeight = o.LabelWeight?.ToString(),
            LabelColor = o.LabelColor?.ToHex(),
            CaptionFontSize = ToDouble(o.CaptionFontSize),
            CaptionWeight = o.CaptionWeight?.ToString(),
            CaptionColor = o.CaptionColor?.ToHex()
        };
    }

    private bool AnySet() =>
        DisplayFontSize is not null || DisplayWeight is not null || DisplayColor is not null
        || HeadlineFontSize is not null || HeadlineWeight is not null || HeadlineColor is not null
        || TitleFontSize is not null || TitleWeight is not null || TitleColor is not null
        || SubtitleFontSize is not null || SubtitleWeight is not null || SubtitleColor is not null
        || BodyFontSize is not null || BodyWeight is not null || BodyColor is not null
        || LabelFontSize is not null || LabelWeight is not null || LabelColor is not null
        || CaptionFontSize is not null || CaptionWeight is not null || CaptionColor is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;

    private static FontWeight? ParseWeight(string? value)
    {
        if (value is null) return null;
        if (Enum.TryParse<FontWeight>(value, ignoreCase: true, out var w))
        {
            return w;
        }
        return null;
    }

    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable button variant overrides.</summary>
public sealed class ButtonVariantOverrideSection
{
    public string? BackgroundColor { get; set; }
    public string? HoverBackgroundColor { get; set; }
    public string? PressBackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double? BorderThickness { get; set; }
    public double? CornerRadius { get; set; }

    internal ButtonVariantOverrides? ToOverrides() => AnySet() ? new ButtonVariantOverrides
    {
        BackgroundColor = Parse(BackgroundColor),
        HoverBackgroundColor = Parse(HoverBackgroundColor),
        PressBackgroundColor = Parse(PressBackgroundColor),
        BorderColor = Parse(BorderColor),
        BorderThickness = ToFloat(BorderThickness),
        CornerRadius = ToFloat(CornerRadius)
    } : null;

    internal static ButtonVariantOverrideSection? From(ButtonVariantOverrides? o)
    {
        if (o is null) return null;
        return new ButtonVariantOverrideSection
        {
            BackgroundColor = o.BackgroundColor?.ToHex(),
            HoverBackgroundColor = o.HoverBackgroundColor?.ToHex(),
            PressBackgroundColor = o.PressBackgroundColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex(),
            BorderThickness = ToDouble(o.BorderThickness),
            CornerRadius = ToDouble(o.CornerRadius)
        };
    }

    private bool AnySet() =>
        BackgroundColor is not null || HoverBackgroundColor is not null
        || PressBackgroundColor is not null || BorderColor is not null
        || BorderThickness is not null || CornerRadius is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable button overrides.</summary>
public sealed class ButtonOverrideSection
{
    public double? PaddingLeft { get; set; }
    public double? PaddingTop { get; set; }
    public double? PaddingRight { get; set; }
    public double? PaddingBottom { get; set; }

    public ButtonVariantOverrideSection? Primary { get; set; }
    public ButtonVariantOverrideSection? Secondary { get; set; }
    public ButtonVariantOverrideSection? Danger { get; set; }
    public ButtonVariantOverrideSection? Ghost { get; set; }

    internal ButtonOverrides? ToOverrides() => AnySet() ? new ButtonOverrides
    {
        Padding = ParsePadding(),
        Primary = Primary?.ToOverrides(),
        Secondary = Secondary?.ToOverrides(),
        Danger = Danger?.ToOverrides(),
        Ghost = Ghost?.ToOverrides()
    } : null;

    internal static ButtonOverrideSection? From(ButtonOverrides? o)
    {
        if (o is null) return null;
        return new ButtonOverrideSection
        {
            PaddingLeft = o.Padding?.Left,
            PaddingTop = o.Padding?.Top,
            PaddingRight = o.Padding?.Right,
            PaddingBottom = o.Padding?.Bottom,
            Primary = ButtonVariantOverrideSection.From(o.Primary),
            Secondary = ButtonVariantOverrideSection.From(o.Secondary),
            Danger = ButtonVariantOverrideSection.From(o.Danger),
            Ghost = ButtonVariantOverrideSection.From(o.Ghost)
        };
    }

    private bool AnySet() =>
        PaddingLeft is not null || PaddingTop is not null || PaddingRight is not null || PaddingBottom is not null
        || Primary is not null || Secondary is not null || Danger is not null || Ghost is not null;

    private EdgeInsets? ParsePadding()
    {
        if (PaddingLeft is null && PaddingTop is null && PaddingRight is null && PaddingBottom is null)
        {
            return null;
        }

        var left = (float)(PaddingLeft ?? 0);
        var top = (float)(PaddingTop ?? 0);
        var right = (float)(PaddingRight ?? 0);
        var bottom = (float)(PaddingBottom ?? 0);

        if (left == top && top == right && right == bottom)
        {
            return EdgeInsets.All(left);
        }

        if (left == right && top == bottom)
        {
            return EdgeInsets.Only(left, top);
        }

        return EdgeInsets.Only(left, top, right, bottom);
    }
}

/// <summary>Serializable text field overrides.</summary>
public sealed class TextFieldOverrideSection
{
    public string? FillColor { get; set; }
    public string? BorderColor { get; set; }
    public string? SelectionColor { get; set; }
    public double? BorderThickness { get; set; }
    public double? Height { get; set; }
    public double? CornerRadius { get; set; }

    internal TextFieldOverrides? ToOverrides() => AnySet() ? new TextFieldOverrides
    {
        FillColor = Parse(FillColor),
        BorderColor = Parse(BorderColor),
        SelectionColor = Parse(SelectionColor),
        BorderThickness = ToFloat(BorderThickness),
        Height = ToFloat(Height),
        CornerRadius = ToVector4(CornerRadius)
    } : null;

    internal static TextFieldOverrideSection? From(TextFieldOverrides? o)
    {
        if (o is null) return null;
        return new TextFieldOverrideSection
        {
            FillColor = o.FillColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex(),
            SelectionColor = o.SelectionColor?.ToHex(),
            BorderThickness = ToDouble(o.BorderThickness),
            Height = ToDouble(o.Height),
            CornerRadius = ToDouble(o.CornerRadius?.X)
        };
    }

    private bool AnySet() =>
        FillColor is not null || BorderColor is not null || SelectionColor is not null
        || BorderThickness is not null || Height is not null || CornerRadius is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? ToVector4(double? value) => value.HasValue ? new Vector4((float)value.Value) : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable checkbox overrides.</summary>
public sealed class CheckboxOverrideSection
{
    public string? CheckColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double? BorderThickness { get; set; }
    public double? CornerRadius { get; set; }

    internal CheckboxOverrides? ToOverrides() => AnySet() ? new CheckboxOverrides
    {
        CheckColor = Parse(CheckColor),
        BackgroundColor = Parse(BackgroundColor),
        BorderColor = Parse(BorderColor),
        BorderThickness = ToFloat(BorderThickness),
        CornerRadius = ToFloat(CornerRadius)
    } : null;

    internal static CheckboxOverrideSection? From(CheckboxOverrides? o)
    {
        if (o is null) return null;
        return new CheckboxOverrideSection
        {
            CheckColor = o.CheckColor?.ToHex(),
            BackgroundColor = o.BackgroundColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex(),
            BorderThickness = ToDouble(o.BorderThickness),
            CornerRadius = ToDouble(o.CornerRadius)
        };
    }

    private bool AnySet() =>
        CheckColor is not null || BackgroundColor is not null || BorderColor is not null
        || BorderThickness is not null || CornerRadius is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable radio button overrides.</summary>
public sealed class RadioButtonOverrideSection
{
    public string? DotColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double? BorderThickness { get; set; }

    internal RadioButtonOverrides? ToOverrides() => AnySet() ? new RadioButtonOverrides
    {
        DotColor = Parse(DotColor),
        BackgroundColor = Parse(BackgroundColor),
        BorderColor = Parse(BorderColor),
        BorderThickness = ToFloat(BorderThickness)
    } : null;

    internal static RadioButtonOverrideSection? From(RadioButtonOverrides? o)
    {
        if (o is null) return null;
        return new RadioButtonOverrideSection
        {
            DotColor = o.DotColor?.ToHex(),
            BackgroundColor = o.BackgroundColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex(),
            BorderThickness = ToDouble(o.BorderThickness)
        };
    }

    private bool AnySet() =>
        DotColor is not null || BackgroundColor is not null || BorderColor is not null || BorderThickness is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable slider overrides.</summary>
public sealed class SliderOverrideSection
{
    public string? ActiveColor { get; set; }
    public string? InactiveColor { get; set; }
    public string? ThumbColor { get; set; }
    public double? TrackHeight { get; set; }
    public double? ThumbWidth { get; set; }
    public double? ThumbHeight { get; set; }
    public double? ThumbRadius { get; set; }

    internal SliderOverrides? ToOverrides() => AnySet() ? new SliderOverrides
    {
        ActiveColor = Parse(ActiveColor),
        InactiveColor = Parse(InactiveColor),
        ThumbColor = Parse(ThumbColor),
        TrackHeight = ToFloat(TrackHeight),
        ThumbWidth = ToFloat(ThumbWidth),
        ThumbHeight = ToFloat(ThumbHeight),
        ThumbRadius = ToFloat(ThumbRadius)
    } : null;

    internal static SliderOverrideSection? From(SliderOverrides? o)
    {
        if (o is null) return null;
        return new SliderOverrideSection
        {
            ActiveColor = o.ActiveColor?.ToHex(),
            InactiveColor = o.InactiveColor?.ToHex(),
            ThumbColor = o.ThumbColor?.ToHex(),
            TrackHeight = ToDouble(o.TrackHeight),
            ThumbWidth = ToDouble(o.ThumbWidth),
            ThumbHeight = ToDouble(o.ThumbHeight),
            ThumbRadius = ToDouble(o.ThumbRadius)
        };
    }

    private bool AnySet() =>
        ActiveColor is not null || InactiveColor is not null || ThumbColor is not null
        || TrackHeight is not null || ThumbWidth is not null || ThumbHeight is not null || ThumbRadius is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable dropdown overrides.</summary>
public sealed class DropdownOverrideSection
{
    public string? ButtonColor { get; set; }
    public string? BorderColor { get; set; }
    public string? MenuColor { get; set; }
    public string? SelectionColor { get; set; }
    public string? HoverColor { get; set; }
    public string? ChevronColor { get; set; }
    public string? SelectionAccentColor { get; set; }

    public double? ButtonHeight { get; set; }
    public double? BorderThickness { get; set; }
    public double? ButtonCornerRadius { get; set; }
    public double? MenuMinWidth { get; set; }
    public double? ItemSpacing { get; set; }
    public double? MenuCornerRadius { get; set; }
    public double? ItemCornerRadius { get; set; }
    public double? MenuGap { get; set; }

    public int? VisibleItemCount { get; set; }

    public double? ButtonPaddingLeft { get; set; }
    public double? ButtonPaddingTop { get; set; }
    public double? ButtonPaddingRight { get; set; }
    public double? ButtonPaddingBottom { get; set; }

    public double? MenuPaddingLeft { get; set; }
    public double? MenuPaddingTop { get; set; }
    public double? MenuPaddingRight { get; set; }
    public double? MenuPaddingBottom { get; set; }

    public double? ItemPaddingLeft { get; set; }
    public double? ItemPaddingTop { get; set; }
    public double? ItemPaddingRight { get; set; }
    public double? ItemPaddingBottom { get; set; }

    internal DropdownOverrides? ToOverrides() => AnySet() ? new DropdownOverrides
    {
        ButtonHeight = ToFloat(ButtonHeight),
        ButtonColor = Parse(ButtonColor),
        BorderColor = Parse(BorderColor),
        BorderThickness = ToFloat(BorderThickness),
        ButtonCornerRadius = ToFloat(ButtonCornerRadius),
        ButtonPadding = ParseButtonPadding(),
        MenuMinWidth = ToFloat(MenuMinWidth),
        VisibleItemCount = VisibleItemCount,
        ItemSpacing = ToFloat(ItemSpacing),
        MenuColor = Parse(MenuColor),
        MenuCornerRadius = ToFloat(MenuCornerRadius),
        MenuPadding = ParseMenuPadding(),
        ItemPadding = ParseItemPadding(),
        ItemCornerRadius = ToFloat(ItemCornerRadius),
        SelectionColor = Parse(SelectionColor),
        HoverColor = Parse(HoverColor),
        ChevronColor = Parse(ChevronColor),
        MenuGap = ToFloat(MenuGap),
        SelectionAccentColor = Parse(SelectionAccentColor)
    } : null;

    internal static DropdownOverrideSection? From(DropdownOverrides? o)
    {
        if (o is null) return null;
        return new DropdownOverrideSection
        {
            ButtonHeight = ToDouble(o.ButtonHeight),
            ButtonColor = o.ButtonColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex(),
            BorderThickness = ToDouble(o.BorderThickness),
            ButtonCornerRadius = ToDouble(o.ButtonCornerRadius),
            ButtonPaddingLeft = o.ButtonPadding?.Left,
            ButtonPaddingTop = o.ButtonPadding?.Top,
            ButtonPaddingRight = o.ButtonPadding?.Right,
            ButtonPaddingBottom = o.ButtonPadding?.Bottom,
            MenuMinWidth = ToDouble(o.MenuMinWidth),
            VisibleItemCount = o.VisibleItemCount,
            ItemSpacing = ToDouble(o.ItemSpacing),
            MenuColor = o.MenuColor?.ToHex(),
            MenuCornerRadius = ToDouble(o.MenuCornerRadius),
            MenuPaddingLeft = o.MenuPadding?.Left,
            MenuPaddingTop = o.MenuPadding?.Top,
            MenuPaddingRight = o.MenuPadding?.Right,
            MenuPaddingBottom = o.MenuPadding?.Bottom,
            ItemPaddingLeft = o.ItemPadding?.Left,
            ItemPaddingTop = o.ItemPadding?.Top,
            ItemPaddingRight = o.ItemPadding?.Right,
            ItemPaddingBottom = o.ItemPadding?.Bottom,
            ItemCornerRadius = ToDouble(o.ItemCornerRadius),
            SelectionColor = o.SelectionColor?.ToHex(),
            HoverColor = o.HoverColor?.ToHex(),
            ChevronColor = o.ChevronColor?.ToHex(),
            MenuGap = ToDouble(o.MenuGap),
            SelectionAccentColor = o.SelectionAccentColor?.ToHex()
        };
    }

    private bool AnySet() =>
        ButtonColor is not null || BorderColor is not null || MenuColor is not null
        || SelectionColor is not null || HoverColor is not null || ChevronColor is not null
        || SelectionAccentColor is not null
        || ButtonHeight is not null || BorderThickness is not null || ButtonCornerRadius is not null
        || MenuMinWidth is not null || ItemSpacing is not null || MenuCornerRadius is not null
        || ItemCornerRadius is not null || MenuGap is not null
        || VisibleItemCount is not null
        || ButtonPaddingLeft is not null || ButtonPaddingTop is not null
        || ButtonPaddingRight is not null || ButtonPaddingBottom is not null
        || MenuPaddingLeft is not null || MenuPaddingTop is not null
        || MenuPaddingRight is not null || MenuPaddingBottom is not null
        || ItemPaddingLeft is not null || ItemPaddingTop is not null
        || ItemPaddingRight is not null || ItemPaddingBottom is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();

    private EdgeInsets? ParseButtonPadding()
    {
        if (ButtonPaddingLeft is null && ButtonPaddingTop is null && ButtonPaddingRight is null && ButtonPaddingBottom is null)
        {
            return null;
        }
        var left = (float)(ButtonPaddingLeft ?? 0);
        var top = (float)(ButtonPaddingTop ?? 0);
        var right = (float)(ButtonPaddingRight ?? 0);
        var bottom = (float)(ButtonPaddingBottom ?? 0);

        if (left == top && top == right && right == bottom)
        {
            return EdgeInsets.All(left);
        }

        if (left == right && top == bottom)
        {
            return EdgeInsets.Only(left, top);
        }

        return EdgeInsets.Only(left, top, right, bottom);
    }

    private EdgeInsets? ParseMenuPadding()
    {
        if (MenuPaddingLeft is null && MenuPaddingTop is null && MenuPaddingRight is null && MenuPaddingBottom is null)
        {
            return null;
        }
        var left = (float)(MenuPaddingLeft ?? 0);
        var top = (float)(MenuPaddingTop ?? 0);
        var right = (float)(MenuPaddingRight ?? 0);
        var bottom = (float)(MenuPaddingBottom ?? 0);

        if (left == top && top == right && right == bottom)
        {
            return EdgeInsets.All(left);
        }

        if (left == right && top == bottom)
        {
            return EdgeInsets.Only(left, top);
        }

        return EdgeInsets.Only(left, top, right, bottom);
    }

    private EdgeInsets? ParseItemPadding()
    {
        if (ItemPaddingLeft is null && ItemPaddingTop is null && ItemPaddingRight is null && ItemPaddingBottom is null)
        {
            return null;
        }
        var left = (float)(ItemPaddingLeft ?? 0);
        var top = (float)(ItemPaddingTop ?? 0);
        var right = (float)(ItemPaddingRight ?? 0);
        var bottom = (float)(ItemPaddingBottom ?? 0);

        if (left == top && top == right && right == bottom)
        {
            return EdgeInsets.All(left);
        }

        if (left == right && top == bottom)
        {
            return EdgeInsets.Only(left, top);
        }

        return EdgeInsets.Only(left, top, right, bottom);
    }
}

/// <summary>Serializable progress bar overrides.</summary>
public sealed class ProgressBarOverrideSection
{
    public double? Height { get; set; }
    public double? CornerRadius { get; set; }
    public double? BorderThickness { get; set; }
    public double? FillPadding { get; set; }

    public string? FillColor { get; set; }
    public string? TrackColor { get; set; }
    public string? BorderColor { get; set; }

    internal ProgressBarOverrides? ToOverrides() => AnySet() ? new ProgressBarOverrides
    {
        Height = ToFloat(Height),
        CornerRadius = ToFloat(CornerRadius),
        BorderThickness = ToFloat(BorderThickness),
        FillPadding = ToFloat(FillPadding),
        FillColor = Parse(FillColor),
        TrackColor = Parse(TrackColor),
        BorderColor = Parse(BorderColor)
    } : null;

    internal static ProgressBarOverrideSection? From(ProgressBarOverrides? o)
    {
        if (o is null) return null;
        return new ProgressBarOverrideSection
        {
            Height = ToDouble(o.Height),
            CornerRadius = ToDouble(o.CornerRadius),
            BorderThickness = ToDouble(o.BorderThickness),
            FillPadding = ToDouble(o.FillPadding),
            FillColor = o.FillColor?.ToHex(),
            TrackColor = o.TrackColor?.ToHex(),
            BorderColor = o.BorderColor?.ToHex()
        };
    }

    private bool AnySet() =>
        Height is not null || CornerRadius is not null || BorderThickness is not null || FillPadding is not null
        || FillColor is not null || TrackColor is not null || BorderColor is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable focus overrides.</summary>
public sealed class FocusOverrideSection
{
    public string? OutlineColor { get; set; }
    public double? OutlineWidth { get; set; }

    internal FocusOverrides? ToOverrides() => AnySet() ? new FocusOverrides
    {
        OutlineColor = Parse(OutlineColor),
        OutlineWidth = ToFloat(OutlineWidth)
    } : null;

    internal static FocusOverrideSection? From(FocusOverrides? o)
    {
        if (o is null) return null;
        return new FocusOverrideSection
        {
            OutlineColor = o.OutlineColor?.ToHex(),
            OutlineWidth = ToDouble(o.OutlineWidth)
        };
    }

    private bool AnySet() => OutlineColor is not null || OutlineWidth is not null;

    private static float? ToFloat(double? value) => value.HasValue ? (float)value.Value : null;
    private static double? ToDouble(float? value) => value.HasValue ? (double)value.Value : null;
    private static Vector4? Parse(string? hex) => hex is null ? null : hex.FromHex();
}

/// <summary>Serializable theme overrides (mirrors <see cref="ThemeOverrides"/>).</summary>
public sealed class ThemeOverridesSection
{
    public ColorOverrideSection? Colors { get; set; }
    public TypographyOverrideSection? Typography { get; set; }
    public ButtonOverrideSection? Button { get; set; }
    public TextFieldOverrideSection? TextField { get; set; }
    public CheckboxOverrideSection? Checkbox { get; set; }
    public RadioButtonOverrideSection? RadioButton { get; set; }
    public SliderOverrideSection? Slider { get; set; }
    public DropdownOverrideSection? Dropdown { get; set; }
    public ProgressBarOverrideSection? ProgressBar { get; set; }
    public FocusOverrideSection? Focus { get; set; }

    /// <summary>Converts this section into a <see cref="ThemeOverrides"/> model.</summary>
    public ThemeOverrides ToOverrides() => new()
    {
        Colors = Colors?.ToOverrides(),
        Typography = Typography?.ToOverrides(),
        Button = Button?.ToOverrides(),
        TextField = TextField?.ToOverrides(),
        Checkbox = Checkbox?.ToOverrides(),
        RadioButton = RadioButton?.ToOverrides(),
        Slider = Slider?.ToOverrides(),
        Dropdown = Dropdown?.ToOverrides(),
        ProgressBar = ProgressBar?.ToOverrides(),
        Focus = Focus?.ToOverrides()
    };

    /// <summary>Builds a section from a <see cref="ThemeOverrides"/> model.</summary>
    public static ThemeOverridesSection FromOverrides(ThemeOverrides o) => new()
    {
        Colors = ColorOverrideSection.From(o.Colors),
        Typography = TypographyOverrideSection.From(o.Typography),
        Button = ButtonOverrideSection.From(o.Button),
        TextField = TextFieldOverrideSection.From(o.TextField),
        Checkbox = CheckboxOverrideSection.From(o.Checkbox),
        RadioButton = RadioButtonOverrideSection.From(o.RadioButton),
        Slider = SliderOverrideSection.From(o.Slider),
        Dropdown = DropdownOverrideSection.From(o.Dropdown),
        ProgressBar = ProgressBarOverrideSection.From(o.ProgressBar),
        Focus = FocusOverrideSection.From(o.Focus)
    };
}

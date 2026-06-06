using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Input;
using Gui.Widgets.Inventory;
using OpenTK.Mathematics;

namespace Gui.Widgets.Framework;

/// <summary>
///     A set of named colors that form the basis of a visual theme.
///     Follows Material-style semantic naming: each surface color has a
///     corresponding "on" color for content placed on top of it.
/// </summary>
public readonly struct ColorScheme
{
    /// <summary>High-emphasis accent color used for buttons and active elements.</summary>
    public Vector4 Primary { get; init; }

    /// <summary>Content color drawn on top of <see cref="Primary" />.</summary>
    public Vector4 OnPrimary { get; init; }

    /// <summary>Medium-emphasis accent color.</summary>
    public Vector4 Secondary { get; init; }

    /// <summary>Content color drawn on top of <see cref="Secondary" />.</summary>
    public Vector4 OnSecondary { get; init; }

    /// <summary>Card and panel background color — the default surface layer.</summary>
    public Vector4 Surface { get; init; }

    /// <summary>Content color drawn on top of <see cref="Surface" />.</summary>
    public Vector4 OnSurface { get; init; }

    /// <summary>Low-emphasis content color drawn on top of <see cref="Surface" />.</summary>
    public Vector4 OnSurfaceVariant { get; init; }

    /// <summary>Window and screen background color (deepest layer).</summary>
    public Vector4 Background { get; init; }

    /// <summary>Content color drawn on top of <see cref="Background" />.</summary>
    public Vector4 OnBackground { get; init; }

    /// <summary>Border and divider color. Supports alpha via the W component.</summary>
    public Vector4 Border { get; init; }

    /// <summary>Destructive action and error state color.</summary>
    public Vector4 Error { get; init; }

    /// <summary>Content color drawn on top of <see cref="Error" />.</summary>
    public Vector4 OnError { get; init; }

    /// <summary>
    ///     Surface tone deeper than <see cref="Surface" /> (between Surface and Background).
    ///     Used for recessed panels such as the Handbook category sidebar.
    /// </summary>
    public Vector4 SurfaceLow { get; init; }

    /// <summary>
    ///     Surface tone lighter than <see cref="Surface" />.
    ///     Used for raised elements such as slot cells or recipe-output tiles.
    /// </summary>
    public Vector4 SurfaceHigh { get; init; }

    /// <summary>
    ///     Translucent overlay applied as the background of an interactive element
    ///     while the pointer hovers over it.
    /// </summary>
    public Vector4 StateHover { get; init; }

    /// <summary>
    ///     Translucent overlay applied as the background of an element in its
    ///     active / selected state (e.g. current tab, selected list row).
    /// </summary>
    public Vector4 StateSelected { get; init; }

    /// <summary>
    ///     Secondary border tone, fainter than <see cref="Border" />. Used for
    ///     internal dividers that should recede visually.
    /// </summary>
    public Vector4 OutlineVariant { get; init; }

    public static ColorScheme Default()
    {
        return new ColorScheme
        {
            Primary = new Vector4(0.82f, 0.67f, 0.33f, 1.0f),
            OnPrimary = new Vector4(0.20f, 0.14f, 0.06f, 1.0f),
            Secondary = new Vector4(0.70f, 0.58f, 0.37f, 1.0f),
            OnSecondary = new Vector4(0.20f, 0.14f, 0.06f, 1.0f),
            Surface = new Vector4(0.16f, 0.12f, 0.07f, 1.0f),
            OnSurface = new Vector4(0.87f, 0.80f, 0.63f, 1.0f),
            OnSurfaceVariant = new Vector4(0.67f, 0.63f, 0.53f, 1.0f),
            Background = new Vector4(0.09f, 0.07f, 0.04f, 1.0f),
            OnBackground = new Vector4(0.87f, 0.80f, 0.63f, 1.0f),
            Border = new Vector4(0.55f, 0.42f, 0.20f, 0.55f),
            Error = new Vector4(0.82f, 0.22f, 0.15f, 1.0f),
            OnError = new Vector4(0.94f, 0.89f, 0.74f, 1.0f),
            SurfaceLow = new Vector4(0.118f, 0.090f, 0.063f, 1.0f),
            SurfaceHigh = new Vector4(0.208f, 0.165f, 0.094f, 1.0f),
            StateHover = new Vector4(0.87f, 0.80f, 0.63f, 0.08f),
            StateSelected = new Vector4(0.82f, 0.67f, 0.33f, 0.18f),
            OutlineVariant = new Vector4(0.55f, 0.42f, 0.20f, 0.25f)
        };
    }
}

/// <summary>
///     A set of named text styles for common typographic roles.
/// </summary>
public readonly struct TextTheme
{
    public TextStyle Headline { get; init; }
    public TextStyle Body { get; init; }
    public TextStyle Label { get; init; }

    public static TextTheme Default(ColorScheme? colors = null)
    {
        var c = colors ?? ColorScheme.Default();
        return new TextTheme
        {
            Headline =
                new TextStyle { FontSize = 24, Weight = FontWeight.Bold, Color = c.OnBackground },
            Body = new TextStyle { FontSize = 14, Color = c.OnBackground },
            Label = new TextStyle { FontSize = 12, Color = c.OnBackground }
        };
    }
}

/// <summary>
///     Style tokens for <see cref="ProgressBar" />: track height, corner radius,
///     border thickness, and the fill / track / border colors.
/// </summary>
public readonly struct ProgressBarStyle
{
    /// <summary>Creates a new <see cref="ProgressBarStyle" /> with default field values.</summary>
    public ProgressBarStyle()
    {
        FillPadding = 4f;
    }

    /// <summary>Height of the progress bar track in pixels.</summary>
    public float Height { get; init; }

    /// <summary>Corner radius applied to both track and fill.</summary>
    public float CornerRadius { get; init; }

    /// <summary>Border thickness around the track.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Color of the filled (progress) portion.</summary>
    public Vector4 FillColor { get; init; }

    /// <summary>BackgroundTexture color of the track.</summary>
    public Vector4 TrackColor { get; init; }

    /// <summary>Border color around the track.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Inset applied inside the track around the fill bar. Defaults to 4 px on all sides.</summary>
    public float FillPadding { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />.
    /// </summary>
    public static ProgressBarStyle Default(
        ColorScheme colors
    )
    {
        return new ProgressBarStyle
        {
            Height = 10,
            CornerRadius = 5,
            BorderThickness = 1,
            FillColor = colors.Primary,
            TrackColor = colors.Surface,
            BorderColor = colors.Border
        };
    }
}

/// <summary>
///     Style tokens for <see cref="Dropdown{T}" />: trigger button and
///     floating menu appearance.
/// </summary>
public readonly struct DropdownStyle
{
    /// <summary>Height of the trigger button in pixels.</summary>
    public float ButtonHeight { get; init; }

    /// <summary>Background color of the trigger button.</summary>
    public Vector4 ButtonColor { get; init; }

    /// <summary>Border color shared by button and menu.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Border thickness in pixels shared by button and menu.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Corner radius applied to the trigger button.</summary>
    public float ButtonCornerRadius { get; init; }

    /// <summary>Horizontal padding inside the trigger button.</summary>
    public EdgeInsets ButtonPadding { get; init; }

    /// <summary>Minimum width of the floating menu in pixels.</summary>
    public float MenuMinWidth { get; init; }

    /// <summary>
    ///     Number of items visible before the menu starts scrolling.
    ///     The menu height is computed as <c>VisibleItemCount × itemHeight + spacing</c>.
    /// </summary>
    public int VisibleItemCount { get; init; }

    /// <summary>Vertical spacing between items in pixels.</summary>
    public float ItemSpacing { get; init; }

    /// <summary>Background color of the floating menu.</summary>
    public Vector4 MenuColor { get; init; }

    /// <summary>Corner radius applied to the floating menu.</summary>
    public float MenuCornerRadius { get; init; }

    /// <summary>Padding inside the floating menu container.</summary>
    public EdgeInsets MenuPadding { get; init; }

    /// <summary>Padding inside each menu item row.</summary>
    public EdgeInsets ItemPadding { get; init; }

    /// <summary>Corner radius applied to each menu item.</summary>
    public float ItemCornerRadius { get; init; }

    /// <summary>Background color of the currently selected item.</summary>
    public Vector4 SelectionColor { get; init; }

    /// <summary>Background color of a hovered (but not selected) item.</summary>
    public Vector4 HoverColor { get; init; }

    /// <summary>Text style for both the button label and menu items.</summary>
    public TextStyle TextStyle { get; init; }

    /// <summary>Color of the trigger button's chevron glyph.</summary>
    public Vector4 ChevronColor { get; init; }

    /// <summary>Vertical gap in pixels between the trigger button and the floating menu.</summary>
    public float MenuGap { get; init; }

    /// <summary>
    ///     Accent color for the selected item's label and check mark.
    /// </summary>
    public Vector4 SelectionAccentColor { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />
    ///     and <paramref name="text" />.
    /// </summary>
    public static DropdownStyle Default(
        ColorScheme colors,
        TextTheme text
    )
    {
        return new DropdownStyle
        {
            ButtonHeight = 35,
            ButtonColor = colors.Surface,
            BorderColor = colors.Border,
            BorderThickness = 1,
            ButtonCornerRadius = 6,
            ButtonPadding = EdgeInsets.Symmetric(horizontal: 10),
            MenuMinWidth = 150,
            VisibleItemCount = 5,
            ItemSpacing = 2,
            MenuColor = new Vector4(
                colors.SurfaceHigh.X,
                colors.SurfaceHigh.Y,
                colors.SurfaceHigh.Z,
                1f
            ),
            MenuCornerRadius = 6,
            MenuPadding = EdgeInsets.All(5),
            ItemPadding = EdgeInsets.Symmetric(
                4,
                8
            ),
            ItemCornerRadius = 4,
            SelectionColor = colors.StateSelected,
            HoverColor = colors.StateHover,
            TextStyle = new TextStyle { FontSize = text.Body.FontSize, Color = colors.OnSurface },
            ChevronColor = colors.OnSurfaceVariant,
            MenuGap = 4,
            SelectionAccentColor = colors.Primary
        };
    }
}

/// <summary>
///     Style tokens for <see cref="Checkbox" />: colors, border, corner radius,
///     and an optional label text style.
/// </summary>
public readonly struct CheckboxStyle
{
    /// <summary>Color of the inner check mark / fill dot.</summary>
    public Vector4 CheckColor { get; init; }

    /// <summary>Background color of the checkbox box.</summary>
    public Vector4 BackgroundColor { get; init; }

    /// <summary>Border color of the checkbox box.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Border thickness in pixels.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Corner radius applied to the box.</summary>
    public float CornerRadius { get; init; }

    /// <summary>Text style used for the optional label.</summary>
    public TextStyle LabelStyle { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />
    ///     and <paramref name="text" />.
    /// </summary>
    public static CheckboxStyle Default(ColorScheme colors, TextTheme text)
    {
        return new CheckboxStyle
        {
            CheckColor = colors.Primary,
            BackgroundColor = colors.SurfaceHigh,
            BorderColor = colors.Border,
            BorderThickness = 1.5f,
            CornerRadius = 2f,
            LabelStyle =
                new TextStyle { FontSize = text.Body.FontSize, Color = colors.OnSurface }
        };
    }
}

/// <summary>
///     Style tokens for <see cref="RadioButton{T}" />: colors, border, and
///     an optional label text style.
/// </summary>
public readonly struct RadioButtonStyle
{
    /// <summary>Color of the inner selection dot.</summary>
    public Vector4 DotColor { get; init; }

    /// <summary>Background color of the outer circle.</summary>
    public Vector4 BackgroundColor { get; init; }

    /// <summary>Border color of the outer circle.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Border thickness in pixels.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Text style used for the optional label.</summary>
    public TextStyle LabelStyle { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />
    ///     and <paramref name="text" />.
    /// </summary>
    public static RadioButtonStyle Default(ColorScheme colors, TextTheme text)
    {
        return new RadioButtonStyle
        {
            DotColor = colors.Primary,
            BackgroundColor = colors.SurfaceHigh,
            BorderColor = colors.Border,
            BorderThickness = 1.5f,
            LabelStyle =
                new TextStyle { FontSize = text.Body.FontSize, Color = colors.OnSurface }
        };
    }
}

/// <summary>
///     Style tokens for <see cref="Slider" />: track and thumb colors and dimensions.
///     Individual color params on <see cref="Slider" /> override the corresponding
///     style field when non-null.
/// </summary>
public readonly struct SliderStyle
{
    /// <summary>Color of the filled (active) portion of the track.</summary>
    public Vector4 ActiveColor { get; init; }

    /// <summary>Color of the unfilled (inactive) portion of the track.</summary>
    public Vector4 InactiveColor { get; init; }

    /// <summary>Color of the thumb handle.</summary>
    public Vector4 ThumbColor { get; init; }

    /// <summary>Height of the track in pixels.</summary>
    public float TrackHeight { get; init; }

    /// <summary>Width of the thumb in pixels.</summary>
    public float ThumbWidth { get; init; }

    /// <summary>Height of the thumb in pixels.</summary>
    public float ThumbHeight { get; init; }

    /// <summary>Corner radius of the thumb.</summary>
    public float ThumbRadius { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />.
    /// </summary>
    public static SliderStyle Default(ColorScheme colors)
    {
        return new SliderStyle
        {
            ActiveColor = colors.Primary,
            InactiveColor = colors.OutlineVariant,
            ThumbColor = colors.Primary,
            TrackHeight = 8,
            ThumbWidth = 12,
            ThumbHeight = 24,
            ThumbRadius = 2
        };
    }
}

/// <summary>Named visual variants for <see cref="Button" />.</summary>
public enum ButtonVariant
{
    /// <summary>High-emphasis; filled with primary color.</summary>
    Primary,

    /// <summary>Medium-emphasis; outlined with transparent fill.</summary>
    Secondary,

    /// <summary>Destructive action; filled with error color.</summary>
    Danger,

    /// <summary>Low-emphasis; no background or border.</summary>
    Ghost
}

/// <summary>Style tokens for a single <see cref="ButtonVariant" />.</summary>
public readonly struct ButtonVariantStyle
{
    /// <summary>Background color in the resting state.</summary>
    public Vector4 BackgroundColor { get; init; }

    /// <summary>Background color while the pointer hovers over the button.</summary>
    public Vector4 HoverBackgroundColor { get; init; }

    /// <summary>Background color while the button is pressed.</summary>
    public Vector4 PressBackgroundColor { get; init; }

    /// <summary>Border color.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Border thickness in pixels.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Corner radius applied to the button container.</summary>
    public float CornerRadius { get; init; }
}

/// <summary>
///     Style tokens for <see cref="Button" />: per-variant colors and shared padding.
/// </summary>
public readonly struct ButtonStyle
{
    /// <summary>Style tokens for <see cref="ButtonVariant.Primary" />.</summary>
    public ButtonVariantStyle Primary { get; init; }

    /// <summary>Style tokens for <see cref="ButtonVariant.Secondary" />.</summary>
    public ButtonVariantStyle Secondary { get; init; }

    /// <summary>Style tokens for <see cref="ButtonVariant.Danger" />.</summary>
    public ButtonVariantStyle Danger { get; init; }

    /// <summary>Style tokens for <see cref="ButtonVariant.Ghost" />.</summary>
    public ButtonVariantStyle Ghost { get; init; }

    /// <summary>Inner padding applied around the child content.</summary>
    public EdgeInsets Padding { get; init; }

    /// <summary>
    ///     Returns the <see cref="ButtonVariantStyle" /> for <paramref name="variant" />.
    /// </summary>
    public ButtonVariantStyle this[ButtonVariant variant] => variant switch
    {
        ButtonVariant.Primary => Primary,
        ButtonVariant.Secondary => Secondary,
        ButtonVariant.Danger => Danger,
        ButtonVariant.Ghost => Ghost,
        _ => Primary
    };

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />.
    /// </summary>
    public static ButtonStyle Default(ColorScheme colors)
    {
        return new ButtonStyle
        {
            Padding = EdgeInsets.Symmetric(10, 20),
            Primary = new ButtonVariantStyle
            {
                BackgroundColor = colors.Primary,
                HoverBackgroundColor = colors.Primary + new Vector4(0.1f, 0.1f, 0.1f, 0f),
                PressBackgroundColor = colors.Primary + new Vector4(-0.08f, -0.08f, -0.08f, 0f),
                BorderColor = Vector4.Zero,
                BorderThickness = 0f,
                CornerRadius = 4f
            },
            Secondary = new ButtonVariantStyle
            {
                BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
                HoverBackgroundColor = colors.StateSelected,
                PressBackgroundColor = new Vector4(
                    colors.Primary.X, colors.Primary.Y, colors.Primary.Z, 0.2f
                ),
                BorderColor = colors.Border,
                BorderThickness = 1f,
                CornerRadius = 4f
            },
            Danger = new ButtonVariantStyle
            {
                BackgroundColor = colors.Error,
                HoverBackgroundColor = colors.Error + new Vector4(0.1f, 0.1f, 0.1f, 0f),
                PressBackgroundColor = colors.Error + new Vector4(-0.08f, -0.08f, -0.08f, 0f),
                BorderColor = Vector4.Zero,
                BorderThickness = 0f,
                CornerRadius = 4f
            },
            Ghost = new ButtonVariantStyle
            {
                BackgroundColor = new Vector4(0f, 0f, 0f, 0f),
                HoverBackgroundColor = colors.StateHover,
                PressBackgroundColor = new Vector4(
                    colors.OnSurface.X, colors.OnSurface.Y, colors.OnSurface.Z, 0.15f
                ),
                BorderColor = Vector4.Zero,
                BorderThickness = 0f,
                CornerRadius = 4f
            }
        };
    }
}

/// <summary>
///     Immutable bundle of visual design tokens (colors and typography) that
///     can be provided to a widget subtree via <see cref="Theme" />.
/// </summary>
public class ThemeData
{
    public ThemeData(
        ColorScheme? colorScheme = null,
        TextTheme? textTheme = null,
        ProgressBarStyle? progressBarStyle = null,
        ItemSlotStyle? itemSlotStyle = null,
        HotkeyBadgeStyle? hotkeyBadgeStyle = null,
        DropdownStyle? dropdownStyle = null,
        CheckboxStyle? checkboxStyle = null,
        RadioButtonStyle? radioButtonStyle = null,
        SliderStyle? sliderStyle = null,
        ButtonStyle? buttonStyle = null
    )
    {
        ColorScheme = colorScheme ?? ColorScheme.Default();
        TextTheme = textTheme ?? TextTheme.Default(ColorScheme);
        ProgressBarStyle = progressBarStyle
                           ?? ProgressBarStyle.Default(ColorScheme);
        ItemSlotStyle = itemSlotStyle
                        ?? ItemSlotStyle.DefaultTheme(ColorScheme);
        HotkeyBadgeStyle = hotkeyBadgeStyle
                           ?? HotkeyBadgeStyle.Default(ColorScheme);
        DropdownStyle = dropdownStyle
                        ?? DropdownStyle.Default(ColorScheme, TextTheme);
        CheckboxStyle = checkboxStyle
                        ?? CheckboxStyle.Default(ColorScheme, TextTheme);
        RadioButtonStyle = radioButtonStyle
                           ?? RadioButtonStyle.Default(ColorScheme, TextTheme);
        SliderStyle = sliderStyle
                      ?? SliderStyle.Default(ColorScheme);
        ButtonStyle = buttonStyle
                      ?? ButtonStyle.Default(ColorScheme);
    }

    /// <summary>
    ///     Observable holder for the global <see cref="Default" /> theme. Subscribe via
    ///     <see cref="ListenableBuilder" /> to rebuild a subtree when the global theme changes.
    /// </summary>
    public static ValueNotifier<ThemeData> DefaultNotifier { get; internal set; } =
        new(new ThemeData());

    public ColorScheme ColorScheme { get; init; }
    public TextTheme TextTheme { get; init; }

    /// <summary>Style tokens for <see cref="ProgressBar" />.</summary>
    public ProgressBarStyle ProgressBarStyle { get; init; }

    /// <summary>Style tokens for <see cref="FlatItemSlot" /> and <see cref="NineSliceItemSlot" />.</summary>
    public ItemSlotStyle ItemSlotStyle { get; init; }

    /// <summary>Style tokens for <see cref="HotkeyBadge" />.</summary>
    public HotkeyBadgeStyle HotkeyBadgeStyle { get; init; }

    /// <summary>Style tokens for <see cref="Dropdown{T}" />.</summary>
    public DropdownStyle DropdownStyle { get; init; }

    /// <summary>Style tokens for <see cref="Checkbox" />.</summary>
    public CheckboxStyle CheckboxStyle { get; init; }

    /// <summary>Style tokens for <see cref="RadioButton{T}" />.</summary>
    public RadioButtonStyle RadioButtonStyle { get; init; }

    /// <summary>Style tokens for <see cref="Slider" />.</summary>
    public SliderStyle SliderStyle { get; init; }

    /// <summary>Style tokens for <see cref="Button" />.</summary>
    public ButtonStyle ButtonStyle { get; init; }

    /// <summary>
    ///     Convenience accessor for <see cref="DefaultNotifier" />.<c>Value</c>. Reassigning
    ///     <c>Default</c> publishes the new instance to all <see cref="ListenableBuilder" />
    ///     subscribers; consumers that read via <see cref="Theme.Of" /> see the change
    ///     automatically when their containing <see cref="ListenableBuilder" /> rebuilds.
    /// </summary>
    public static ThemeData Default
    {
        get => DefaultNotifier.Value;
        internal set => DefaultNotifier.Value = value;
    }
}

/// <summary>
///     An <see cref="InheritedWidget" /> that provides <see cref="ThemeData" />
///     to its descendants. Use <see cref="Of" /> to read the current theme
///     from any <see cref="BuildContext" />.
/// </summary>
public class Theme(
    ThemeData data,
    Widget child,
    Key? key = null) : InheritedWidget(child,
    key)
{
    public ThemeData Data { get; } = data;

    public override bool UpdateShouldNotify(
        InheritedWidget oldWidget
    )
    {
        return !ReferenceEquals(
            Data,
            ((Theme)oldWidget).Data
        );
    }

    /// <summary>
    ///     Returns the <see cref="ThemeData" /> from the nearest <see cref="Theme" />
    ///     ancestor, or <see cref="ThemeData.Default" /> if none is found.
    /// </summary>
    public static ThemeData Of(
        BuildContext context
    )
    {
        var theme = context.DependOnInheritedWidgetOfExactType<Theme>();
        return theme?.Data ?? ThemeData.Default;
    }
}

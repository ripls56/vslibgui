using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Style tokens for a flat inventory slot widget, controlling slot size,
///     background color, border colors, and hover overlay.
///     Pass via <see cref="FlatItemSlot" /> constructor.
/// </summary>
/// <example>
///     <code>
/// // Basic 32px slot
/// new ItemSlotStyle { Size = 32 }
/// 
/// // Custom colors
/// new ItemSlotStyle
/// {
///     Size = 48,
///     BackgroundColor = new Vector4(0, 0, 0, 0.5f),
///     BorderColor = new Vector4(0.5f, 0.5f, 0.5f, 1f)
/// }
/// </code>
/// </example>
public struct ItemSlotStyle
{
    /// <summary>Slot widget side length in pixels.</summary>
    public float Size { get; init; }

    /// <summary>Custom background color. Overrides theme when set.</summary>
    public Vector4? BackgroundColor { get; init; }

    /// <summary>Custom border color. Overrides theme when set.</summary>
    public Vector4? BorderColor { get; init; }

    /// <summary>Custom border color on hover. Used with <see cref="BorderColor" /> for animation.</summary>
    public Vector4? BorderHoverColor { get; init; }

    /// <summary>Custom hover highlight overlay color. Overrides theme when set.</summary>
    public Vector4? HoverColor { get; init; }

    /// <summary>Custom item padding. Overrides theme when set.</summary>
    public EdgeInsets? Padding { get; init; }

    /// <summary>Default style: 48px slot, all colors from theme.</summary>
    public static ItemSlotStyle Default => new()
    {
        Size = 48,
        BackgroundColor = null,
        BorderColor = null,
        BorderHoverColor = null,
        HoverColor = null,
        Padding = null
    };

    /// <summary>Theme-level style with all colors resolved from <paramref name="colors" />.</summary>
    public static ItemSlotStyle DefaultTheme(ColorScheme colors)
    {
        return new ItemSlotStyle
        {
            Size = 48f,
            BackgroundColor = colors.SurfaceHigh,
            BorderColor = colors.Border,
            BorderHoverColor = colors.Primary,
            HoverColor = colors.StateHover,
            Padding = EdgeInsets.All(3)
        };
    }
}

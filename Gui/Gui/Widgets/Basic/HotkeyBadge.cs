using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     Style tokens for <see cref="HotkeyBadge" />: background, border,
///     corner radius, text style, and internal padding.
/// </summary>
public struct HotkeyBadgeStyle
{
    /// <summary>Badge background color.</summary>
    public Vector4 BackgroundColor { get; init; }

    /// <summary>Badge border color.</summary>
    public Vector4 BorderColor { get; init; }

    /// <summary>Border thickness in pixels.</summary>
    public float BorderThickness { get; init; }

    /// <summary>Corner radius applied to all four corners.</summary>
    public float CornerRadius { get; init; }

    /// <summary>Text style for the key label inside the badge.</summary>
    public TextStyle TextStyle { get; init; }

    /// <summary>Internal padding around the label or icon.</summary>
    public EdgeInsets Padding { get; init; }

    /// <summary>Minimum badge width so narrow keys like "I" don't look too thin.</summary>
    public float MinWidth { get; init; }

    /// <summary>
    ///     Creates a default style derived from the given <paramref name="colors" />.
    /// </summary>
    public static HotkeyBadgeStyle Default(
        ColorScheme colors
    )
    {
        return new HotkeyBadgeStyle
        {
            BackgroundColor = new Vector4(
                0f,
                0f,
                0f,
                0.45f
            ),
            BorderColor = new Vector4(
                1f,
                1f,
                1f,
                0.2f
            ),
            BorderThickness = 1f,
            CornerRadius = 3f,
            TextStyle = new TextStyle
            {
                FontSize = 16,
                Color = new Vector4(
                    0.92f,
                    0.92f,
                    0.92f,
                    1f
                ),
                OutlineWidth = 0f
            },
            Padding = EdgeInsets.All(8),
            MinWidth = 18f
        };
    }
}

/// <summary>
///     A keyboard key or mouse button badge rendered as a styled rounded
///     rectangle with a text label or icon inside. Reads its visual style
///     from <see cref="HotkeyBadgeStyle" /> in the current <see cref="Theme" />.
/// </summary>
public class HotkeyBadge : StatelessWidget
{
    /// <summary>Creates a hotkey badge with a text label.</summary>
    public HotkeyBadge(
        string label,
        HotkeyBadgeStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Label = label;
        StyleOverride = style;
    }

    /// <summary>Creates a hotkey badge with an icon.</summary>
    public HotkeyBadge(
        Widget iconWidget,
        HotkeyBadgeStyle? style = null,
        Framework.Key? key = null
    ) : base(key)
    {
        IconWidget = iconWidget;
        StyleOverride = style;
    }

    /// <summary>
    ///     Text label for the key (e.g. "Ctrl", "E", "Shift").
    ///     Mutually exclusive with <see cref="IconWidget" />.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    ///     Optional icon widget (e.g. Lucide mouse icon) displayed
    ///     instead of a text label.
    /// </summary>
    public Widget? IconWidget { get; }

    /// <summary>
    ///     Optional style override. When <c>null</c>, uses
    ///     <see cref="Theme" />.
    /// </summary>
    public HotkeyBadgeStyle? StyleOverride { get; }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        var style = StyleOverride ?? Theme.Of(context).HotkeyBadgeStyle;

        var content = IconWidget
                      ?? new Text(
                          Label ?? "",
                          style.TextStyle
                      );

        Widget box = new Container(
            new BoxStyle
            {
                Color = style.BackgroundColor,
                BorderThickness = style.BorderThickness,
                BorderColor = style.BorderColor,
                CornerRadius = new Vector4(style.CornerRadius),
                Padding = style.Padding
            },
            content
        );

        // Enforce minimum width without clamping wider content
        if (style.MinWidth > 0)
        {
            box = new ConstrainedBox(
                new LayoutConstraints(style.MinWidth),
                box
            );
        }

        return box;
    }
}

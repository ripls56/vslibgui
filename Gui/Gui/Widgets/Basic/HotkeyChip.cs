using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Widgets.Basic;

/// <summary>
///     Renders a resolved hotkey mapping as a compact inline row of key
///     badges. Modifier keys (Ctrl/Alt/Shift) and the bound key each get
///     their own <see cref="HotkeyBadge" />, joined by "+" separators. Mouse
///     buttons render as icons instead of text. The whole chip scales to the
///     surrounding font size so it fits within a line of running text.
/// </summary>
public class HotkeyChip : StatelessWidget
{
    private const int EscapeKeyCode = 50;

    /// <summary>Creates a hotkey chip for the given key mapping.</summary>
    /// <param name="mapping">Resolved key binding, or null when unavailable.</param>
    /// <param name="fallbackText">Label shown when <paramref name="mapping" /> is null.</param>
    /// <param name="fontSize">Surrounding font size the chip scales itself to.</param>
    /// <param name="key">Optional widget key for reconciliation.</param>
    public HotkeyChip(
        KeyCombination? mapping,
        string fallbackText,
        float fontSize,
        Framework.Key? key = null
    ) : base(key)
    {
        Mapping = mapping;
        FallbackText = fallbackText;
        FontSize = fontSize <= 0 ? 14f : fontSize;
    }

    /// <summary>Resolved key binding, or null when unavailable.</summary>
    public KeyCombination? Mapping { get; }

    /// <summary>Text shown when <see cref="Mapping" /> is null or unbound.</summary>
    public string FallbackText { get; }

    /// <summary>Surrounding font size the chip scales itself to.</summary>
    public float FontSize { get; }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        var style = ScaleStyle(Theme.Of(context).HotkeyBadgeStyle);

        if (Mapping == null || Mapping.KeyCode < 0)
        {
            return new HotkeyBadge(
                FallbackText,
                style
            );
        }

        var parts = new List<Widget>();
        AddModifierBadges(
            parts,
            style
        );
        AddKeyBadge(
            parts,
            Mapping.KeyCode,
            style
        );
        if (Mapping.SecondKeyCode is > 0)
        {
            AddKeyBadge(
                parts,
                Mapping.SecondKeyCode.Value,
                style
            );
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        return new Row(
            FontSize * 0.2f,
            crossAxisAlignment: CrossAxisAlignment.Center,
            mainAxisSize: MainAxisSize.Min,
            children: JoinWithSeparators(
                parts,
                style.TextStyle
            )
        );
    }

    private void AddModifierBadges(
        List<Widget> parts,
        HotkeyBadgeStyle style
    )
    {
        if (Mapping!.Ctrl)
        {
            parts.Add(new HotkeyBadge("Ctrl", style));
        }

        if (Mapping.Alt)
        {
            parts.Add(new HotkeyBadge("Alt", style));
        }

        if (Mapping.Shift)
        {
            parts.Add(new HotkeyBadge("Shift", style));
        }
    }

    private void AddKeyBadge(
        List<Widget> parts,
        int keyCode,
        HotkeyBadgeStyle style
    )
    {
        if (Mapping!.IsMouseButton(keyCode))
        {
            parts.Add(
                new HotkeyBadge(
                    BuildMouseIcon(
                        keyCode,
                        style.TextStyle.Color
                    ),
                    style
                )
            );
            return;
        }

        parts.Add(
            new HotkeyBadge(
                KeyName(keyCode),
                style
            )
        );
    }

    private Widget BuildMouseIcon(
        int keyCode,
        Vector4 color
    )
    {
        var path = (keyCode - KeyCombination.MouseStart) switch
        {
            0 => "textures/icons/mouse-left.svg",
            2 => "textures/icons/mouse-right.svg",
            _ => "textures/icons/mouse.svg"
        };
        return new Icon(
            "gui",
            path,
            FontSize,
            color
        );
    }

    private static string KeyName(
        int keyCode
    )
    {
        if (keyCode == EscapeKeyCode)
        {
            return "Esc";
        }

        return GlKeyNames.ToString((GlKeys)keyCode) ?? "?";
    }

    private static List<Widget> JoinWithSeparators(
        List<Widget> parts,
        TextStyle textStyle
    )
    {
        var result = new List<Widget>(parts.Count * 2 - 1);
        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                result.Add(new Text("+", textStyle));
            }

            result.Add(parts[i]);
        }

        return result;
    }

    private HotkeyBadgeStyle ScaleStyle(
        HotkeyBadgeStyle baseStyle
    )
    {
        var textStyle = baseStyle.TextStyle;
        textStyle.FontSize = FontSize;
        return new HotkeyBadgeStyle
        {
            BackgroundColor = baseStyle.BackgroundColor,
            BorderColor = baseStyle.BorderColor,
            BorderThickness = baseStyle.BorderThickness,
            CornerRadius = baseStyle.CornerRadius,
            TextStyle = textStyle,
            Padding = EdgeInsets.Symmetric(
                FontSize * 0.12f,
                FontSize * 0.38f
            ),
            MinWidth = FontSize * 1.15f
        };
    }
}

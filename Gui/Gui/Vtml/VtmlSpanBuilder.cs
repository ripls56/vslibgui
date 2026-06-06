using System;
using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Inventory;
using Gui.Widgets.Layout;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Vtml;

/// <summary>
///     Converts a flat list of <see cref="VtmlInlineElement" /> objects into
///     an <see cref="InlineSpan" /> tree suitable for <see cref="RichText" />.
/// </summary>
public static class VtmlSpanBuilder
{
    private static readonly Vector4 LinkColor = new(
        0.39f,
        0.58f,
        0.93f,
        1f
    );

    /// <summary>
    ///     Builds an <see cref="InlineSpan" /> tree from VTML inline elements.
    /// </summary>
    /// <param name="elements">Flat list of VTML inline elements.</param>
    /// <param name="baseStyle">Base text style for fallback.</param>
    /// <param name="onLinkClick">Optional link click callback.</param>
    /// <returns>Root <see cref="TextSpan" /> containing all children.</returns>
    public static InlineSpan Build(
        List<VtmlInlineElement> elements,
        TextStyle baseStyle,
        Action<string>? onLinkClick = null
    )
    {
        if (elements.Count == 0)
        {
            return new TextSpan();
        }

        var children = new List<InlineSpan>();
        foreach (var elem in elements)
        {
            children.Add(
                ConvertElement(
                    elem,
                    baseStyle,
                    onLinkClick
                )
            );
        }

        return new TextSpan(children: children);
    }

    private static InlineSpan ConvertElement(
        VtmlInlineElement elem,
        TextStyle baseStyle,
        Action<string>? onLinkClick
    )
    {
        switch (elem)
        {
            case VtmlTextRun run:
                return ConvertTextRun(
                    run,
                    onLinkClick
                );

            case VtmlLineBreak:
                return new TextSpan("\n");

            case VtmlClearFloat:
                return new ClearSpan();

            case VtmlIconRun icon:
                return ConvertIcon(icon);

            case VtmlHotkeyRun hotkey:
                return ConvertHotkey(hotkey);

            case VtmlItemStackRun item:
                return ConvertItemStack(item);

            default:
                return new TextSpan("");
        }
    }

    private static InlineSpan ConvertTextRun(
        VtmlTextRun run,
        Action<string>? onLinkClick
    )
    {
        if (run.Href != null && onLinkClick != null)
        {
            var href = run.Href;
            return new TextSpan(
                run.Text,
                new SpanStyle
                {
                    Color = new Vector4(
                        LinkColor.X,
                        LinkColor.Y,
                        LinkColor.Z,
                        run.Style.Color.W
                    ),
                    FontSize = run.Style.FontSize,
                    FontFamily = run.Style.FontFamily,
                    Weight = run.Style.Weight,
                    Boldness = run.Style.Boldness,
                    OutlineWidth = run.Style.OutlineWidth,
                    OutlineColor = run.Style.OutlineColor,
                    GlowWidth = run.Style.GlowWidth,
                    GlowColor = run.Style.GlowColor,
                    Decoration = TextDecoration.Underline
                },
                recognizer: new TapGestureRecognizer { OnTap = () => onLinkClick(href) }
            );
        }

        return new TextSpan(
            run.Text,
            new SpanStyle
            {
                Color = run.Style.Color,
                FontSize = run.Style.FontSize,
                FontFamily = run.Style.FontFamily,
                Weight = run.Style.Weight,
                Boldness = run.Style.Boldness,
                OutlineWidth = run.Style.OutlineWidth,
                OutlineColor = run.Style.OutlineColor,
                GlowWidth = run.Style.GlowWidth,
                GlowColor = run.Style.GlowColor
            }
        );
    }

    private static InlineSpan ConvertIcon(
        VtmlIconRun icon
    )
    {
        var domain = "game";
        string path;

        if (icon.Path != null)
        {
            var colonIdx = icon.Path.IndexOf(':');
            if (colonIdx > 0)
            {
                domain = icon.Path.Substring(
                    0,
                    colonIdx
                );
                path = icon.Path.Substring(colonIdx + 1);
            }
            else
            {
                path = icon.Path;
            }
        }
        else if (icon.Name != null)
        {
            path = icon.Name.StartsWith("wp")
                ? $"icons/waypoint/{icon.Name.Substring(2).ToLowerInvariant()}.svg"
                : $"icons/{icon.Name}.svg";
        }
        else
        {
            return new TextSpan("");
        }

        return new WidgetSpan(
            new Icon(
                domain,
                path,
                icon.Size
            )
        );
    }

    private static InlineSpan ConvertHotkey(
        VtmlHotkeyRun hotkey
    )
    {
        var fontSize = hotkey.Style.FontSize > 0
            ? hotkey.Style.FontSize
            : 14f;
        var mapping = ResolveHotkeyMapping(hotkey.HotkeyCode);
        return new WidgetSpan(
            new HotkeyChip(
                mapping,
                $"[{hotkey.HotkeyCode}]",
                fontSize
            )
        );
    }

    private static InlineSpan ConvertItemStack(
        VtmlItemStackRun item
    )
    {
        var size = item.FontHeight * 1.3f * item.RSize;

        Widget child = new SizedBox(
            size,
            size
        );

        var capi = GuiModSystem.Instance?.Capi;
        if (capi != null)
        {
            var codes = item.Code.Split('|');
            if (codes.Length > 0 && !string.IsNullOrEmpty(codes[0]))
            {
                var code = codes[0].Trim();
                var collectible = item.ItemType == "item"
                    ? capi.World.GetItem(new AssetLocation(code))
                    : (CollectibleObject?)capi.World.GetBlock(
                        new AssetLocation(code)
                    );
                if (collectible != null)
                {
                    child = new ItemStackDisplay(
                        new ItemStack(collectible),
                        size,
                        size,
                        Math.Max(
                            16,
                            (int)size
                        )
                    );
                }
            }
        }

        var floatType = item.FloatType == VtmlFloat.None
            ? VtmlFloat.Inline
            : item.FloatType;

        return new WidgetSpan(
            child,
            @float: floatType
        );
    }

    private static KeyCombination? ResolveHotkeyMapping(
        string hotkeyCode
    )
    {
        return GuiModSystem.Instance?.Capi?.Input
            .GetHotKeyByCode(hotkeyCode)?.CurrentMapping;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using ExCSS;
using Gui.Core.Basic;
using Gui.Rendering.Text;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using FontWeight = Gui.Rendering.Text.FontWeight;

namespace Gui.Vtml;

/// <summary>
///     Converts a VTML token tree (produced by <see cref="VtmlParser.Tokenize" />)
///     into a flat list of <see cref="VtmlInlineElement" /> objects suitable for
///     layout by <see cref="RenderRichText" />.
/// </summary>
public static class VtmlConverter
{
    /// <summary>
    ///     Parses a raw VTML string and converts it to inline elements.
    /// </summary>
    /// <param name="vtml">The VTML markup string.</param>
    /// <param name="baseStyle">Base text style applied to unstyled text.</param>
    /// <param name="logger">Logger for parse errors (null to suppress).</param>
    /// <returns>List of inline elements ready for layout.</returns>
    public static List<VtmlInlineElement> Convert(
        string vtml,
        TextStyle baseStyle,
        ILogger? logger = null
    )
    {
        if (string.IsNullOrEmpty(vtml))
        {
            return [];
        }

        var tokens = VtmlParser.Tokenize(
            logger ?? NullLogger.Instance,
            vtml
        );

        var result = new List<VtmlInlineElement>();
        var fontStack = new Stack<TextStyle>();
        fontStack.Push(baseStyle);

        ConvertTokens(
            tokens,
            result,
            fontStack,
            null
        );
        return result;
    }

    private static void ConvertTokens(
        VtmlToken[] tokens,
        List<VtmlInlineElement> result,
        Stack<TextStyle> fontStack,
        string? currentHref
    )
    {
        foreach (var token in tokens)
        {
            ConvertToken(
                token,
                result,
                fontStack,
                currentHref
            );
        }
    }

    private static void ConvertToken(
        VtmlToken token,
        List<VtmlInlineElement> result,
        Stack<TextStyle> fontStack,
        string? currentHref
    )
    {
        if (token is VtmlTextToken textToken)
        {
            if (!string.IsNullOrEmpty(textToken.Text))
            {
                EmitTextWithLineBreaks(
                    textToken.Text,
                    fontStack.Peek(),
                    currentHref,
                    result
                );
            }

            return;
        }

        if (token is not VtmlTagToken tag)
        {
            return;
        }

        switch (tag.Name)
        {
            case "br":
                result.Add(new VtmlLineBreak());
                break;

            case "strong":
            case "b":
                ConvertWithStyle(
                    tag,
                    result,
                    fontStack,
                    currentHref,
                    style =>
                    {
                        style.Weight = FontWeight.Bold;
                        style.Boldness = 0.5f;
                        return style;
                    }
                );
                break;

            case "i":
                ConvertWithStyle(
                    tag,
                    result,
                    fontStack,
                    currentHref,
                    style =>
                    {
                        style.Weight = FontWeight.Italic;
                        return style;
                    }
                );
                break;

            case "font":
                ConvertWithStyle(
                    tag,
                    result,
                    fontStack,
                    currentHref,
                    style => ApplyFontAttributes(
                        tag,
                        style
                    )
                );
                break;

            case "code":
                ConvertWithStyle(
                    tag,
                    result,
                    fontStack,
                    currentHref,
                    style =>
                    {
                        style.FontFamily = "monospace";
                        return style;
                    }
                );
                break;

            case "a":
            {
                tag.Attributes.TryGetValue(
                    "href",
                    out var href
                );
                ConvertChildren(
                    tag,
                    result,
                    fontStack,
                    href ?? currentHref
                );
                break;
            }

            case "hk":
            case "hotkey":
            {
                var text = tag.ContentText?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    text = text switch
                    {
                        "leftmouse" => "primarymouse",
                        "rightmouse" => "secondarymouse",
                        "toolmode" => "toolmodeselect",
                        _ => text
                    };
                    result.Add(
                        new VtmlHotkeyRun { HotkeyCode = text, Style = fontStack.Peek() }
                    );
                }

                break;
            }

            case "icon":
            {
                tag.Attributes.TryGetValue(
                    "name",
                    out var iconName
                );
                tag.Attributes.TryGetValue(
                    "path",
                    out var iconPath
                );
                iconName ??= tag.ContentText?.Trim();

                result.Add(
                    new VtmlIconRun
                    {
                        Name = iconName, Path = iconPath, Size = fontStack.Peek().FontSize
                    }
                );
                break;
            }

            case "itemstack":
                ConvertItemStack(
                    tag,
                    result,
                    fontStack
                );
                break;

            case "clear":
                result.Add(new VtmlClearFloat());
                break;

            default:
                // Unknown tag — process children so content isn't lost
                ConvertChildren(
                    tag,
                    result,
                    fontStack,
                    currentHref
                );
                break;
        }
    }

    private static void ConvertWithStyle(
        VtmlTagToken tag,
        List<VtmlInlineElement> result,
        Stack<TextStyle> fontStack,
        string? currentHref, System.Func<TextStyle, TextStyle> modify
    )
    {
        var style = modify(fontStack.Peek());
        fontStack.Push(style);
        ConvertChildren(
            tag,
            result,
            fontStack,
            currentHref
        );
        fontStack.Pop();
    }

    private static void ConvertChildren(
        VtmlTagToken tag,
        List<VtmlInlineElement> result,
        Stack<TextStyle> fontStack,
        string? currentHref
    )
    {
        foreach (var child in tag.ChildElements)
        {
            ConvertToken(
                child,
                result,
                fontStack,
                currentHref
            );
        }
    }

    private static void EmitTextWithLineBreaks(
        string text,
        TextStyle style,
        string? href,
        List<VtmlInlineElement> result
    )
    {
        // Split on \r\n or \n, emitting VtmlLineBreak between segments
        var lines = text.Split(
            ["\r\n", "\n"],
            StringSplitOptions.None
        );
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                result.Add(new VtmlLineBreak());
            }

            if (lines[i].Length > 0)
            {
                result.Add(
                    new VtmlTextRun { Text = lines[i], Style = style, Href = href }
                );
            }
        }
    }

    private static void ConvertItemStack(
        VtmlTagToken tag,
        List<VtmlInlineElement> result,
        Stack<TextStyle> fontStack
    )
    {
        var floatType = VtmlFloat.Inline;
        if (tag.Attributes.TryGetValue(
                "floattype",
                out var ftStr
            ))
        {
            floatType = ftStr.ToLowerInvariant() switch
            {
                "left" => VtmlFloat.Left,
                "right" => VtmlFloat.Right,
                "none" => VtmlFloat.None,
                _ => VtmlFloat.Inline
            };
        }

        tag.Attributes.TryGetValue(
            "code",
            out var code
        );
        code ??= tag.ContentText?.Trim() ?? "";

        var itemType = tag.Attributes.GetValueOrDefault("type", "block");

        var rsize = 1f;
        if (tag.Attributes.TryGetValue(
                "rsize",
                out var rsizeStr
            ))
        {
            float.TryParse(
                rsizeStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out rsize
            );
        }

        float offx = 0f, offy = 0f;
        if (tag.Attributes.TryGetValue(
                "offx",
                out var offxStr
            ))
        {
            float.TryParse(
                offxStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out offx
            );
        }

        if (tag.Attributes.TryGetValue(
                "offy",
                out var offyStr
            ))
        {
            float.TryParse(
                offyStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out offy
            );
        }

        result.Add(
            new VtmlItemStackRun
            {
                Code = code,
                ItemType = itemType,
                FloatType = floatType,
                RSize = rsize,
                OffX = offx,
                OffY = offy,
                FontHeight = fontStack.Peek().FontSize
            }
        );
    }

    private static TextStyle ApplyFontAttributes(
        VtmlTagToken tag,
        TextStyle style
    )
    {
        if (tag.Attributes.TryGetValue(
                "size",
                out var sizeStr
            ) &&
            float.TryParse(
                sizeStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var size
            ))
        {
            style.FontSize = size;
        }

        if (tag.Attributes.TryGetValue(
                "color",
                out var colorStr
            ))
        {
            if (TryParseHexColor(colorStr, out var color) ||
                TryParseNamedColor(colorStr, out color))
            {
                style.Color = color;
            }
        }

        if (tag.Attributes.TryGetValue(
                "opacity",
                out var opStr
            ) &&
            float.TryParse(
                opStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var opacity
            ))
        {
            var c = style.Color;
            style.Color = new Vector4(
                c.X,
                c.Y,
                c.Z,
                c.W * opacity
            );
        }

        if (tag.Attributes.TryGetValue(
                "weight",
                out var weightStr
            ) &&
            weightStr == "bold")
        {
            style.Weight = FontWeight.Bold;
            style.Boldness = 0.5f;
        }

        if (tag.Attributes.TryGetValue(
                "family",
                out var family
            ))
        {
            style.FontFamily = family;
        }

        if (tag.Attributes.TryGetValue(
                "align",
                out var alignStr
            ))
        {
            style.Align = alignStr.ToLowerInvariant() switch
            {
                "right" => TextAlignment.Right,
                "center" => TextAlignment.Center,
                _ => TextAlignment.Left
            };
        }

        return style;
    }

    internal static bool TryParseHexColor(
        string text,
        out Vector4 color
    )
    {
        color = Vector4.One;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var span = text.AsSpan();
        if (span[0] == '#')
        {
            span = span.Slice(1);
        }

        if (span.Length == 6)
        {
            if (int.TryParse(
                    span.Slice(
                        0,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var r
                ) &&
                int.TryParse(
                    span.Slice(
                        2,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var g
                ) &&
                int.TryParse(
                    span.Slice(
                        4,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var b
                ))
            {
                color = new Vector4(
                    r / 255f,
                    g / 255f,
                    b / 255f,
                    1f
                );
                return true;
            }
        }
        else if (span.Length == 8)
        {
            if (int.TryParse(
                    span.Slice(
                        0,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var a
                ) &&
                int.TryParse(
                    span.Slice(
                        2,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var r
                ) &&
                int.TryParse(
                    span.Slice(
                        4,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var g
                ) &&
                int.TryParse(
                    span.Slice(
                        6,
                        2
                    ),
                    NumberStyles.HexNumber,
                    null,
                    out var b
                ))
            {
                color = new Vector4(
                    r / 255f,
                    g / 255f,
                    b / 255f,
                    a / 255f
                );
                return true;
            }
        }

        return false;
    }

    private static bool TryParseNamedColor(string name, out Vector4 color)
    {
        var c = Colors.GetColor(name);
        if (c == null)
        {
            color = Vector4.One;
            return false;
        }

        color = new Vector4(c.Value.R / 255f, c.Value.G / 255f, c.Value.B / 255f, c.Value.A / 255f);
        return true;
    }

    /// <summary>
    ///     Minimal logger that discards all messages. Used when no logger
    ///     is provided to <see cref="Convert" />.
    /// </summary>
    internal sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public bool TraceLog { get; set; }

        public event LogEntryDelegate EntryAdded
        {
            add { }
            remove { }
        }

        public void ClearWatchers()
        {
        }

        public void Log(
            EnumLogType logType,
            string format,
            params object[] args
        )
        {
        }

        public void Log(
            EnumLogType logType,
            string message
        )
        {
        }

        public void LogException(
            EnumLogType logType,
            Exception e
        )
        {
        }

        public void Chat(
            string format,
            params object[] args
        )
        {
        }

        public void Chat(
            string message
        )
        {
        }

        public void Event(
            string format,
            params object[] args
        )
        {
        }

        public void Event(
            string message
        )
        {
        }

        public void StoryEvent(
            string format,
            params object[] args
        )
        {
        }

        public void StoryEvent(
            string message
        )
        {
        }

        public void Build(
            string format,
            params object[] args
        )
        {
        }

        public void Build(
            string message
        )
        {
        }

        public void VerboseDebug(
            string format,
            params object[] args
        )
        {
        }

        public void VerboseDebug(
            string message
        )
        {
        }

        public void Debug(
            string format,
            params object[] args
        )
        {
        }

        public void Debug(
            string message
        )
        {
        }

        public void Notification(
            string format,
            params object[] args
        )
        {
        }

        public void Notification(
            string message
        )
        {
        }

        public void Warning(
            string format,
            params object[] args
        )
        {
        }

        public void Warning(
            string message
        )
        {
        }

        public void Warning(
            Exception e
        )
        {
        }

        public void Error(
            string format,
            params object[] args
        )
        {
        }

        public void Error(
            string message
        )
        {
        }

        public void Error(
            Exception e
        )
        {
        }

        public void Fatal(
            string format,
            params object[] args
        )
        {
        }

        public void Fatal(
            string message
        )
        {
        }

        public void Fatal(
            Exception e
        )
        {
        }

        public void Audit(
            string format,
            params object[] args
        )
        {
        }

        public void Audit(
            string message
        )
        {
        }

        public void Worldgen(
            string format,
            params object[] args
        )
        {
        }
    }
}

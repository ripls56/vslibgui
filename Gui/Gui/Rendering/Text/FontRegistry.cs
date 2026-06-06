using System.Collections.Generic;
using SkiaSharp;

namespace Gui.Rendering.Text;

public static class FontRegistry
{
    private static readonly Dictionary<string, string> FontMappings = new()
    {
        { "sans-serif", "Arial" },
        { "serif", "Times New Roman" },
        { "monospace", "Courier New" }
    };

    private static readonly Dictionary<(string, FontWeight), SKTypeface> CustomTypefaces = new();

    public static string ResolveFontFamily(
        string name
    ) =>
        FontMappings.GetValueOrDefault(name.ToLower(),
            name); // Fallback to literal name (system font)

    public static void RegisterFontAlias(
        string alias,
        string systemFamily
    ) =>
        FontMappings[alias.ToLower()] = systemFamily;

    public static void RegisterCustomFont(
        string familyName,
        FontWeight weight,
        SKTypeface typeface
    )
    {
        if (typeface != null)
        {
            CustomTypefaces[(familyName.ToLower(), weight)] = typeface;
        }
    }

    public static SKTypeface? GetCustomTypeface(
        string familyName,
        FontWeight weight
    ) =>
        CustomTypefaces.GetValueOrDefault((familyName.ToLower(), weight));
}

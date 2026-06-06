using System;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui;

/// <summary>
///     Root configuration object for the GUI mod, loaded from <c>gui.json</c>.
/// </summary>
public class GuiConfig
{
    /// <summary>
    ///     User-defined color overrides. Set any subset of the nine semantic colors;
    ///     omitted fields keep their default values.
    /// </summary>
    public ThemeSection? Theme { get; set; }
}

/// <summary>
///     Color overrides for <see cref="ColorScheme" />. Each field is optional.
///     Colors must be hex strings in <c>#RRGGBB</c> or <c>#RRGGBBAA</c> format.
/// </summary>
public class ThemeSection
{
    /// <summary>High-emphasis accent color used for buttons and active elements.</summary>
    public string? Primary { get; set; }

    /// <summary>Content color drawn on top of <see cref="Primary" />.</summary>
    public string? OnPrimary { get; set; }

    /// <summary>Medium-emphasis accent color.</summary>
    public string? Secondary { get; set; }

    /// <summary>Content color drawn on top of <see cref="Secondary" />.</summary>
    public string? OnSecondary { get; set; }

    /// <summary>Card and panel background color.</summary>
    public string? Surface { get; set; }

    /// <summary>Content color drawn on top of <see cref="Surface" />.</summary>
    public string? OnSurface { get; set; }

    /// <summary>Low-emphasis content color drawn on top of <see cref="Surface" />.</summary>
    public string? OnSurfaceVariant { get; set; }

    /// <summary>Window and screen background color.</summary>
    public string? Background { get; set; }

    /// <summary>Content color drawn on top of <see cref="Background" />.</summary>
    public string? OnBackground { get; set; }

    /// <summary>Border and divider color. Supports alpha via <c>#RRGGBBAA</c>.</summary>
    public string? Border { get; set; }

    /// <summary>Destructive action and error state color.</summary>
    public string? Error { get; set; }

    /// <summary>Content color drawn on top of <see cref="Error" />.</summary>
    public string? OnError { get; set; }

    /// <summary>Surface tone deeper than <see cref="Surface" /> (recessed panels).</summary>
    public string? SurfaceLow { get; set; }

    /// <summary>Surface tone lighter than <see cref="Surface" /> (raised elements).</summary>
    public string? SurfaceHigh { get; set; }

    /// <summary>Hover overlay color (supports alpha via <c>#RRGGBBAA</c>).</summary>
    public string? StateHover { get; set; }

    /// <summary>Selected / active overlay color (supports alpha via <c>#RRGGBBAA</c>).</summary>
    public string? StateSelected { get; set; }

    /// <summary>Subtle divider color (supports alpha via <c>#RRGGBBAA</c>).</summary>
    public string? OutlineVariant { get; set; }

    /// <summary>
    ///     Builds a <see cref="ColorScheme" /> by merging this section's overrides with
    ///     <paramref name="fallback" />. Null fields and unparseable hex strings fall back
    ///     to the corresponding <paramref name="fallback" /> value.
    /// </summary>
    public ColorScheme ToColorScheme(ColorScheme fallback)
    {
        return new ColorScheme
        {
            Primary = Parse(Primary, fallback.Primary),
            OnPrimary = Parse(OnPrimary, fallback.OnPrimary),
            Secondary = Parse(Secondary, fallback.Secondary),
            OnSecondary = Parse(OnSecondary, fallback.OnSecondary),
            Surface = Parse(Surface, fallback.Surface),
            OnSurface = Parse(OnSurface, fallback.OnSurface),
            OnSurfaceVariant = Parse(OnSurfaceVariant, fallback.OnSurfaceVariant),
            Background = Parse(Background, fallback.Background),
            OnBackground = Parse(OnBackground, fallback.OnBackground),
            Border = Parse(Border, fallback.Border),
            Error = Parse(Error, fallback.Error),
            OnError = Parse(OnError, fallback.OnError),
            SurfaceLow = Parse(SurfaceLow, fallback.SurfaceLow),
            SurfaceHigh = Parse(SurfaceHigh, fallback.SurfaceHigh),
            StateHover = Parse(StateHover, fallback.StateHover),
            StateSelected = Parse(StateSelected, fallback.StateSelected),
            OutlineVariant = Parse(OutlineVariant, fallback.OutlineVariant)
        };
    }

    /// <summary>
    ///     Creates a <see cref="ThemeSection" /> populated with the hex representations
    ///     of all colors in <paramref name="scheme" />. Used to write the template config
    ///     on first launch so users have a ready-to-edit starting point.
    /// </summary>
    internal static ThemeSection FromColorScheme(ColorScheme scheme)
    {
        return new ThemeSection
        {
            Primary = ToHex(scheme.Primary),
            OnPrimary = ToHex(scheme.OnPrimary),
            Secondary = ToHex(scheme.Secondary),
            OnSecondary = ToHex(scheme.OnSecondary),
            Surface = ToHex(scheme.Surface),
            OnSurface = ToHex(scheme.OnSurface),
            OnSurfaceVariant = ToHex(scheme.OnSurfaceVariant),
            Background = ToHex(scheme.Background),
            OnBackground = ToHex(scheme.OnBackground),
            Border = ToHex(scheme.Border),
            Error = ToHex(scheme.Error),
            OnError = ToHex(scheme.OnError),
            SurfaceLow = ToHex(scheme.SurfaceLow),
            SurfaceHigh = ToHex(scheme.SurfaceHigh),
            StateHover = ToHex(scheme.StateHover),
            StateSelected = ToHex(scheme.StateSelected),
            OutlineVariant = ToHex(scheme.OutlineVariant)
        };
    }

    private static Vector4 Parse(string? hex, Vector4 fallback)
    {
        if (hex == null)
        {
            return fallback;
        }

        try
        {
            return hex.FromHex();
        }
        catch
        {
            return fallback;
        }
    }

    private static string ToHex(Vector4 c)
    {
        var r = (int)MathF.Round(c.X * 255);
        var g = (int)MathF.Round(c.Y * 255);
        var b = (int)MathF.Round(c.Z * 255);
        var a = (int)MathF.Round(c.W * 255);
        return a == 255 ? $"#{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }
}

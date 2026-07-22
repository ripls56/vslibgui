using Gui.Widgets.Framework;

namespace Gui.Theming;

/// <summary>A named, ready-to-apply theme shown in the settings gallery.</summary>
public sealed record ThemePreset
{
    /// <summary>Creates a preset.</summary>
    public ThemePreset(string id, string displayName, ColorScheme scheme)
    {
        Id = id;
        DisplayName = displayName;
        Scheme = scheme;
    }

    /// <summary>The asset/file identifier, e.g. <c>neon_city</c>.</summary>
    public string Id { get; }

    /// <summary>The human-readable label, e.g. <c>Neon City</c>.</summary>
    public string DisplayName { get; }

    /// <summary>The resolved color scheme to apply.</summary>
    public ColorScheme Scheme { get; }

    /// <summary>
    ///     Turns a file id like <c>neon_city</c> into a display label like <c>Neon City</c> by
    ///     splitting on underscores and title-casing each word.
    /// </summary>
    public static string DeriveDisplayName(string id)
    {
        var parts = id.Split('_');
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length > 0)
            {
                parts[i] = char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant();
            }
        }

        return string.Join(' ', parts);
    }
}

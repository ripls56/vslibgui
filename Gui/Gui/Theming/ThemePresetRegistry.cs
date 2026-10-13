using System;
using System.Collections.Generic;
using System.Linq;
using Gui.Widgets.Framework;

namespace Gui.Theming;

/// <summary>
///     Catalogs the shipped theme presets. The Vintage Story asset API cannot enumerate files,
///     so the known ids are listed explicitly; each is loaded on demand through a supplied
///     loader (the runtime wires it to <c>capi.Assets</c>).
/// </summary>
public static class ThemePresetRegistry
{
    private static readonly string[] Ids =
    {
        "arctic",
        "ash",
        "aurora",
        "autumn",
        "blood_moon",
        "candy",
        "copper",
        "default",
        "ember",
        "forest",
        "magenta",
        "mauve",
        "midnight",
        "mist",
        "neon_city",
        "ocean",
        "rose_gold",
        "sage",
        "slate",
        "sunset",
        "toxic",
        "void"
    };

    /// <summary>The file ids of every shipped preset, alphabetical.</summary>
    public static IReadOnlyCollection<string> PresetIds => Ids;

    /// <summary>
    ///     Loads every preset whose section resolves. <paramref name="loadSection" /> maps an id
    ///     to its deserialized <see cref="ThemeSection" /> (or null if the asset is unavailable).
    /// </summary>
    public static IReadOnlyList<ThemePreset> LoadAll(Func<string, ThemeSection?> loadSection)
    {
        return Ids
            .Select(id => (Id: id, Section: loadSection(id)))
            .Where(x => x.Section is not null)
            .Select
            (x => new ThemePreset
                (
                    x.Id,
                    ThemePreset.DeriveDisplayName(x.Id),
                    x.Section!.ToColorScheme(ColorScheme.Default())
                )
            )
            .ToList();
    }
}

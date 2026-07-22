using Gui;
using Gui.Widgets.Framework;

namespace Gui.Theming;

/// <summary>
///     Applies a preset to the live UI and produces the config object to persist. Split out from
///     the dialog so the logic is testable without a GUI.
/// </summary>
public static class ThemeApplier
{
    /// <summary>Applies a preset with no overrides (shim — equivalent to empty overrides).</summary>
    public static void Apply(ThemePreset preset) => Apply(preset, new ThemeOverrides());

    /// <summary>Applies a preset with the given overrides layered on top.</summary>
    public static void Apply(ThemePreset preset, ThemeOverrides overrides)
    {
        var baseTheme = new ThemeData(preset.Scheme);
        ThemeData.DefaultNotifier.Value = overrides.ApplyOnto(baseTheme);
    }

    /// <summary>Builds persisted config without overrides (shim).</summary>
    public static GuiConfig BuildPersistedConfig(ThemePreset preset, bool onboarded)
        => BuildPersistedConfig(preset, new ThemeOverrides(), onboarded);

    /// <summary>Builds persisted config including overrides.</summary>
    public static GuiConfig BuildPersistedConfig(
        ThemePreset preset, ThemeOverrides overrides, bool onboarded)
    {
        return new GuiConfig
        {
            Theme = ThemeSection.FromColorScheme(preset.Scheme),
            Custom = ThemeOverridesSection.FromOverrides(overrides),
            Onboarded = onboarded
        };
    }
}

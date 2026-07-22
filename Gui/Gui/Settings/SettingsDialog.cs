using System;
using System.Collections.Generic;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Settings;

/// <summary>
///     The settings/onboarding window. Shown once automatically on first world entry (welcome
///     banner) and re-opened any time via <c>.ui settings</c>.
/// </summary>
public class SettingsDialog : GuiBase
{
    private readonly bool _isFirstRun;
    private readonly Action<ThemePreset> _onPersist;
    private readonly Action<ThemeOverrides>? _onPersistOverrides;
    private readonly IReadOnlyList<ThemePreset> _presets;
    private ThemeOverrides _overrides;
    private ThemePreset _selectedPreset;

    /// <summary>Creates the settings dialog (no overrides).</summary>
    public SettingsDialog
    (
        ICoreClientAPI capi,
        IReadOnlyList<ThemePreset> presets,
        bool isFirstRun,
        Action<ThemePreset> onPersist
    )
        : this(capi, presets, isFirstRun, onPersist, new ThemeOverrides(), null)
    {
    }

    /// <summary>Creates the settings dialog with initial overrides and an overrides-persist callback.</summary>
    public SettingsDialog
    (
        ICoreClientAPI capi,
        IReadOnlyList<ThemePreset> presets,
        bool isFirstRun,
        Action<ThemePreset> onPersist,
        ThemeOverrides initialOverrides,
        Action<ThemeOverrides>? onPersistOverrides
    )
        : base(capi)
    {
        _presets = presets;
        _isFirstRun = isFirstRun;
        _onPersist = onPersist;
        _onPersistOverrides = onPersistOverrides;
        _selectedPreset = ActivePreset(presets);
        _overrides = initialOverrides.DeepCopy();
    }

    /// <inheritdoc />
    protected override WindowConfig CreateWindowConfig()
    {
        return new WindowConfig
        {
            Draggable = true,
            Resizable = false,
            Size = new Vector2(capi.Render.FrameWidth * 0.5f, capi.Render.FrameHeight * 0.7f),
        };
    }

    /// <inheritdoc />
    protected override Widget Build()
    {
        return new WindowFrame
        (
            _isFirstRun ? "Welcome" : "Settings",
            onClose: () => { TryClose(); },
            fillHeight: true,
            child: new SettingsContent
            (
                _presets,
                _selectedPreset,
                OnPresetSelected,
                Confirm,
                () => TryClose(),
                _isFirstRun,
                _overrides,
                OnOverridesChanged
            )
        );
    }

    private void OnPresetSelected(ThemePreset preset) => _selectedPreset = preset;

    private void OnOverridesChanged(ThemeOverrides draft) => _overrides = draft;

    private void Confirm()
    {
        ThemeApplier.Apply(_selectedPreset, _overrides);
        _onPersist(_selectedPreset);
        _onPersistOverrides?.Invoke(_overrides);
        TryClose();
    }

    private static ThemePreset ActivePreset(IReadOnlyList<ThemePreset> presets)
    {
        var primary = ThemeData.DefaultNotifier.Value.ColorScheme.Primary;
        foreach (var preset in presets)
        {
            if (preset.Scheme.Primary == primary)
            {
                return preset;
            }
        }
        return presets[0];
    }
}

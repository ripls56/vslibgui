using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Theming;

namespace Gui.Settings;

public class SettingsContent : StatefulWidget
{
    public SettingsContent
    (
        IReadOnlyList<ThemePreset> presets,
        ThemePreset selectedPreset,
        Action<ThemePreset> onPresetSelected,
        Action onConfirm,
        Action onSkip,
        bool isFirstRun,
        ThemeOverrides initialOverrides,
        Action<ThemeOverrides> onOverridesChanged
    )
    {
        Presets = presets;
        SelectedPreset = selectedPreset;
        OnPresetSelected = onPresetSelected;
        OnConfirm = onConfirm;
        OnSkip = onSkip;
        IsFirstRun = isFirstRun;
        InitialOverrides = initialOverrides;
        OnOverridesChanged = onOverridesChanged;
    }

    public IReadOnlyList<ThemePreset> Presets { get; }
    public ThemePreset SelectedPreset { get; }
    public Action<ThemePreset> OnPresetSelected { get; }
    public Action OnConfirm { get; }
    public Action OnSkip { get; }
    public bool IsFirstRun { get; }
    public ThemeOverrides InitialOverrides { get; }
    public Action<ThemeOverrides> OnOverridesChanged { get; }

    public override State CreateState() => new SettingsContentState();
}

internal sealed class SettingsContentState : State<SettingsContent>
{
    private int _activeTab;
    private int _resetNonce;

    public override void InitState()
    {
        base.InitState();
        _activeTab = 0;
        _resetNonce = 0;
    }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var tabs = new List<TabItem>
        {
            new()
            {
                Label = "Presets",
                Content = new ThemePresetGallery(Widget.Presets, Widget.OnPresetSelected)
            },
            new()
            {
                Label = "Customize",
                Content = new CustomizeTab
                (
                    Widget.InitialOverrides,
                    Widget.SelectedPreset,
                    Widget.OnOverridesChanged,
                    key: new ValueKey<string>($"customize-{_resetNonce}")
                )
            }
        };

        return new Container
        (
            new BoxStyle
            {
                Color = colors.Background,
            },
            child: new Column
            (
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 0,
                children:
                [
                    new Expanded
                    (
                        child: new TabView(tabs, initialIndex: _activeTab)
                        {
                            OnTabChanged = index => SetState(() => _activeTab = index)
                        }
                    ),
                    Footer(theme, colors)
                ]
            )
        );
    }

    private Widget Footer(ThemeData theme, ColorScheme colors)
    {
        var buttons = new List<Widget>
        {
            new Button
            (
                new Text("Skip", theme.TextTheme.Label),
                ButtonVariant.Ghost,
                onTap: _ => Widget.OnSkip()
            )
        };

        buttons.Add(new Spacer());
        if (_activeTab == 1)
        {
            buttons.Add
            (
                new Button
                (
                    new Text("Reset to preset", theme.TextTheme.Label),
                    ButtonVariant.Ghost,
                    onTap: _ =>
                    {
                        Widget.OnOverridesChanged(new ThemeOverrides());
                        SetState(() => _resetNonce++);
                    }
                )
            );
        }

        buttons.Add
        (
            new Button
            (
                new Text
                (
                    "Use this theme",
                    theme.TextTheme.Body with
                    {
                        Color = colors.OnPrimary, Weight = FontWeight.SemiBold
                    }
                ),
                ButtonVariant.Primary,
                onTap: _ => Widget.OnConfirm()
            )
        );

        return new Column
        (
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Divider(color: colors.OutlineVariant),
                new Padding
                (
                    edgeInsets: EdgeInsets.Ltrb(24, 16, 24, 16),
                    child: new Row
                    (
                        children: buttons,
                        spacing: 12
                    )
                )
            ]
        );
    }
}

using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Settings.CategoryEditors;
using Gui.Theming;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Settings;

/// <summary>The "Customize" tab: fine-tune the selected preset's theme.</summary>
public sealed class CustomizeTab : StatefulWidget
{
    /// <summary>Creates the customize tab.</summary>
    public CustomizeTab
    (
        ThemeOverrides initialDraft,
        ThemePreset preset,
        Action<ThemeOverrides> onDraftChanged,
        Widgets.Framework.Key? key = null
    )
        : base(key)
    {
        InitialDraft = initialDraft;
        Preset = preset;
        OnDraftChanged = onDraftChanged;
    }

    /// <summary>Initial draft overrides.</summary>
    public ThemeOverrides InitialDraft { get; }

    /// <summary>The selected preset.</summary>
    public ThemePreset Preset { get; }

    /// <summary>Callback when draft changes.</summary>
    public Action<ThemeOverrides> OnDraftChanged { get; }

    /// <inheritdoc />
    public override State CreateState() => new CustomizeTabState();
}

internal sealed class CustomizeTabState : State<CustomizeTab>
{
    private ThemeOverrides _draft = null!;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        _draft = Widget.InitialDraft.DeepCopy();
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var baseTheme = new ThemeData(Widget.Preset.Scheme);
        return new Padding
        (
            EdgeInsets.All(24),
            new Column
            (
                24,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    Preview(baseTheme),
                    new Expanded
                    (
                        new TabView
                        (
                            CategoryTabs(baseTheme),
                            TabPosition.Left
                        )
                        {
                            TabWidth = 130
                        }
                    )
                ]
            )
        );
    }

    private Widget Preview(ThemeData baseTheme)
    {
        var themedData = _draft.ApplyOnto(baseTheme);
        var scheme = themedData.ColorScheme;
        return new Theme
        (
            themedData,
            new Container
            (
                new BoxStyle
                {
                    Color = scheme.SurfaceHigh,
                    BorderColor = scheme.OutlineVariant,
                    BorderThickness = 1f,
                    CornerRadius = new Vector4(12f),
                    Padding = EdgeInsets.All(20)
                },
                new Column
                (
                    12,
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    children:
                    [
                        new Text("Live preview", themedData.TextTheme.Label),
                        new Row
                        (
                            12,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                new Button(new Text("Primary Action")),
                                new Button(new Text("Secondary"), ButtonVariant.Secondary),
                                new Expanded(child: new TextField())
                            ]
                        )
                    ]
                )
            )
        );
    }

    private List<TabItem> CategoryTabs(ThemeData baseTheme)
    {
        var changed = new Action
        (() =>
            {
                Widget.OnDraftChanged(_draft);
                SetState(() => { });
            }
        );

        Widget Scrolled(Widget editor)
        {
            return new SingleChildScrollView
            (
                new Padding(EdgeInsets.Only(left: 20), editor)
            );
        }

        return new List<TabItem>
        {
            new()
            {
                Label = "Colors",
                Content = Scrolled(new ColorsCategoryEditor(_draft, baseTheme, changed))
            },
            new()
            {
                Label = "Typography",
                Content = Scrolled(new TypographyCategoryEditor(_draft, baseTheme, changed))
            },
            new()
            {
                Label = "Buttons",
                Content = Scrolled(new ButtonsCategoryEditor(_draft, baseTheme, changed))
            },
            new()
            {
                Label = "Inputs",
                Content = Scrolled(new InputsCategoryEditor(_draft, baseTheme, changed))
            },
            new()
            {
                Label = "Misc",
                Content = Scrolled(new MiscCategoryEditor(_draft, baseTheme, changed))
            }
        };
    }

    internal ThemeOverrides CurrentDraft() => _draft;

    internal void ResetDraft()
    {
        _draft = new ThemeOverrides();
        SetState(() => { });
    }
}

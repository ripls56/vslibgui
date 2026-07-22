using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Core.Scroll;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Theming;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Widgets.Theming;

/// <summary>
///     A master-detail theme picker. A scrollable list of presets on the left drives a live
///     preview on the right: the preview is scoped to the selected preset's own
///     <see cref="ColorScheme" />, so buttons, checkboxes and fields render exactly as real
///     UI would in that theme. Tapping a preset selects and applies it immediately.
/// </summary>
public class ThemePresetGallery : StatelessWidget
{
    private readonly Action<ThemePreset> _onSelect;
    private readonly IReadOnlyList<ThemePreset> _presets;

    /// <summary>Creates the gallery.</summary>
    public ThemePresetGallery
    (
        IReadOnlyList<ThemePreset> presets,
        Action<ThemePreset> onSelect
    )
    {
        _presets = presets;
        _onSelect = onSelect;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        return new ThemePresetBrowser(_presets, _onSelect);
    }
}

/// <summary>
///     The stateful master-detail browser backing <see cref="ThemePresetGallery" />.
///     Holds the selected preset index and rebuilds the preview when it changes.
/// </summary>
internal sealed class ThemePresetBrowser : StatefulWidget
{
    /// <summary>Creates the browser.</summary>
    public ThemePresetBrowser
    (
        IReadOnlyList<ThemePreset> presets,
        Action<ThemePreset> onSelect
    )
    {
        Presets = presets;
        OnSelect = onSelect;
    }

    /// <summary>The presets available for selection.</summary>
    public IReadOnlyList<ThemePreset> Presets { get; }

    /// <summary>Invoked with a preset when the player selects it.</summary>
    public Action<ThemePreset> OnSelect { get; }

    /// <inheritdoc />
    public override State CreateState() => new ThemePresetBrowserState();
}

/// <summary>
///     Drives selection: the initial index matches the currently applied theme, and each
///     tap updates the index (rebuilding the preview) and forwards the preset upstream.
/// </summary>
internal sealed class ThemePresetBrowserState : State<ThemePresetBrowser>
{
    private const float ListWidth = 180f;
    private const float ItemRadius = 6f;
    private const float SwatchSize = 13f;
    private const float ItemSpacing = 4f;

    private int _selectedIndex;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        _selectedIndex = IndexOfActive(Widget.Presets);
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;
        var selected = Widget.Presets[_selectedIndex];

        return new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            mainAxisSize: MainAxisSize.Min,
            spacing: 0,
            children:
            [
                BuildTopStrip(colors),
                new Expanded
                (
                    child: new Row
                    (
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            BuildPresetList(colors),
                            new Divider(1f, color: colors.OutlineVariant, axis: Axis.Vertical),
                            new Expanded
                                (child: new Padding(EdgeInsets.All(24), BuildPreview(selected)))
                        ]
                    )
                )
            ]
        );
    }

    private Widget BuildTopStrip(ColorScheme colors)
    {
        return new Padding
        (
            edgeInsets: EdgeInsets.Symmetric(horizontal: 24) with
            {
                Top = 16, Bottom = 16
            },
            child: new Row
            (
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new Text
                    (
                        "Pick a theme",
                        new TextStyle
                        {
                            FontSize = 18, Weight = FontWeight.SemiBold, Color = colors.OnBackground
                        }
                    ),
                    new Text
                    (
                        $"{_selectedIndex + 1} of {Widget.Presets.Count}",
                        new TextStyle
                        {
                            FontSize = 13, Color = colors.OnSurfaceVariant
                        }
                    )
                ]
            )
        );
    }

    private Widget BuildPresetList(ColorScheme colors)
    {
        var items = new List<Widget>();
        for (var i = 0; i < Widget.Presets.Count; i++)
        {
            var index = i;
            items.Add(BuildListItem(Widget.Presets[index], index, colors));
        }

        return new SizedBox
        (
            width: ListWidth,
            child: new Container
            (
                new BoxStyle
                {
                    Color = colors.SurfaceLow
                },
                new SingleChildScrollView
                (
                    new Padding
                    (
                        EdgeInsets.All(12),
                        new Column
                        (
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            mainAxisSize: MainAxisSize.Min,
                            spacing: ItemSpacing,
                            children: items
                        )
                    )
                )
            )
        );
    }

    private Widget BuildListItem
    (
        ThemePreset preset,
        int index,
        ColorScheme colors
    )
    {
        var selected = index == _selectedIndex;

        return new Button
        (
            new Row
            (
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new Expanded
                    (
                        child: new Text
                        (
                            preset.DisplayName,
                            new TextStyle
                            {
                                FontSize = 14,
                                Color = selected ? colors.OnSurface : colors.OnSurfaceVariant,
                                Weight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                                Overflow = TextOverflow.Ellipsis,
                                MaxLines = 1
                            }
                        )
                    ),
                    new SizedBox(width: 8),
                    new Row
                    (
                        spacing: 4,
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Center,
                        children:
                        [
                            Swatch(preset.Scheme.Primary),
                            Swatch(preset.Scheme.SurfaceHigh)
                        ]
                    )
                ]
            ),
            ButtonVariant.Ghost,
            style: ItemButtonStyle(colors, selected),
            onTap: _ => Select(index, preset)
        );
    }

    private static ButtonStyle ItemButtonStyle(ColorScheme colors, bool selected)
    {
        return new ButtonStyle
        {
            Padding = EdgeInsets.Symmetric(10, 12),
            Ghost = new ButtonVariantStyle
            {
                BackgroundColor = selected ? colors.StateSelected : Vector4.Zero,
                HoverBackgroundColor = colors.StateHover,
                PressBackgroundColor = colors.StateSelected,
                BorderColor = Vector4.Zero,
                BorderThickness = 0,
                CornerRadius = ItemRadius
            }
        };
    }

    private static Widget BuildPreview(ThemePreset preset)
    {
        var scheme = preset.Scheme;

        return new Theme
        (
            new ThemeData(colorScheme: scheme),
            new Container
            (
                new BoxStyle
                {
                    Color = scheme.SurfaceHigh,
                    BorderColor = scheme.OutlineVariant,
                    BorderThickness = 1f,
                    CornerRadius = new Vector4(12f),
                    Padding = EdgeInsets.All(24)
                },
                new Column
                (
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 20,
                    children:
                    [
                        new Text
                        (
                            "PREVIEW",
                            new TextStyle
                            {
                                FontSize = 11,
                                Weight = FontWeight.Bold,
                                Color = scheme.OnSurfaceVariant
                            }
                        ),
                        new Row
                        (
                            spacing: 12,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                new Button
                                (
                                    new Text
                                    (
                                        "Primary Action",
                                        new TextStyle
                                        {
                                            FontSize = 13, Color = scheme.OnPrimary
                                        }
                                    ),
                                    ButtonVariant.Primary
                                ),
                                new Button
                                (
                                    new Text
                                    (
                                        "Secondary",
                                        new TextStyle
                                        {
                                            FontSize = 13, Color = scheme.OnSurfaceVariant
                                        }
                                    ),
                                    ButtonVariant.Secondary
                                )
                            ]
                        ),

                        new Builder
                        (context =>
                            {
                                var theme = Theme.Of(context);
                                return new SizedBox
                                (
                                    320,
                                    child: new TextField
                                    (
                                        new TextEditingController("Example text field"),
                                        style: theme.TextFieldStyle
                                    )
                                );
                            }
                        ),

                        new Column
                        (
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            mainAxisSize: MainAxisSize.Min,
                            spacing: 12,
                            children:
                            [
                                new Checkbox(true, label: "Enabled option", size: 20),
                                new Checkbox(false, label: "Disabled option", size: 20)
                            ]
                        ),
                        new Row
                        (
                            spacing: 8,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                Swatch(scheme.Primary),
                                Swatch(scheme.Secondary),
                                Swatch(scheme.SurfaceLow),
                                Swatch(scheme.Error),
                                Swatch(scheme.OutlineVariant)
                            ]
                        )
                    ]
                )
            )
        );
    }

    private static Widget Swatch(Vector4 color)
    {
        return new Container
        (
            new BoxStyle
            {
                Color = color,
                Width = SwatchSize,
                Height = SwatchSize,
                CornerRadius = new Vector4(SwatchSize / 2f)
            }
        );
    }

    private void Select(int index, ThemePreset preset)
    {
        SetState(() => _selectedIndex = index);
        Widget.OnSelect(preset);
    }

    private static int IndexOfActive(IReadOnlyList<ThemePreset> presets)
    {
        var active = ThemeData.DefaultNotifier.Value.ColorScheme;
        for (var i = 0; i < presets.Count; i++)
        {
            if (presets[i].Scheme.Primary == active.Primary)
            {
                return i;
            }
        }
        return 0;
    }
}

/// <summary>
///     A single gallery card. Its content is wrapped in a <see cref="Theme" /> scoped to the
///     preset's <see cref="ColorScheme" />, so every base widget inside (button, checkbox,
///     text) renders in the preset's colors — a live preview of the theme.
/// </summary>
public sealed class PresetCard : StatefulWidget
{
    /// <summary>Creates a card.</summary>
    public PresetCard
    (
        ThemePreset preset,
        bool isSelected,
        Action<ThemePreset> onSelect
    )
    {
        Preset = preset;
        IsSelected = isSelected;
        OnSelect = onSelect;
    }

    /// <summary>The preset this card previews.</summary>
    public ThemePreset Preset { get; }

    /// <summary>Whether this preset is the currently applied theme.</summary>
    public bool IsSelected { get; }

    /// <summary>Invoked when the card is tapped.</summary>
    public Action<ThemePreset> OnSelect { get; }

    /// <inheritdoc />
    public override State CreateState() => new PresetCardState();
}

/// <summary>
///     Drives the card's interaction and entrance motion via implicit animations:
///     hover lifts and recolours the surface, press scales it down, selection
///     transitions the surface colour, and the card fades and slides in on mount.
/// </summary>
internal sealed class PresetCardState : State<PresetCard>
{
    private static readonly TimeSpan SurfaceDuration = TimeSpan.FromMilliseconds(160);
    private static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(140);
    private static readonly TimeSpan EntranceDuration = TimeSpan.FromMilliseconds(220);

    private bool _isHovered;
    private bool _isPressed;
    private bool _visible;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        SetState(() => _visible = true);
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var scheme = Widget.Preset.Scheme;
        var lifted = Widget.IsSelected || _isHovered;
        var scale = _isPressed ? 0.97f : _isHovered ? 1.03f : 1.0f;

        var content = new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Start,
            mainAxisSize: MainAxisSize.Min,
            spacing: 8,
            children:
            [
                new Row
                (
                    spacing: 8,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children:
                    [
                        new Button
                        (
                            new Text
                            (
                                "Aa",
                                new TextStyle
                                {
                                    FontSize = 12, Color = scheme.OnPrimary
                                }
                            ),
                            ButtonVariant.Primary
                        ),
                        new Checkbox(true, size: 18)
                    ]
                ),
                new Text
                (
                    Widget.Preset.DisplayName,
                    new TextStyle
                    {
                        FontSize = 12, Color = scheme.OnSurfaceVariant
                    }
                )
            ]
        );

        var surface = new AnimatedContainer
        (
            new BoxStyle
            {
                Color = lifted ? scheme.SurfaceHigh : scheme.Surface,
                BorderColor = Widget.IsSelected ? scheme.Primary : scheme.OutlineVariant,
                BorderThickness = Widget.IsSelected ? 2f : 1f,
                CornerRadius = new Vector4(_isHovered ? 9f : 6f),
                Padding = EdgeInsets.All(10)
            },
            SurfaceDuration,
            curve: Curves.EaseOut,
            child: content
        );

        var themed = new Theme(new ThemeData(colorScheme: scheme), surface);
        var press = new AnimatedScale(scale, PressDuration, curve: Curves.EaseOut, child: themed);
        var slide = new AnimatedSlide
        (
            _visible ? Vector2.Zero : new Vector2(0, 10),
            EntranceDuration,
            curve: Curves.EaseOut,
            child: press
        );
        var fade = new AnimatedOpacity
        (
            _visible ? 1f : 0f,
            EntranceDuration,
            curve: Curves.EaseOut,
            child: slide
        );

        return new MouseRegion
        (
            cursor: MouseCursor.LinkSelect,
            child: new GestureDetector
            (
                onTap: _ => Widget.OnSelect(Widget.Preset),
                onEnter: _ => SetState(() => _isHovered = true),
                onExit: _ => SetState
                (() =>
                    {
                        _isHovered = false;
                        _isPressed = false;
                    }
                ),
                onPress: _ => SetState(() => _isPressed = true),
                onRelease: _ => SetState(() => _isPressed = false),
                child: fade
            )
        );
    }
}

using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Example.Sidebar;

internal class ExampleSidebar : StatelessWidget
{
    // Layout constants shared with ExampleContent.
    internal const float IconWidth = 48f;
    internal const float ExpandedWidth = 164f;

    private static readonly (string IconPath, string Label)[] Entries =
    [
        ("textures/icons/layout-dashboard.svg", "Layout"),
        ("textures/icons/mouse-pointer.svg", "Input"),
        ("textures/icons/scroll.svg", "Scroll"),
        ("textures/icons/sparkles.svg", "Animations"),
        ("textures/icons/type.svg", "Typography"),
        ("textures/icons/package.svg", "Inventory"),
        ("textures/icons/palette.svg", "Theming")
    ];

    public ExampleSidebar(int selectedIndex, Action<int> onSelect)
    {
        SelectedIndex = selectedIndex;
        OnSelect = onSelect;
    }

    public int SelectedIndex { get; }
    public Action<int> OnSelect { get; }

    public override Widget Build(BuildContext context)
    {
        var items = new List<Widget>();
        for (var i = 0; i < Entries.Length; i++)
        {
            items.Add(new ExampleSidebarItem(
                Entries[i].IconPath,
                Entries[i].Label,
                i,
                SelectedIndex,
                OnSelect
            ));
        }

        // SizedBox forces the column to be ExpandedWidth wide so CrossAxisAlignment.End
        // right-aligns items — the icon lands at x=TransparentW..ExpandedWidth and the
        // label expands leftward into the transparent (game-world-visible) region.
        return new SizedBox(
            ExpandedWidth,
            child: new Padding(
                EdgeInsets.Symmetric(24),
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.End,
                    spacing: 6,
                    children: items
                )
            )
        );
    }
}

internal class ExampleSidebarItem : StatefulWidget
{
    public ExampleSidebarItem(
        string iconPath,
        string label,
        int index,
        int selectedIndex,
        Action<int> onSelect
    )
    {
        IconPath = iconPath;
        Label = label;
        Index = index;
        SelectedIndex = selectedIndex;
        OnSelect = onSelect;
    }

    public string IconPath { get; }
    public string Label { get; }
    public int Index { get; }
    public int SelectedIndex { get; }
    public Action<int> OnSelect { get; }

    public override State CreateState() => new ExampleSidebarItemState();
}

internal class ExampleSidebarItemState : State<ExampleSidebarItem>
{
    private bool _hovered;

    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;
        var selected = Widget.Index == Widget.SelectedIndex;
        var active = _hovered || selected;

        var bgColor = selected
            ? colors.Surface
            : _hovered
                ? new Vector4(colors.Surface.X * 1.2f, colors.Surface.Y * 1.2f,
                    colors.Surface.Z * 1.2f, 1f)
                : new Vector4(colors.Background.X * 0.85f, colors.Background.Y * 0.85f,
                    colors.Background.Z * 0.85f,
                    0.95f);

        var iconColor = active
            ? colors.Primary
            : new Vector4(colors.Primary.X * 0.5f, colors.Primary.Y * 0.5f, colors.Primary.Z * 0.5f,
                1f);

        // Left corners rounded (face the game world), right side can also be rounded.
        var radius = new Vector4(0, 0, 5, 5);

        return new GestureDetector(
            onTap: _ => Widget.OnSelect(Widget.Index),
            onEnter: _ => SetState(() => _hovered = true),
            onExit: _ => SetState(() => _hovered = false),
            child: new AnimatedContainer(
                duration: TimeSpan.FromMilliseconds(220),
                curve: Curves.EaseOutCubic,
                style: new BoxStyle
                {
                    Color = bgColor,
                    CornerRadius = radius,
                    BorderThickness = active ? 1f : 0f,
                    BorderColor = colors.Primary,
                    Height = 44,
                    // Right-aligned in ExpandedWidth column → grows leftward on expand.
                    Width = active ? ExampleSidebar.ExpandedWidth : ExampleSidebar.IconWidth,
                    ClipBehavior = ClipBehavior.HardEdge
                },
                child: new Row(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children:
                    [
                        // Icon — left side, leads the slide-out animation.
                        new SizedBox(
                            ExampleSidebar.IconWidth,
                            child: new Center(
                                new Icon("gui", Widget.IconPath, 20, iconColor)
                            )
                        ),
                        // Label — right side. Interval delays the fade until the icon has
                        // nearly finished sliding, so text never overlaps a half-open button.
                        new Expanded(
                            new AnimatedOpacity(
                                active ? 1f : 0f,
                                TimeSpan.FromMilliseconds(220),
                                new Interval(0.65, 1.0, Curves.EaseOut),
                                child: new Text(Widget.Label,
                                    new TextStyle
                                    {
                                        FontSize = 13,
                                        Color = colors.OnSurface,
                                        SoftWrap = false,
                                        Overflow = TextOverflow.Clip,
                                        MaxLines = 1
                                    })
                            )
                        )
                    ]
                )
            )
        );
    }
}

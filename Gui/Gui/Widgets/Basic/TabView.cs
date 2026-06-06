using System.Collections.Generic;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

public class TabItem
{
    public required string Label { get; set; }
    public required Widget Content { get; set; }
}

public enum TabPosition
{
    Top,
    Left
}

public class TabView : StatefulWidget
{
    public TabView(
        List<TabItem> tabs,
        TabPosition position = TabPosition.Top,
        int initialIndex = 0
    )
    {
        Tabs = tabs;
        Position = position;
        InitialIndex = initialIndex;
    }

    public List<TabItem> Tabs { get; }
    public TabPosition Position { get; }
    public int InitialIndex { get; }

    public override State CreateState() => new TabViewState();
}

internal class TabViewState : State<TabView>
{
    private int _activeIndex;

    public override void InitState()
    {
        base.InitState();
        _activeIndex = Widget.InitialIndex;
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var tabButtons = Widget.Tabs.ConvertAll(t =>
            {
                var index = Widget.Tabs.IndexOf(t);
                var isActive = index == _activeIndex;

                return (Widget)new GestureDetector(
                    onTap: e => SetState(() => _activeIndex = index),
                    child: new Container(
                        new BoxStyle
                        {
                            Color = isActive
                                ? new Vector4(
                                    colors.Primary.X,
                                    colors.Primary.Y,
                                    colors.Primary.Z,
                                    0.15f
                                )
                                : Vector4.Zero,
                            Padding = EdgeInsets.All(4),
                            CornerRadius = new Vector4(4),
                            BorderThickness = 0
                        },
                        new Padding(
                            EdgeInsets.All(8),
                            new Text(
                                t.Label,
                                new TextStyle
                                {
                                    FontSize = theme.TextTheme.Body.FontSize,
                                    Color = isActive
                                        ? colors.Primary
                                        : colors.OnSurface
                                }
                            )
                        )
                    )
                );
            }
        );

        Widget header = Widget.Position == TabPosition.Top
            ? new Row(children: tabButtons)
            : new Column(children: tabButtons);

        Widget content = new Container(
            new BoxStyle
            {
                Color = new Vector4(
                    colors.Surface.X,
                    colors.Surface.Y,
                    colors.Surface.Z,
                    0.05f
                )
            },
            Widget.Tabs[_activeIndex].Content
        );

        if (Widget.Position == TabPosition.Top)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: [header, new Expanded(content)]
            );
        }

        return new Row(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: [header, new Expanded(content)]
        );
    }
}

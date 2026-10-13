using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Core.Scroll;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Sound;
using Gui.Widgets.Animations;
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
    public TabView
    (
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

    public Action<int>? OnTabChanged { get; set; }

    public float? TabWidth { get; set; }

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

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var tabButtons = Widget.Tabs.ConvertAll
        (t =>
            {
                var index = Widget.Tabs.IndexOf(t);
                var isActive = index == _activeIndex;

                return new TabButtonWidget
                (
                    t.Label,
                    isActive,
                    Widget.Position,
                    Widget.TabWidth,
                    () =>
                    {
                        SetState(() => _activeIndex = index);
                        Widget.OnTabChanged?.Invoke(index);
                    }
                );
            }
        );

        var content = Widget.Tabs[_activeIndex].Content;

        content = new AnimatedSize
        (
            TimeSpan.FromMilliseconds(400),
            Curves.EaseOutCubic,
            content
        );

        if (Widget.Position == TabPosition.Top)
        {
            return new Column
            (
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Padding
                    (
                        EdgeInsets.Only(bottom: 12),
                        new Container
                        (
                            new BoxStyle
                            {
                                Color = colors.SurfaceLow,
                                CornerRadius = new Vector4(8),
                                Padding = EdgeInsets.All(4)
                            },
                            new Row
                            (
                                mainAxisSize: MainAxisSize.Min,
                                children: tabButtons
                            )
                        )
                    ),
                    new Expanded(content)
                ]
            );
        }

        return new Row
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Column(children: tabButtons),
                new Divider(color: colors.OutlineVariant, axis: Axis.Vertical),
                new Expanded(content)
            ]
        );
    }
}

internal class TabButtonWidget : StatefulWidget
{
    public TabButtonWidget
    (
        string label,
        bool isActive,
        TabPosition position,
        float? width,
        Action onTap
    )
    {
        Label = label;
        IsActive = isActive;
        Position = position;
        Width = width;
        OnTap = onTap;
    }

    public string Label { get; }
    public bool IsActive { get; }
    public TabPosition Position { get; }
    public float? Width { get; }
    public Action OnTap { get; }

    public override State CreateState() => new TabButtonWidgetState();
}

internal class TabButtonWidgetState : State<TabButtonWidget>
{
    private bool _isHovered;
    private bool _isPressed;

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var textStyle = new TextStyle
        {
            FontSize = theme.TextTheme.Label.FontSize,
            Color = Widget.IsActive ? colors.Primary : colors.OnSurfaceVariant,
            Weight = Widget.IsActive ? FontWeight.SemiBold : FontWeight.Normal
        };

        Widget label = new AnimatedText
        (
            Widget.Label,
            textStyle,
            TimeSpan.FromMilliseconds(200)
        );

        if (Widget.Position == TabPosition.Top)
        {
            return BuildTopTab(colors, label);
        }

        return BuildLeftTab(colors, label);
    }

    private Widget BuildTopTab(ColorScheme colors, Widget label)
    {
        Widget content = new AnimatedContainer
        (
            duration: TimeSpan.FromMilliseconds(200),
            curve: Curves.EaseOutCubic,
            style: new BoxStyle
            {
                Padding = EdgeInsets.Symmetric(horizontal: 16, vertical: 8),
                Color = Widget.IsActive
                    ? colors.SurfaceHigh
                    : _isHovered
                        ? colors.StateHover
                        : Vector4.Zero,
                CornerRadius = new Vector4(6),
                Width = Widget.Width
            },
            child: label
        );

        return new Padding
        (
            EdgeInsets.Symmetric(horizontal: 2),
            WrapGesture
            (
                new AnimatedScale
                (
                    _isPressed ? 0.96f : _isHovered ? 1.03f : 1.0f,
                    TimeSpan.FromMilliseconds(140),
                    Curves.EaseOut,
                    child: content
                )
            )
        );
    }

    private Widget BuildLeftTab(ColorScheme colors, Widget label)
    {
        Widget content = new AnimatedContainer
        (
            duration: TimeSpan.FromMilliseconds(250),
            curve: Curves.EaseOutCubic,
            style: new BoxStyle
            {
                Padding = EdgeInsets.Symmetric(horizontal: 12, vertical: 8),
                Color = Widget.IsActive
                    ? colors.StateSelected
                    : _isHovered
                        ? colors.StateHover
                        : Vector4.Zero,
                CornerRadius = new Vector4(0, 0, 4, 4),
                Width = Widget.Width
            },
            child: new Row
            (
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new AnimatedContainer
                    (
                        duration: TimeSpan.FromMilliseconds(300),
                        style: new BoxStyle
                        {
                            Width = 3,
                            Height = 16,
                            Color = Widget.IsActive
                                ? colors.Primary
                                : _isHovered
                                    ? colors.Border
                                    : Vector4.Zero,
                            CornerRadius = new Vector4(1.5f)
                        }
                    ),
                    new SizedBox(width: 12),
                    new Expanded(child: label)
                ]
            )
        );

        return WrapGesture
        (
            new AnimatedScale
            (
                _isPressed ? 0.96f : _isHovered ? 1.03f : 1.0f,
                TimeSpan.FromMilliseconds(140),
                Curves.EaseOut,
                child: content
            )
        );
    }

    private Widget WrapGesture(Widget child)
    {
        return new GestureDetector
        (
            onTap: _ => Widget.OnTap(),
            onEnter: _ => SetState(() => _isHovered = true),
            onExit: _ => SetState
            (() =>
                {
                    _isHovered = false;
                    _isPressed = false;
                }
            ),
            onPress: _ => SetState
            (() =>
                {
                    _isPressed = true;
                    if (!Widget.IsActive)
                    {
                        this.ToggleOn();
                    }
                }
            ),
            onRelease: _ => SetState(() => _isPressed = false),
            child: child
        );
    }
}

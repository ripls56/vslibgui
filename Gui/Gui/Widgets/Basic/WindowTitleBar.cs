using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Rendering.Text;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     A window title bar with a title, minimize (collapse) and close buttons.
///     <para>
///         Wrap your window content with this using a <see cref="Column" />:
///         <code>
/// new Column(children: [
///     new WindowTitleBar(
///         title: "My Window",
///         onClose: () => TryClose(),
///     ),
///     new Expanded(child: myContent),
/// ])
/// </code>
///     </para>
///     <para>
///         The minimize button toggles <see cref="IsMinimized" /> state, hiding
///         the sibling content. Use <see cref="WindowTitleBar" /> inside a
///         <see cref="WindowFrame" /> for automatic minimize support, or manage
///         visibility yourself based on the <paramref name="onMinimizeChanged" /> callback.
///     </para>
/// </summary>
public class WindowTitleBar : StatefulWidget
{
    public WindowTitleBar(
        string title = "",
        float height = 28f,
        Action? onClose = null,
        Action<bool>? onMinimizeChanged = null,
        Vector4? backgroundColor = null,
        Vector4? textColor = null,
        Vector4? buttonHoverColor = null,
        Vector4? closeHoverColor = null,
        float fontSize = 14f,
        bool showMinimize = true,
        bool showClose = true,
        Framework.Key? key = null
    ) : base(key)
    {
        Title = title;
        Height = height;
        OnClose = onClose;
        OnMinimizeChanged = onMinimizeChanged;
        var defaults = ThemeData.Default.ColorScheme;
        BackgroundColor = backgroundColor ?? defaults.Surface;
        TextColor = textColor ?? defaults.OnSurface;
        ButtonHoverColor = buttonHoverColor ?? new Vector4(
            defaults.Surface.X + 0.12f,
            defaults.Surface.Y + 0.12f,
            defaults.Surface.Z + 0.12f,
            1f
        );
        CloseHoverColor = closeHoverColor ?? defaults.Error;
        FontSize = fontSize;
        ShowMinimize = showMinimize;
        ShowClose = showClose;
    }

    internal string Title { get; }
    internal float Height { get; }
    internal Action? OnClose { get; }
    internal Action<bool>? OnMinimizeChanged { get; }
    internal Vector4 BackgroundColor { get; }
    internal Vector4 TextColor { get; }
    internal Vector4 ButtonHoverColor { get; }
    internal Vector4 CloseHoverColor { get; }
    internal float FontSize { get; }
    internal bool ShowMinimize { get; }
    internal bool ShowClose { get; }

    public override State CreateState() => new WindowTitleBarState();

    private class WindowTitleBarState : State<WindowTitleBar>
    {
        private bool _closeHovered;
        private bool _isMinimized;
        private bool _minimizeHovered;

        public override Widget Build(
            BuildContext context
        )
        {
            var w = Widget;
            var btnSize = w.Height - 4;

            var buttons = new List<Widget>();

            if (w.ShowMinimize)
            {
                buttons.Add(
                    _TitleBarButton(
                        _isMinimized
                            ? "+"
                            : "\u2013",
                        btnSize,
                        _minimizeHovered,
                        w.ButtonHoverColor,
                        w.TextColor,
                        w.FontSize,
                        _ => SetState(() => _minimizeHovered = true),
                        _ => SetState(() => _minimizeHovered = false),
                        _ =>
                        {
                            SetState(() => _isMinimized = !_isMinimized);
                            w.OnMinimizeChanged?.Invoke(_isMinimized);
                        }
                    )
                );
            }

            if (w.ShowClose)
            {
                buttons.Add(
                    _TitleBarButton(
                        "\u00d7",
                        btnSize,
                        _closeHovered,
                        w.CloseHoverColor,
                        w.TextColor,
                        w.FontSize + 2,
                        _ => SetState(() => _closeHovered = true),
                        _ => SetState(() => _closeHovered = false),
                        _ => w.OnClose?.Invoke()
                    )
                );
            }

            return new Container(
                new BoxStyle { Height = w.Height, Color = w.BackgroundColor },
                new Row(
                    children:
                    [
                        new SizedBox(8),
                        new Expanded(
                            new Align(
                                Alignment.CenterLeft,
                                new Text(
                                    w.Title,
                                    new TextStyle { FontSize = w.FontSize, Color = w.TextColor }
                                )
                            )
                        ),
                        ..buttons,
                        new SizedBox(2)
                    ]
                )
            );
        }

        private static Widget _TitleBarButton(
            string label,
            float size,
            bool isHovered,
            Vector4 hoverColor,
            Vector4 textColor,
            float fontSize,
            Action<PointerEvent> onEnter,
            Action<PointerEvent> onExit,
            Action<PointerEvent> onTap
        )
        {
            return new GestureDetector(
                onTap: onTap,
                onEnter: onEnter,
                onExit: onExit,
                child: new Container(
                    new BoxStyle
                    {
                        Width = size,
                        Height = size,
                        Color = isHovered
                            ? hoverColor
                            : Vector4.Zero,
                        CornerRadius = new Vector4(3)
                    },
                    new Center(
                        new Text(
                            label,
                            new TextStyle { FontSize = fontSize, Color = textColor }
                        )
                    )
                )
            );
        }
    }
}

/// <summary>
///     Convenience widget that wraps content with a <see cref="WindowTitleBar" />
///     and handles minimize (collapse/expand) automatically.
///     <code>
/// protected override Widget Build() => new WindowFrame(
///     title: "My Window",
///     onClose: () => TryClose(),
///     child: myContent,
/// );
/// </code>
/// </summary>
public class WindowFrame : StatefulWidget
{
    public WindowFrame(
        string title = "",
        Widget? child = null,
        Action? onClose = null,
        float titleBarHeight = 28f,
        Vector4? titleBarColor = null,
        Vector4? textColor = null,
        float fontSize = 14f,
        bool showMinimize = true,
        bool showClose = true,
        bool fillHeight = false,
        Framework.Key? key = null
    ) : base(key)
    {
        Title = title;
        Child = child;
        OnClose = onClose;
        TitleBarHeight = titleBarHeight;
        var defaults = ThemeData.Default.ColorScheme;
        TitleBarColor = titleBarColor ?? defaults.Surface;
        TextColor = textColor ?? defaults.OnSurface;
        FontSize = fontSize;
        ShowMinimize = showMinimize;
        ShowClose = showClose;
        FillHeight = fillHeight;
    }

    internal string Title { get; }
    internal Widget? Child { get; }
    internal Action? OnClose { get; }
    internal float TitleBarHeight { get; }
    internal Vector4 TitleBarColor { get; }
    internal Vector4 TextColor { get; }
    internal float FontSize { get; }
    internal bool ShowMinimize { get; }
    internal bool ShowClose { get; }

    /// <summary>
    ///     When true, the child is wrapped in an <see cref="Expanded" /> and the
    ///     frame fills the height it is given, so a scrollable child receives
    ///     <c>windowHeight - titleBarHeight</c>. When false (default), the frame
    ///     hugs its child's intrinsic height (content-sized windows).
    /// </summary>
    internal bool FillHeight { get; }

    public override State CreateState() => new WindowFrameState();

    private class WindowFrameState : State<WindowFrame>
    {
        private bool _isMinimized;

        public override Widget Build(
            BuildContext context
        )
        {
            var w = Widget;
            var children = new List<Widget>
            {
                new WindowTitleBar(
                    w.Title,
                    w.TitleBarHeight,
                    w.OnClose,
                    minimized => SetState(() => _isMinimized = minimized),
                    w.TitleBarColor,
                    w.TextColor,
                    fontSize: w.FontSize,
                    showMinimize: w.ShowMinimize,
                    showClose: w.ShowClose
                )
            };

            var fill = w.FillHeight && !_isMinimized;

            if (!_isMinimized && w.Child != null)
            {
                children.Add(fill
                    ? new Expanded(w.Child)
                    : w.Child);
            }

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                mainAxisSize: fill
                    ? MainAxisSize.Max
                    : MainAxisSize.Min,
                children: children);
        }
    }
}

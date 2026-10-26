using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Core.Windowing;
using Gui.Rendering.Text;
using Gui.Sound;
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
///         The minimize button toggles the minimized state and reports it through
///         <paramref name="onMinimizeChanged" />. When <paramref name="collapseWindow" /> is
///         <c>true</c> (default) it also asks the hosting window (<see cref="IWindowHost" />)
///         to shrink to the bar height, so the hidden content no longer swallows clicks.
///         Use <see cref="WindowFrame" /> to hide the content automatically.
///     </para>
///     <para>
///         Appearance comes from <paramref name="style" />, falling back to
///         <see cref="ThemeData.WindowTitleBarStyle" />. An explicitly passed legacy
///         parameter (<paramref name="height" />, <paramref name="backgroundColor" />, ...)
///         overrides the matching style field.
///     </para>
/// </summary>
public class WindowTitleBar : StatefulWidget
{
    /// <summary>Creates a window title bar.</summary>
    /// <param name="title">Title text.</param>
    /// <param name="height">Overrides <see cref="WindowTitleBarStyle.Height" />.</param>
    /// <param name="onClose">Called when the close button is tapped.</param>
    /// <param name="onMinimizeChanged">Called with the new minimized state.</param>
    /// <param name="backgroundColor">
    ///     Overrides <see cref="WindowTitleBarStyle.BackgroundColor" />.
    /// </param>
    /// <param name="textColor">Overrides the title and glyph text colors.</param>
    /// <param name="buttonHoverColor">Overrides the minimize hover and pressed fills.</param>
    /// <param name="closeHoverColor">
    ///     Overrides <see cref="WindowTitleBarStyle.CloseHoverColor" />.
    /// </param>
    /// <param name="fontSize">Overrides the title and glyph font sizes.</param>
    /// <param name="showMinimize">Shows the minimize button.</param>
    /// <param name="showClose">Shows the close button.</param>
    /// <param name="style">Style tokens; <c>null</c> uses the theme's style.</param>
    /// <param name="leading">Widget placed directly before the button group.</param>
    /// <param name="trailing">Widget placed directly after the button group.</param>
    /// <param name="collapseWindow">
    ///     When <c>true</c>, minimizing collapses the host window to the bar height.
    /// </param>
    /// <param name="key">Widget key.</param>
    public WindowTitleBar
    (
        string title = "",
        float? height = null,
        Action? onClose = null,
        Action<bool>? onMinimizeChanged = null,
        Vector4? backgroundColor = null,
        Vector4? textColor = null,
        Vector4? buttonHoverColor = null,
        Vector4? closeHoverColor = null,
        float? fontSize = null,
        bool showMinimize = true,
        bool showClose = true,
        WindowTitleBarStyle? style = null,
        Widget? leading = null,
        Widget? trailing = null,
        bool collapseWindow = true,
        Framework.Key? key = null
    ) : base(key)
    {
        Title = title;
        Height = height;
        OnClose = onClose;
        OnMinimizeChanged = onMinimizeChanged;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
        ButtonHoverColor = buttonHoverColor;
        CloseHoverColor = closeHoverColor;
        FontSize = fontSize;
        ShowMinimize = showMinimize;
        ShowClose = showClose;
        Style = style;
        Leading = leading;
        Trailing = trailing;
        CollapseWindow = collapseWindow;
    }

    /// <summary>
    ///     Legacy 3.x constructor. Kept so mods compiled against <c>Gui.dll</c> 3.x still bind:
    ///     the parameter list is part of the binary identity of a constructor, so the 4.x
    ///     constructor cannot replace it. Declare new options as properties and set them in an
    ///     object initializer instead of adding parameters here.
    /// </summary>
    [Obsolete(
        "Use the WindowTitleBar constructor and set Style, Leading and Trailing in an object " +
        "initializer. This overload will be removed in 6.0.0.")]
    public WindowTitleBar
    (
        string title,
        float height,
        Action? onClose,
        Action<bool>? onMinimizeChanged,
        Vector4? backgroundColor,
        Vector4? textColor,
        Vector4? buttonHoverColor,
        Vector4? closeHoverColor,
        float fontSize,
        bool showMinimize,
        bool showClose,
        Framework.Key? key
    ) : this
    (
        title,
        height,
        onClose,
        onMinimizeChanged,
        backgroundColor,
        textColor,
        buttonHoverColor,
        closeHoverColor,
        fontSize,
        showMinimize,
        showClose,
        style: null,
        leading: null,
        trailing: null,
        collapseWindow: false,
        key: key
    )
    {
    }

    /// <summary>Title text.</summary>
    public string Title { get; init; }

    /// <summary>Bar height. Overrides <see cref="WindowTitleBarStyle.Height" />.</summary>
    public float? Height { get; init; }

    /// <summary>Called when the close button is tapped.</summary>
    public Action? OnClose { get; init; }

    /// <summary>Called with the new minimized state.</summary>
    public Action<bool>? OnMinimizeChanged { get; init; }

    /// <summary>
    ///     Bar background. Overrides <see cref="WindowTitleBarStyle.BackgroundColor" />.
    /// </summary>
    public Vector4? BackgroundColor { get; init; }

    /// <summary>Overrides the title and glyph text colors.</summary>
    public Vector4? TextColor { get; init; }

    /// <summary>Overrides the minimize hover and pressed fills.</summary>
    public Vector4? ButtonHoverColor { get; init; }

    /// <summary>Overrides <see cref="WindowTitleBarStyle.CloseHoverColor" />.</summary>
    public Vector4? CloseHoverColor { get; init; }

    /// <summary>Overrides the title and glyph font sizes.</summary>
    public float? FontSize { get; init; }

    /// <summary>Shows the minimize button.</summary>
    public bool ShowMinimize { get; init; }

    /// <summary>Shows the close button.</summary>
    public bool ShowClose { get; init; }

    /// <summary>
    ///     Style tokens. <c>null</c> uses <see cref="ThemeData.WindowTitleBarStyle" />.
    ///     New options belong here, not in the constructor: adding a property is additive
    ///     for mods compiled against an earlier release, adding a parameter is not.
    /// </summary>
    public WindowTitleBarStyle? Style { get; init; }

    /// <summary>Widget placed directly before the button group.</summary>
    public Widget? Leading { get; init; }

    /// <summary>Widget placed directly after the button group.</summary>
    public Widget? Trailing { get; init; }

    /// <summary>
    ///     When <c>true</c>, minimizing collapses the host window to the bar height.
    /// </summary>
    public bool CollapseWindow { get; init; }

    /// <inheritdoc />
    public override State CreateState() => new WindowTitleBarState();

    private class WindowTitleBarState : State<WindowTitleBar>
    {
        private bool _closeHovered;
        private bool _closePressed;
        private IWindowHost? _host;
        private bool _isMinimized;
        private bool _minimizeHovered;
        private bool _minimizePressed;
        private float _resolvedHeight;

        public override void InitState()
        {
            _host = Element.Owner?.GetWindowHost();
        }

        public override void UpdateWidget
        (
            WindowTitleBar oldWidget
        )
        {
            if (!_isMinimized || oldWidget.CollapseWindow == Widget.CollapseWindow)
            {
                return;
            }

            _host?.SetHostCollapsed(this, Widget.CollapseWindow, _resolvedHeight);
        }

        public override void Dispose()
        {
            _host?.SetHostCollapsed(this, false, 0);
        }

        public override Widget Build
        (
            BuildContext context
        )
        {
            var w = Widget;
            var s = w.Style ?? Theme.Of(context).WindowTitleBarStyle;

            var height = w.Height ?? s.Height;
            _resolvedHeight = height;
            var btnSize = s.ButtonSize > 0
                ? s.ButtonSize
                : height - 4;
            var titleStyle = s.TitleTextStyle;
            var glyphStyle = s.ButtonTextStyle;

            if (w.TextColor is { } textColor)
            {
                titleStyle.Color = textColor;
                glyphStyle.Color = textColor;
            }

            if (w.FontSize is { } fontSize)
            {
                titleStyle.FontSize = fontSize;
                glyphStyle.FontSize = fontSize;
            }

            var buttons = _BuildButtons(s, btnSize, glyphStyle);

            var group = new List<Widget>();
            if (w.Leading != null)
            {
                group.Add(w.Leading);
            }

            group.AddRange(buttons);

            if (w.Trailing != null)
            {
                group.Add(w.Trailing);
            }

            Widget titleWidget = new Expanded
            (
                new Align
                (
                    s.TitleAlignment switch
                    {
                        TitleBarAlignment.Center => Alignment.Center,
                        TitleBarAlignment.Right => Alignment.CenterRight,
                        _ => Alignment.CenterLeft
                    },
                    new Text(w.Title, titleStyle)
                )
            );

            var children = new List<Widget>();
            if (s.ButtonPlacement == TitleBarButtonPlacement.Start)
            {
                children.AddRange(group);
                children.Add(titleWidget);
            }
            else
            {
                children.Add(titleWidget);
                children.AddRange(group);
            }

            return new Container
            (
                new BoxStyle
                {
                    Height = height,
                    Color = w.BackgroundColor ?? s.BackgroundColor,
                    Gradient = s.BackgroundGradient,
                    CornerRadius = s.CornerRadius,
                    BorderThickness = s.BorderThickness,
                    BorderColor = s.BorderColor,
                    Padding = s.Padding
                },
                new Row
                (
                    children: children
                )
            );
        }

        private List<Widget> _BuildButtons
        (
            WindowTitleBarStyle s,
            float btnSize,
            TextStyle glyphStyle
        )
        {
            var w = Widget;
            var minimizeHover = w.ButtonHoverColor ?? s.ButtonHoverColor;
            var minimizePressed = w.ButtonHoverColor ?? s.ButtonPressedColor;
            var closeHover = w.CloseHoverColor ?? s.CloseHoverColor;
            var closeGlyphStyle = glyphStyle;
            closeGlyphStyle.FontSize = glyphStyle.FontSize + 2;

            var buttons = new List<Widget>();

            if (w.ShowMinimize)
            {
                buttons.Add
                (
                    _TitleBarButton
                    (
                        _isMinimized
                            ? s.RestoreGlyph
                            : s.MinimizeGlyph,
                        btnSize,
                        s.ButtonCornerRadius,
                        _minimizePressed
                            ? minimizePressed
                            : _minimizeHovered
                                ? minimizeHover
                                : s.ButtonColor,
                        glyphStyle,
                        _ => SetState(() => _minimizeHovered = true),
                        _ => SetState
                        (
                            () =>
                            {
                                _minimizeHovered = false;
                                _minimizePressed = false;
                            }
                        ),
                        _ => SetState(() => _minimizePressed = true),
                        _ => SetState(() => _minimizePressed = false),
                        _ => _ToggleMinimized()
                    )
                );
            }

            if (w.ShowClose)
            {
                buttons.Add
                (
                    _TitleBarButton
                    (
                        s.CloseGlyph,
                        btnSize,
                        s.ButtonCornerRadius,
                        _closeHovered || _closePressed
                            ? closeHover
                            : s.ButtonColor,
                        closeGlyphStyle,
                        _ => SetState(() => _closeHovered = true),
                        _ => SetState
                        (
                            () =>
                            {
                                _closeHovered = false;
                                _closePressed = false;
                            }
                        ),
                        _ => SetState(() => _closePressed = true),
                        _ => SetState(() => _closePressed = false),
                        _ =>
                        {
                            this.Close();
                            w.OnClose?.Invoke();
                        }
                    )
                );
            }

            if (s.ReverseButtonOrder)
            {
                buttons.Reverse();
            }

            if (s.ButtonSpacing > 0 && buttons.Count > 1)
            {
                for (var i = buttons.Count - 1; i > 0; i--)
                {
                    buttons.Insert(i, new SizedBox(s.ButtonSpacing));
                }
            }

            return buttons;
        }

        private void _ToggleMinimized()
        {
            if (_isMinimized)
            {
                this.ToggleOn();
            }
            else
            {
                this.ToggleOff();
            }

            SetState(() => _isMinimized = !_isMinimized);

            if (Widget.CollapseWindow)
            {
                _host?.SetHostCollapsed(this, _isMinimized, _resolvedHeight);
            }

            Widget.OnMinimizeChanged?.Invoke(_isMinimized);
        }

        private static Widget _TitleBarButton
        (
            string label,
            float size,
            float cornerRadius,
            Vector4 fill,
            TextStyle textStyle,
            Action<PointerEvent> onEnter,
            Action<PointerEvent> onExit,
            Action<PointerEvent> onPress,
            Action<PointerEvent> onRelease,
            Action<PointerEvent> onTap
        )
        {
            return new GestureDetector
            (
                onTap: onTap,
                onEnter: onEnter,
                onExit: onExit,
                onPress: onPress,
                onRelease: onRelease,
                child: new Container
                (
                    new BoxStyle
                    {
                        Width = size,
                        Height = size,
                        Color = fill,
                        CornerRadius = new Vector4(cornerRadius)
                    },
                    new Center
                    (
                        new Text
                        (
                            label,
                            textStyle
                        )
                    )
                )
            );
        }
    }
}

/// <summary>
///     Convenience widget that wraps content with a <see cref="WindowTitleBar" />
///     and handles minimize (collapse/expand) automatically: while minimized the child is
///     removed and, unless <paramref name="collapseWindow" /> is <c>false</c>, the host
///     window shrinks to the title bar.
///     <para>
///         The constructor is frozen: its parameter list is a binary contract with mods
///         compiled against an earlier release, so a new option is a property set in an
///         object initializer, never a new parameter. See <c>compat/API-POLICY.md</c>.
///     </para>
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
    /// <summary>Creates a window frame.</summary>
    /// <param name="title">Title text.</param>
    /// <param name="child">Window content, hidden while minimized.</param>
    /// <param name="onClose">Called when the close button is tapped.</param>
    /// <param name="titleBarHeight">Overrides <see cref="WindowTitleBarStyle.Height" />.</param>
    /// <param name="titleBarColor">
    ///     Overrides <see cref="WindowTitleBarStyle.BackgroundColor" />.
    /// </param>
    /// <param name="textColor">Overrides the title and glyph text colors.</param>
    /// <param name="fontSize">Overrides the title and glyph font sizes.</param>
    /// <param name="showMinimize">Shows the minimize button.</param>
    /// <param name="showClose">Shows the close button.</param>
    /// <param name="fillHeight">See <see cref="FillHeight" />.</param>
    /// <param name="style">Title bar style tokens; <c>null</c> uses the theme's style.</param>
    /// <param name="leading">Widget placed directly before the button group.</param>
    /// <param name="trailing">Widget placed directly after the button group.</param>
    /// <param name="collapseWindow">
    ///     When <c>true</c>, minimizing collapses the host window to the bar height.
    /// </param>
    /// <param name="onMinimizeChanged">Called with the new minimized state.</param>
    /// <param name="key">Widget key.</param>
    public WindowFrame
    (
        string title = "",
        Widget? child = null,
        Action? onClose = null,
        float? titleBarHeight = null,
        Vector4? titleBarColor = null,
        Vector4? textColor = null,
        float? fontSize = null,
        bool showMinimize = true,
        bool showClose = true,
        bool fillHeight = false,
        WindowTitleBarStyle? style = null,
        Widget? leading = null,
        Widget? trailing = null,
        bool collapseWindow = true,
        Action<bool>? onMinimizeChanged = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Title = title;
        Child = child;
        OnClose = onClose;
        TitleBarHeight = titleBarHeight;
        TitleBarColor = titleBarColor;
        TextColor = textColor;
        FontSize = fontSize;
        ShowMinimize = showMinimize;
        ShowClose = showClose;
        FillHeight = fillHeight;
        Style = style;
        Leading = leading;
        Trailing = trailing;
        CollapseWindow = collapseWindow;
        OnMinimizeChanged = onMinimizeChanged;
    }

    /// <summary>
    ///     Legacy 3.x constructor. Kept so mods compiled against <c>Gui.dll</c> 3.x still bind:
    ///     the parameter list is part of the binary identity of a constructor, so the 4.x
    ///     constructor cannot replace it. Declare new options as properties and set them in an
    ///     object initializer instead of adding parameters here.
    /// </summary>
    [Obsolete(
        "Use the WindowFrame constructor and set Style, Leading, Trailing, CollapseWindow and " +
        "OnMinimizeChanged in an object initializer. This overload will be removed in 6.0.0.")]
    public WindowFrame
    (
        string title,
        Widget? child,
        Action? onClose,
        float titleBarHeight,
        Vector4? titleBarColor,
        Vector4? textColor,
        float fontSize,
        bool showMinimize,
        bool showClose,
        bool fillHeight,
        Framework.Key? key
    ) : this
    (
        title,
        child,
        onClose,
        titleBarHeight,
        titleBarColor,
        textColor,
        fontSize,
        showMinimize,
        showClose,
        fillHeight,
        style: null,
        leading: null,
        trailing: null,
        collapseWindow: false,
        onMinimizeChanged: null,
        key: key
    )
    {
    }

    /// <summary>Title text.</summary>
    public string Title { get; init; }

    /// <summary>Window content, hidden while minimized.</summary>
    public Widget? Child { get; init; }

    /// <summary>Called when the close button is tapped.</summary>
    public Action? OnClose { get; init; }

    /// <summary>Bar height. Overrides <see cref="WindowTitleBarStyle.Height" />.</summary>
    public float? TitleBarHeight { get; init; }

    /// <summary>
    ///     Bar background. Overrides <see cref="WindowTitleBarStyle.BackgroundColor" />.
    /// </summary>
    public Vector4? TitleBarColor { get; init; }

    /// <summary>Overrides the title and glyph text colors.</summary>
    public Vector4? TextColor { get; init; }

    /// <summary>Overrides the title and glyph font sizes.</summary>
    public float? FontSize { get; init; }

    /// <summary>Shows the minimize button.</summary>
    public bool ShowMinimize { get; init; }

    /// <summary>Shows the close button.</summary>
    public bool ShowClose { get; init; }

    /// <summary>
    ///     Title bar style tokens. <c>null</c> uses
    ///     <see cref="ThemeData.WindowTitleBarStyle" />. New options belong here, not in
    ///     the constructor: adding a property is additive for mods compiled against an
    ///     earlier release, adding a parameter is not.
    /// </summary>
    public WindowTitleBarStyle? Style { get; init; }

    /// <summary>Widget placed directly before the button group.</summary>
    public Widget? Leading { get; init; }

    /// <summary>Widget placed directly after the button group.</summary>
    public Widget? Trailing { get; init; }

    /// <summary>
    ///     When <c>true</c>, minimizing collapses the host window to the bar height.
    /// </summary>
    public bool CollapseWindow { get; init; }

    /// <summary>Called with the new minimized state.</summary>
    public Action<bool>? OnMinimizeChanged { get; init; }

    /// <summary>
    ///     When true, the child is wrapped in an <see cref="Expanded" /> and the
    ///     frame fills the height it is given, so a scrollable child receives
    ///     <c>windowHeight - titleBarHeight</c>. When false (default), the frame
    ///     hugs its child's intrinsic height (content-sized windows).
    /// </summary>
    public bool FillHeight { get; init; }

    /// <inheritdoc />
    public override State CreateState() => new WindowFrameState();

    private class WindowFrameState : State<WindowFrame>
    {
        private bool _isMinimized;

        public override Widget Build
        (
            BuildContext context
        )
        {
            var w = Widget;
            var children = new List<Widget>
            {
                new WindowTitleBar
                (
                    w.Title,
                    w.TitleBarHeight,
                    w.OnClose,
                    minimized =>
                    {
                        SetState(() => _isMinimized = minimized);
                        w.OnMinimizeChanged?.Invoke(minimized);
                    },
                    w.TitleBarColor,
                    w.TextColor,
                    fontSize: w.FontSize,
                    showMinimize: w.ShowMinimize,
                    showClose: w.ShowClose,
                    style: w.Style,
                    leading: w.Leading,
                    trailing: w.Trailing,
                    collapseWindow: w.CollapseWindow
                )
            };

            var fill = w.FillHeight && !_isMinimized;

            if (!_isMinimized && w.Child != null)
            {
                children.Add
                (
                    fill
                        ? new Expanded(w.Child)
                        : w.Child
                );
            }

            return new Column
            (
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                mainAxisSize: fill
                    ? MainAxisSize.Max
                    : MainAxisSize.Min,
                children: children
            );
        }
    }
}

using System;
using System.Collections.Generic;
using Gui.Core.Painting;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Widgets.Input;

/// <summary>A single selectable option in a <see cref="Dropdown{T}" />.</summary>
public class DropdownItem<T>
{
    public required T Value { get; set; }
    public required string Label { get; set; }
}

/// <summary>
///     A button that opens a scrollable floating menu of selectable options.
///     Supports per-widget style overrides via <see cref="Style" />.
/// </summary>
public class Dropdown<T> : StatefulWidget
{
    public Dropdown(
        T value,
        List<DropdownItem<T>> items,
        Action<T>? onChanged = null,
        float? width = null,
        int? visibleItemCount = null,
        DropdownStyle? style = null
    )
    {
        Value = value;
        Items = items;
        OnChanged = onChanged;
        Width = width;
        VisibleItemCount = visibleItemCount;
        Style = style;
    }

    /// <summary>Currently selected value.</summary>
    public T Value { get; }

    /// <summary>All available options.</summary>
    public List<DropdownItem<T>> Items { get; }

    /// <summary>Called when the user selects a different option.</summary>
    public Action<T>? OnChanged { get; }

    /// <summary>Fixed width for the trigger button and menu. Defaults to the button's natural width.</summary>
    public float? Width { get; }

    /// <summary>
    ///     Max visible items before scrolling. Overrides <see cref="DropdownStyle.VisibleItemCount" />.
    /// </summary>
    public int? VisibleItemCount { get; }

    /// <summary>
    ///     Per-widget style override. Falls back to <see cref="Theme.DropdownStyle" /> when null.
    /// </summary>
    public DropdownStyle? Style { get; }

    public override State CreateState() => new DropdownState<T>();
}

internal class DropdownState<T> : State<Dropdown<T>>
{
    private readonly LayerLink _link = new();
    private readonly ScrollController _scrollController = new();
    private OverlayEntry? _barrierEntry;
    private bool _isOpen;
    private OverlayEntry? _overlayEntry;

    private DropdownStyle _getStyle(BuildContext context) =>
        Widget.Style ?? Theme.Of(context).DropdownStyle;

    private float _menuMaxHeight(DropdownStyle style)
    {
        var visibleCount = Widget.VisibleItemCount ?? style.VisibleItemCount;
        var count = Math.Min(Widget.Items.Count, visibleCount);
        if (count <= 0)
        {
            return style.MenuPadding.Vertical;
        }

        var itemHeight = style.ItemPadding.Vertical + style.TextStyle.FontSize;
        return count * itemHeight
               + (count - 1) * style.ItemSpacing
               + style.MenuPadding.Vertical;
    }

    private Widget _buildMenu(DropdownStyle style)
    {
        return new Container(
            new BoxStyle
            {
                Color = style.MenuColor,
                BorderThickness = style.BorderThickness,
                BorderColor = style.BorderColor,
                CornerRadius = new Vector4(style.MenuCornerRadius),
                Padding = style.MenuPadding
            },
            new ConstrainedBox(
                new LayoutConstraints(maxHeight: _menuMaxHeight(style)),
                new SingleChildScrollView(
                    controller: _scrollController,
                    child: new Column(
                        style.ItemSpacing,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children: Widget.Items.ConvertAll(item =>
                            (Widget)new DropdownItemTile<T>(
                                item,
                                item.Value!.Equals(Widget.Value),
                                style,
                                () =>
                                {
                                    Widget.OnChanged?.Invoke(item.Value);
                                    _close();
                                }
                            )
                        )
                    )
                )
            )
        );
    }

    private void _toggle(BuildContext context)
    {
        if (_isOpen)
        {
            _close();
        }
        else
        {
            _open(context);
        }
    }

    private void _open(BuildContext context)
    {
        if (_isOpen)
        {
            return;
        }

        var overlay = Overlay.Overlay.Of(context);
        if (overlay == null)
        {
            return;
        }

        var style = _getStyle(context);
        var size = Element.RenderObject!.Size;

        _scrollToSelected(style);

        Widget menu = new SizedBox(
            Widget.Width ?? size.X,
            child: new DropdownMenu(_buildMenu(style))
        );

        _barrierEntry = new OverlayEntry(_buildBarrier());

        _overlayEntry = new OverlayEntry(
            new CompositedTransformFollower(
                _link,
                new Vector2(0, size.Y + style.MenuGap),
                menu
            )
        );

        overlay.Insert(_barrierEntry);
        overlay.Insert(_overlayEntry);
        SetState(() => _isOpen = true);
    }

    private Widget _buildBarrier()
    {
        return new Positioned(
            0,
            0,
            0,
            0,
            child: new GestureDetector(
                onTap: _ => _close(),
                child: new Container(new BoxStyle { HitTestBehavior = HitTestBehavior.Opaque })
            )
        );
    }

    private void _scrollToSelected(DropdownStyle style)
    {
        var idx = Widget.Items.FindIndex(i => i.Value!.Equals(Widget.Value));
        if (idx < 0)
        {
            return;
        }

        var itemHeight = style.ItemPadding.Vertical + style.TextStyle.FontSize;
        var itemOffset = idx * (itemHeight + style.ItemSpacing);
        var menuInnerHeight = _menuMaxHeight(style) - style.MenuPadding.Vertical;

        var viewStart = _scrollController.Offset;
        var viewEnd = viewStart + menuInnerHeight;
        var itemBottom = itemOffset + itemHeight;

        float scrollTo;
        if (itemBottom > viewEnd)
        {
            scrollTo = itemBottom - menuInnerHeight;
        }
        else if (itemOffset < viewStart)
        {
            scrollTo = itemOffset;
        }
        else
        {
            return;
        }

        _scrollController.JumpTo(Math.Max(0, scrollTo));
    }

    private void _close()
    {
        if (!_isOpen)
        {
            return;
        }

        _overlayEntry?.Remove();
        _overlayEntry = null;
        _barrierEntry?.Remove();
        _barrierEntry = null;
        SetState(() => _isOpen = false);
    }

    public override Widget Build(BuildContext context)
    {
        var style = _getStyle(context);
        var label = Widget.Items.Find(i => i.Value!.Equals(Widget.Value))?.Label ?? "Select...";

        return new CompositedTransformTarget(
            _link,
            new GestureDetector(
                onTap: e => _toggle(context),
                child: new Container(
                    new BoxStyle
                    {
                        Width = Widget.Width,
                        Height = style.ButtonHeight,
                        Color = style.ButtonColor,
                        BorderThickness = style.BorderThickness,
                        BorderColor = style.BorderColor,
                        CornerRadius = new Vector4(style.ButtonCornerRadius),
                        Padding = style.ButtonPadding
                    },
                    new Center(
                        new Row(
                            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                new Expanded(
                                    new Text(label, style.TextStyle)
                                ),
                                new AnimatedRotation(
                                    _isOpen ? MathF.PI : 0f,
                                    TimeSpan.FromMilliseconds(150),
                                    Curves.EaseOut,
                                    child: new Text(
                                        "▼",
                                        style.TextStyle with { Color = style.ChevronColor }
                                    )
                                )
                            ]
                        )
                    )
                )
            )
        );
    }

    public override void Dispose()
    {
        _close();
        _scrollController.Dispose();
        base.Dispose();
    }
}

internal class DropdownItemTile<T> : StatefulWidget
{
    public DropdownItemTile(
        DropdownItem<T> item,
        bool isSelected,
        DropdownStyle style,
        Action onTap
    )
    {
        Item = item;
        IsSelected = isSelected;
        Style = style;
        OnTap = onTap;
    }

    public DropdownItem<T> Item { get; }
    public bool IsSelected { get; }
    public DropdownStyle Style { get; }
    public Action OnTap { get; }

    public override State CreateState() => new DropdownItemTileState<T>();
}

internal class DropdownItemTileState<T> : State<DropdownItemTile<T>>
{
    private bool _isHovered;

    public override Widget Build(BuildContext context)
    {
        var style = Widget.Style;
        var bgColor = Widget.IsSelected
            ? style.SelectionColor
            : _isHovered
                ? style.HoverColor
                : Vector4.Zero;

        var labelColor = Widget.IsSelected
            ? style.SelectionAccentColor
            : style.TextStyle.Color;

        return new GestureDetector(
            onTap: _ => Widget.OnTap(),
            onEnter: _ => SetState(() => _isHovered = true),
            onExit: _ => SetState(() => _isHovered = false),
            child: new AnimatedContainer(
                duration: TimeSpan.FromMilliseconds(150),
                curve: Curves.EaseOut,
                style: new BoxStyle
                {
                    Color = bgColor,
                    Padding = style.ItemPadding,
                    CornerRadius = new Vector4(style.ItemCornerRadius)
                },
                child: new Text(
                    Widget.Item.Label,
                    style.TextStyle with
                    {
                        MaxLines = 1, Overflow = TextOverflow.Ellipsis, Color = labelColor
                    }
                )
            )
        );
    }
}

/// <summary>
///     Plays a scale-and-fade entry animation when the dropdown menu first mounts,
///     growing downward from the trigger's top edge.
/// </summary>
internal class DropdownMenu : StatefulWidget
{
    public DropdownMenu(
        Widget child
    )
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState() => new DropdownMenuState();
}

internal class DropdownMenuState : State<DropdownMenu>
{
    private AnimationController _anim = null!;

    public override void InitState()
    {
        base.InitState();
        _anim = new AnimationController(
            TimeSpan.FromMilliseconds(140),
            Element.Owner!.GetTickerProvider()
        );
        _anim.OnValueChanged += _onTick;
        _anim.Forward(0.0);
    }

    public override void Dispose()
    {
        _anim.OnValueChanged -= _onTick;
        _anim.Dispose();
        base.Dispose();
    }

    private void _onTick(
        double value
    ) =>
        SetState(() => { });

    public override Widget Build(
        BuildContext context
    )
    {
        var t = (float)Curves.EaseOut.Transform(_anim.Value);
        return new Opacity(
            t,
            Transform.ScaleXy(
                Widget.Child,
                1f,
                0.85f + 0.15f * t,
                Alignment.TopCenter
            )
        );
    }
}

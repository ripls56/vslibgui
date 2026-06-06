using System;
using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Widgets.Scroll;

/// <summary>
///     Position of the scrollbar relative to its child content.
/// </summary>
public enum ScrollbarPosition
{
    /// <summary>Scrollbar appears to the right of the content.</summary>
    Right,

    /// <summary>Scrollbar appears to the left of the content.</summary>
    Left
}

/// <summary>
///     A wrapper widget that displays a scrollbar alongside its child.
///     The scrollbar is driven by a <see cref="ScrollController" /> and supports
///     thumb dragging and click-to-jump on the track.
/// </summary>
/// <example>
///     <code>
/// var controller = new ScrollController();
/// new Scrollbar(
///   controller: controller,
///   child: new ListView(itemBuilder: ..., controller: controller)
/// )
/// </code>
/// </example>
public class Scrollbar : StatefulWidget
{
    /// <summary>Creates a new scrollbar wrapper widget.</summary>
    public Scrollbar(
        ScrollController controller,
        Widget child,
        ScrollbarPosition position = ScrollbarPosition.Right,
        float width = 12,
        Vector4? trackColor = null,
        Vector4? thumbColor = null,
        float thumbRadius = 4,
        bool reverse = false,
        Framework.Key? key = null
    ) : base(key)
    {
        Controller = controller;
        Child = child;
        Position = position;
        Width = width;
        TrackColor = trackColor;
        ThumbColor = thumbColor;
        ThumbRadius = thumbRadius;
        Reverse = reverse;
    }

    /// <summary>The scroll controller that drives the scrollbar.</summary>
    public ScrollController Controller { get; }

    /// <summary>The scrollable child widget.</summary>
    public Widget Child { get; }

    /// <summary>Position of the scrollbar (left or right). Defaults to right.</summary>
    public ScrollbarPosition Position { get; }

    /// <summary>Width of the scrollbar track in pixels. Defaults to 12.</summary>
    public float Width { get; }

    /// <summary>Optional override for the track color. Uses theme if null.</summary>
    public Vector4? TrackColor { get; }

    /// <summary>Optional override for the thumb color. Uses theme if null.</summary>
    public Vector4? ThumbColor { get; }

    /// <summary>Corner radius of the thumb. Defaults to 4.</summary>
    public float ThumbRadius { get; }

    /// <summary>
    ///     When true, the thumb position is inverted: offset 0 maps to the
    ///     bottom of the track (matching a reverse-mode ListView).
    /// </summary>
    public bool Reverse { get; }

    /// <summary>
    ///     When true (default), the scrollbar collapses and fades out after a period of
    ///     inactivity and reappears on scroll. When false, it stays permanently visible.
    /// </summary>
    public bool AutoHide { get; init; } = true;

    /// <inheritdoc />
    public override State CreateState() => new ScrollbarState();
}

internal class ScrollbarState : State<Scrollbar>
{
    private static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(1100);

    private float _dragStartOffset;
    private float _dragStartY;

    private AnimationController _fade = null!;
    private TimeSpan _idleElapsed;
    private Ticker _idleTicker = null!;
    private bool _isDragging;
    private float _trackHeight;
    private RenderObject? _trackRenderObject;

    public override void InitState()
    {
        base.InitState();
        Widget.Controller.OnChanged += _HandleChanged;
        var vsync = Element.Owner!.GetTickerProvider();
        _fade = new AnimationController(TimeSpan.FromMilliseconds(250), vsync);
        _fade.OnValueChanged += _OnFadeTick;
        _idleTicker = vsync.CreateTicker(_OnIdleTick);
    }

    private void _OnFadeTick(double _) => SetState(() => { });

    private void _Show()
    {
        _idleElapsed = TimeSpan.Zero;
        _fade.Forward();
        _idleTicker.Start();
    }

    private void _OnIdleTick(TimeSpan dt)
    {
        if (_isDragging)
        {
            _idleElapsed = TimeSpan.Zero;
            return;
        }

        _idleElapsed += dt;
        if (_idleElapsed < HideDelay)
        {
            return;
        }

        _idleTicker.Stop();
        _fade.Reverse();
    }

    public override void UpdateWidget(
        Scrollbar oldWidget
    )
    {
        base.UpdateWidget(oldWidget);
        if (!ReferenceEquals(
                oldWidget.Controller,
                Widget.Controller
            ))
        {
            oldWidget.Controller.OnChanged -= _HandleChanged;
            Widget.Controller.OnChanged += _HandleChanged;
        }
    }

    private void _HandleChanged()
    {
        if (Widget.AutoHide)
        {
            _Show();
        }

        SetState(() => { });
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;

        var trackColor = Widget.TrackColor ?? new Vector4(
            colors.OnSurface.X,
            colors.OnSurface.Y,
            colors.OnSurface.Z,
            0.1f
        );
        var thumbColor = Widget.ThumbColor ?? new Vector4(
            colors.OnSurface.X,
            colors.OnSurface.Y,
            colors.OnSurface.Z,
            0.4f
        );

        var visibility = Widget.AutoHide ? (float)_fade.Value : 1f;
        var trackWidth = Widget.Width * visibility;
        var isRight = Widget.Position == ScrollbarPosition.Right;

        var scrollbarTrack = new ScrollbarTrackWidget(
            Widget.Controller.Offset,
            Widget.Controller.ContentSize,
            Widget.Controller.ViewportSize,
            trackColor: trackColor,
            thumbColor: thumbColor,
            thumbRadius: Widget.ThumbRadius,
            reverse: Widget.Reverse,
            onLayout: (
                height,
                ro
            ) =>
            {
                _trackHeight = height;
                _trackRenderObject = ro;
            },
            child: new GestureDetector(
                onPress: _OnPress,
                onRelease: _OnRelease,
                onMove: _OnMove
            )
        );

        var contentPadding = isRight
            ? EdgeInsets.Only(right: trackWidth)
            : EdgeInsets.Only(trackWidth);

        return new Stack(new Widget[]
        {
            new Positioned(
                0,
                0,
                0,
                0,
                child: new Padding(contentPadding, Widget.Child)
            ),
            new Positioned(
                top: 0,
                bottom: 0,
                left: isRight ? null : 0,
                right: isRight ? 0 : null,
                width: trackWidth,
                child: scrollbarTrack
            )
        });
    }

    private float _ToLocalY(
        PointerEvent e
    )
    {
        if (_trackRenderObject == null)
        {
            return e.Y;
        }

        return _trackRenderObject.GlobalToLocal(
                new Vector2(
                    e.X,
                    e.Y
                )
            )
            .Y;
    }

    private void _OnPress(
        PointerEvent e
    )
    {
        var ctrl = Widget.Controller;
        var maxOffset = ctrl.MaxScrollExtent;
        if (maxOffset <= 0)
        {
            return;
        }

        var thumbHeight = _ComputeThumbHeight();
        var maxTravel = _trackHeight - thumbHeight;
        if (maxTravel <= 0)
        {
            return;
        }

        var localY = _ToLocalY(e);
        var ratio = ctrl.Offset / maxOffset;
        if (Widget.Reverse)
        {
            ratio = 1 - ratio;
        }

        var thumbY = ratio * maxTravel;

        // Click outside thumb → jump to that position
        if (localY < thumbY || localY > thumbY + thumbHeight)
        {
            var clickRatio = Math.Clamp(
                (localY - thumbHeight / 2) / maxTravel,
                0,
                1
            );
            var newOffset = Widget.Reverse
                ? (1 - clickRatio) * maxOffset
                : clickRatio * maxOffset;
            ctrl.JumpTo(
                Math.Clamp(
                    newOffset,
                    0,
                    maxOffset
                )
            );
        }

        _isDragging = true;
        _dragStartY = localY;
        _dragStartOffset = ctrl.Offset;
    }

    private void _OnRelease(
        PointerEvent e
    ) =>
        _isDragging = false;

    private void _OnMove(
        PointerEvent e
    )
    {
        if (!_isDragging || _trackHeight <= 0)
        {
            return;
        }

        var ctrl = Widget.Controller;
        var maxOffset = ctrl.MaxScrollExtent;
        if (maxOffset <= 0)
        {
            return;
        }

        var thumbHeight = _ComputeThumbHeight();
        var maxTravel = _trackHeight - thumbHeight;
        if (maxTravel <= 0)
        {
            return;
        }

        var localY = _ToLocalY(e);
        var deltaY = localY - _dragStartY;
        var scrollDelta = deltaY / maxTravel * maxOffset;
        if (Widget.Reverse)
        {
            scrollDelta = -scrollDelta;
        }

        var newOffset = Math.Clamp(
            _dragStartOffset + scrollDelta,
            0,
            maxOffset
        );
        ctrl.JumpTo(newOffset);
    }

    private float _ComputeThumbHeight()
    {
        var ctrl = Widget.Controller;
        if (ctrl.ContentSize <= 0)
        {
            return 20;
        }

        return Math.Max(
            20,
            ctrl.ViewportSize / ctrl.ContentSize * _trackHeight
        );
    }

    public override void Dispose()
    {
        Widget.Controller.OnChanged -= _HandleChanged;
        _fade.OnValueChanged -= _OnFadeTick;
        _fade.Dispose();
        _idleTicker.Dispose();
        base.Dispose();
    }
}

/// <summary>
///     Internal render widget for the scrollbar track and thumb.
/// </summary>
internal class ScrollbarTrackWidget : SingleChildWidget
{
    /// <summary>Creates a new scrollbar track render widget.</summary>
    public ScrollbarTrackWidget(
        float scrollOffset,
        float contentSize,
        float viewportSize,
        Action<float, RenderObject> onLayout,
        Widget child,
        Vector4 trackColor,
        Vector4 thumbColor,
        float thumbRadius = 4,
        bool reverse = false
    ) : base(child)
    {
        ScrollOffset = scrollOffset;
        ContentSize = contentSize;
        ViewportSize = viewportSize;
        OnLayout = onLayout;
        TrackColor = trackColor;
        ThumbColor = thumbColor;
        ThumbRadius = thumbRadius;
        Reverse = reverse;
    }

    /// <summary>Current scroll offset in pixels.</summary>
    public float ScrollOffset { get; }

    /// <summary>Total content size in pixels.</summary>
    public float ContentSize { get; }

    /// <summary>Visible viewport size in pixels.</summary>
    public float ViewportSize { get; }

    /// <summary>Callback invoked after layout with the track height and render object.</summary>
    public Action<float, RenderObject> OnLayout { get; }

    /// <summary>Background color of the scrollbar track.</summary>
    public Vector4 TrackColor { get; }

    /// <summary>Color of the draggable thumb.</summary>
    public Vector4 ThumbColor { get; }

    /// <summary>Corner radius of the thumb.</summary>
    public float ThumbRadius { get; }

    /// <summary>Whether the thumb position is inverted (reverse mode).</summary>
    public bool Reverse { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderScrollbarTrack(OnLayout)
        {
            ScrollOffset = ScrollOffset,
            ContentSize = ContentSize,
            ViewportSize = ViewportSize,
            TrackColor = TrackColor,
            ThumbColor = ThumbColor,
            ThumbRadius = ThumbRadius,
            Reverse = Reverse
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderScrollbarTrack)renderObject;
        ro.ScrollOffset = ScrollOffset;
        ro.ContentSize = ContentSize;
        ro.ViewportSize = ViewportSize;
        ro.TrackColor = TrackColor;
        ro.ThumbColor = ThumbColor;
        ro.ThumbRadius = ThumbRadius;
        ro.Reverse = Reverse;
    }
}

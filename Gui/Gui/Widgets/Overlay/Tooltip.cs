using System;
using Gui.Core.Framework;
using Gui.Core.Overlay;
using Gui.Core.Painting;
using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Overlay;

/// <summary>
///     Wraps a <see cref="Child" /> widget and shows a floating <see cref="Content" />
///     widget above it after the pointer has hovered for <see cref="WaitDuration" />.
///     The tooltip fades in and out over <see cref="FadeDuration" />.
///     <para>
///         Uses <see cref="CompositedTransformTarget" />/<see cref="CompositedTransformFollower" />
///         so the tooltip tracks the trigger's position during scrolling.
///         Requires an <see cref="Overlay" /> ancestor (provided by <c>GuiBase</c>).
///     </para>
///     <code>
/// new Tooltip(
///   content: new Text("Saves the current file"),
///   child: new IconButton(icon: "save", onPressed: Save)
/// )
/// </code>
/// </summary>
public class Tooltip : StatefulWidget
{
    public Tooltip(
        Widget child,
        Widget content,
        TimeSpan? waitDuration = null,
        TimeSpan? fadeDuration = null,
        float verticalGap = 8f,
        float? anchorX = null,
        bool useGlobalOverlay = false,
        Framework.Key? key = null
    ) : base(key)
    {
        Child = child;
        Content = content;
        WaitDuration = waitDuration ?? TimeSpan.FromMilliseconds(500);
        FadeDuration = fadeDuration ?? TimeSpan.FromMilliseconds(150);
        VerticalGap = verticalGap;
        AnchorX = anchorX;
        UseGlobalOverlay = useGlobalOverlay;
    }

    /// <summary>The widget that acts as the hover trigger.</summary>
    public Widget Child { get; }

    /// <summary>The widget displayed inside the tooltip bubble.</summary>
    public Widget Content { get; }

    /// <summary>
    ///     How long the pointer must remain over <see cref="Child" /> before the tooltip
    ///     appears. Defaults to 500 ms.
    /// </summary>
    public TimeSpan WaitDuration { get; }

    /// <summary>
    ///     Duration of the fade-in and fade-out animations. Defaults to 150 ms.
    /// </summary>
    public TimeSpan FadeDuration { get; }

    /// <summary>
    ///     Pixel gap between the trigger's top edge and the tooltip's bottom edge.
    ///     Defaults to 8.
    /// </summary>
    public float VerticalGap { get; }

    /// <summary>
    ///     Horizontal anchor position within the child, in pixels from the child's
    ///     left edge. When null (default), the tooltip centers on the child.
    /// </summary>
    public float? AnchorX { get; }

    /// <summary>
    ///     When true, the tooltip overlay entry is inserted into
    ///     <see cref="GuiGlobalOverlay" /> instead of the nearest
    ///     <see cref="Overlay" /> ancestor. Use this when the trigger lives
    ///     inside a shrink-wrapped window that would clip the tooltip.
    /// </summary>
    public bool UseGlobalOverlay { get; }

    public override State CreateState() => new TooltipState();
}

internal class TooltipState : State<Tooltip>
{
    private readonly LayerLink _link = new();
    private AnimationController _anim = null!;
    private Ticker _delayTicker = null!;
    private TimeSpan _elapsed;
    private bool _isVisible;
    private OverlayEntry? _overlayEntry;

    public override void InitState()
    {
        base.InitState();
        var vsync = Element.Owner!.GetTickerProvider();
        _anim = new AnimationController(
            Widget.FadeDuration,
            vsync
        );
        _anim.OnValueChanged += _onAnimTick;
        _anim.OnStatusChanged += _onAnimStatus;
        _delayTicker = vsync.CreateTicker(_onDelayTick);
    }

    public override void UpdateWidget(
        Tooltip oldWidget
    )
    {
        base.UpdateWidget(oldWidget);
        if (_overlayEntry != null && _isVisible)
        {
            _overlayEntry.Widget = _buildOverlay();
            _overlayEntry.MarkNeedsBuild();
        }
    }

    public override void Dispose()
    {
        _delayTicker.Dispose();
        _anim.OnValueChanged -= _onAnimTick;
        _anim.OnStatusChanged -= _onAnimStatus;
        _overlayEntry?.Remove();
        _overlayEntry = null;
        _isVisible = false;
        _anim.Dispose();
        base.Dispose();
    }

    private void _onEnter(
        PointerEvent _
    )
    {
        _elapsed = TimeSpan.Zero;
        if (_isVisible)
        {
            _anim.Forward();
            return;
        }

        _delayTicker.Start();
    }

    private void _onExit(
        PointerEvent _
    )
    {
        _delayTicker.Stop();
        _elapsed = TimeSpan.Zero;
        if (_isVisible)
        {
            _close();
        }
    }

    private void _onDelayTick(
        TimeSpan dt
    )
    {
        _elapsed += dt;
        if (_elapsed < Widget.WaitDuration)
        {
            return;
        }

        _delayTicker.Stop();
        _elapsed = TimeSpan.Zero;
        _open();
    }

    private void _open()
    {
        if (_isVisible)
        {
            return;
        }

        _isVisible = true;
        _overlayEntry = new OverlayEntry(_buildOverlay());

        if (Widget.UseGlobalOverlay)
        {
            GuiGlobalOverlay.Insert(_overlayEntry);
        }
        else
        {
            var overlay = Overlay.Of(
                new BuildContext(
                    Widget,
                    Element
                )
            );
            if (overlay == null)
            {
                _isVisible = false;
                _overlayEntry = null;
                return;
            }

            overlay.Insert(_overlayEntry);
        }

        _anim.Forward(0.0);
    }

    private void _close()
    {
        if (!_isVisible)
        {
            return;
        }

        _anim.Reverse();
    }

    private void _onAnimTick(
        double value
    )
    {
        if (_overlayEntry == null)
        {
            return;
        }

        _overlayEntry.Widget = _buildOverlay();
        _overlayEntry.MarkNeedsBuild();
    }

    private void _onAnimStatus(
        AnimationStatus status
    )
    {
        if (status != AnimationStatus.Dismissed)
        {
            return;
        }

        if (!_isVisible)
        {
            return;
        }

        _isVisible = false;
        _overlayEntry?.Remove();
        _overlayEntry = null;
    }

    private Widget _buildOverlay()
    {
        var ro = Element.RenderObject;
        var size = ro?.Size ?? Vector2.Zero;
        var anchorX = Widget.AnchorX ?? size.X / 2f;

        // Flip tooltip below the trigger when there is not enough room above.
        // When using the global overlay the tooltip lives in a different window,
        // so we need screen-absolute coordinates for clamping / flip logic.
        var globalPos = ro?.LocalToGlobal(Vector2.Zero) ?? Vector2.Zero;
        if (Widget.UseGlobalOverlay && ro != null)
        {
            globalPos += ro.GetScreenOffset();
        }

        var showBelow = globalPos.Y < 200f;

        var offsetY = showBelow
            ? size.Y + Widget.VerticalGap
            : -Widget.VerticalGap;
        var fracY = showBelow
            ? 0f
            : -1f;

        var t = (float)Curves.EaseOut.Transform(_anim.Value);
        var animatedOffsetY = offsetY + (showBelow ? -1f : 1f) * (1f - t) * 6f;

        // Read theme from the tooltip's own element context
        var theme = Element.DependOnInheritedWidgetOfExactType<Theme>();
        var colors = theme?.Data.ColorScheme ?? ThemeData.Default.ColorScheme;

        return new CompositedTransformFollower(
            _link,
            new Vector2(
                anchorX,
                animatedOffsetY
            ),
            new ClampedTranslation(
                globalPos.X + anchorX,
                fracY,
                new Opacity(
                    t,
                    new Container(
                        new BoxStyle
                        {
                            Color = colors.Background,
                            BorderThickness = 1f,
                            BorderColor = colors.Border,
                            CornerRadius = new Vector4(4f),
                            Padding = EdgeInsets.Symmetric(
                                5f,
                                10f
                            )
                        },
                        Widget.Content
                    )
                )
            )
        );
    }

    public override Widget Build(
        BuildContext context
    )
    {
        return new CompositedTransformTarget(
            _link,
            new MouseRegion(
                onEnter: _onEnter,
                onExit: _onExit,
                child: Widget.Child
            )
        );
    }
}

/// <summary>
///     Like <see cref="FractionalTranslation" /> but clamps the horizontal
///     position so the child stays within the overlay bounds.
/// </summary>
internal class ClampedTranslation : SingleChildWidget
{
    public ClampedTranslation(
        float anchorX,
        float fractionalY,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        AnchorX = anchorX;
        FractionalY = fractionalY;
    }

    /// <summary>
    ///     Anchor X position in global/stack coordinates (the
    ///     point the tooltip is centered on).
    /// </summary>
    public float AnchorX { get; }

    /// <summary>Vertical fractional shift (−1 = above, 0 = below).</summary>
    public float FractionalY { get; }

    public override RenderObject CreateRenderObject() =>
        new RenderClampedTranslation { AnchorX = AnchorX, FractionalY = FractionalY };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderClampedTranslation)renderObject;
        ro.AnchorX = AnchorX;
        ro.FractionalY = FractionalY;
    }
}

using System;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     Wraps a <see cref="Child" /> and overlays a cursor-tracking
///     <see cref="SpotlightBorder" /> while the pointer hovers over it. Hover detection,
///     pointer tracking, and the fade in/out are handled internally, so any widget can gain
///     the spotlight effect by wrapping it.
/// </summary>
public class SpotlightHover : StatefulWidget
{
    /// <summary>Creates a spotlight-on-hover wrapper around <paramref name="child" />.</summary>
    /// <param name="child">The widget to overlay the spotlight on.</param>
    /// <param name="glowColor">Glow RGBA; its alpha scales the peak opacity.</param>
    /// <param name="cornerRadius">Corner radius of the spotlight border in pixels.</param>
    /// <param name="fadeDuration">Fade in/out duration. Defaults to 150 ms.</param>
    /// <param name="cursor">Cursor shown while hovering. Optional.</param>
    public SpotlightHover(
        Widget child,
        Vector4 glowColor,
        float cornerRadius = 0f,
        TimeSpan? fadeDuration = null,
        MouseCursor? cursor = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Child = child;
        GlowColor = glowColor;
        CornerRadius = cornerRadius;
        FadeDuration = fadeDuration ?? TimeSpan.FromMilliseconds(150);
        Cursor = cursor;
    }

    /// <summary>The widget the spotlight is drawn over.</summary>
    public Widget Child { get; }

    /// <summary>Glow color as RGBA (each channel 0–1); alpha scales peak opacity.</summary>
    public Vector4 GlowColor { get; }

    /// <summary>Corner radius of the spotlight border in pixels.</summary>
    public float CornerRadius { get; }

    /// <summary>Duration of the fade-in and fade-out.</summary>
    public TimeSpan FadeDuration { get; }

    /// <summary>Cursor shown while the pointer is inside the region.</summary>
    public MouseCursor? Cursor { get; }

    /// <inheritdoc />
    public override State CreateState() => new SpotlightHoverState();
}

internal class SpotlightHoverState : State<SpotlightHover>
{
    private readonly ValueNotifier<Vector2?> _pointer = new(null);
    private AnimationController _fade = null!;

    public override void InitState()
    {
        base.InitState();
        _fade = new AnimationController(Widget.FadeDuration, Element.Owner!.GetTickerProvider());
        _fade.OnValueChanged += _onFadeTick;
    }

    public override void Dispose()
    {
        _fade.OnValueChanged -= _onFadeTick;
        _fade.Dispose();
        _pointer.Dispose();
        base.Dispose();
    }

    private void _onFadeTick(double _) => SetState(() => { });

    private void _onEnter(PointerEvent e)
    {
        _pointer.Value = new Vector2(e.X, e.Y);
        _fade.Forward();
    }

    private void _onHover(PointerEvent e) => _pointer.Value = new Vector2(e.X, e.Y);

    private void _onExit(PointerEvent e) => _fade.Reverse();

    public override Widget Build(BuildContext context)
    {
        var intensity = (float)_fade.Value;
        var content = Widget.Child;
        if (intensity > 0f)
        {
            content = new Stack([
                Widget.Child,
                new Positioned(
                    0,
                    0,
                    0,
                    0,
                    child: new SpotlightBorder(
                        intensity,
                        Widget.GlowColor,
                        _pointer,
                        Widget.CornerRadius
                    )
                )
            ]);
        }

        return new MouseRegion(
            cursor: Widget.Cursor,
            onEnter: _onEnter,
            onExit: _onExit,
            onHover: _onHover,
            child: content
        );
    }
}

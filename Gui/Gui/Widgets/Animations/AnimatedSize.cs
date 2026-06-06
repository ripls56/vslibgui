using System;
using Gui.Core.Animations;
using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Animations;

/// <summary>
///     A widget that animates its size whenever its child's layout size changes.
///     Wraps <see cref="RenderAnimatedSize" /> which owns an internal
///     <see cref="AnimationController" />.
/// </summary>
public class AnimatedSize : SingleChildWidget
{
    /// <summary>The easing curve applied to the animation.</summary>
    public readonly Curve Curve;

    /// <summary>How long the size transition takes.</summary>
    public readonly TimeSpan Duration;

    /// <summary>
    ///     Creates a new <see cref="AnimatedSize" /> widget.
    /// </summary>
    /// <param name="duration">How long the size transition takes.</param>
    /// <param name="curve">The easing curve. Defaults to <see cref="Curves.Linear" />.</param>
    /// <param name="child">The child widget whose size changes are animated.</param>
    /// <param name="key">Optional key for element identity.</param>
    public AnimatedSize(
        TimeSpan duration,
        Curve? curve = null,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        Duration = duration;
        Curve = curve ?? Curves.Linear;
    }

    /// <inheritdoc />
    public override Element CreateElement() => new AnimatedSizeElement(this);

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderAnimatedSize(
            Duration,
            Curve
        );
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderAnimatedSize)renderObject;
        ro.Duration = Duration;
    }
}

/// <summary>
///     Element for <see cref="AnimatedSize" />. Injects the ticker provider
///     into the render object after creation during mount.
/// </summary>
public class AnimatedSizeElement : SingleChildElement
{
    /// <summary>
    ///     Creates a new <see cref="AnimatedSizeElement" />.
    /// </summary>
    public AnimatedSizeElement(
        AnimatedSize widget
    ) : base(widget)
    {
    }

    /// <inheritdoc />
    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        ((RenderAnimatedSize)RenderObject!).Initialize(
            Owner!.GetTickerProvider()
        );
    }
}

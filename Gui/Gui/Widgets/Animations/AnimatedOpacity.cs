using System;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;

namespace Gui.Widgets.Animations;

/// <summary>
///     Automatically animates changes to <see cref="OpacityValue" /> over
///     <see cref="ImplicitlyAnimatedWidget.Duration" />.
/// </summary>
public class AnimatedOpacity : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated opacity widget that transitions between opacity
    ///     values over the given <paramref name="duration" />.
    /// </summary>
    public AnimatedOpacity(
        float opacity,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Widget? child = null,
        Framework.Key? key = null
    )
        : base(
            duration,
            curve,
            onEnd,
            key
        )
    {
        OpacityValue = opacity;
        Child = child;
    }

    /// <summary>Target opacity value in [0.0, 1.0].</summary>
    public float OpacityValue { get; }

    /// <summary>The child widget to apply opacity to.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedOpacityState();
}

/// <summary>
///     State for <see cref="AnimatedOpacity" /> that manages the opacity tween.
/// </summary>
internal class AnimatedOpacityState
    : ImplicitlyAnimatedWidgetState<AnimatedOpacity>
{
    private FloatTween? _opacityTween;

    /// <inheritdoc />
    protected override void ForEachTween(
        TweenVisitor visitor
    )
    {
        _opacityTween = (FloatTween)visitor.Visit(
            _opacityTween,
            Widget.OpacityValue,
            v => new FloatTween(
                v,
                v
            )
        );
    }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        return new Opacity(
            _opacityTween!.Evaluate(Animation),
            Widget.Child
        );
    }
}

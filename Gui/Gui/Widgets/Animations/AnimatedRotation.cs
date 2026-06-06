using System;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using SkiaSharp;

namespace Gui.Widgets.Animations;

/// <summary>
///     Automatically animates changes to <see cref="Angle" /> (in radians) over
///     <see cref="ImplicitlyAnimatedWidget.Duration" />.
/// </summary>
public class AnimatedRotation : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated rotation widget that transitions between angle
    ///     values over the given <paramref name="duration" />.
    /// </summary>
    public AnimatedRotation(
        float angle,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Alignment? alignment = null,
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
        Angle = angle;
        Alignment = alignment ?? Alignment.Center;
        Child = child;
    }

    /// <summary>Target rotation angle in radians.</summary>
    public float Angle { get; }

    /// <summary>Transform origin alignment. Defaults to center.</summary>
    public Alignment? Alignment { get; }

    /// <summary>The child widget.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedRotationState();
}

/// <summary>
///     State for <see cref="AnimatedRotation" /> that manages the angle tween.
/// </summary>
internal class AnimatedRotationState
    : ImplicitlyAnimatedWidgetState<AnimatedRotation>
{
    private FloatTween? _angleTween;

    /// <inheritdoc />
    protected override void ForEachTween(
        TweenVisitor visitor
    )
    {
        _angleTween = (FloatTween)visitor.Visit(
            _angleTween,
            Widget.Angle,
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
        var angle = _angleTween!.Evaluate(Animation);
        return new Transform(
            Widget.Child,
            SKMatrix.CreateRotation(angle),
            Widget.Alignment
        );
    }
}

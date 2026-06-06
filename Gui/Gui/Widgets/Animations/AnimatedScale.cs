using System;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using SkiaSharp;

namespace Gui.Widgets.Animations;

/// <summary>
///     Automatically animates changes to <see cref="Scale" /> over
///     <see cref="ImplicitlyAnimatedWidget.Duration" />, applying the
///     transform via <see cref="Transform" />.
/// </summary>
public class AnimatedScale : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated scale widget that transitions between scale
    ///     values over the given <paramref name="duration" />.
    /// </summary>
    public AnimatedScale(
        float scale,
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
        Scale = scale;
        Alignment = alignment ?? Alignment.Center;
        Child = child;
    }

    /// <summary>Target uniform scale factor.</summary>
    public float Scale { get; }

    /// <summary>Transform origin alignment. Defaults to center.</summary>
    public Alignment? Alignment { get; }

    /// <summary>The child widget.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedScaleState();
}

/// <summary>
///     State for <see cref="AnimatedScale" /> that manages the scale tween.
/// </summary>
internal class AnimatedScaleState
    : ImplicitlyAnimatedWidgetState<AnimatedScale>
{
    private FloatTween? _scaleTween;

    /// <inheritdoc />
    protected override void ForEachTween(
        TweenVisitor visitor
    )
    {
        _scaleTween = (FloatTween)visitor.Visit(
            _scaleTween,
            Widget.Scale,
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
        var scale = _scaleTween!.Evaluate(Animation);
        return new Transform(
            Widget.Child,
            SKMatrix.CreateScale(
                scale,
                scale
            ),
            Widget.Alignment
        );
    }
}

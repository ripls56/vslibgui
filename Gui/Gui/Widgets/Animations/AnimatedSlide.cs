using System;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Animations;

/// <summary>
///     Automatically animates changes to <see cref="Offset" /> over
///     <see cref="ImplicitlyAnimatedWidget.Duration" />. The offset is expressed
///     as pixel translation values applied via <see cref="Transform" />.
/// </summary>
public class AnimatedSlide : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated slide widget that transitions between offset
    ///     values over the given <paramref name="duration" />.
    /// </summary>
    public AnimatedSlide(
        Vector2 offset,
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
        Offset = offset;
        Child = child;
    }

    /// <summary>Target translation offset.</summary>
    public Vector2 Offset { get; }

    /// <summary>The child widget.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedSlideState();
}

/// <summary>
///     State for <see cref="AnimatedSlide" /> that manages the offset tween.
/// </summary>
internal class AnimatedSlideState
    : ImplicitlyAnimatedWidgetState<AnimatedSlide>
{
    private OffsetTween? _offsetTween;

    /// <inheritdoc />
    protected override void ForEachTween(
        TweenVisitor visitor
    )
    {
        _offsetTween = (OffsetTween)visitor.Visit(
            _offsetTween,
            Widget.Offset,
            v => new OffsetTween(
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
        var offset = _offsetTween!.Evaluate(Animation);
        return new Transform(
            Widget.Child,
            SKMatrix.CreateTranslation(
                offset.X,
                offset.Y
            )
        );
    }
}

using System;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Widgets.Animations;

/// <summary>
///     Automatically animates changes to <see cref="PaddingValue" /> over
///     <see cref="ImplicitlyAnimatedWidget.Duration" />.
/// </summary>
public class AnimatedPadding : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated padding widget that transitions between padding
    ///     values over the given <paramref name="duration" />.
    /// </summary>
    public AnimatedPadding(
        EdgeInsets padding,
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
        PaddingValue = padding;
        Child = child;
    }

    /// <summary>Target padding.</summary>
    public EdgeInsets PaddingValue { get; }

    /// <summary>The child widget.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedPaddingState();
}

/// <summary>
///     State for <see cref="AnimatedPadding" /> that manages the padding tween.
/// </summary>
internal class AnimatedPaddingState
    : ImplicitlyAnimatedWidgetState<AnimatedPadding>
{
    private EdgeInsetsTween? _paddingTween;

    /// <inheritdoc />
    protected override void ForEachTween(
        TweenVisitor visitor
    )
    {
        _paddingTween = (EdgeInsetsTween)visitor.Visit(
            _paddingTween,
            Widget.PaddingValue,
            v => new EdgeInsetsTween(
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
        return new Padding(
            _paddingTween!.Evaluate(Animation),
            Widget.Child
        );
    }
}

using System;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;

namespace Gui.Widgets.Animations;

/// <summary>
///     A container that implicitly animates changes to its <see cref="Style" />
///     properties over the given <see cref="ImplicitlyAnimatedWidget.Duration" />.
/// </summary>
public class AnimatedContainer : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated container that transitions between styles.
    /// </summary>
    /// <param name="style">The target box style.</param>
    /// <param name="duration">How long the transition takes.</param>
    /// <param name="curve">Easing curve (defaults to linear).</param>
    /// <param name="onEnd">Callback fired when animation completes.</param>
    /// <param name="child">Optional child widget.</param>
    /// <param name="key">Optional key for element identity.</param>
    public AnimatedContainer(
        BoxStyle style,
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
        Style = style;
        Child = child;
    }

    /// <summary>The target box style to animate towards.</summary>
    public BoxStyle Style { get; }

    /// <summary>Optional child widget rendered inside the container.</summary>
    public Widget? Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedContainerState();

    private class AnimatedContainerState
        : ImplicitlyAnimatedWidgetState<AnimatedContainer>
    {
        private ColorTween? _colorTween;
        private GradientTween? _gradientTween;
        private FloatTween? _heightTween;
        private Vector4Tween? _radiusTween;
        private FloatTween? _widthTween;

        /// <inheritdoc />
        protected override void ForEachTween(
            TweenVisitor visitor
        )
        {
            var s = Widget.Style;

            _colorTween = (ColorTween)visitor.Visit(
                _colorTween,
                s.Color,
                v => new ColorTween(
                    v,
                    v
                )
            );

            _gradientTween = (GradientTween)visitor.Visit(
                _gradientTween,
                s.Gradient,
                v => new GradientTween(
                    v,
                    v
                )
            );

            _radiusTween = (Vector4Tween)visitor.Visit(
                _radiusTween,
                s.CornerRadius,
                v => new Vector4Tween(
                    v,
                    v
                )
            );

            var targetW = s.Width ?? 0;
            _widthTween = (FloatTween)visitor.Visit(
                _widthTween,
                targetW,
                v => new FloatTween(
                    v,
                    v
                )
            );

            var targetH = s.Height ?? 0;
            _heightTween = (FloatTween)visitor.Visit(
                _heightTween,
                targetH,
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
            var currentStyle = new BoxStyle
            {
                Color = _colorTween!.Evaluate(Animation),
                Gradient = _gradientTween!.Evaluate(Animation),
                CornerRadius = _radiusTween!.Evaluate(Animation),
                Width = Widget.Style.Width != null
                    ? _widthTween!.Evaluate(Animation)
                    : null,
                Height = Widget.Style.Height != null
                    ? _heightTween!.Evaluate(Animation)
                    : null,
                BorderThickness = Widget.Style.BorderThickness,
                BorderColor = Widget.Style.BorderColor,
                Padding = Widget.Style.Padding,
                ClipBehavior = Widget.Style.ClipBehavior,
                HitTestBehavior = Widget.Style.HitTestBehavior,
                Texture = Widget.Style.Texture,
                BoxShadows = Widget.Style.BoxShadows
            };

            return new Container(
                currentStyle,
                Widget.Child
            );
        }
    }
}

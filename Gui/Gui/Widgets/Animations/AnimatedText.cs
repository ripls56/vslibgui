using System;
using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using TextWidget = Gui.Widgets.Basic.Text;

namespace Gui.Widgets.Animations;

/// <summary>
///     Displays text that implicitly animates changes to its <see cref="Style" />
///     over the given <see cref="ImplicitlyAnimatedWidget.Duration" />. The
///     interpolatable fields (color, font size, outline, glow and boldness)
///     transition smoothly; the text <see cref="Content" /> and the remaining
///     fields (font family, weight, alignment, decoration, overflow, wrap and
///     max lines) update instantly.
/// </summary>
public class AnimatedText : ImplicitlyAnimatedWidget
{
    /// <summary>
    ///     Creates an animated text widget that transitions between text styles.
    /// </summary>
    /// <param name="content">The text to display (updated instantly, never animated).</param>
    /// <param name="style">The target text style whose fields are animated.</param>
    /// <param name="duration">How long the transition takes.</param>
    /// <param name="curve">Easing curve (defaults to linear).</param>
    /// <param name="onEnd">Callback fired when the animation completes.</param>
    /// <param name="key">Optional key for element identity.</param>
    public AnimatedText
    (
        string content,
        TextStyle style,
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Framework.Key? key = null
    )
        : base
        (
            duration,
            curve,
            onEnd,
            key
        )
    {
        Content = content;
        Style = style;
    }

    /// <summary>
    ///     The text content to display. Changes apply instantly; it is never
    ///     interpolated.
    /// </summary>
    public string Content { get; }

    /// <summary>The target text style to animate towards.</summary>
    public TextStyle Style { get; }

    /// <inheritdoc />
    public override State CreateState() => new AnimatedTextState();

    private class AnimatedTextState
        : ImplicitlyAnimatedWidgetState<AnimatedText>
    {
        private FloatTween? _boldnessTween;
        private ColorTween? _colorTween;
        private FloatTween? _fontSizeTween;
        private ColorTween? _glowColorTween;
        private FloatTween? _glowWidthTween;
        private ColorTween? _outlineColorTween;
        private FloatTween? _outlineWidthTween;

        /// <inheritdoc />
        protected override void ForEachTween
        (
            TweenVisitor visitor
        )
        {
            var s = Widget.Style;

            _boldnessTween = (FloatTween)visitor.Visit
            (
                _boldnessTween,
                s.Boldness,
                v => new FloatTween
                (
                    v,
                    v
                )
            );

            _colorTween = (ColorTween)visitor.Visit
            (
                _colorTween,
                s.Color,
                v => new ColorTween
                (
                    v,
                    v
                )
            );

            _fontSizeTween = (FloatTween)visitor.Visit
            (
                _fontSizeTween,
                s.FontSize,
                v => new FloatTween
                (
                    v,
                    v
                )
            );

            _glowColorTween = (ColorTween)visitor.Visit
            (
                _glowColorTween,
                s.GlowColor,
                v => new ColorTween
                (
                    v,
                    v
                )
            );

            _glowWidthTween = (FloatTween)visitor.Visit
            (
                _glowWidthTween,
                s.GlowWidth,
                v => new FloatTween
                (
                    v,
                    v
                )
            );

            _outlineColorTween = (ColorTween)visitor.Visit
            (
                _outlineColorTween,
                s.OutlineColor,
                v => new ColorTween
                (
                    v,
                    v
                )
            );

            _outlineWidthTween = (FloatTween)visitor.Visit
            (
                _outlineWidthTween,
                s.OutlineWidth,
                v => new FloatTween
                (
                    v,
                    v
                )
            );
        }

        /// <inheritdoc />
        public override Widget Build
        (
            BuildContext context
        )
        {
            var interpolated = new TextStyle
            {
                Color = _colorTween!.Evaluate(Animation),
                FontSize = _fontSizeTween!.Evaluate(Animation),
                OutlineWidth = _outlineWidthTween!.Evaluate(Animation),
                OutlineColor = _outlineColorTween!.Evaluate(Animation),
                GlowWidth = _glowWidthTween!.Evaluate(Animation),
                GlowColor = _glowColorTween!.Evaluate(Animation),
                Boldness = _boldnessTween!.Evaluate(Animation),
                FontFamily = Widget.Style.FontFamily,
                Weight = Widget.Style.Weight,
                Align = Widget.Style.Align,
                Overflow = Widget.Style.Overflow,
                Decoration = Widget.Style.Decoration,
                SoftWrap = Widget.Style.SoftWrap,
                MaxLines = Widget.Style.MaxLines
            };

            return new TextWidget
            (
                Widget.Content,
                interpolated
            );
        }
    }
}

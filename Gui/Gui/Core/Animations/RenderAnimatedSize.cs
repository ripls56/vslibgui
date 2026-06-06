using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Animations;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Animations;

/// <summary>
///     A <see cref="RenderProxyBox" /> that smoothly animates its size whenever
///     its child's intrinsic size changes. The animation is driven by an
///     internal <see cref="AnimationController" /> with a configurable duration
///     and curve.
/// </summary>
public class RenderAnimatedSize : RenderProxyBox
{
    private readonly Curve _initialCurve;
    private readonly TimeSpan _initialDuration;
    private readonly SizeTween _sizeTween;
    private AnimationController? _controller;
    private CurvedAnimation? _curvedAnimation;
    private bool _hasLayoutOnce;
    private Vector2 _lastChildSize;

    /// <summary>
    ///     Creates a new <see cref="RenderAnimatedSize" />.
    /// </summary>
    /// <param name="duration">How long the size transition takes.</param>
    /// <param name="curve">The easing curve applied to the animation.</param>
    /// <param name="vsync">Ticker provider for driving the animation.</param>
    public RenderAnimatedSize(
        TimeSpan duration,
        Curve curve,
        ITickerProvider vsync
    )
    {
        _initialDuration = duration;
        _initialCurve = curve;
        _sizeTween = new SizeTween(
            Vector2.Zero,
            Vector2.Zero
        );
        Initialize(vsync);
    }

    /// <summary>
    ///     Creates a new <see cref="RenderAnimatedSize" /> without a ticker provider.
    ///     Call <see cref="Initialize" /> before the first layout to supply one.
    /// </summary>
    /// <param name="duration">How long the size transition takes.</param>
    /// <param name="curve">The easing curve applied to the animation.</param>
    public RenderAnimatedSize(
        TimeSpan duration,
        Curve curve
    )
    {
        _initialDuration = duration;
        _initialCurve = curve;
        _sizeTween = new SizeTween(
            Vector2.Zero,
            Vector2.Zero
        );
    }

    /// <summary>
    ///     Total time to animate from the old size to the new size.
    /// </summary>
    public TimeSpan Duration
    {
        get => _controller?.Duration ?? _initialDuration;
        set
        {
            if (_controller != null)
            {
                _controller.Duration = value;
            }
        }
    }

    /// <summary>
    ///     Supplies the ticker provider and creates the internal animation
    ///     controller. Must be called before the first layout when the
    ///     two-argument constructor was used.
    /// </summary>
    /// <param name="vsync">Ticker provider for driving the animation.</param>
    public void Initialize(
        ITickerProvider vsync
    )
    {
        _controller = new AnimationController(
            _initialDuration,
            vsync
        );
        _curvedAnimation = new CurvedAnimation(
            _controller,
            _initialCurve
        );
        _controller.OnValueChanged += _ => MarkNeedsLayout();
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        if (Children.Count == 0)
        {
            Size = Constraints.Constrain(Vector2.Zero);
            return;
        }

        var child = Children[0];
        child.Layout(Constraints);
        var childSize = child.Size;

        if (!_hasLayoutOnce)
        {
            _lastChildSize = childSize;
            _sizeTween.Begin = childSize;
            _sizeTween.End = childSize;
            _hasLayoutOnce = true;
            Size = Constraints.Constrain(childSize);
            return;
        }

        if (_controller == null || _curvedAnimation == null)
        {
            Size = Constraints.Constrain(childSize);
            return;
        }

        if (childSize != _lastChildSize)
        {
            _sizeTween.Begin = _sizeTween.Evaluate(_curvedAnimation);
            _sizeTween.End = childSize;
            _lastChildSize = childSize;
            _controller.Value = 0;
            _controller.Forward();
        }

        Size = Constraints.Constrain(_sizeTween.Evaluate(_curvedAnimation));
    }

    /// <inheritdoc />
    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas != null)
        {
            context.Canvas.Save();
            context.Canvas.ClipRect(
                new SKRect(
                    0,
                    0,
                    Size.X,
                    Size.Y
                )
            );
            base.Paint(context);
            context.Canvas.Restore();
        }
        else
        {
            base.Paint(context);
        }
    }

    /// <summary>
    ///     Disposes the internal animation controller.
    /// </summary>
    public override void Dispose()
    {
        _controller?.Dispose();
        base.Dispose();
    }
}

using System;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Animations;

/// <summary>
///     Abstract base class for widgets that automatically animate when their
///     properties change. Subclasses declare which properties to animate by
///     overriding <see cref="ImplicitlyAnimatedWidgetState{T}.ForEachTween" />
///     in their companion state class.
/// </summary>
public abstract class ImplicitlyAnimatedWidget : StatefulWidget
{
    /// <summary>
    ///     Creates an implicitly animated widget with the given animation parameters.
    /// </summary>
    /// <param name="duration">How long the animation takes.</param>
    /// <param name="curve">Easing curve (defaults to linear).</param>
    /// <param name="onEnd">Callback fired when animation completes.</param>
    /// <param name="key">Optional key for element identity.</param>
    protected ImplicitlyAnimatedWidget(
        TimeSpan duration,
        Curve? curve = null,
        Action? onEnd = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Duration = duration;
        Curve = curve ?? Curves.Linear;
        OnEnd = onEnd;
    }

    /// <summary>Duration of the animation when a property changes.</summary>
    public TimeSpan Duration { get; }

    /// <summary>
    ///     Easing curve applied to the animation. Defaults to
    ///     <see cref="Curves.Linear" />.
    /// </summary>
    public Curve Curve { get; }

    /// <summary>
    ///     Optional callback invoked when the animation reaches
    ///     <see cref="AnimationStatus.Completed" />.
    /// </summary>
    public Action? OnEnd { get; }
}

/// <summary>
///     Helper that visits tweens during <see cref="ImplicitlyAnimatedWidgetState{T}.ForEachTween" />.
///     Creates new tweens or updates existing ones when the target value changes,
///     and tracks whether any tween was modified so the controller knows to animate.
/// </summary>
public class TweenVisitor
{
    private readonly IAnimation _animation;

    internal TweenVisitor(
        IAnimation animation
    )
    {
        _animation = animation;
    }

    /// <summary>
    ///     <c>true</c> if any tween's target value was updated during this visit pass.
    /// </summary>
    internal bool Changed { get; private set; }

    /// <summary>
    ///     Registers a tween for implicit animation. If <paramref name="current" />
    ///     is null, creates a new tween via <paramref name="constructor" />. If the
    ///     target value changed, updates the tween's begin/end and marks the visitor
    ///     as changed.
    /// </summary>
    /// <typeparam name="T">The animated value type.</typeparam>
    /// <param name="current">The existing tween, or null on first visit.</param>
    /// <param name="targetValue">The desired end value.</param>
    /// <param name="constructor">
    ///     Factory that creates a new <see cref="Tween{T}" /> with Begin=End=targetValue.
    /// </param>
    /// <returns>The current or newly created tween.</returns>
    public Tween<T> Visit<T>(
        Tween<T>? current,
        T targetValue,
        Func<T, Tween<T>> constructor
    )
    {
        if (current == null)
        {
            return constructor(targetValue);
        }

        if (!Equals(
                current.End,
                targetValue
            ))
        {
            current.Begin = current.Evaluate(_animation);
            current.End = targetValue;
            Changed = true;
        }

        return current;
    }
}

/// <summary>
///     Abstract state class for <see cref="ImplicitlyAnimatedWidget" /> subclasses.
///     Manages the <see cref="AnimationController" /> and
///     <see cref="CurvedAnimation" /> lifecycle, and drives rebuilds when the
///     animation value changes.
///     <para>
///         Subclasses must override <see cref="ForEachTween" /> to register their
///         tweens, and <see cref="State{T}.Build" /> to use the interpolated values.
///     </para>
/// </summary>
/// <typeparam name="T">
///     The concrete <see cref="ImplicitlyAnimatedWidget" /> subclass.
/// </typeparam>
public abstract class ImplicitlyAnimatedWidgetState<T> : State<T>
    where T : ImplicitlyAnimatedWidget
{
    private CurvedAnimation _curvedAnimation = null!;

    /// <summary>The underlying animation controller.</summary>
    protected AnimationController Controller { get; private set; } = null!;

    /// <summary>
    ///     The curved animation driven by the controller. Use this to evaluate
    ///     tweens in <see cref="State{T}.Build" />.
    /// </summary>
    protected IAnimation Animation => _curvedAnimation;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        Controller = new AnimationController(
            Widget.Duration,
            Element.Owner!.GetTickerProvider()
        );
        _curvedAnimation = new CurvedAnimation(
            Controller,
            Widget.Curve
        );
        Controller.OnStatusChanged += HandleStatusChanged;
        Controller.OnValueChanged += HandleValueChanged;

        var visitor = new TweenVisitor(_curvedAnimation);
        ForEachTween(visitor);
    }

    /// <inheritdoc />
    public override void UpdateWidget(
        T oldWidget
    )
    {
        base.UpdateWidget(oldWidget);

        if (Widget.Duration != oldWidget.Duration)
        {
            Controller.Duration = Widget.Duration;
        }

        if (Widget.Curve != oldWidget.Curve)
        {
            _curvedAnimation = new CurvedAnimation(
                Controller,
                Widget.Curve
            );
        }

        var visitor = new TweenVisitor(_curvedAnimation);
        ForEachTween(visitor);

        if (visitor.Changed)
        {
            Controller.Value = 0;
            Controller.Forward();
        }
    }

    /// <summary>
    ///     Called during <see cref="InitState" /> and <see cref="UpdateWidget" /> to
    ///     register all tweens. Implementations must call
    ///     <see cref="TweenVisitor.Visit{T}" /> for each animated property and store
    ///     the returned tween.
    /// </summary>
    /// <param name="visitor">The visitor used to create or update tweens.</param>
    protected abstract void ForEachTween(
        TweenVisitor visitor
    );

    private void HandleStatusChanged(
        AnimationStatus status
    )
    {
        if (status == AnimationStatus.Completed)
        {
            Widget.OnEnd?.Invoke();
        }
    }

    private void HandleValueChanged(
        double _
    ) =>
        SetState(() => { });

    /// <inheritdoc />
    public override void Dispose()
    {
        Controller.OnStatusChanged -= HandleStatusChanged;
        Controller.OnValueChanged -= HandleValueChanged;
        Controller.Dispose();
        base.Dispose();
    }
}

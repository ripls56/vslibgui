using System;
using System.Diagnostics;
using Gui.Widgets.Animations;

namespace Gui.Widgets.Gestures;

/// <summary>
///     Manages scroll state, ticker lifecycle, and physics integration.
/// </summary>
public class ScrollController : IDisposable
{
    private readonly ScrollPhysics _physics;
    private readonly Stopwatch _simStopwatch = new();

    private float _contentSize;

    private Simulation? _simulation;
    private Ticker? _ticker;

    private float _viewportSize;

    public ScrollController(
        ScrollPhysics? physics = null
    )
    {
        _physics = physics ?? new ClampingScrollPhysics();
    }

    /// <summary>Current scroll offset in pixels.</summary>
    public float Offset
    {
        get;
        set
        {
            if (Math.Abs(field - value) > 0.001f)
            {
                field = value;
                OnChanged?.Invoke();
            }
        }
    }

    /// <summary>
    ///     The visible viewport height (or width for horizontal scroll).
    ///     Set by the owning scrollable widget during layout.
    /// </summary>
    public float ViewportSize
    {
        get => _viewportSize;
        set
        {
            if (Math.Abs(_viewportSize - value) > 0.001f)
            {
                _viewportSize = value;
                OnChanged?.Invoke();
            }
        }
    }

    /// <summary>
    ///     The total content height (or width for horizontal scroll).
    ///     Set by the owning scrollable widget during layout.
    /// </summary>
    public float ContentSize
    {
        get => _contentSize;
        set
        {
            if (Math.Abs(_contentSize - value) > 0.001f)
            {
                _contentSize = value;
                OnChanged?.Invoke();
            }
        }
    }

    /// <summary>
    ///     Maximum scrollable offset: <c>ContentSize - ViewportSize</c>, clamped to 0.
    /// </summary>
    public float MaxScrollExtent => Math.Max(
        0,
        _contentSize - _viewportSize
    );

    /// <summary>
    ///     When set, the next layout update will jump to the maximum
    ///     scroll extent. Cleared automatically after the jump.
    /// </summary>
    public bool PendingScrollToEnd { get; set; }

    /// <summary>
    ///     True while a simulation (fling or AnimateTo) is running.
    /// </summary>
    public bool IsAnimating => _simulation != null;

    public float MinScroll { get; private set; }
    public float MaxScroll { get; private set; }

    public void Dispose() => _ticker?.Dispose();

    public event Action? OnChanged;

    public void Attach(
        ITickerProvider provider
    )
    {
        _ticker?.Dispose();
        _ticker = provider.CreateTicker(OnTick);
    }

    private void OnTick(
        TimeSpan elapsed
    )
    {
        if (_simulation == null)
        {
            return;
        }

        var t = (float)_simStopwatch.Elapsed.TotalSeconds;
        var newOffset = _simulation.X(t);
        var velocity = _simulation.Dx(t);

        // Boundary handling
        if (newOffset < MinScroll || newOffset > MaxScroll)
        {
            newOffset = Math.Clamp(
                newOffset,
                MinScroll,
                MaxScroll
            );
            Stop();
        }

        if (Math.Abs(velocity) < _physics.MinVelocity)
        {
            Stop();
        }

        Offset = newOffset;
    }

    public void StartSimulation(
        float velocity,
        float minScroll,
        float maxScroll
    )
    {
        Stop();
        MinScroll = minScroll;
        MaxScroll = maxScroll;

        // VelocityTracker measures dy as last.Y - first.Y (downward drag = positive velocity).
        // The scroll offset increases as content moves up (scrolling down), so we negate to
        // convert the pointer-drag velocity into the simulation's initial scroll velocity.
        _simulation = _physics.CreateFlingSimulation(
            Offset,
            -velocity
        );
        _simStopwatch.Restart();
        _ticker?.Start();
    }

    public void Stop()
    {
        _simulation = null;
        _simStopwatch.Stop();
        _ticker?.Stop();
    }

    public void JumpTo(
        float value
    )
    {
        Stop();
        Offset = value;
    }

    /// <summary>
    ///     Smoothly animates the scroll offset to <paramref name="offset" /> over
    ///     <paramref name="duration" /> using the given easing <paramref name="curve" />.
    ///     Cancels any in-flight fling or previous animation.
    /// </summary>
    public void AnimateTo(
        float offset,
        TimeSpan duration,
        Curve? curve = null,
        float minScroll = 0f,
        float maxScroll = float.MaxValue
    )
    {
        Stop();
        MinScroll = minScroll;
        MaxScroll = maxScroll;
        offset = Math.Clamp(
            offset,
            MinScroll,
            MaxScroll
        );

        if (duration <= TimeSpan.Zero || Math.Abs(Offset - offset) < 0.5f)
        {
            Offset = offset;
            return;
        }

        _simulation = new ScrollToSimulation(
            Offset,
            offset,
            duration,
            curve ?? Curves.EaseOut
        );
        _simStopwatch.Restart();
        _ticker?.Start();
    }

    private sealed class ScrollToSimulation : Simulation
    {
        private readonly Curve _curve;
        private readonly float _durationSeconds;
        private readonly float _end;
        private readonly float _start;

        public ScrollToSimulation(
            float start,
            float end,
            TimeSpan duration,
            Curve curve
        )
        {
            _start = start;
            _end = end;
            _durationSeconds = (float)duration.TotalSeconds;
            _curve = curve;
        }

        public override float X(
            float time
        )
        {
            if (_durationSeconds <= 0f)
            {
                return _end;
            }

            var t = Math.Clamp(
                time / _durationSeconds,
                0f,
                1f
            );
            return _start + (_end - _start) * (float)_curve.Transform(t);
        }

        public override float Dx(
            float time
        )
        {
            // Return a value above MinVelocity to keep the ticker alive
            // during the animation, and 0 when complete to trigger Stop().
            return time >= _durationSeconds
                ? 0f
                : 200f;
        }
    }
}

using System;

namespace Gui.Widgets.Animations;

/// <summary>Represents the current direction and terminal state of an animation.</summary>
public enum AnimationStatus
{
    /// <summary>Animation is at 0.0 and is not running.</summary>
    Dismissed,

    /// <summary>Animation is advancing toward 1.0.</summary>
    Forward,

    /// <summary>Animation is retreating toward 0.0.</summary>
    Reverse,

    /// <summary>Animation is at 1.0 and is not running.</summary>
    Completed
}

/// <summary>
///     Drives a <c>double</c> value from 0.0 to 1.0 (or in reverse) over a given
///     <c>duration</c>. The value is advanced each game frame by a <see cref="Ticker" /> tied to
///     the GUI's frame loop via <see cref="ITickerProvider" />.
///     <para>
///         Typical usage:
///         <code>
/// // In State.InitState():
/// _ctrl = new AnimationController(TimeSpan.FromMilliseconds(300),
///     Element.Owner.GetTickerProvider());
/// _ctrl.Forward();
/// 
/// // In State.Dispose():
/// _ctrl.Dispose();
/// </code>
///     </para>
/// </summary>
public class AnimationController : IAnimation, IDisposable
{
    private readonly Ticker _ticker;
    private TimeSpan _duration;
    private double _value;

    /// <param name="duration">Total time to animate from 0.0 to 1.0 (or 1.0 to 0.0).</param>
    /// <param name="vsync">Ticker provider from <c>Element.Owner.GetTickerProvider()</c>.</param>
    public AnimationController(
        TimeSpan duration,
        ITickerProvider vsync
    )
    {
        _duration = duration;
        _ticker = vsync.CreateTicker(OnTick);
    }

    /// <summary>
    ///     Total time to animate from 0.0 to 1.0 (or 1.0 to 0.0).
    ///     Can be updated at runtime; takes effect on the next tick.
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set => _duration = value;
    }

    /// <summary><c>true</c> if the ticker is currently running.</summary>
    public bool IsAnimating => _ticker.IsTicking;

    /// <summary>
    ///     Current animation position in the range [0.0, 1.0].
    /// </summary>
    /// <remarks>
    ///     The setter directly mutates the position without updating <see cref="Status" />,
    ///     firing <see cref="OnValueChanged" />, or firing <see cref="OnStatusChanged" />.
    ///     Use <see cref="Forward" />, <see cref="Reverse" />, or <see cref="Reset" /> for
    ///     lifecycle-correct transitions. Only use the setter for scrubbing scenarios where
    ///     you intentionally manage status yourself.
    /// </remarks>
    public double Value
    {
        get => _value;
        set => _value = Math.Clamp(
            value,
            0.0,
            1.0
        );
    }

    /// <summary>Current animation direction / terminal state.</summary>
    public AnimationStatus Status { get; private set; } = AnimationStatus.Dismissed;

    public void Dispose() => _ticker.Dispose();

    /// <summary>Fired every tick with the updated <see cref="Value" />.</summary>
    public event Action<double>? OnValueChanged;

    /// <summary>
    ///     Fired when the animation reaches a terminal state (<see cref="AnimationStatus.Completed" />
    ///     or <see cref="AnimationStatus.Dismissed" />). Use this to implement ping-pong loops or
    ///     chain animations.
    /// </summary>
    public event Action<AnimationStatus>? OnStatusChanged;

    private void OnTick(
        TimeSpan delta
    )
    {
        var oldValue = _value;

        var deltaPercent = _duration.TotalMilliseconds > 0
            ? delta.TotalMilliseconds / _duration.TotalMilliseconds
            : 1.0;

        var change = deltaPercent * (Status == AnimationStatus.Reverse
            ? -1
            : 1);
        _value += change;

        var hitTarget = false;
        if (Status == AnimationStatus.Forward && _value >= 1.0)
        {
            hitTarget = true;
        }
        else if (Status == AnimationStatus.Reverse && _value <= 0.0)
        {
            hitTarget = true;
        }

        if (hitTarget)
        {
            _value = Math.Clamp(
                _value,
                0.0,
                1.0
            );
            Stop();
            Status = _value >= 1.0
                ? AnimationStatus.Completed
                : AnimationStatus.Dismissed;
            OnStatusChanged?.Invoke(Status);
        }

        if (Math.Abs(_value - oldValue) > 1e-6)
        {
            OnValueChanged?.Invoke(_value);
        }
    }

    /// <summary>
    ///     Starts animating toward 1.0. If <paramref name="from" /> is supplied, snaps
    ///     <see cref="Value" /> to that position before starting.
    /// </summary>
    public void Forward(
        double? from = null
    )
    {
        if (from.HasValue)
        {
            _value = from.Value;
        }

        Status = AnimationStatus.Forward;
        if (_value < 1.0)
        {
            _ticker.Start();
        }
        else
        {
            Stop();
            Status = AnimationStatus.Completed;
        }

        OnStatusChanged?.Invoke(Status);
    }

    /// <summary>Starts animating toward 0.0 from the current <see cref="Value" />.</summary>
    public void Reverse()
    {
        Status = AnimationStatus.Reverse;
        if (_value > 0.0)
        {
            _ticker.Start();
        }
        else
        {
            Stop();
            Status = AnimationStatus.Dismissed;
        }

        OnStatusChanged?.Invoke(Status);
    }

    /// <summary>
    ///     Resumes animation in the current direction if the ticker is paused and there is
    ///     remaining distance to travel. No-op if already animating or already at the target.
    /// </summary>
    public void Resume()
    {
        if (_ticker.IsTicking)
        {
            return;
        }

        if (Status == AnimationStatus.Forward && _value < 1.0)
        {
            _ticker.Start();
        }
        else if (Status == AnimationStatus.Reverse && _value > 0.0)
        {
            _ticker.Start();
        }
    }

    /// <summary>Pauses the animation at its current <see cref="Value" />.</summary>
    public void Stop() => _ticker.Stop();

    /// <summary>Stops the animation and snaps <see cref="Value" /> back to 0.0.</summary>
    public void Reset()
    {
        Stop();
        _value = 0.0;
        Status = AnimationStatus.Dismissed;
        OnValueChanged?.Invoke(_value);
        OnStatusChanged?.Invoke(Status);
    }
}

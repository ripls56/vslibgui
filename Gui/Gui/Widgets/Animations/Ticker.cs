using System;

namespace Gui.Widgets.Animations;

public class Ticker : IDisposable
{
    private readonly Action<TimeSpan> _onTick;
    private bool _muted = true;

    public Ticker(
        Action<TimeSpan> onTick
    )
    {
        _onTick = onTick;
    }

    public bool IsTicking => !_muted;
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        Stop();
        IsDisposed = true;
    }

    public void Start() => _muted = false;

    public void Stop() => _muted = true;

    /// <summary>
    ///     Called each frame by <see cref="TickerScheduler" />. <paramref name="frameDelta" />
    ///     is the time elapsed since the last frame — not the time since <see cref="Start" />.
    /// </summary>
    public void Tick(
        TimeSpan frameDelta
    )
    {
        if (_muted)
        {
            return;
        }

        _onTick(frameDelta);
    }
}

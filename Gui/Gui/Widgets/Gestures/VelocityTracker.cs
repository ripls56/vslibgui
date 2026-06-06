using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Mathematics;

namespace Gui.Widgets.Gestures;

/// <summary>
///     Tracks the velocity of pointer movements using a sliding window and high-precision timer.
/// </summary>
public class VelocityTracker
{
    private const double HorizonSeconds = 0.2; // Slightly wider for robustness

    private readonly List<EventPoint> _points = [];
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void AddPosition(
        float x,
        float y
    )
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        _points.Add(
            new EventPoint
            {
                Position = new Vector2(
                    x,
                    y
                ),
                TimeSeconds = now
            }
        );
        Prune(now);
    }

    private void Prune(
        double now
    )
    {
        // Prune relative to the LATEST point we have, not absolute current time
        // to avoid clearing the whole buffer if there's a small delay in GetVelocity call
        if (_points.Count == 0)
        {
            return;
        }

        var referenceTime = _points[^1].TimeSeconds;

        while (_points.Count > 0 && referenceTime - _points[0].TimeSeconds > HorizonSeconds)
        {
            _points.RemoveAt(0);
        }
    }

    public float GetVelocity()
    {
        // Don't prune here with absolute 'now', it can wipe valid points if there's a pause
        if (_points.Count < 2)
        {
            return 0;
        }

        var first = _points[0];
        var last = _points[^1];

        var duration = last.TimeSeconds - first.TimeSeconds;
        if (duration < 0.001)
        {
            return 0; // Avoid division by zero or jitter
        }

        var deltaY = last.Position.Y - first.Position.Y;
        return deltaY / (float)duration; // px/s
    }

    public void Clear() => _points.Clear();

    private struct EventPoint
    {
        public Vector2 Position;
        public double TimeSeconds;
    }
}

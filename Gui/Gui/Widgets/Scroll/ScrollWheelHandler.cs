using System;
using Gui.Widgets.Gestures;

namespace Gui.Widgets.Scroll;

/// <summary>
///     Handles kinetic wheel-scroll physics shared by scroll containers.
///     Accumulates wheel impulses into a velocity and feeds them to
///     <see cref="ScrollController.StartSimulation" />, producing momentum-based
///     inertia scrolling. One instance per scroll container.
/// </summary>
internal sealed class ScrollWheelHandler
{
    /// <summary>Base impulse magnitude added to velocity on each wheel tick.</summary>
    private const float WheelImpulseStrength = 1200f;

    /// <summary>Timestamp of the previous wheel event, used to detect burst vs. sustained scrolling.</summary>
    private DateTime _lastTime = DateTime.MinValue;

    /// <summary>Current accumulated scroll velocity in pixels per second.</summary>
    private float _velocity;

    /// <summary>
    ///     Returns <c>true</c> when the controller can accept a wheel delta in the given
    ///     direction. Initializes <see cref="ScrollController.StartSimulation" /> when
    ///     <see cref="ScrollController.MaxScroll" /> is zero so the controller is ready
    ///     before the first real scroll event.
    ///     <para>
    ///         Returns <c>false</c> when the offset is already at the boundary that the
    ///         <paramref name="delta" /> would push toward, preventing over-consumption of
    ///         events that should bubble up to a parent scrollable.
    ///     </para>
    /// </summary>
    /// <param name="controller">The scroll controller to test.</param>
    /// <param name="delta">Raw wheel delta (positive = up/left).</param>
    /// <param name="maxScroll">Current maximum scroll extent in pixels.</param>
    /// <param name="reverse">When <c>true</c>, the scroll axis is reversed.</param>
    public static bool CanScroll(
        ScrollController controller,
        float delta,
        float maxScroll,
        bool reverse
    )
    {
        if (controller.MaxScroll == 0)
        {
            controller.StartSimulation(0, 0, maxScroll);
        }

        if (!reverse)

        {
            if (Math.Abs(controller.Offset - controller.MinScroll) < 0.1 && Math.Sign(delta) == 1)
            {
                return false;
            }

            if (Math.Abs(controller.Offset - controller.MaxScroll) < 0.1 && Math.Sign(delta) == -1)
            {
                return false;
            }
        }
        else
        {
            if (Math.Abs(controller.Offset - controller.MinScroll) < 0.1 && Math.Sign(delta) == -1)
            {
                return false;
            }

            if (Math.Abs(controller.Offset - controller.MaxScroll) < 0.1 && Math.Sign(delta) == 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Applies a wheel <paramref name="delta" /> to the controller using kinetic physics.
    ///     Accumulates impulses when ticks arrive rapidly (dt &lt; 100 ms) and resets
    ///     velocity when direction reverses or ticks are spaced far apart.
    /// </summary>
    /// <param name="delta">Raw wheel delta (positive = up/left).</param>
    /// <param name="reverse">When <c>true</c>, the scroll axis is reversed.</param>
    /// <param name="maxScroll">Current maximum scroll extent in pixels.</param>
    /// <param name="controller">The scroll controller to drive.</param>
    /// <returns>
    ///     <c>true</c> when a simulation was started and the event should be
    ///     considered consumed; <c>false</c> when there is no scrollable content
    ///     (<paramref name="maxScroll" /> is zero) or the boundary guard in
    ///     <see cref="CanScroll" /> blocked the delta.
    /// </returns>
    public bool Apply(
        float delta,
        bool reverse,
        float maxScroll,
        ScrollController controller
    )
    {
        if (!CanScroll(controller, delta, maxScroll, reverse))
        {
            return false;
        }

        var now = DateTime.Now;
        var dt = (now - _lastTime).TotalSeconds;
        _lastTime = now;

        var impulse = -delta * WheelImpulseStrength;
        if (reverse)
        {
            impulse = -impulse;
        }

        if (Math.Sign(impulse) != Math.Sign(_velocity) && Math.Abs(_velocity) > 1)
        {
            _velocity = impulse;
        }
        else if (dt < 0.1)
        {
            _velocity += impulse;
        }
        else
        {
            _velocity = impulse;
        }

        _velocity = Math.Clamp(_velocity, -8000f, 8000f);
        controller.StartSimulation(-_velocity, 0, maxScroll);
        return maxScroll > 0;
    }

    /// <summary>Clamps controller offset into [0, maxScroll] with ±50 tolerance.</summary>
    public static void ClampOffset(ScrollController controller, float maxScroll)
    {
        if (controller.Offset < -50 || controller.Offset > maxScroll + 50)
        {
            controller.JumpTo(Math.Clamp(controller.Offset, 0, maxScroll));
        }
    }
}

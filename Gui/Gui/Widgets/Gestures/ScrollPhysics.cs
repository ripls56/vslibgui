using System;
using Gui.Widgets.Animations;

namespace Gui.Widgets.Gestures;

/// <summary>
///     Defines how scrolling behaves, including friction and edge handling.
/// </summary>
public abstract class ScrollPhysics
{
    /// <summary>
    ///     Exponential decay constant for the friction simulation, used as e^(-Drag * t).
    ///     Higher values = faster deceleration. Typical range: 2–30.
    /// </summary>
    public abstract float Drag { get; }

    /// <summary>
    ///     Simulation velocity (px/s) below which the fling is considered stopped.
    /// </summary>
    public abstract float MinVelocity { get; }

    public virtual Simulation CreateFlingSimulation(
        float position,
        float velocity
    )
    {
        return new FrictionSimulation(
            Drag,
            position,
            velocity
        );
    }

    public virtual float ApplyPhysicsToOffset(
        float offset,
        float min,
        float max
    )
    {
        return Math.Clamp(
            offset,
            min,
            max
        );
    }
}

public class ClampingScrollPhysics : ScrollPhysics
{
    public override float Drag => 25.0f; // Very strong friction
    public override float MinVelocity => 100.0f; // High stop threshold
}

public class BouncingScrollPhysics : ScrollPhysics
{
    // Future implementation: handle overscroll with spring simulation
    public override float Drag => 2.5f;
    public override float MinVelocity => 15.0f;
}

using System;

namespace Gui.Widgets.Animations;

public abstract class Simulation
{
    public abstract float X(
        float time
    );

    public abstract float Dx(
        float time
    );
}

public class FrictionSimulation : Simulation
{
    private readonly float _drag;
    private readonly float _initialPosition;
    private readonly float _initialVelocity;

    public FrictionSimulation(
        float drag,
        float initialPosition,
        float initialVelocity
    )
    {
        _drag = drag;
        _initialPosition = initialPosition;
        _initialVelocity = initialVelocity;
    }

    public override float X(
        float time
    )
    {
        // Physics formula for position with friction: x(t) = x0 + v0 * (1 - e^(-drag * t)) / drag
        if (Math.Abs(_drag) < 0.0001f)
        {
            return _initialPosition + _initialVelocity * time;
        }

        return _initialPosition + _initialVelocity * (1 - (float)Math.Exp(-_drag * time)) / _drag;
    }

    public override float Dx(
        float time
    )
    {
        // Physics formula for velocity: v(t) = v0 * e^(-drag * t)
        return _initialVelocity * (float)Math.Exp(-_drag * time);
    }
}

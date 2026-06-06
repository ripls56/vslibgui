using System;

namespace Gui.Widgets.Animations;

/// <summary>Base class for animation easing curves.</summary>
public abstract class Curve
{
    /// <summary>Maps animation time <paramref name="t" /> in [0,1] to an eased value.</summary>
    public abstract double Transform(
        double t
    );
}

/// <summary>
///     Maps a sub-range [<see cref="Begin" />, <see cref="End" />] of the animation
///     timeline to [0, 1], applying an optional inner curve within that range.
///     Values before <see cref="Begin" /> return 0; values after <see cref="End" />
///     return 1. Useful for sequencing implicit animations — e.g.
///     <c>new Interval(0.7, 1.0)</c> delays an effect until the last 30 % of the
///     shared duration.
/// </summary>
public class Interval : Curve
{
    private readonly Curve _inner;

    public Interval(double begin, double end, Curve? inner = null)
    {
        Begin = begin;
        End = end;
        _inner = inner ?? Curves.Linear;
    }

    /// <summary>Fraction of the total duration at which this interval begins.</summary>
    public double Begin { get; }

    /// <summary>Fraction of the total duration at which this interval ends.</summary>
    public double End { get; }

    public override double Transform(double t)
    {
        if (t <= Begin)
        {
            return 0;
        }

        if (t >= End)
        {
            return 1;
        }

        return _inner.Transform((t - Begin) / (End - Begin));
    }
}

internal class LinearCurve : Curve
{
    public override double Transform(double t) => t;
}

internal class EaseInCurve : Curve
{
    public override double Transform(double t) => t * t;
}

internal class EaseOutCurve : Curve
{
    public override double Transform(double t) => t * (2 - t);
}

internal class SmoothStepCurve : Curve
{
    public override double Transform(double t) => t * t * (3 - 2 * t);
}

internal class EaseInCubicCurve : Curve
{
    public override double Transform(double t) => t * t * t;
}

internal class EaseOutCubicCurve : Curve
{
    public override double Transform(double t)
    {
        var u = 1 - t;
        return 1 - u * u * u;
    }
}

internal class EaseInOutCubicCurve : Curve
{
    public override double Transform(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
}

internal class EaseInQuartCurve : Curve
{
    public override double Transform(double t) => t * t * t * t;
}

internal class EaseOutQuartCurve : Curve
{
    public override double Transform(double t)
    {
        var u = 1 - t;
        return 1 - u * u * u * u;
    }
}

internal class EaseInOutQuartCurve : Curve
{
    public override double Transform(double t) =>
        t < 0.5 ? 8 * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 4) / 2;
}

internal class EaseInQuintCurve : Curve
{
    public override double Transform(double t) => t * t * t * t * t;
}

internal class EaseOutQuintCurve : Curve
{
    public override double Transform(double t)
    {
        var u = 1 - t;
        return 1 - u * u * u * u * u;
    }
}

internal class EaseInOutQuintCurve : Curve
{
    public override double Transform(double t) =>
        t < 0.5 ? 16 * t * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 5) / 2;
}

internal class EaseInExpoCurve : Curve
{
    public override double Transform(double t) => t == 0 ? 0 : Math.Pow(2, 10 * t - 10);
}

internal class EaseOutExpoCurve : Curve
{
    public override double Transform(double t) => t == 1 ? 1 : 1 - Math.Pow(2, -10 * t);
}

internal class EaseInOutExpoCurve : Curve
{
    public override double Transform(double t)
    {
        if (t == 0)
        {
            return 0;
        }

        if (t == 1)
        {
            return 1;
        }

        return t < 0.5
            ? Math.Pow(2, 20 * t - 10) / 2
            : (2 - Math.Pow(2, -20 * t + 10)) / 2;
    }
}

internal class EaseInBackCurve : Curve
{
    private const double C1 = 1.70158;
    private const double C3 = C1 + 1;

    public override double Transform(double t) => C3 * t * t * t - C1 * t * t;
}

internal class EaseOutBackCurve : Curve
{
    private const double C1 = 1.70158;
    private const double C3 = C1 + 1;

    public override double Transform(double t)
    {
        var u = t - 1;
        return 1 + C3 * u * u * u + C1 * u * u;
    }
}

internal class EaseInOutBackCurve : Curve
{
    private const double C1 = 1.70158;
    private const double C2 = C1 * 1.525;

    public override double Transform(double t)
    {
        if (t < 0.5)
        {
            var u = 2 * t;
            return u * u * ((C2 + 1) * u - C2) / 2;
        }
        else
        {
            var u = 2 * t - 2;
            return (u * u * ((C2 + 1) * u + C2) + 2) / 2;
        }
    }
}

internal class EaseInElasticCurve : Curve
{
    private const double C4 = 2 * Math.PI / 3;

    public override double Transform(double t)
    {
        if (t == 0)
        {
            return 0;
        }

        if (t == 1)
        {
            return 1;
        }

        return -Math.Pow(2, 10 * t - 10) * Math.Sin((10 * t - 10.75) * C4);
    }
}

internal class EaseOutElasticCurve : Curve
{
    private const double C4 = 2 * Math.PI / 3;

    public override double Transform(double t)
    {
        if (t == 0)
        {
            return 0;
        }

        if (t == 1)
        {
            return 1;
        }

        return Math.Pow(2, -10 * t) * Math.Sin((10 * t - 0.75) * C4) + 1;
    }
}

internal class EaseInOutElasticCurve : Curve
{
    private const double C5 = 2 * Math.PI / 4.5;

    public override double Transform(double t)
    {
        if (t == 0)
        {
            return 0;
        }

        if (t == 1)
        {
            return 1;
        }

        return t < 0.5
            ? -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * C5)) / 2
            : Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * C5) / 2 + 1;
    }
}

internal static class BounceOutHelper
{
    internal static double Evaluate(double t)
    {
        const double n1 = 7.5625;
        const double d1 = 2.75;
        if (t < 1 / d1)
        {
            return n1 * t * t;
        }

        if (t < 2 / d1)
        {
            t -= 1.5 / d1;
            return n1 * t * t + 0.75;
        }

        if (t < 2.5 / d1)
        {
            t -= 2.25 / d1;
            return n1 * t * t + 0.9375;
        }

        t -= 2.625 / d1;
        return n1 * t * t + 0.984375;
    }
}

internal class BounceOutCurve : Curve
{
    public override double Transform(double t) => BounceOutHelper.Evaluate(t);
}

internal class BounceInCurve : Curve
{
    public override double Transform(double t) => 1 - BounceOutHelper.Evaluate(1 - t);
}

internal class BounceInOutCurve : Curve
{
    public override double Transform(double t)
    {
        return t < 0.5
            ? (1 - BounceOutHelper.Evaluate(1 - 2 * t)) / 2
            : (1 + BounceOutHelper.Evaluate(2 * t - 1)) / 2;
    }
}

/// <summary>Named easing curve constants.</summary>
public static class Curves
{
    /// <summary>Constant speed — no easing.</summary>
    public static readonly Curve Linear = new LinearCurve();

    /// <summary>Slow start, fast end (t²).</summary>
    public static readonly Curve EaseIn = new EaseInCurve();

    /// <summary>Fast start, slow end.</summary>
    public static readonly Curve EaseOut = new EaseOutCurve();

    /// <summary>Slow start and end — smooth step (t²(3-2t)).</summary>
    public static readonly Curve EaseInOut = new SmoothStepCurve();

    /// <summary>Slow start, fast end (t³).</summary>
    public static readonly Curve EaseInCubic = new EaseInCubicCurve();

    /// <summary>Fast start, slow end (cubic).</summary>
    public static readonly Curve EaseOutCubic = new EaseOutCubicCurve();

    /// <summary>Slow start and end (cubic).</summary>
    public static readonly Curve EaseInOutCubic = new EaseInOutCubicCurve();

    /// <summary>Slow start, fast end (t⁴).</summary>
    public static readonly Curve EaseInQuart = new EaseInQuartCurve();

    /// <summary>Fast start, slow end (quartic).</summary>
    public static readonly Curve EaseOutQuart = new EaseOutQuartCurve();

    /// <summary>Slow start and end (quartic).</summary>
    public static readonly Curve EaseInOutQuart = new EaseInOutQuartCurve();

    /// <summary>Slow start, fast end (t⁵).</summary>
    public static readonly Curve EaseInQuint = new EaseInQuintCurve();

    /// <summary>Fast start, slow end (quintic).</summary>
    public static readonly Curve EaseOutQuint = new EaseOutQuintCurve();

    /// <summary>Slow start and end (quintic).</summary>
    public static readonly Curve EaseInOutQuint = new EaseInOutQuintCurve();

    /// <summary>Extremely slow start, instant finish (2^(10t-10)).</summary>
    public static readonly Curve EaseInExpo = new EaseInExpoCurve();

    /// <summary>Instant start, extremely slow finish.</summary>
    public static readonly Curve EaseOutExpo = new EaseOutExpoCurve();

    /// <summary>Extremely slow start and finish (exponential).</summary>
    public static readonly Curve EaseInOutExpo = new EaseInOutExpoCurve();

    /// <summary>Overshoots slightly before settling (spring-like start).</summary>
    public static readonly Curve EaseInBack = new EaseInBackCurve();

    /// <summary>Overshoots target then snaps back.</summary>
    public static readonly Curve EaseOutBack = new EaseOutBackCurve();

    /// <summary>Overshoots on both ends.</summary>
    public static readonly Curve EaseInOutBack = new EaseInOutBackCurve();

    /// <summary>Elastic oscillation at the start.</summary>
    public static readonly Curve EaseInElastic = new EaseInElasticCurve();

    /// <summary>Elastic oscillation at the end.</summary>
    public static readonly Curve EaseOutElastic = new EaseOutElasticCurve();

    /// <summary>Elastic oscillation on both ends.</summary>
    public static readonly Curve EaseInOutElastic = new EaseInOutElasticCurve();

    /// <summary>Bounces at the end like a ball hitting the floor.</summary>
    public static readonly Curve BounceOut = new BounceOutCurve();

    /// <summary>Bounces at the start.</summary>
    public static readonly Curve BounceIn = new BounceInCurve();

    /// <summary>Bounces on both ends.</summary>
    public static readonly Curve BounceInOut = new BounceInOutCurve();
}

/// <summary>Wraps an <see cref="AnimationController" /> and applies an easing curve to its value.</summary>
public interface IAnimation
{
    /// <summary>Current animation value in [0,1] (may exceed range for Back/Elastic curves).</summary>
    double Value { get; }

    /// <summary>Current animation status.</summary>
    AnimationStatus Status { get; }
}

/// <summary>Applies a <see cref="Curve" /> to an <see cref="AnimationController" />'s raw value.</summary>
public class CurvedAnimation : IAnimation
{
    private readonly Curve _curve;
    private readonly AnimationController _parent;

    /// <summary>Creates a curved animation wrapping <paramref name="parent" />.</summary>
    public CurvedAnimation(
        AnimationController parent,
        Curve curve
    )
    {
        _parent = parent;
        _curve = curve;
    }

    /// <inheritdoc />
    public double Value => _curve.Transform(_parent.Value);

    /// <inheritdoc />
    public AnimationStatus Status => _parent.Status;
}

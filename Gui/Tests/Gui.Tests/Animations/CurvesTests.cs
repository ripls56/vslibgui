using Gui.Widgets.Animations;

namespace Gui.Tests.Animations;

[TestFixture]
public class CurvesTests
{
    private static void AssertBoundaries(Curve curve)
    {
        Assert.That(curve.Transform(0), Is.EqualTo(0).Within(1e-9), "t=0 must be 0");
        Assert.That(curve.Transform(1), Is.EqualTo(1).Within(1e-9), "t=1 must be 1");
    }

    private static void AssertMidpoint(Curve curve, double expected, double tolerance = 0.01) =>
        Assert.That(curve.Transform(0.5), Is.EqualTo(expected).Within(tolerance));


    [Test]
    public void Linear_Boundaries() => AssertBoundaries(Curves.Linear);

    [Test]
    public void Linear_Midpoint() => AssertMidpoint(Curves.Linear, 0.5);

    [Test]
    public void EaseIn_Boundaries() => AssertBoundaries(Curves.EaseIn);

    [Test]
    public void EaseIn_Midpoint() => AssertMidpoint(Curves.EaseIn, 0.25);

    [Test]
    public void EaseOut_Boundaries() => AssertBoundaries(Curves.EaseOut);

    [Test]
    public void EaseOut_Midpoint() => AssertMidpoint(Curves.EaseOut, 0.75);

    [Test]
    public void EaseInOut_Boundaries() => AssertBoundaries(Curves.EaseInOut);

    [Test]
    public void EaseInOut_Midpoint() => AssertMidpoint(Curves.EaseInOut, 0.5);


    [Test]
    public void EaseInCubic_Boundaries() => AssertBoundaries(Curves.EaseInCubic);

    [Test]
    public void EaseInCubic_Midpoint() => AssertMidpoint(Curves.EaseInCubic, 0.125);

    [Test]
    public void EaseOutCubic_Boundaries() => AssertBoundaries(Curves.EaseOutCubic);

    [Test]
    public void EaseOutCubic_Midpoint() => AssertMidpoint(Curves.EaseOutCubic, 0.875);

    [Test]
    public void EaseInOutCubic_Boundaries() => AssertBoundaries(Curves.EaseInOutCubic);

    [Test]
    public void EaseInOutCubic_Midpoint() => AssertMidpoint(Curves.EaseInOutCubic, 0.5);


    [Test]
    public void EaseInQuart_Boundaries() => AssertBoundaries(Curves.EaseInQuart);

    [Test]
    public void EaseInQuart_Midpoint() => AssertMidpoint(Curves.EaseInQuart, 0.0625);

    [Test]
    public void EaseOutQuart_Boundaries() => AssertBoundaries(Curves.EaseOutQuart);

    [Test]
    public void EaseOutQuart_Midpoint() => AssertMidpoint(Curves.EaseOutQuart, 0.9375);

    [Test]
    public void EaseInOutQuart_Boundaries() => AssertBoundaries(Curves.EaseInOutQuart);

    [Test]
    public void EaseInOutQuart_Midpoint() => AssertMidpoint(Curves.EaseInOutQuart, 0.5);


    [Test]
    public void EaseInQuint_Boundaries() => AssertBoundaries(Curves.EaseInQuint);

    [Test]
    public void EaseInQuint_Midpoint() => AssertMidpoint(Curves.EaseInQuint, 0.03125);

    [Test]
    public void EaseOutQuint_Boundaries() => AssertBoundaries(Curves.EaseOutQuint);

    [Test]
    public void EaseOutQuint_Midpoint() => AssertMidpoint(Curves.EaseOutQuint, 0.96875);

    [Test]
    public void EaseInOutQuint_Boundaries() => AssertBoundaries(Curves.EaseInOutQuint);

    [Test]
    public void EaseInOutQuint_Midpoint() => AssertMidpoint(Curves.EaseInOutQuint, 0.5);


    [Test]
    public void EaseInExpo_Boundaries() => AssertBoundaries(Curves.EaseInExpo);

    [Test]
    public void EaseInExpo_Midpoint() => AssertMidpoint(Curves.EaseInExpo, 0.03125); // 2^(-5)

    [Test]
    public void EaseOutExpo_Boundaries() => AssertBoundaries(Curves.EaseOutExpo);

    [Test]
    public void EaseOutExpo_Midpoint() => AssertMidpoint(Curves.EaseOutExpo, 0.96875); // 1-2^(-5)

    [Test]
    public void EaseInOutExpo_Boundaries() => AssertBoundaries(Curves.EaseInOutExpo);

    [Test]
    public void EaseInOutExpo_Midpoint() => AssertMidpoint(Curves.EaseInOutExpo, 0.5);


    [Test]
    public void EaseInBack_Boundaries() => AssertBoundaries(Curves.EaseInBack);

    [Test]
    public void EaseInBack_Overshoots_Below_Zero()
    {
        // Back curve dips below 0 in the middle
        var mid = Curves.EaseInBack.Transform(0.5);
        Assert.That(mid, Is.LessThan(0));
    }

    [Test]
    public void EaseOutBack_Boundaries() => AssertBoundaries(Curves.EaseOutBack);

    [Test]
    public void EaseOutBack_Overshoots_Above_One()
    {
        var mid = Curves.EaseOutBack.Transform(0.5);
        Assert.That(mid, Is.GreaterThan(1));
    }

    [Test]
    public void EaseInOutBack_Boundaries() => AssertBoundaries(Curves.EaseInOutBack);

    [Test]
    public void EaseInOutBack_Midpoint() => AssertMidpoint(Curves.EaseInOutBack, 0.5);


    [Test]
    public void EaseInElastic_Boundaries() => AssertBoundaries(Curves.EaseInElastic);

    [Test]
    public void EaseOutElastic_Boundaries() => AssertBoundaries(Curves.EaseOutElastic);

    [Test]
    public void EaseInOutElastic_Boundaries() => AssertBoundaries(Curves.EaseInOutElastic);

    [Test]
    public void EaseInOutElastic_Midpoint() => AssertMidpoint(Curves.EaseInOutElastic, 0.5);


    [Test]
    public void BounceOut_Boundaries() => AssertBoundaries(Curves.BounceOut);

    [Test]
    public void BounceIn_Boundaries() => AssertBoundaries(Curves.BounceIn);

    [Test]
    public void BounceInOut_Boundaries() => AssertBoundaries(Curves.BounceInOut);

    [Test]
    public void BounceInOut_Midpoint() => AssertMidpoint(Curves.BounceInOut, 0.5);

    [Test]
    public void BounceOut_IsMonotonicallyIncreasing_AtKeyPoints()
    {
        double[] points = [0, 0.25, 0.5, 0.75, 1.0];
        for (var i = 1; i < points.Length; i++)
        {
            Assert.That(
                Curves.BounceOut.Transform(points[i]),
                Is.GreaterThanOrEqualTo(Curves.BounceOut.Transform(points[i - 1]) - 0.001)
            );
        }
    }
}

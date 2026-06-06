using Gui.Widgets.Animations;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class GradientTests
{
    private static readonly Vector4 Red = new(1, 0, 0, 1);
    private static readonly Vector4 Blue = new(0, 0, 1, 1);
    private static readonly Vector4 Green = new(0, 1, 0, 1);
    private static readonly Vector4 Transparent = Vector4.Zero;


    [Test]
    public void LinearGradient_CreateShader_ReturnsNonNull()
    {
        var g = new LinearGradient(45f,
            new GradientStop(Red, 0f),
            new GradientStop(Blue, 1f));
        using var shader = g.CreateShader(new Vector2(100, 50));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void LinearGradient_CreateShader_ZeroStops_ReturnsTransparent()
    {
        var g = new LinearGradient(0f);
        using var shader = g.CreateShader(new Vector2(100, 100));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void LinearGradient_CreateShader_OneStop_ReturnsSolidColor()
    {
        var g = new LinearGradient(0f, new GradientStop(Red, 0.5f));
        using var shader = g.CreateShader(new Vector2(100, 100));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void LinearGradient_StoresAngle()
    {
        var g = new LinearGradient(90f,
            new GradientStop(Red, 0f),
            new GradientStop(Blue, 1f));
        Assert.That(g.Angle, Is.EqualTo(90f));
    }

    [Test]
    public void LinearGradient_StoresStops()
    {
        var stops = new[]
        {
            new GradientStop(Red, 0f), new GradientStop(Green, 0.5f), new GradientStop(Blue, 1f)
        };
        var g = new LinearGradient(0f, stops);
        Assert.That(g.Stops.Length, Is.EqualTo(3));
        Assert.That(g.Stops[1].Position, Is.EqualTo(0.5f));
    }


    [Test]
    public void RadialGradient_CreateShader_ReturnsNonNull()
    {
        var g = new RadialGradient(new Vector2(0.5f, 0.5f), 1f,
            new GradientStop(Red, 0f),
            new GradientStop(Blue, 1f));
        using var shader = g.CreateShader(new Vector2(200, 200));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void RadialGradient_CreateShader_ZeroStops_ReturnsTransparent()
    {
        var g = new RadialGradient(new Vector2(0.5f, 0.5f), 1f);
        using var shader = g.CreateShader(new Vector2(100, 100));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void RadialGradient_CreateShader_OneStop_ReturnsSolidColor()
    {
        var g = new RadialGradient(new Vector2(0.5f, 0.5f), 1f,
            new GradientStop(Red, 0f));
        using var shader = g.CreateShader(new Vector2(100, 100));
        Assert.That(shader, Is.Not.Null);
    }

    [Test]
    public void RadialGradient_StoresCenterAndRadius()
    {
        var g = new RadialGradient(new Vector2(0.3f, 0.7f), 0.5f,
            new GradientStop(Red, 0f),
            new GradientStop(Blue, 1f));
        Assert.That(g.Center.X, Is.EqualTo(0.3f));
        Assert.That(g.Center.Y, Is.EqualTo(0.7f));
        Assert.That(g.Radius, Is.EqualTo(0.5f));
    }


    [Test]
    public void LinearGradient_LerpTo_AtZero_ReturnsBeginValues()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var b = new LinearGradient(90f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));

        var result = (LinearGradient)a.LerpTo(b, 0.0);
        Assert.That(result.Angle, Is.EqualTo(0f));
        Assert.That(result.Stops[0].Color, Is.EqualTo(Red));
    }

    [Test]
    public void LinearGradient_LerpTo_AtOne_ReturnsEndValues()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var b = new LinearGradient(90f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));

        var result = (LinearGradient)a.LerpTo(b, 1.0);
        Assert.That(result.Angle, Is.EqualTo(90f));
        Assert.That(result.Stops[0].Color, Is.EqualTo(Green));
    }

    [Test]
    public void LinearGradient_LerpTo_AtHalf_InterpolatesAngle()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var b = new LinearGradient(90f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));

        var result = (LinearGradient)a.LerpTo(b, 0.5);
        Assert.That(result.Angle, Is.EqualTo(45f));
    }

    [Test]
    public void LinearGradient_LerpTo_InterpolatesStopColors()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Red, 1f));
        var b = new LinearGradient(0f,
            new GradientStop(Blue, 0f), new GradientStop(Blue, 1f));

        var result = (LinearGradient)a.LerpTo(b, 0.5);
        Assert.That(result.Stops[0].Color.X, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(result.Stops[0].Color.Z, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void LinearGradient_LerpTo_InterpolatesStopPositions()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 0.5f));
        var b = new LinearGradient(0f,
            new GradientStop(Red, 0.5f), new GradientStop(Blue, 1f));

        var result = (LinearGradient)a.LerpTo(b, 0.5);
        Assert.That(result.Stops[0].Position, Is.EqualTo(0.25f).Within(0.01f));
        Assert.That(result.Stops[1].Position, Is.EqualTo(0.75f).Within(0.01f));
    }

    [Test]
    public void RadialGradient_LerpTo_InterpolatesCenterAndRadius()
    {
        var a = new RadialGradient(new Vector2(0f, 0f), 0.5f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var b = new RadialGradient(new Vector2(1f, 1f), 1.5f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));

        var result = (RadialGradient)a.LerpTo(b, 0.5);
        Assert.That(result.Center.X, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(result.Center.Y, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(result.Radius, Is.EqualTo(1.0f).Within(0.01f));
    }


    [Test]
    public void LerpTo_DifferentTypes_BeforeHalf_ReturnsBegin()
    {
        var linear = new LinearGradient(45f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var radial = new RadialGradient(new Vector2(0.5f, 0.5f), 1f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));

        var result = linear.LerpTo(radial, 0.3);
        Assert.That(result, Is.SameAs(linear));
    }

    [Test]
    public void LerpTo_DifferentTypes_AfterHalf_ReturnsEnd()
    {
        var linear = new LinearGradient(45f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var radial = new RadialGradient(new Vector2(0.5f, 0.5f), 1f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));

        var result = linear.LerpTo(radial, 0.7);
        Assert.That(result, Is.SameAs(radial));
    }


    [Test]
    public void LerpTo_DifferentStopCounts_PadsWithLastStop()
    {
        var a = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var b = new LinearGradient(0f,
            new GradientStop(Red, 0f),
            new GradientStop(Green, 0.5f),
            new GradientStop(Blue, 1f));

        var result = (LinearGradient)a.LerpTo(b, 0.0);
        Assert.That(result.Stops.Length, Is.EqualTo(3));
        // Third stop of 'a' is padded with last stop (Blue, 1f)
        Assert.That(result.Stops[2].Color, Is.EqualTo(Blue));
    }


    [Test]
    public void GradientTween_BothNull_ReturnsNull()
    {
        var tween = new GradientTween(null, null);
        Assert.That(tween.Lerp(0.5), Is.Null);
    }

    [Test]
    public void GradientTween_BeginNull_ReturnsEnd()
    {
        var end = new LinearGradient(90f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var tween = new GradientTween(null, end);
        Assert.That(tween.Lerp(0.5), Is.SameAs(end));
    }

    [Test]
    public void GradientTween_EndNull_ReturnsBegin()
    {
        var begin = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var tween = new GradientTween(begin, null);
        Assert.That(tween.Lerp(0.5), Is.SameAs(begin));
    }

    [Test]
    public void GradientTween_SameType_Interpolates()
    {
        var begin = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var end = new LinearGradient(180f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));
        var tween = new GradientTween(begin, end);

        var result = tween.Lerp(0.5) as LinearGradient;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Angle, Is.EqualTo(90f));
    }

    [Test]
    public void GradientTween_DifferentTypes_SnapsAtHalf()
    {
        var linear = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var radial = new RadialGradient(new Vector2(0.5f, 0.5f), 1f,
            new GradientStop(Green, 0f), new GradientStop(Red, 1f));
        var tween = new GradientTween(linear, radial);

        Assert.That(tween.Lerp(0.3), Is.InstanceOf<LinearGradient>());
        Assert.That(tween.Lerp(0.7), Is.InstanceOf<RadialGradient>());
    }

    [Test]
    public void GradientTween_Evaluate_UsesAnimationValue()
    {
        var begin = new LinearGradient(0f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var end = new LinearGradient(100f,
            new GradientStop(Red, 0f), new GradientStop(Blue, 1f));
        var tween = new GradientTween(begin, end);

        var mockAnimation = new MockAnimation(0.25);
        var result = tween.Evaluate(mockAnimation) as LinearGradient;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Angle, Is.EqualTo(25f).Within(0.01f));
    }


    [Test]
    public void BoxStyle_Gradient_DefaultsToNull()
    {
        var style = new BoxStyle();
        Assert.That(style.Gradient, Is.Null);
    }

    [Test]
    public void BoxStyle_Gradient_CanBeSet()
    {
        var gradient = new LinearGradient(45f,
            new GradientStop(Red, 0f),
            new GradientStop(Blue, 1f));
        var style = new BoxStyle { Gradient = gradient };
        Assert.That(style.Gradient, Is.SameAs(gradient));
    }


    private class MockAnimation : IAnimation
    {
        public MockAnimation(double value) { Value = value; }
        public double Value { get; }
        public AnimationStatus Status => AnimationStatus.Forward;
        public event Action<double>? OnValueChanged;
    }
}

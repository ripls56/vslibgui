using Gui.Rendering;
using Gui.Widgets.Animations;
using OpenTK.Mathematics;

namespace Gui.Tests.Animations;

[TestFixture]
public class TweenTests
{
    [Test]
    public void EdgeInsetsTween_InterpolatesAllSides()
    {
        var tween = new EdgeInsetsTween(
            new EdgeInsets(0, 0, 0, 0),
            new EdgeInsets(10, 20, 30, 40));
        var mid = tween.Lerp(0.5);
        Assert.That(mid.Left, Is.EqualTo(5f).Within(0.01f));
        Assert.That(mid.Top, Is.EqualTo(10f).Within(0.01f));
        Assert.That(mid.Right, Is.EqualTo(15f).Within(0.01f));
        Assert.That(mid.Bottom, Is.EqualTo(20f).Within(0.01f));
    }

    [Test]
    public void EdgeInsetsTween_AtZero_ReturnsBegin()
    {
        var begin = new EdgeInsets(1, 2, 3, 4);
        var end = new EdgeInsets(10, 20, 30, 40);
        var tween = new EdgeInsetsTween(begin, end);
        var result = tween.Lerp(0.0);
        Assert.That(result, Is.EqualTo(begin));
    }

    [Test]
    public void EdgeInsetsTween_AtOne_ReturnsEnd()
    {
        var begin = new EdgeInsets(1, 2, 3, 4);
        var end = new EdgeInsets(10, 20, 30, 40);
        var tween = new EdgeInsetsTween(begin, end);
        var result = tween.Lerp(1.0);
        Assert.That(result, Is.EqualTo(end));
    }

    [Test]
    public void SizeTween_InterpolatesWidthAndHeight()
    {
        var tween = new SizeTween(new Vector2(100, 50), new Vector2(200, 150));
        var mid = tween.Lerp(0.5);
        Assert.That(mid.X, Is.EqualTo(150f).Within(0.01f));
        Assert.That(mid.Y, Is.EqualTo(100f).Within(0.01f));
    }

    [Test]
    public void SizeTween_AtZero_ReturnsBegin()
    {
        var begin = new Vector2(10, 20);
        var end = new Vector2(100, 200);
        var tween = new SizeTween(begin, end);
        Assert.That(tween.Lerp(0.0), Is.EqualTo(begin));
    }

    [Test]
    public void SizeTween_AtOne_ReturnsEnd()
    {
        var begin = new Vector2(10, 20);
        var end = new Vector2(100, 200);
        var tween = new SizeTween(begin, end);
        Assert.That(tween.Lerp(1.0), Is.EqualTo(end));
    }
}

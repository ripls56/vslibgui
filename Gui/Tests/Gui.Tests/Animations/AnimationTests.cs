using Gui.Widgets.Animations;
using OpenTK.Mathematics;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimationTests
{
    private class MockTickerProvider : ITickerProvider
    {
        public readonly List<Ticker> Tickers = [];

        public Ticker CreateTicker(Action<TimeSpan> onTick)
        {
            var t = new Ticker(onTick);
            Tickers.Add(t);
            return t;
        }

        public void Advance(TimeSpan elapsed)
        {
            foreach (var t in Tickers)
            {
                t.Tick(elapsed);
            }
        }
    }

    [Test]
    public void AnimationController_ShouldProgressValue()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        double lastValue = -1;
        controller.OnValueChanged += v => lastValue = v;

        controller.Forward();
        Assert.That(controller.Value, Is.EqualTo(0));

        vsync.Advance(TimeSpan.FromMilliseconds(0));
        vsync.Advance(TimeSpan.FromMilliseconds(50));
        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
        Assert.That(lastValue, Is.EqualTo(controller.Value));

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        Assert.That(controller.Value, Is.EqualTo(1.0));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Completed));
    }

    [Test]
    public void Resume_ShouldContinueForwardFromCurrentPosition()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(50)); // 0.5
        controller.Stop();

        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Forward));
        Assert.That(controller.IsAnimating, Is.False);

        controller.Resume();
        Assert.That(controller.IsAnimating, Is.True);
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Forward));

        vsync.Advance(TimeSpan.FromMilliseconds(50));
        Assert.That(controller.Value, Is.EqualTo(1.0));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Completed));
    }

    [Test]
    public void Resume_ShouldContinueReverseFromCurrentPosition()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Completed));

        controller.Reverse();
        vsync.Advance(TimeSpan.FromMilliseconds(50)); // 0.5
        controller.Stop();

        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Reverse));
        Assert.That(controller.IsAnimating, Is.False);

        controller.Resume();
        Assert.That(controller.IsAnimating, Is.True);
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Reverse));

        vsync.Advance(TimeSpan.FromMilliseconds(50));
        Assert.That(controller.Value, Is.EqualTo(0.0));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Dismissed));
    }

    [Test]
    public void Resume_ShouldDoNothingWhenCompleted()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Completed));

        controller.Resume();
        Assert.That(controller.IsAnimating, Is.False);
        Assert.That(controller.Value, Is.EqualTo(1.0));
    }

    [Test]
    public void Resume_ShouldDoNothingWhenDismissed()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        controller.Resume();
        Assert.That(controller.IsAnimating, Is.False);
        Assert.That(controller.Value, Is.EqualTo(0.0));
    }

    [Test]
    public void Resume_ShouldDoNothingWhenAlreadyAnimating()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(10));

        Assert.That(controller.IsAnimating, Is.True);
        var valueBefore = controller.Value;

        controller.Resume(); // no-op
        Assert.That(controller.Status, Is.EqualTo(AnimationStatus.Forward));
        Assert.That(controller.Value, Is.EqualTo(valueBefore));
    }

    [Test]
    public void ColorTween_ShouldInterpolateRGBA()
    {
        var start = new Vector4(1, 0, 0, 1); // Red
        var end = new Vector4(0, 0, 1, 0); // Transparent Blue
        var tween = new ColorTween(start, end);

        var middle = tween.Lerp(0.5);
        Assert.That(middle.X, Is.EqualTo(0.5f));
        Assert.That(middle.Y, Is.EqualTo(0.0f));
        Assert.That(middle.Z, Is.EqualTo(0.5f));
        Assert.That(middle.W, Is.EqualTo(0.5f));
    }

    [Test]
    public void CurvedAnimation_ShouldTransformTime()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(TimeSpan.FromMilliseconds(100), vsync);
        var curved = new CurvedAnimation(controller, Curves.EaseIn); // t^2

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(0));
        vsync.Advance(TimeSpan.FromMilliseconds(50)); // 50% linear

        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
        // EaseIn (t^2) -> 0.5 * 0.5 = 0.25
        Assert.That(curved.Value, Is.InRange(0.24, 0.26));
    }
}

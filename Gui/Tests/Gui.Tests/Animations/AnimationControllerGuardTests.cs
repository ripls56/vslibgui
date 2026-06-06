using Gui.Widgets.Animations;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimationControllerGuardTests
{
    [Test]
    public void OnValueChanged_NotFired_WhenValueUnchanged()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(
            TimeSpan.FromMilliseconds(100), vsync);

        var fireCount = 0;
        controller.OnValueChanged += _ => fireCount++;

        controller.Forward();
        // Advance by 0ms — value stays at 0.0, should not fire.
        vsync.Advance(TimeSpan.FromMilliseconds(0));

        Assert.That(fireCount, Is.EqualTo(0));
    }

    [Test]
    public void OnValueChanged_Fires_WhenValueChanges()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(
            TimeSpan.FromMilliseconds(100), vsync);

        var fireCount = 0;
        controller.OnValueChanged += _ => fireCount++;

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(50));

        Assert.That(fireCount, Is.EqualTo(1));
        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
    }

    [Test]
    public void Duration_CanBeUpdatedAtRuntime()
    {
        var vsync = new MockTickerProvider();
        var controller = new AnimationController(
            TimeSpan.FromMilliseconds(100), vsync);

        // Change duration to 200ms at runtime.
        controller.Duration = TimeSpan.FromMilliseconds(200);

        controller.Forward();
        vsync.Advance(TimeSpan.FromMilliseconds(100));

        // 100ms / 200ms = 0.5
        Assert.That(controller.Value, Is.InRange(0.49, 0.51));
    }
}

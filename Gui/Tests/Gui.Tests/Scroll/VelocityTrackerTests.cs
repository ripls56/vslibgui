using Gui.Widgets.Gestures;

namespace Gui.Tests.Scroll;

[TestFixture]
public class VelocityTrackerTests
{
    [Test]
    public void Should_CalculateZeroVelocity_When_NoEvents()
    {
        var tracker = new VelocityTracker();
        Assert.That(tracker.GetVelocity(), Is.EqualTo(0));
    }

    [Test]
    public void Should_CalculateVelocity_When_EventsAdded()
    {
        var tracker = new VelocityTracker();

        tracker.AddPosition(0, 0);
        Thread.Sleep(50);
        tracker.AddPosition(0, 100);

        var velocity = tracker.GetVelocity();

        // 100px in ~50ms -> ~2000 px/s
        Assert.That(velocity, Is.GreaterThan(1000));
    }

    [Test]
    public void Should_DiscardOldEvents()
    {
        var tracker = new VelocityTracker();
        tracker.AddPosition(0, 0);
        tracker.AddPosition(0, 100);

        Thread.Sleep(200); // Beyond horizon

        var velocity = tracker.GetVelocity();
        Assert.That(velocity, Is.EqualTo(0));
    }
}

using Gui.Widgets.Animations;

namespace Gui.Tests.Animations;

[TestFixture]
public class SimulationTests
{
    [Test]
    public void FrictionSimulation_Should_DecelerateOverTime()
    {
        // Start at position 0, with velocity 1000, and friction coefficient 0.1
        var sim = new FrictionSimulation(0.1f, 0, 1000);

        var pos0 = sim.X(0);
        var pos1 = sim.X(0.5f);
        var pos2 = sim.X(1.0f);

        Assert.That(pos0, Is.EqualTo(0));
        Assert.That(pos1, Is.GreaterThan(pos0));
        Assert.That(pos2, Is.GreaterThan(pos1));

        var vel0 = sim.Dx(0);
        var vel1 = sim.Dx(0.5f);
        var vel2 = sim.Dx(1.0f);

        Assert.That(vel0, Is.EqualTo(1000));
        Assert.That(vel1, Is.LessThan(vel0));
        Assert.That(vel2, Is.LessThan(vel1));
    }

    [Test]
    public void FrictionSimulation_Should_EventuallyStop()
    {
        var sim = new FrictionSimulation(0.5f, 0, 1000);

        // At some very far point in time, velocity should be near zero
        var velFar = sim.Dx(100.0f);
        Assert.That(velFar, Is.LessThan(1.0f));
    }
}

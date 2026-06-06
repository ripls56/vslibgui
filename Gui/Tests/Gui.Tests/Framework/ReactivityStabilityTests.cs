using Gui.Widgets.Basic;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class ReactivityStabilityTests
{
    private class TestStatefulWidget : StatefulWidget
    {
        public int Counter { get; private set; } = 0;

        public void Increment()
        {
            // We can't easily access State from outside, so we'll simulate an external trigger
            // In a real app, this would be an event handler calling SetState
        }

        public override State CreateState() => new TestState();

        public class TestState : State
        {
            public int BuildCount { get; private set; }

            public override Widget Build(BuildContext context)
            {
                BuildCount++;
                return new Container();
            }

            public void TriggerUpdate() => SetState(() => { });
        }
    }

    [Test]
    public void SetState_ShouldScheduleRebuild()
    {
        var buildOwner = new BuildOwner();
        var widget = new TestStatefulWidget();
        var element = (StatefulElement)widget.CreateElement();

        element.AssignOwner(buildOwner);
        element.Mount(null);

        // Initial build
        buildOwner.BuildDirtyElements();
        var state = (TestStatefulWidget.TestState)element.State;
        Assert.That(state.BuildCount, Is.EqualTo(1), "Initial build should happen once");

        // Trigger update
        state.TriggerUpdate();
        Assert.That(element.IsDirty, Is.True, "Element should be marked dirty after SetState");

        // Process updates
        buildOwner.BuildDirtyElements();
        Assert.That(state.BuildCount, Is.EqualTo(2), "Build should run again after SetState");
        Assert.That(element.IsDirty, Is.False, "Element should be clean after rebuild");
    }
}

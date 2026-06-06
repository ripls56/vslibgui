using Gui.Widgets.Basic;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class ReactiveTests
{
    private class TestWidget : StatelessWidget
    {
        public TestWidget(Widget child)
        {
            Child = child;
        }

        public int BuildCount { get; private set; }
        public Widget Child { get; }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            return Child;
        }
    }

    private class TestStatefulWidget : StatefulWidget
    {
        public Action<State>? OnBuild;
        public override State CreateState() => new TestState();
    }

    private class TestState : State<TestStatefulWidget>
    {
        public int BuildCount { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            Widget.OnBuild?.Invoke(this);
            return new Container();
        }

        public void TriggerSetState() => SetState(() => { });
    }

    // Simulates parent (like _SampleContentState) holding a bool value,
    // passing it to a controlled child (like Checkbox) via widget props.
    private class ControlledWidget : StatefulWidget
    {
        public ControlledWidget(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
        public override State CreateState() => new ControlledState();
    }

    private class ControlledState : State<ControlledWidget>
    {
        public bool? UpdateWidgetOldValue { get; private set; }
        public int UpdateWidgetCallCount { get; private set; }

        public override Widget Build(BuildContext context) => new Container();

        public override void UpdateWidget(ControlledWidget oldWidget)
        {
            UpdateWidgetOldValue = oldWidget.Value;
            UpdateWidgetCallCount++;
        }
    }

    private class ParentWidget : StatefulWidget
    {
        public override State CreateState() => new ParentState();
    }

    private class ParentState : State<ParentWidget>
    {
        private bool _value = true;

        public override Widget Build(BuildContext context)
            => new ControlledWidget(_value);

        public void Toggle() => SetState(() => _value = !_value);
    }

    [Test]
    public void UpdateWidget_ShouldBeCalled_WhenParentRebuildsWithNewValue()
    {
        var owner = new BuildOwner();
        var root = new ParentWidget();
        var rootElement = root.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null);

        var parentState = (ParentState)((StatefulElement)rootElement).State;
        var childElement = (StatefulElement)rootElement.VisitChildrenAndGetFirst();
        var childState = (ControlledState)childElement.State;

        Assert.That(childState.UpdateWidgetCallCount, Is.EqualTo(0),
            "UpdateWidget should not be called on initial mount");

        parentState.Toggle(); // _value: true → false
        owner.BuildDirtyElements();

        Assert.That(childState.UpdateWidgetCallCount, Is.EqualTo(1),
            "UpdateWidget should be called once after parent rebuild");
        Assert.That(childState.UpdateWidgetOldValue, Is.EqualTo(true), "Old value should be true");
        Assert.That(childState.Widget.Value, Is.EqualTo(false), "New value should be false");
    }

    [Test]
    public void SetState_Should_Only_Rebuild_Target_And_Children()
    {
        // Setup
        var owner = new BuildOwner();
        // Mock ticker? BuildOwner throws if no ticker.
        // We need a dummy TickerProvider.
        // For this test, we might not need it if we don't use animations.
        // But BuildOwner.GetTickerProvider throws.
        // Let's rely on the fact that we don't call GetTickerProvider in this simple test.

        State? childState = null;
        var childWidget = new TestStatefulWidget { OnBuild = s => childState = s };

        var parentWidget = new TestWidget(childWidget);
        var rootElement = parentWidget.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null); // Initial build

        Assert.That(parentWidget.BuildCount, Is.EqualTo(1));
        var state = (childState as TestState)!;
        Assert.That(state.BuildCount, Is.EqualTo(1));

        // Trigger child rebuild
        state.TriggerSetState();

        // Process dirty elements
        owner.BuildDirtyElements();

        // Verify
        Assert.That(parentWidget.BuildCount, Is.EqualTo(1), "Parent should NOT have rebuilt");
        Assert.That(state.BuildCount, Is.EqualTo(2), "Child SHOULD have rebuilt");
    }
}

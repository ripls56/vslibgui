using Gui.Widgets.Basic;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class RedundantBuildTests
{
    private class CountingWidget : StatefulWidget
    {
        public int BuildCount { get; set; }
        public Func<Widget>? ChildBuilder { get; set; }
        public override State CreateState() => new CountingState();
    }

    private class CountingState : State<CountingWidget>
    {
        public int StateRebuildCount { get; private set; }

        public override Widget Build(BuildContext context)
        {
            Widget.BuildCount++;
            StateRebuildCount++;
            return Widget.ChildBuilder?.Invoke() ?? new Container();
        }

        public void TriggerSetState() => SetState(() => { });
    }

    [Test]
    public void BuildOwner_ShouldNotRebuildElementTwice_IfParentAlsoRebuilds()
    {
        var owner = new BuildOwner();

        // Use a stable child widget instance so we can track its BuildCount
        // across parent rebuilds (parent returns the same child widget object).
        var stableChildWidget = new CountingWidget { ChildBuilder = () => new Container() };

        var parentWidget = new CountingWidget { ChildBuilder = () => stableChildWidget };

        var rootElement = parentWidget.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null);

        // Capture states after initial mount
        var parentState = (CountingState)((StatefulElement)rootElement).State;
        var childElement = (StatefulElement)rootElement.VisitChildrenAndGetFirst();
        var childState = (CountingState)childElement.State;

        Assert.That(parentWidget.BuildCount, Is.EqualTo(1), "Parent should build once on mount");
        Assert.That(stableChildWidget.BuildCount, Is.EqualTo(1),
            "Child should build once on mount");

        // Mark both dirty simultaneously
        parentState.TriggerSetState();
        childState.TriggerSetState();

        Assert.That(owner.HasDirtyElements, Is.True);

        // Rebuild dirty pass
        owner.BuildDirtyElements();

        // EXPECTATION: Each builds only once more (not twice for the child)
        Assert.That(parentWidget.BuildCount, Is.EqualTo(2),
            "Parent should build exactly once more");
        Assert.That(stableChildWidget.BuildCount, Is.EqualTo(2),
            "Child should build exactly once, even if parent also triggered a rebuild of it");
    }
}

public static class ElementTestExtensions
{
    public static Element VisitChildrenAndGetFirst(this Element element)
    {
        Element? first = null;
        element.VisitChildren(child =>
        {
            if (first == null)
            {
                first = child;
            }
        });
        return first!;
    }
}

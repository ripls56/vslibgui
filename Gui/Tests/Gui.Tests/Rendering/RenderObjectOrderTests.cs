using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Rendering;

/// <summary>
///     Verifies that RenderObject child ordering stays consistent
///     when a StatefulWidget's Build() returns a different widget type,
///     causing the old RenderObject to be removed and a new one appended.
/// </summary>
[TestFixture]
public class RenderObjectOrderTests
{
    private static (BuildOwner owner, Element root)
        MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        return (owner, element);
    }

    private static T? FindState<T>(Element root) where T : State
    {
        T? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el is StatefulElement se && se.State is T typed)
            {
                result = typed;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }

    [Test]
    public void StatefulChild_RebuildPreservesRenderObjectOrder()
    {
        var col = new Column(
            children:
            [
                new ToggleWidget(),
                new SizedBox(50, 20)
            ]
        );

        var (owner, root) = MountTree(col);
        var ro = root.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Initially: toggle builds SizedBox(10px tall), static is 20px
        Assert.That(ro.Children.Count, Is.EqualTo(2));
        Assert.That(ro.Children[0].Size.Y, Is.EqualTo(10),
            "Toggle child should be 10px tall initially");
        Assert.That(ro.Children[1].Size.Y, Is.EqualTo(20),
            "Static child should be 20px tall");
        Assert.That(ro.Children[0].Y, Is.EqualTo(0));
        Assert.That(ro.Children[1].Y, Is.EqualTo(10));

        // Toggle: SizedBox → Column (different widget type → RO replaced)
        var state = FindState<ToggleState>(root)!;
        state.Toggle();
        owner.BuildDirtyElements();
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // After rebuild: toggle's RO must still be child[0]
        Assert.That(ro.Children.Count, Is.EqualTo(2));
        Assert.That(ro.Children[0].Y, Is.EqualTo(0),
            "Toggle widget's RO should remain first child at Y=0");
        Assert.That(ro.Children[1].Y, Is.GreaterThan(0),
            "Static SizedBox should be positioned below toggle");
        Assert.That(ro.Children[1].Size.Y, Is.EqualTo(20),
            "Static SizedBox should retain its 20px height");
    }

    [Test]
    public void StatefulChild_MultipleRebuilds_OrderStable()
    {
        var col = new Column(
            children:
            [
                new ToggleWidget(),
                new SizedBox(50, 20)
            ]
        );

        var (owner, root) = MountTree(col);
        var ro = root.RenderObject!;
        var state = FindState<ToggleState>(root)!;

        // Toggle back and forth several times
        for (var i = 0; i < 4; i++)
        {
            state.Toggle();
            owner.BuildDirtyElements();
            ro.Layout(LayoutConstraints.Loose(500, 500));

            Assert.That(ro.Children[0].Y, Is.EqualTo(0),
                $"Iteration {i}: first child should be at Y=0");
            Assert.That(ro.Children[1].Y, Is.GreaterThan(0),
                $"Iteration {i}: second child should be below first");
            Assert.That(ro.Children[1].Size.Y, Is.EqualTo(20),
                $"Iteration {i}: static SizedBox size unchanged");
        }
    }
}

/// <summary>
///     Test helper: StatefulWidget that toggles its child between
///     a SizedBox (10px) and a Column wrapping a SizedBox (40px).
///     The type change forces RenderObject replacement.
/// </summary>
internal class ToggleWidget : StatefulWidget
{
    public override State CreateState() => new ToggleState();
}

internal class ToggleState : State<ToggleWidget>
{
    public bool ShowLarge;

    public void Toggle() => SetState(() => ShowLarge = !ShowLarge);

    public override Widget Build(BuildContext context)
    {
        if (ShowLarge)
        {
            return new Column(
                children: [new SizedBox(50, 40)]
            );
        }

        return new SizedBox(50, 10);
    }
}

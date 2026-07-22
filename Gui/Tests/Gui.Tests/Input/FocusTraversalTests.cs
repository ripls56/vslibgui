using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;

namespace Gui.Tests.Input;

[TestFixture, NonParallelizable]
public class FocusTraversalTests
{
    [SetUp]
    public void SetUp()
    {
        _owner = TestHelpers.NewBuildOwner();
        _manager = new FocusManager();
        _owner.FocusManager = _manager;
    }

    private FocusManager _manager = null!;
    private BuildOwner _owner = null!;

    [Test]
    public void FocusNext_TwoNodes_MovesForward()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2]));
        SetOwners(root);

        _manager.RequestFocus(f1.FocusNode);
        _manager.FocusNext(root);

        Assert.That(f2.FocusNode.HasFocus, Is.True);
        Assert.That(f1.FocusNode.HasFocus, Is.False);
    }

    [Test]
    public void FocusNext_AtLast_WrapsAround()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2]));
        SetOwners(root);

        _manager.RequestFocus(f2.FocusNode);
        _manager.FocusNext(root);

        Assert.That(f1.FocusNode.HasFocus, Is.True);
    }

    [Test]
    public void KeyboardTraversal_ShowsFocusIndicator()
    {
        var f1 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1]));
        SetOwners(root);

        _manager.FocusNext(root);

        Assert.That(f1.FocusNode.ShowsFocusIndicator, Is.True);
    }

    [Test]
    public void ProgrammaticFocus_HidesFocusIndicator()
    {
        var f1 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1]));
        SetOwners(root);

        _manager.RequestFocus(f1.FocusNode);

        Assert.That(f1.FocusNode.HasFocus, Is.True);
        Assert.That(f1.FocusNode.ShowsFocusIndicator, Is.False);
    }

    [Test]
    public void FocusNext_FromNoFocus_SelectsFirst()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2]));
        SetOwners(root);

        _manager.FocusNext(root);

        Assert.That(f1.FocusNode.HasFocus, Is.True);
    }

    [Test]
    public void FocusPrevious_MovesBackward()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2]));
        SetOwners(root);

        _manager.RequestFocus(f2.FocusNode);
        _manager.FocusPrevious(root);

        Assert.That(f1.FocusNode.HasFocus, Is.True);
    }

    [Test]
    public void SkipTraversal_NodeSkipped()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget
        {
            SkipTraversal = true
        };
        var f3 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2, f3]));
        SetOwners(root);

        _manager.RequestFocus(f1.FocusNode);
        _manager.FocusNext(root);

        Assert.That(f3.FocusNode.HasFocus, Is.True);
    }

    [Test]
    public void CannotRequestFocus_NodeExcluded()
    {
        var f1 = new TestFocusWidget();
        var f2 = new TestFocusWidget
        {
            CanRequestFocus = false
        };
        var f3 = new TestFocusWidget();
        var root = MountTree(new Column(children: [f1, f2, f3]));
        SetOwners(root);

        _manager.RequestFocus(f1.FocusNode);
        _manager.FocusNext(root);

        Assert.That(f3.FocusNode.HasFocus, Is.True);
    }

    [Test]
    public void FocusScope_ContainsTraversal()
    {
        var outer = new TestFocusWidget();
        var inner = new TestFocusWidget();
        var root = MountTree
        (
            new Column
            (
                children:
                [
                    outer,
                    new FocusScope
                    {
                        Child = inner
                    }
                ]
            )
        );
        SetOwners(root);

        _manager.RequestFocus(inner.FocusNode);
        _manager.FocusNext(root);

        Assert.That(inner.FocusNode.HasFocus, Is.True);
        Assert.That(outer.FocusNode.HasFocus, Is.False);
    }

    [Test]
    public void FocusNext_RespectsTraversalOrder()
    {
        // Visual tree order: A, B, C. TraversalOrder pins C between A and B.
        var a = new TestFocusWidget
        {
            TraversalOrder = 1f
        };
        var b = new TestFocusWidget
        {
            TraversalOrder = 3f
        };
        var c = new TestFocusWidget
        {
            TraversalOrder = 2f
        };
        var root = MountTree(new Column(children: [a, b, c]));
        SetOwners(root);

        _manager.RequestFocus(a.FocusNode);
        _manager.FocusNext(root);

        // Sorted order is A(1), C(2), B(3) — next after A is C, not the visual neighbour B.
        Assert.That(c.FocusNode.HasFocus, Is.True);
        Assert.That(b.FocusNode.HasFocus, Is.False);
    }

    [Test]
    public void Traversal_Includes_Button()
    {
        var root = MountTree
        (
            new Column
            (
                children:
                [
                    new Button(new Container()),
                    new Button(new Container())
                ]
            )
        );
        SetOwners(root);

        var order = _manager.TraversalPolicy.GetTraversalOrder(root, _manager);

        Assert.That(order.Count, Is.EqualTo(2));
    }

    [Test]
    public void Traversal_Includes_MixedInteractiveWidgets()
    {
        var root = MountTree
        (
            new Column
            (
                children:
                [
                    new Button(new Container()),
                    new Checkbox(false)
                ]
            )
        );
        SetOwners(root);

        var order = _manager.TraversalPolicy.GetTraversalOrder(root, _manager);

        Assert.That(order.Count, Is.EqualTo(2));
    }

    [Test]
    public void Traversal_NumericField_DedupsWithInnerTextField()
    {
        var root = MountTree(new Column(children: [new NumericField()]));
        SetOwners(root);

        var order = _manager.TraversalPolicy.GetTraversalOrder(root, _manager);

        Assert.That(order.Count, Is.EqualTo(1));
    }

    private Element MountTree(Widget widget)
    {
        var element = widget.CreateElement();
        element.AssignOwner(_owner);
        element.Mount(null);
        _owner.BuildDirtyElements();
        return element;
    }

    private static void SetOwners(Element element)
    {
        if (element.Widget is IFocusable focusable && focusable.FocusNode != null)
        {
            focusable.FocusNode.Owner = element;
        }

        element.VisitChildren(SetOwners);
    }

    private sealed class TestFocusWidget : StatelessWidget, IFocusable
    {
        public FocusNode FocusNode { get; } = new FocusNode();

        public bool SkipTraversal
        {
            set => FocusNode.SkipTraversal = value;
        }

        public bool CanRequestFocus
        {
            set => FocusNode.CanRequestFocus = value;
        }

        public float TraversalOrder
        {
            set => FocusNode.TraversalOrder = value;
        }
        FocusNode? IFocusable.FocusNode => FocusNode;

        public override Widget Build(BuildContext context) => new Container();
    }
}

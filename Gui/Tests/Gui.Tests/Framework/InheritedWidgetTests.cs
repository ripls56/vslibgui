using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class InheritedWidgetTests
{
    private class TestData : InheritedWidget
    {
        public TestData(int value, Widget child, bool shouldNotify = true)
            : base(child)
        {
            Value = value;
            ShouldNotify = shouldNotify;
        }

        public int Value { get; }
        public bool ShouldNotify { get; }

        public override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return ShouldNotify &&
                   Value != ((TestData)oldWidget).Value;
        }
    }

    private class ReaderWidget : StatefulWidget
    {
        public ReaderWidget(Widget? innerChild = null)
        {
            InnerChild = innerChild;
        }

        public Widget? InnerChild { get; }

        public override State CreateState() => new ReaderState();
    }

    private class ReaderState : State<ReaderWidget>
    {
        public int ReadValue { get; private set; } = -1;
        public int BuildCount { get; private set; }
        public int DidChangeCount { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            var data = context.DependOnInheritedWidgetOfExactType<TestData>();
            ReadValue = data?.Value ?? -1;
            return Widget.InnerChild ?? new Leaf();
        }

        public override void DidChangeDependencies() => DidChangeCount++;
    }

    private class NonDependentWidget : StatefulWidget
    {
        public override State CreateState() => new NonDependentState();
    }

    private class NonDependentState : State<NonDependentWidget>
    {
        public int BuildCount { get; private set; }

        public override Widget Build(BuildContext context)
        {
            BuildCount++;
            return new Leaf();
        }
    }

    private class Leaf : Widget
    {
        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new LeafRo();
    }

    private class LeafRo : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    private class RootWidget : StatefulWidget
    {
        public RootWidget(Widget child)
        {
            InitialChild = child;
        }

        public Widget InitialChild { get; }
        public override State CreateState() => new RootState();
    }

    private class RootState : State<RootWidget>
    {
        public Widget? NextChild { get; set; }

        public override Widget Build(BuildContext context) => NextChild ?? Widget.InitialChild;

        public void RebuildWith(Widget child) => SetState(() => { NextChild = child; });
    }

    private static (BuildOwner owner, Element root, T state)
        MountTree<T>(Widget widget) where T : State
    {
        var owner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);

        T? foundState = null;

        void FindState(Element el)
        {
            if (el is StatefulElement se && se.State is T typed)
            {
                foundState = typed;
            }

            el.VisitChildren(FindState);
        }

        FindState(element);

        return (owner, element, foundState!);
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

    private static List<T> FindAllStates<T>(Element root) where T : State
    {
        var results = new List<T>();

        void Visit(Element el)
        {
            if (el is StatefulElement se && se.State is T typed)
            {
                results.Add(typed);
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return results;
    }


    [Test]
    public void Descendant_CanRetrieveData()
    {
        var reader = new ReaderWidget();
        var tree = new TestData(42, reader);

        var (_, _, state) = MountTree<ReaderState>(tree);

        Assert.That(state.ReadValue, Is.EqualTo(42));
    }

    [Test]
    public void Descendant_RebuildsWhenDataChanges()
    {
        var reader = new ReaderWidget();
        var rootWidget = new RootWidget(new TestData(1, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ReaderState>(root)!;

        Assert.That(readerState.ReadValue, Is.EqualTo(1));
        var buildsBefore = readerState.BuildCount;

        rootState.RebuildWith(new TestData(2, reader));
        owner.BuildDirtyElements();

        Assert.That(readerState.ReadValue, Is.EqualTo(2));
        Assert.That(readerState.BuildCount, Is.GreaterThan(buildsBefore));
    }

    [Test]
    public void UpdateShouldNotify_False_SuppressesRebuild()
    {
        var reader = new ReaderWidget();
        var rootWidget = new RootWidget(
            new TestData(1, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ReaderState>(root)!;

        rootState.RebuildWith(
            new TestData(2, reader, false));
        owner.BuildDirtyElements();

        Assert.That(readerState.DidChangeCount, Is.EqualTo(0));
    }

    [Test]
    public void NonDependent_DoesNotRebuild()
    {
        var nonDep = new NonDependentWidget();
        var rootWidget = new RootWidget(new TestData(1, nonDep));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var nonDepState = FindState<NonDependentState>(root)!;
        var buildsBefore = nonDepState.BuildCount;

        rootState.RebuildWith(new TestData(2, nonDep));
        owner.BuildDirtyElements();

        // No DidChangeDependencies triggered, so at most 1 parent-driven rebuild
        Assert.That(nonDepState.BuildCount, Is.LessThanOrEqualTo(buildsBefore + 1));
    }

    [Test]
    public void MultipleDescendants_AllNotified()
    {
        // Nest reader2 inside reader1: TestData -> reader1 -> reader2
        var reader2 = new ReaderWidget();
        var reader1 = new ReaderWidget(reader2);
        var rootWidget = new RootWidget(new TestData(1, reader1));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);

        var states = FindAllStates<ReaderState>(root);
        Assert.That(states.Count, Is.EqualTo(2));
        Assert.That(states[0].ReadValue, Is.EqualTo(1));
        Assert.That(states[1].ReadValue, Is.EqualTo(1));

        rootState.RebuildWith(new TestData(99, reader1));
        owner.BuildDirtyElements();

        Assert.That(states[0].DidChangeCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(states[1].DidChangeCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Unmount_CleansDependencies()
    {
        var reader = new ReaderWidget();
        var rootWidget = new RootWidget(new TestData(1, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ReaderState>(root)!;
        var didChangeBefore = readerState.DidChangeCount;

        // Remove the reader by swapping to a plain Leaf
        rootState.RebuildWith(new Leaf());
        owner.BuildDirtyElements();

        // After unmount, DidChangeCount should not have increased
        Assert.That(readerState.DidChangeCount, Is.EqualTo(didChangeBefore));
    }

    [Test]
    public void DidChangeDependencies_CalledOnChange()
    {
        var reader = new ReaderWidget();
        var rootWidget = new RootWidget(new TestData(1, reader));

        var (owner, root, rootState) = MountTree<RootState>(rootWidget);
        var readerState = FindState<ReaderState>(root)!;

        Assert.That(readerState.DidChangeCount, Is.EqualTo(0));

        rootState.RebuildWith(new TestData(2, reader));
        owner.BuildDirtyElements();

        Assert.That(readerState.DidChangeCount, Is.GreaterThanOrEqualTo(1));
    }
}

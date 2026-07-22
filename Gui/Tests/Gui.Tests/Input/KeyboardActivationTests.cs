using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Vintagestory.API.Client;

namespace Gui.Tests.Input;

[TestFixture, NonParallelizable]
public class KeyboardActivationTests
{
    [SetUp]
    public void SetUp()
    {
        _owner = TestHelpers.NewBuildOwner();
        _manager = new FocusManager();
        _owner.FocusManager = _manager;
    }

    private BuildOwner _owner = null!;
    private FocusManager _manager = null!;

    [Test]
    public void Activate_Enter_InvokesActivatable()
    {
        var widget = new TestActivatableWidget();
        var root = MountTree(widget);
        SetOwners(root);
        _manager.RequestFocus(widget.FocusNode);

        Dispatch((int)GlKeys.Enter);

        Assert.That(widget.ActivateCount, Is.EqualTo(1));
    }

    [Test]
    public void Activate_Space_InvokesActivatable()
    {
        var widget = new TestActivatableWidget();
        var root = MountTree(widget);
        SetOwners(root);
        _manager.RequestFocus(widget.FocusNode);

        Dispatch((int)GlKeys.Space);

        Assert.That(widget.ActivateCount, Is.EqualTo(1));
    }

    [Test]
    public void Activate_NonActivatable_NotActivatedAndUnhandled()
    {
        var widget = new TestFocusOnlyWidget();
        var root = MountTree(widget);
        SetOwners(root);
        _manager.RequestFocus(widget.FocusNode);

        var e = new KeyboardEvent(KeyEventType.KeyDown, (int)GlKeys.Enter);
        new EventDispatcher().DispatchKeyDown(_manager, e);

        Assert.That(e.Handled, Is.False);
    }

    private void Dispatch(int keyCode)
    {
        new EventDispatcher().DispatchKeyDown
        (
            _manager,
            new KeyboardEvent(KeyEventType.KeyDown, keyCode)
        );
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

    private sealed class TestActivatableWidget : StatelessWidget, IFocusable, IActivatable
    {
        public int ActivateCount;
        public FocusNode FocusNode { get; } = new();

        public void Activate() => ActivateCount++;
        FocusNode? IFocusable.FocusNode => FocusNode;

        public override Widget Build(BuildContext context) => new Container();
    }

    private sealed class TestFocusOnlyWidget : StatelessWidget, IFocusable
    {
        public FocusNode FocusNode { get; } = new();
        FocusNode? IFocusable.FocusNode => FocusNode;

        public override Widget Build(BuildContext context) => new Container();
    }
}

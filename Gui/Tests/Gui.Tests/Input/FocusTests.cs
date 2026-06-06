using Gui.Widgets.Input;

namespace Gui.Tests.Input;

[TestFixture]
public class FocusTests
{
    [Test]
    public void FocusNode_ShouldNotifyListeners_WhenFocusChanges()
    {
        var node = new FocusNode();
        var notified = false;
        node.AddListener(() => notified = true);

        node.RequestFocus();

        Assert.That(node.HasFocus, Is.True);
        Assert.That(notified, Is.True);
    }

    [Test]
    public void FocusManager_ShouldOnlyAllowOneFocusedNode()
    {
        var manager = new FocusManager();
        var node1 = new FocusNode();
        var node2 = new FocusNode();

        manager.RequestFocus(node1);
        Assert.That(node1.HasFocus, Is.True);
        Assert.That(manager.PrimaryFocus, Is.EqualTo(node1));

        manager.RequestFocus(node2);
        Assert.That(node1.HasFocus, Is.False);
        Assert.That(node2.HasFocus, Is.True);
        Assert.That(manager.PrimaryFocus, Is.EqualTo(node2));
    }

    [Test]
    public void FocusNode_Unfocus_ShouldClearManagerFocus()
    {
        var manager = new FocusManager();
        var node = new FocusNode();
        manager.RequestFocus(node);

        node.Unfocus();

        Assert.That(node.HasFocus, Is.False);
        Assert.That(manager.PrimaryFocus, Is.Null);
    }
}

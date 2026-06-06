using Gui.Widgets.Framework;
using Gui.Widgets.Input;

namespace Gui.Tests.Modules;

[TestFixture]
public class ControllerTests
{
    [Test]
    public void ValueNotifier_ShouldNotifyListeners_WhenValueChanged()
    {
        var notifier = new ValueNotifier<int>(0);
        var notifiedValue = -1;
        notifier.AddListener(() => notifiedValue = notifier.Value);

        notifier.Value = 42;

        Assert.That(notifiedValue, Is.EqualTo(42));
    }

    [Test]
    public void TextEditingController_ShouldManageTextAndSelection()
    {
        var controller = new TextEditingController("initial");
        Assert.That(controller.Text, Is.EqualTo("initial"));

        var notified = false;
        controller.AddListener(() => notified = true);

        controller.Text = "new text";

        Assert.That(notified, Is.True);
        Assert.That(controller.Text, Is.EqualTo("new text"));
    }

    [Test]
    public void TextEditingController_Clear_ShouldResetText()
    {
        var controller = new TextEditingController("some text");
        controller.Clear();
        Assert.That(controller.Text, Is.Empty);
    }

    [Test]
    public void TextEditingController_AsListenable_ShouldNotify()
    {
        IListenable listenable = new TextEditingController();
        var notified = false;
        listenable.AddListener(() => notified = true);

        ((TextEditingController)listenable).Text = "trigger";

        Assert.That(notified, Is.True);
    }
}

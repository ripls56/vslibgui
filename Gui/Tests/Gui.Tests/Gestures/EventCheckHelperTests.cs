using Gui.Widgets.Events;
using Gui.Widgets.Gestures;

namespace Gui.Tests.Gestures;

[TestFixture]
public class EventCheckHelperTests
{
    private class PlainObject
    {
    }

    private class DownHandler : IPointerDownHandler
    {
        public void OnPointerDown(PointerEvent e) { }
    }

    private class ClickHandler : IPointerClickHandler
    {
        public void OnPointerClick(PointerEvent e) { }
    }

    private class SelectiveOff : IPointerClickHandler,
        ISelectiveEventHandler
    {
        public void OnPointerClick(PointerEvent e) { }
        public bool HandlesEvent(Type type) => false;
    }

    private class SelectiveOn : IPointerClickHandler,
        ISelectiveEventHandler
    {
        public void OnPointerClick(PointerEvent e) { }
        public bool HandlesEvent(Type type) => true;
    }

    [Test]
    public void HandlesAnyPointerEvent_NoInterfaces_ReturnsFalse()
    {
        Assert.That(
            EventCheckHelper.HandlesAnyPointerEvent(new PlainObject()),
            Is.False);
    }

    [Test]
    public void HandlesAnyPointerEvent_WithHandler_ReturnsTrue()
    {
        Assert.That(
            EventCheckHelper.HandlesAnyPointerEvent(new DownHandler()),
            Is.True);
    }

    [Test]
    public void HandlesAnyPointerEvent_ClickHandler_ReturnsTrue()
    {
        Assert.That(
            EventCheckHelper.HandlesAnyPointerEvent(new ClickHandler()),
            Is.True);
    }

    [Test]
    public void HandlesAnyPointerEvent_SelectiveOff_ReturnsFalse()
    {
        Assert.That(
            EventCheckHelper.HandlesAnyPointerEvent(new SelectiveOff()),
            Is.False);
    }

    [Test]
    public void HandlesAnyPointerEvent_SelectiveOn_ReturnsTrue()
    {
        Assert.That(
            EventCheckHelper.HandlesAnyPointerEvent(new SelectiveOn()),
            Is.True);
    }

    [Test]
    public void IsInteractive_PlainObject_ReturnsFalse()
    {
        Assert.That(
            EventCheckHelper.IsInteractive(new PlainObject()),
            Is.False);
    }

    [Test]
    public void IsInteractive_PointerHandler_ReturnsTrue()
    {
        Assert.That(
            EventCheckHelper.IsInteractive(new DownHandler()),
            Is.True);
    }
}

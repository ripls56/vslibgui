using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class ElementTests
{
    private class MockWidget : Widget
    {
        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new MockRenderObject();
    }

    private class MockRenderObject : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    [Test]
    public void Element_Mount_ShouldAssignRenderObject()
    {
        var widget = new MockWidget();
        var element = new RenderObjectElement(widget);

        Assert.That(element.RenderObject, Is.Null);
        element.Mount(null);
        Assert.That(element.RenderObject, Is.Not.Null);
    }

    [Test]
    public void Element_Update_ShouldSyncProperties()
    {
        var widget1 = new MockWidget();
        var widget2 = new MockWidget();
        var element = new RenderObjectElement(widget1);
        element.Mount(null);

        element.Update(widget2);
        Assert.That(element.Widget, Is.SameAs(widget2));
    }
}

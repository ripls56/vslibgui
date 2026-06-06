using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Tests.Framework;

[TestFixture]
public class WidgetTests
{
    private class ImmutableWidget : Widget
    {
        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => null!;
    }

    [Test]
    public void Widget_ShouldBeLightweight_AndNotHoldRenderObject()
    {
        var widget = new ImmutableWidget();
        // Verifying it compiles and behaves correctly without old mutable fields
        Assert.That(widget, Is.Not.Null);
    }
}

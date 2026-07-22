using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Debugging;

[TestFixture]
public class OwnerElementTests
{
    [Test]
    public void Mount_SetsOwnerElementOnRenderObject()
    {
        var owner = new BuildOwner();
        var el = new SizedBox(20, 20).CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();

        Assert.That(el.RenderObject!.OwnerElement, Is.SameAs(el));
    }
}

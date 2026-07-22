using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Debugging;

[TestFixture]
public class ProfilerLabelsTests
{
    private static (Element root, RenderObject ro) Inflate()
    {
        var owner = new BuildOwner();
        var el = new SizedBox(20, 20).CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        return (el, el.RenderObject!);
    }

    [Test]
    public void Describe_UsesOwningWidgetTypeName()
    {
        var (_, ro) = Inflate();

        Assert.That(ProfilerLabels.Describe(ro), Is.EqualTo("SizedBox"));
    }

    [Test]
    public void Describe_FallsBackToRenderTypeName_WhenNoOwner()
    {
        var (_, ro) = Inflate();
        ro.OwnerElement = null;

        Assert.That(ProfilerLabels.Describe(ro), Is.EqualTo(ro.GetType().Name));
    }

    [Test]
    public void DescribePath_ContainsOwningWidgetName()
    {
        var (_, ro) = Inflate();

        Assert.That(ProfilerLabels.DescribePath(ro), Does.Contain("SizedBox"));
    }
}

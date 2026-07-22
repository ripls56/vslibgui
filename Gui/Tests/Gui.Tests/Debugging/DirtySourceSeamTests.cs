using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Debugging;

[TestFixture]
public class DirtySourceSeamTests
{
    private static RenderObject Inflate()
    {
        var owner = new BuildOwner();
        var el = new SizedBox(10, 10).CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        return el.RenderObject!;
    }

    [TearDown]
    public void ClearAmbient() => GuiProfiler.Current = null;

    [Test]
    public void MarkNeedsPaint_RecordsInitiator_WhenCurrentSet()
    {
        var node = Inflate();
        var profiler = new GuiProfiler();
        GuiProfiler.Current = profiler;
        RenderObject.AdvanceFrame();

        node.MarkNeedsPaint();

        var top = profiler.TopDirtyInitiators(5);
        Assert.That(top, Has.Count.EqualTo(1));
        Assert.That(top[0].Node, Is.SameAs(node));
    }

    [Test]
    public void MarkNeedsPaint_NoRecord_WhenCurrentNull()
    {
        var node = Inflate();
        var profiler = new GuiProfiler();
        GuiProfiler.Current = null;
        RenderObject.AdvanceFrame();

        node.MarkNeedsPaint();

        Assert.That(profiler.TopDirtyInitiators(5), Is.Empty);
    }

    [Test]
    public void MarkNeedsLayout_CountsInitiatorOnce_NotTwiceViaMarkNeedsPaint()
    {
        var node = Inflate();
        var profiler = new GuiProfiler();
        GuiProfiler.Current = profiler;
        RenderObject.AdvanceFrame();

        node.MarkNeedsLayout();

        Assert.That(profiler.TopDirtyInitiators(5)[0].InitiatedFrames, Is.EqualTo(1));
    }
}

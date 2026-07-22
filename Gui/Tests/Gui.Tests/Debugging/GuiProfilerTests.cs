using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Debugging;

[TestFixture]
public class GuiProfilerTests
{
    [Test]
    public void RecordFrame_CountsReRecordsAndFrequency()
    {
        var profiler = new GuiProfiler();

        profiler.RecordFrame(true);
        profiler.RecordFrame(false);
        profiler.RecordFrame(true);
        profiler.RecordFrame(true);

        Assert.That(profiler.FramesSampled, Is.EqualTo(4));
        Assert.That(profiler.ReRecordsInWindow, Is.EqualTo(3));
        Assert.That(profiler.RepaintFrequency, Is.EqualTo(0.75f));
    }

    [Test]
    public void RecordFrame_WindowCapsAtSixty_DropsOldest()
    {
        var profiler = new GuiProfiler();

        for (var i = 0; i < GuiProfiler.Window; i++)
        {
            profiler.RecordFrame(true);
        }

        Assert.That(profiler.ReRecordsInWindow, Is.EqualTo(GuiProfiler.Window));

        for (var i = 0; i < GuiProfiler.Window; i++)
        {
            profiler.RecordFrame(false);
        }

        Assert.That(profiler.FramesSampled, Is.EqualTo(GuiProfiler.Window));
        Assert.That(profiler.ReRecordsInWindow, Is.EqualTo(0));
    }

    [Test]
    public void Reset_ClearsWindow()
    {
        var profiler = new GuiProfiler();
        profiler.RecordFrame(true);

        profiler.Reset();

        Assert.That(profiler.FramesSampled, Is.EqualTo(0));
        Assert.That(profiler.ReRecordsInWindow, Is.EqualTo(0));
        Assert.That(profiler.RepaintFrequency, Is.EqualTo(0f));
    }

    private static RenderObject InflateRenderObject()
    {
        var owner = new BuildOwner();
        var el = new SizedBox(10, 10).CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        return el.RenderObject!;
    }

    [Test]
    public void RecordDirtyInitiator_DedupsWithinFrame()
    {
        var profiler = new GuiProfiler();
        var node = InflateRenderObject();
        RenderObject.AdvanceFrame();

        profiler.RecordDirtyInitiator(node);
        profiler.RecordDirtyInitiator(node);

        var top = profiler.TopDirtyInitiators(5);
        Assert.That(top, Has.Count.EqualTo(1));
        Assert.That(top[0].Node, Is.SameAs(node));
        Assert.That(top[0].InitiatedFrames, Is.EqualTo(1));
    }

    [Test]
    public void RecordDirtyInitiator_CountsAcrossFrames()
    {
        var profiler = new GuiProfiler();
        var node = InflateRenderObject();

        RenderObject.AdvanceFrame();
        profiler.RecordDirtyInitiator(node);
        RenderObject.AdvanceFrame();
        profiler.RecordDirtyInitiator(node);

        Assert.That(profiler.TopDirtyInitiators(5)[0].InitiatedFrames, Is.EqualTo(2));
    }

    [Test]
    public void TopDirtyInitiators_SortedByCountDescending()
    {
        var profiler = new GuiProfiler();
        var quiet = InflateRenderObject();
        var noisy = InflateRenderObject();

        RenderObject.AdvanceFrame();
        profiler.RecordDirtyInitiator(quiet);
        profiler.RecordDirtyInitiator(noisy);
        RenderObject.AdvanceFrame();
        profiler.RecordDirtyInitiator(noisy);

        var top = profiler.TopDirtyInitiators(5);
        Assert.That(top[0].Node, Is.SameAs(noisy));
        Assert.That(top[0].InitiatedFrames, Is.EqualTo(2));
        Assert.That(top[1].Node, Is.SameAs(quiet));
    }

    [Test]
    public void Reset_ClearsDirtyInitiators()
    {
        var profiler = new GuiProfiler();
        var node = InflateRenderObject();
        RenderObject.AdvanceFrame();
        profiler.RecordDirtyInitiator(node);

        profiler.Reset();

        Assert.That(profiler.TopDirtyInitiators(5), Is.Empty);
    }
}

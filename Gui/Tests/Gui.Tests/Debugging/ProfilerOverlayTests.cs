using Gui.Debugging;

namespace Gui.Tests.Debugging;

[TestFixture]
public class ProfilerOverlayTests
{
    [Test]
    public void BuildLines_IncludesStagesAndChurnHeadline()
    {
        var metrics = new PerformanceMetrics();
        metrics.RecordFrameTime(16.0f);
        metrics.RecordPaintTime(4.0f);
        var profiler = new GuiProfiler();
        for (var i = 0; i < GuiProfiler.Window; i++)
        {
            profiler.RecordFrame(true);
        }

        var lines = ProfilerOverlay.BuildLines(metrics, profiler, 5);

        Assert.That(lines, Is.Not.Empty);
        Assert.That(lines, Has.Some.Contains("frame"));
        Assert.That(lines, Has.Some.Contains("fps"));
        Assert.That(lines, Has.Some.Contains("paint"));
        Assert.That(lines, Has.Some.Contains("re-record"));
        Assert.That(lines, Has.Some.Contains("60/60"));
        Assert.That(lines, Has.Some.Contains("EVERY frame"));
    }
}

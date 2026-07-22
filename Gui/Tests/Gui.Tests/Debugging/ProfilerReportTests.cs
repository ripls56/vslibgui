using Gui.Debugging;

namespace Gui.Tests.Debugging;

[TestFixture]
public class ProfilerReportTests
{
    [Test]
    public void BuildWindow_IncludesTitleStagesAndChurn()
    {
        var metrics = new PerformanceMetrics();
        metrics.RecordFrameTime(16.0f);
        metrics.RecordPaintTime(3.5f);
        var profiler = new GuiProfiler();
        for (var i = 0; i < GuiProfiler.Window; i++)
        {
            profiler.RecordFrame(true);
        }

        var report = ProfilerReport.BuildWindow("MyHud", metrics, profiler, 5);

        Assert.That(report, Does.Contain("MyHud"));
        Assert.That(report, Does.Contain("frame:"));
        Assert.That(report, Does.Contain("fps"));
        Assert.That(report, Does.Contain("paint"));
        Assert.That(report, Does.Contain("re-record"));
        Assert.That(report, Does.Contain("60/60"));
    }
}

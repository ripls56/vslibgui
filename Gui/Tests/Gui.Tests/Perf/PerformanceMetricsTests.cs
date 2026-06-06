using Gui.Debugging;

namespace Gui.Tests.Perf;

public class PerformanceMetricsTests
{
    [Test]
    public void Should_Track_FrameTime_Averaged()
    {
        var metrics = new PerformanceMetrics();

        metrics.RecordFrameTime(16.6f);
        metrics.RecordFrameTime(16.7f);

        // Simple smoothing verification
        Assert.That(metrics.FrameTime, Is.InRange(16.0f, 17.0f));
    }

    [Test]
    public void Should_Reset_Counters_Each_Frame()
    {
        var metrics = new PerformanceMetrics();

        metrics.IncrementLayout();
        metrics.IncrementPaint();
        metrics.WidgetCount = 10;

        Assert.That(metrics.LayoutCalls, Is.EqualTo(1));
        Assert.That(metrics.PaintCalls, Is.EqualTo(1));

        metrics.OnFrameStart(0);

        Assert.That(metrics.LayoutCalls, Is.EqualTo(0));
        Assert.That(metrics.PaintCalls, Is.EqualTo(0));
        Assert.That(metrics.WidgetCount, Is.EqualTo(10)); // Persists until recalculated
    }
}

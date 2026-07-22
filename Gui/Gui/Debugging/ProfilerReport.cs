using System.Globalization;
using System.Text;

namespace Gui.Debugging;

/// <summary>
///     Formats one window's metrics and profiler data into a copyable multi-line
///     report for the <c>/ui prof dump</c> command. Pure logic, unit-testable without
///     a game session.
/// </summary>
public static class ProfilerReport
{
    /// <summary>Builds the report block for a single window.</summary>
    public static string BuildWindow
    (
        string title,
        PerformanceMetrics metrics,
        GuiProfiler profiler,
        int topN
    )
    {
        var c = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine($"== {title} ==");
        sb.AppendLine
        (
            string.Format
            (
                c,
                "frame: {0:0.00} ms ({1:0} fps)",
                metrics.FrameTime,
                metrics.FrameTime > 0f
                    ? 1000f / metrics.FrameTime
                    : 0f
            )
        );
        sb.AppendLine
        (
            string.Format
            (
                c,
                "stages ms: begin {0:0.00} build {1:0.00} layout {2:0.00} paint {3:0.00} end {4:0.00}",
                metrics.BeginTime,
                metrics.BuildTime,
                metrics.LayoutTime,
                metrics.PaintTime,
                metrics.EndTime
            )
        );
        sb.AppendLine
        (
            string.Format
            (
                c,
                "re-record: {0}/{1} frames ({2:0.0}%)",
                profiler.ReRecordsInWindow,
                profiler.FramesSampled,
                profiler.RepaintFrequency * 100f
            )
        );

        var top = profiler.TopDirtyInitiators(topN);
        if (top.Count == 0)
        {
            sb.AppendLine("dirty initiators: none");
        }
        else
        {
            sb.AppendLine("dirty initiators (frames):");
            foreach (var hotspot in top)
            {
                sb.AppendLine
                (
                    string.Format
                    (
                        c,
                        "  {0} x{1}  [{2}]",
                        ProfilerLabels.Describe(hotspot.Node),
                        hotspot.InitiatedFrames,
                        ProfilerLabels.DescribePath(hotspot.Node)
                    )
                );
            }
        }

        return sb.ToString();
    }
}

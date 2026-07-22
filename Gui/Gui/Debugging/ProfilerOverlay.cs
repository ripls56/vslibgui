using System.Collections.Generic;
using System.Globalization;

namespace Gui.Debugging;

/// <summary>
///     Builds the lines of text rendered by the profiler debug overlay. Pure logic so
///     it can be unit-tested without a Skia surface; <see cref="DebugPainter" /> draws
///     the returned lines.
/// </summary>
public static class ProfilerOverlay
{
    /// <summary>
    ///     Builds overlay lines for one window: stage breakdown, re-record frequency,
    ///     and the top dirty initiators.
    /// </summary>
    /// <param name="metrics">Stage timing data for this frame.</param>
    /// <param name="profiler">Per-window profiler holding re-record and dirty history.</param>
    /// <param name="topN">Maximum number of dirty initiator lines to include.</param>
    /// <returns>Ordered list of display strings ready for <see cref="DebugPainter" />.</returns>
    public static IReadOnlyList<string> BuildLines
    (
        PerformanceMetrics metrics,
        GuiProfiler profiler,
        int topN
    )
    {
        var c = CultureInfo.InvariantCulture;
        var lines = new List<string>
        {
            "GUI PROFILER",
            string.Format
            (
                c,
                "frame {0:0.00} ms ({1:0} fps)",
                metrics.FrameTime,
                metrics.FrameTime > 0f
                    ? 1000f / metrics.FrameTime
                    : 0f
            ),
            string.Format
            (
                c,
                "begin {0:0.00} build {1:0.00} layout {2:0.00}",
                metrics.BeginTime,
                metrics.BuildTime,
                metrics.LayoutTime
            ),
            string.Format
            (
                c,
                "paint {0:0.00} end {1:0.00}",
                metrics.PaintTime,
                metrics.EndTime
            ),
            string.Format
            (
                c,
                "re-record {0}/{1}",
                profiler.ReRecordsInWindow,
                profiler.FramesSampled
            ),
        };

        if (profiler.FramesSampled > 0 && profiler.RepaintFrequency >= 1f)
        {
            lines.Add("re-records EVERY frame");
        }

        foreach (var hotspot in profiler.TopDirtyInitiators(topN))
        {
            lines.Add
            (
                string.Format
                (
                    c,
                    "dirty {0} x{1}",
                    ProfilerLabels.Describe(hotspot.Node),
                    hotspot.InitiatedFrames
                )
            );
        }

        return lines;
    }
}

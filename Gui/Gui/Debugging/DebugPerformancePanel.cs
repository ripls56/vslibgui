using System;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Debugging;

/// <summary>
///     Renders a live snapshot of <see cref="PerformanceMetrics" /> as a compact text column,
///     refreshed every 500 ms.
/// </summary>
public class DebugPerformancePanel : StatefulWidget
{
    private static readonly Vector4 White = Vector4.One;
    private static readonly Vector4 Yellow = new(1f, 1f, 0.5f, 1f);
    private static readonly Vector4 Red = new(1f, 0.5f, 0.5f, 1f);

    /// <summary>Initializes a new <see cref="DebugPerformancePanel" />.</summary>
    /// <param name="metrics">Live metrics reference; properties are read on each periodic rebuild.</param>
    public DebugPerformancePanel(PerformanceMetrics? metrics)
    {
        Metrics = metrics;
    }

    /// <summary>Live metrics reference passed from the parent.</summary>
    public PerformanceMetrics? Metrics { get; }

    /// <inheritdoc />
    public override State CreateState() => new DebugPerformancePanelState();

    private class DebugPerformancePanelState : State<DebugPerformancePanel>
    {
        private AnimationController? _ticker;

        public override void InitState()
        {
            base.InitState();
            _ticker = new AnimationController(
                TimeSpan.FromMilliseconds(500),
                Element.Owner!.GetTickerProvider()
            );
            _ticker.OnStatusChanged += status =>
            {
                if (status != AnimationStatus.Completed)
                {
                    return;
                }

                SetState(() => { });
                _ticker?.Reset();
                _ticker?.Forward();
            };
            _ticker.Forward();
        }

        public override Widget Build(BuildContext context)
        {
            var metrics = Widget.Metrics;

            if (metrics == null)
            {
                return new Text("No metrics", new TextStyle { FontSize = 11, Color = White });
            }

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                spacing: 2,
                children:
                [
                    new Text($"Frame: {metrics.FrameTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = White }),
                    new Text($"  Begin:  {metrics.BeginTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = Yellow }),
                    new Text($"  Build:  {metrics.BuildTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = Yellow }),
                    new Text($"  Layout: {metrics.LayoutTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = Yellow }),
                    new Text($"  Paint:  {metrics.PaintTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = Yellow }),
                    new Text($"  End:    {metrics.EndTime:F2} ms",
                        new TextStyle { FontSize = 11, Color = Yellow }),
                    new Text($"Widgets: {metrics.WidgetCount}",
                        new TextStyle { FontSize = 11, Color = White }),
                    new Text($"Layout Calls: {metrics.LayoutCalls}",
                        new TextStyle { FontSize = 11, Color = White }),
                    new Text($"Paint Calls:  {metrics.PaintCalls}",
                        new TextStyle { FontSize = 11, Color = White }),
                    new Text(
                        $"Boundary Hits:   {metrics.RepaintBoundaryHits}",
                        new TextStyle { FontSize = 11, Color = White }
                    ),
                    new Text(
                        $"Boundary Misses: {metrics.RepaintBoundaryMisses}",
                        new TextStyle { FontSize = 11, Color = White }
                    ),
                    new Text(
                        $"Alloc/Frame: {metrics.AllocationsThisFrame / 1024f:F1} KB",
                        new TextStyle { FontSize = 11, Color = Red }
                    ),
                    new Text(
                        $"Total Mem:   {metrics.TotalAllocatedMemory / 1024f / 1024f:F1} MB",
                        new TextStyle { FontSize = 11, Color = Red }
                    )
                ]
            );
        }

        public override void Dispose()
        {
            _ticker?.Dispose();
            base.Dispose();
        }
    }
}

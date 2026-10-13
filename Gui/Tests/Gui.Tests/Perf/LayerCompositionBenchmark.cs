using System.Diagnostics;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Core.Windowing;
using Gui.Debugging;
using Gui.Rendering;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Config;
using TextWidget = Gui.Widgets.Basic.Text;

namespace Gui.Tests.Perf;

[TestFixture]
[Explicit("Measurement harness, not an assertion. Run manually.")]
public class LayerCompositionBenchmark
{
    private const float ScreenW = 900f;
    private const float ScreenH = 700f;
    private const int Frames = 300;

    [Test]
    public void Measure_HoverOneButton_LayerVsFullRootRecord()
    {
        RuntimeEnv.GUIScale = 1f;

        var owner = TestHelpers.NewBuildOwner();
        var root = Mount(BuildScreen(), owner);
        var rootRo = root.RenderObject!;
        rootRo.MarkNeedsLayout();

        using var bmp = new SKBitmap
        (
            (int)ScreenW,
            (int)ScreenH,
            SKColorType.Rgba8888,
            SKAlphaType.Premul
        );
        using var canvas = new SKCanvas(bmp);
        using var pipeline = new FramePipeline(_ => { });

        RunFrame(pipeline, root, canvas, owner);

        var boundary = FindFirstBoundary(rootRo)!;
        var insideLeaf = FindFirstBox(boundary)!;
        var outsideLeaf = FindOutsideBoundaryBox(rootRo)!;

        // Warm both paths.
        for (var i = 0; i < 20; i++)
        {
            insideLeaf.MarkNeedsPaint();
            RunFrame(pipeline, root, canvas, owner);
            outsideLeaf.MarkNeedsPaint();
            RunFrame(pipeline, root, canvas, owner);
        }

        var layer = Measure
        (
            pipeline,
            root,
            canvas,
            owner,
            insideLeaf
        );
        var full = Measure
        (
            pipeline,
            root,
            canvas,
            owner,
            outsideLeaf
        );

        TestContext.Out.WriteLine($"nodes in tree            : {CountNodes(rootRo)}");
        TestContext.Out.WriteLine($"boundaries in tree       : {CountBoundaries(rootRo)}");
        TestContext.Out.WriteLine("--- record/repaint only (what layering actually saves)");
        TestContext.Out.WriteLine($"dirty inside boundary    : {layer.paintMs:F4} ms/frame");
        TestContext.Out.WriteLine($"dirty outside boundary   : {full.paintMs:F4} ms/frame");
        TestContext.Out.WriteLine($"ratio                    : {full.paintMs / layer.paintMs:F1}x");
        TestContext.Out.WriteLine("--- whole frame incl. replaying the root layer to the surface");
        TestContext.Out.WriteLine($"dirty inside boundary    : {layer.frameMs:F4} ms/frame");
        TestContext.Out.WriteLine($"dirty outside boundary   : {full.frameMs:F4} ms/frame");
        TestContext.Out.WriteLine($"ratio                    : {full.frameMs / layer.frameMs:F2}x");
    }

    /// <summary>
    ///     Runs <see cref="Frames" /> frames, dirtying <paramref name="leaf" /> each time.
    ///     Returns the mean whole-frame wall time and the mean of the pipeline's own
    ///     record/repaint measurement, which excludes replaying the root layer onto the
    ///     surface — the part layering cannot avoid.
    /// </summary>
    private static (double frameMs, double paintMs) Measure
    (
        FramePipeline pipeline,
        Element root,
        SKCanvas canvas,
        BuildOwner owner,
        RenderBox leaf
    )
    {
        var metrics = new PerformanceMetrics();
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < Frames; i++)
        {
            leaf.MarkNeedsPaint();
            RunFrame
            (
                pipeline,
                root,
                canvas,
                owner,
                metrics
            );
        }

        sw.Stop();
        return (sw.Elapsed.TotalMilliseconds / Frames, metrics.PaintTime);
    }

    private static void RunFrame
    (
        FramePipeline pipeline,
        Element root,
        SKCanvas canvas,
        BuildOwner owner,
        PerformanceMetrics? metrics = null
    )
    {
        RenderObject.AdvanceFrame();
        pipeline.Run
        (
            MakeArgs
            (
                root,
                canvas,
                owner,
                metrics
            )
        );
    }

    /// <summary>A plausible screen: a titled panel with a grid of buttons and labels.</summary>
    private static Widget BuildScreen()
    {
        var rows = new List<Widget>();
        for (var r = 0; r < 12; r++)
        {
            var cells = new List<Widget>();
            for (var c = 0; c < 6; c++)
            {
                cells.Add(new Button(new TextWidget($"Action {r}-{c}")));
            }

            rows.Add(new Row(8f, children: cells));
        }

        var body = new List<Widget>
        {
            new TextWidget("Settings")
        };
        body.AddRange(rows);

        return new Theme
        (
            ThemeData.Default,
            new Container
            (
                new BoxStyle
                {
                    Color = new Vector4(0.1f, 0.1f, 0.12f, 1f),
                    Width = ScreenW,
                    Height = ScreenH,
                    Padding = EdgeInsets.All(12f)
                },
                new Column(6f, children: body)
            )
        );
    }

    private static int CountNodes
    (
        RenderObject node
    )
    {
        var n = 1;
        foreach (var child in node.Children)
        {
            n += CountNodes(child);
        }

        return n;
    }

    private static int CountBoundaries
    (
        RenderObject node
    )
    {
        var n = node.IsRepaintBoundary ? 1 : 0;
        foreach (var child in node.Children)
        {
            n += CountBoundaries(child);
        }

        return n;
    }

    private static RenderRepaintBoundary? FindFirstBoundary
    (
        RenderObject node
    )
    {
        if (node is RenderRepaintBoundary rb)
        {
            return rb;
        }

        foreach (var child in node.Children)
        {
            var found = FindFirstBoundary(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RenderBox? FindFirstBox
    (
        RenderObject node
    )
    {
        foreach (var child in node.Children)
        {
            if (child is RenderBox box && !box.IsRepaintBoundary)
            {
                return box;
            }

            var found = FindFirstBox(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>A RenderBox that no repaint boundary encloses — dirtying it re-records the root.</summary>
    private static RenderBox? FindOutsideBoundaryBox
    (
        RenderObject node
    )
    {
        if (node is RenderBox box && !box.IsRepaintBoundary && box.Children.Count > 0)
        {
            return box;
        }

        foreach (var child in node.Children)
        {
            if (child.IsRepaintBoundary)
            {
                continue;
            }

            var found = FindOutsideBoundaryBox(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Element Mount
    (
        Widget widget,
        BuildOwner owner
    )
    {
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return element;
    }

    private static FrameArgs MakeArgs
    (
        Element root,
        SKCanvas canvas,
        BuildOwner owner,
        PerformanceMetrics? metrics = null
    ) =>
        new()
        {
            RootElement = root,
            Canvas = canvas,
            WindowPos = Vector2.Zero,
            WindowSize = new Vector2(ScreenW, ScreenH),
            LayoutSize = new Vector2(ScreenW, ScreenH),
            UiScale = 1f,
            LocalElapsedMs = 0.0,
            WindowConfig = new WindowConfig
            {
                Size = new Vector2(ScreenW, ScreenH),
                Draggable = false,
                Resizable = false
            },
            PerformanceMetrics = metrics ?? new PerformanceMetrics(),
            Profiler = new GuiProfiler(),
            BuildOwner = owner,
            DebugSettings = new DebugSettings(),
            ViolationReporter = null,
            IsActiveWindow = true,
            ShouldCommitLayout = false,
            OnCommitLayout = () => { },
            OnShrinkWrapCompleted = _ => Vector2.Zero,
            IsPositionInitialized = true,
            ScreenW = ScreenW,
            ScreenH = ScreenH,
            PaintResizePendingOverlay = _ => { },
            IsResizePending = false,
            SyncHitBounds = () => { }
        };
}

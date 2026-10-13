using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Core.Windowing;
using Gui.Debugging;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Config;

namespace Gui.Tests.Windowing;

/// <summary>
///     Covers layered composition: a dirty repaint boundary must re-record only its own
///     layer while the window root recording is replayed untouched.
/// </summary>
[TestFixture]
public class FramePipelineLayerTests
{
    private const float ScreenW = 200f;
    private const float ScreenH = 100f;

    private static readonly Vector4 Blue = new(0, 0, 1, 1);
    private static readonly Vector4 Red = new(1, 0, 0, 1);
    private static readonly Vector4 Green = new(0, 1, 0, 1);

    [SetUp]
    public void SetUp() => RuntimeEnv.GUIScale = 1f;

    [Test]
    public void DirtyBoundary_RepaintsOwnLayer_WithoutRerecordingRoot()
    {
        var owner = new BuildOwner();
        var root = MountRoot
        (
            new Container
            (
                new BoxStyle { Color = Blue, Width = 200, Height = 100 },
                new RepaintBoundary
                (
                    new Container(new BoxStyle { Color = Red, Width = 40, Height = 40 })
                )
            ),
            owner
        );

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

        // Frame 1: full record.
        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(root, canvas, owner));

        Assert.That(ColorAt(bmp, 5, 5), Is.EqualTo(Red), "boundary content must paint");
        Assert.That(ColorAt(bmp, 150, 80), Is.EqualTo(Blue), "root content must paint");

        var boundary = FindBoundary(rootRo)!;
        var leaf = FindColoredBox(boundary, Red)!;

        // Act: dirty a leaf inside the boundary only.
        leaf.Color = Green;

        Assert.That
        (
            rootRo.ChildNeedsPaint,
            Is.False,
            "MarkNeedsPaint must stop at the boundary instead of dirtying the root layer"
        );

        // Frame 2: layer-only repaint.
        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(root, canvas, owner));

        Assert.That
        (
            rootRo.WasPaintedThisFrame,
            Is.False,
            "root layer must be replayed, not re-recorded"
        );
        Assert.That(ColorAt(bmp, 5, 5), Is.EqualTo(Green), "boundary layer must show new pixels");
        Assert.That(ColorAt(bmp, 150, 80), Is.EqualTo(Blue), "replayed root pixels must survive");
    }

    [Test]
    public void DirtyOutsideBoundary_StillRerecordsRoot_AndKeepsBoundaryPixels()
    {
        var owner = new BuildOwner();
        var root = MountRoot
        (
            new Container
            (
                new BoxStyle { Color = Blue, Width = 200, Height = 100 },
                new RepaintBoundary
                (
                    new Container(new BoxStyle { Color = Red, Width = 40, Height = 40 })
                )
            ),
            owner
        );

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

        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(root, canvas, owner));

        var outerBox = FindColoredBox(rootRo, Blue)!;
        outerBox.Color = Green;

        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(root, canvas, owner));

        Assert.That(rootRo.WasPaintedThisFrame, Is.True, "root must re-record");
        Assert.That(ColorAt(bmp, 150, 80), Is.EqualTo(Green));
        Assert.That
        (
            ColorAt(bmp, 5, 5),
            Is.EqualTo(Red),
            "clean boundary must still composite into the new root recording"
        );
    }

    [Test]
    public void MarkNeedsPaint_StillReachesRoot_WhenBoundaryHasNoLayerYet()
    {
        // No pipeline, so no registry and no recorded layer: propagation must not be
        // swallowed, otherwise the very first frame would never paint.
        var rootRo = new RenderBox();
        var boundary = new RenderRepaintBoundary();
        var leaf = new RenderBox();
        rootRo.AddChild(boundary);
        boundary.AddChild(leaf);
        rootRo.ResetDirtyFlags();

        leaf.MarkNeedsPaint();

        Assert.That(boundary.ChildNeedsPaint, Is.True);
        Assert.That(rootRo.ChildNeedsPaint, Is.True);
    }

    [Test]
    public void BoundaryLayer_DoesNotCullPaintingOutsideItsBox()
    {
        // A layer recording culls ops outside its bounds, so a scaled-up child and an
        // outer shadow must widen those bounds — this is what Button relies on.
        var owner = new BuildOwner();
        var root = MountRoot
        (
            new Container
            (
                new BoxStyle { Color = Blue, Width = 200, Height = 100 },
                new Center
                (
                    new RepaintBoundary
                    (
                        new Transform
                        (
                            new Container
                            (
                                new BoxStyle { Color = Red, Width = 20, Height = 20 }
                            ),
                            SKMatrix.CreateScale(3f, 3f),
                            Alignment.Center
                        )
                    )
                )
            ),
            owner
        );

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

        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(root, canvas, owner));

        // The 20x20 box scaled 3x around its centre covers 60x60 centred in the window.
        Assert.That(ColorAt(bmp, 100, 50), Is.EqualTo(Red), "centre of the scaled box");
        Assert.That
        (
            ColorAt(bmp, 100, 25),
            Is.EqualTo(Red),
            "scaled artwork outside the boundary's own box must not be culled"
        );
    }

    private static Vector4 ColorAt
    (
        SKBitmap bmp,
        int x,
        int y
    )
    {
        var c = bmp.GetPixel(x, y);
        return new Vector4
        (
            MathF.Round(c.Red / 255f),
            MathF.Round(c.Green / 255f),
            MathF.Round(c.Blue / 255f),
            MathF.Round(c.Alpha / 255f)
        );
    }

    private static RenderRepaintBoundary? FindBoundary
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
            var found = FindBoundary(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RenderBox? FindColoredBox
    (
        RenderObject node,
        Vector4 color
    )
    {
        if (node is RenderBox box && box.Color == color)
        {
            return box;
        }

        foreach (var child in node.Children)
        {
            var found = FindColoredBox(child, color);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Element MountRoot
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
        BuildOwner owner
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
            PerformanceMetrics = new PerformanceMetrics(),
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

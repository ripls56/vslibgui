using Gui.Core.Framework;
using Gui.Core.Windowing;
using Gui.Debugging;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Windowing;

[TestFixture]
public class FramePipelineCollapseTests
{
    private const float ScreenW = 800f;
    private const float ScreenH = 600f;
    private static readonly Vector2 CollapsedSize = new(200, 28);

    private static Element MountRoot(Widget widget, BuildOwner owner)
    {
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return element;
    }

    private static FrameArgs MakeArgs(
        Element root,
        SKCanvas canvas,
        WindowConfig config,
        Func<Vector2, Vector2> onShrinkWrapCompleted,
        Action syncHitBounds,
        BuildOwner owner) =>
        new()
        {
            RootElement = root,
            Canvas = canvas,
            WindowPos = new Vector2(10, 10),
            WindowSize = CollapsedSize,
            LayoutSize = CollapsedSize,
            UiScale = 1f,
            LocalElapsedMs = 0.0,
            WindowConfig = config,
            PerformanceMetrics = new PerformanceMetrics(),
            Profiler = new GuiProfiler(),
            BuildOwner = owner,
            DebugSettings = new DebugSettings(),
            ViolationReporter = null,
            IsActiveWindow = true,
            ShouldCommitLayout = false,
            OnCommitLayout = () => { },
            OnShrinkWrapCompleted = onShrinkWrapCompleted,
            IsPositionInitialized = true,
            ScreenW = ScreenW,
            ScreenH = ScreenH,
            PaintResizePendingOverlay = _ => { },
            IsResizePending = false,
            SyncHitBounds = syncHitBounds,
            CollapsedSize = CollapsedSize,
        };

    private static RenderObject RunCollapsed(
        WindowConfig config,
        Func<Vector2, Vector2> onShrinkWrapCompleted,
        Action syncHitBounds)
    {
        var owner = new BuildOwner();
        var root = MountRoot(new SizedBox(width: 120, height: 300), owner);
        var rootRo = root.RenderObject!;
        rootRo.MarkNeedsLayout();

        using var bmp = new SKBitmap(
            (int)ScreenW, (int)ScreenH, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);

        using var pipeline = new FramePipeline(_ => { });
        pipeline.Run(MakeArgs(root, canvas, config, onShrinkWrapCompleted, syncHitBounds, owner));
        return rootRo;
    }

    [Test]
    public void Collapsed_FixedSize_LaysOutToBarRect_DespiteMinHeight()
    {
        var synced = 0;
        var config = new WindowConfig
        {
            Size = new Vector2(300, 400),
            MinSize = new Vector2(100, 50),
            Draggable = false,
            Resizable = false,
        };

        var rootRo = RunCollapsed(config, _ => Vector2.Zero, () => synced++);

        Assert.That(rootRo.Size.X, Is.EqualTo(200f).Within(0.01f));
        Assert.That(rootRo.Size.Y, Is.EqualTo(28f).Within(0.01f));
        Assert.That(synced, Is.EqualTo(1), "SyncHitBounds must run after the collapsed layout");
    }

    [Test]
    public void Collapsed_ShrinkWrap_LaysOutToBarRect_AndSkipsShrinkWrapCallback()
    {
        var synced = 0;
        var shrinkWrapCalls = 0;
        var config = new WindowConfig
        {
            MinSize = new Vector2(100, 50),
            Draggable = false,
            Resizable = false,
        };

        var rootRo = RunCollapsed
        (
            config,
            size =>
            {
                shrinkWrapCalls++;
                return Vector2.Zero;
            },
            () => synced++
        );

        Assert.That(rootRo.Size.X, Is.EqualTo(200f).Within(0.01f));
        Assert.That(rootRo.Size.Y, Is.EqualTo(28f).Within(0.01f));
        Assert.That(shrinkWrapCalls, Is.EqualTo(0),
            "a collapsed window must not report a content-driven size");
        Assert.That(synced, Is.EqualTo(1), "SyncHitBounds must run after the collapsed layout");
    }
}

using Gui.Core.Framework;
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

[TestFixture]
public class FramePipelineShrinkWrapTests
{
    private const float ScreenW = 800f;
    private const float ScreenH = 600f;

    private static Element MountRoot(Widget widget, BuildOwner owner)
    {
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return element;
    }

    private static Vector2 BottomCenter(Vector2 size) =>
        new((ScreenW - size.X) / 2f, ScreenH - size.Y - 12f);

    private static FrameArgs MakeArgs(
        Element root,
        SKCanvas canvas,
        WindowConfig config,
        Vector2 staleWindowPos,
        Func<Vector2, Vector2> onShrinkWrapCompleted,
        BuildOwner owner) =>
        new()
        {
            RootElement = root,
            Canvas = canvas,
            WindowPos = staleWindowPos,
            WindowSize = new Vector2(100, 50),
            LayoutSize = new Vector2(100, 50),
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
            SyncHitBounds = () => { },
        };

    [Test]
    public void ShrinkWrap_Reposition_AppliesNewPositionSameFrame()
    {
        var owner = new BuildOwner();
        var root = MountRoot(new SizedBox(width: 120, height: 40), owner);
        var rootRo = root.RenderObject!;
        rootRo.MarkNeedsLayout();

        using var bmp = new SKBitmap(
            (int)ScreenW, (int)ScreenH, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);

        var pipeline = new FramePipeline(_ => { });

        var repositioned = Vector2.Zero;
        Vector2 reposition(Vector2 newSize)
        {
            repositioned = BottomCenter(newSize);
            return repositioned;
        }

        pipeline.Run(MakeArgs(
            root, canvas,
            new WindowConfig { Draggable = false, Resizable = false },
            staleWindowPos: Vector2.Zero,
            reposition,
            owner));

        Assert.That(repositioned, Is.Not.EqualTo(Vector2.Zero),
            "shrink-wrap reposition callback must run");
        Assert.That(rootRo.ScreenOffset.X, Is.EqualTo(repositioned.X).Within(0.01f));
        Assert.That(rootRo.ScreenOffset.Y, Is.EqualTo(repositioned.Y).Within(0.01f));
    }

    [Test]
    public void FixedSize_KeepsProvidedWindowPos()
    {
        var owner = new BuildOwner();
        var root = MountRoot(new SizedBox(width: 120, height: 40), owner);
        var rootRo = root.RenderObject!;
        rootRo.MarkNeedsLayout();

        using var bmp = new SKBitmap(
            (int)ScreenW, (int)ScreenH, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);

        var pipeline = new FramePipeline(_ => { });
        var fixedPos = new Vector2(123, 234);

        pipeline.Run(MakeArgs(
            root, canvas,
            new WindowConfig { Size = new Vector2(200, 100), Draggable = false, Resizable = false },
            staleWindowPos: fixedPos,
            _ => Vector2.Zero,
            owner));

        Assert.That(rootRo.ScreenOffset.X, Is.EqualTo(fixedPos.X).Within(0.01f));
        Assert.That(rootRo.ScreenOffset.Y, Is.EqualTo(fixedPos.Y).Within(0.01f));
    }

    [Test]
    public void ShrinkWrap_GrowingContent_ClipsToNewSizeNotStaleSnapshot()
    {
        var owner = new BuildOwner();
        var content = new Container(new BoxStyle
        {
            Color = new Vector4(1, 0, 0, 1),
            Width = 120,
            Height = 50,
        });
        var root = MountRoot(content, owner);
        var rootRo = root.RenderObject!;
        rootRo.MarkNeedsLayout();

        using var bmp = new SKBitmap(200, 100, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        RuntimeEnv.GUIScale = 1f;
        RenderObject.AdvanceFrame();
        var pipeline = new FramePipeline(_ => { });

        pipeline.Run(MakeArgs(
            root, canvas,
            new WindowConfig { Draggable = false, Resizable = false },
            staleWindowPos: Vector2.Zero,
            _ => Vector2.Zero,
            owner));

        var insideStaleClip = bmp.GetPixel(50, 25);
        var beyondStaleClip = bmp.GetPixel(115, 25);
        Assert.That(insideStaleClip.Alpha, Is.GreaterThan(200),
            "content inside the window must paint");
        Assert.That(beyondStaleClip.Alpha, Is.GreaterThan(200),
            "content past the stale WindowSize must not be clipped away (caused the flicker)");
    }
}

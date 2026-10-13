using System;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Core.Windowing;
using Gui.Debugging;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Config;
using TextWidget = Gui.Widgets.Basic.Text;

namespace Gui.Tests.Windowing;

/// <summary>
///     Reproduces the guide-screen layout: pinned header + Expanded scroll with
///     RichText paragraphs, and the sidebar row wrapped the way StaggeredReveal
///     wraps it (Opacity + AnimatedScale with a zero duration).
/// </summary>
[TestFixture]
public class GuideReproTests
{
    private const float W = 300f;
    private const float H = 200f;

    private static readonly Vector4 Bg = new(0f, 0f, 1f, 1f);
    private static readonly Vector4 Fg = new(1f, 0f, 0f, 1f);

    [SetUp]
    public void SetUp() => RuntimeEnv.GUIScale = 1f;

    [Test]
    public void BodyStructure_RichTextInScroll_Paints()
    {
        var tree = new Container
        (
            new BoxStyle { Color = Bg, Width = W, Height = H },
            new Padding
            (
                EdgeInsets.All(8),
                new Column
                (
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 8,
                    children:
                    [
                        new TextWidget("Header", new TextStyle { FontSize = 20, Color = Fg }),
                        new Container(new BoxStyle { Height = 1, Color = Fg }),
                        new Expanded
                        (
                            new SingleChildScrollView
                            (
                                child: new Column
                                (
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 8,
                                    children:
                                    [
                                        RichPara("First paragraph of the guide body"),
                                        RichPara("Second paragraph with more words in it"),
                                    ]
                                )
                            )
                        ),
                    ]
                )
            )
        );

        var bmp = Paint(tree);
        Assert.That(CountColor(bmp, Fg), Is.GreaterThan(20), "RichText paragraphs must paint");
    }

    [Test]
    public void SidebarRow_ZeroDurationAnimatedScale_Paints()
    {
        var row = new Row
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Container(new BoxStyle { Width = 3, Color = Fg }),
                new Expanded
                (
                    new Padding
                    (
                        EdgeInsets.Symmetric(horizontal: 8, vertical: 7),
                        new Row
                        (
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                new TextWidget("Guide title", new TextStyle { FontSize = 13, Color = Fg }),
                            ]
                        )
                    )
                ),
            ]
        );

        // Exactly what AtlasUi.StaggeredReveal produces at rest: Opacity(1) over an
        // AnimatedScale whose duration is TimeSpan.Zero.
        var tree = new Container
        (
            new BoxStyle { Color = Bg, Width = W, Height = H },
            new Padding
            (
                EdgeInsets.All(8),
                new Opacity
                (
                    1f,
                    child: new AnimatedScale
                    (
                        scale: 1f,
                        duration: TimeSpan.Zero,
                        alignment: Alignment.Center,
                        child: row
                    )
                )
            )
        );

        var bmp = Paint(tree);
        Assert.That(CountColor(bmp, Fg), Is.GreaterThan(20), "row under zero-duration AnimatedScale must paint");
    }

    [Test]
    public void AnimatedText_Paints()
    {
        var tree = new Container
        (
            new BoxStyle { Color = Bg, Width = W, Height = H },
            new Padding
            (
                EdgeInsets.All(8),
                new AnimatedText
                (
                    "animated label",
                    new TextStyle { FontSize = 13, Color = Fg },
                    TimeSpan.FromMilliseconds(140),
                    Curves.EaseOut
                )
            )
        );

        var bmp = Paint(tree);
        Assert.That(CountColor(bmp, Fg), Is.GreaterThan(10), "AnimatedText must paint");
    }

    private static RichText RichPara(string text) =>
        new
        (
            new TextSpan(text),
            new TextStyle { FontSize = 15, Color = Fg, SoftWrap = true }
        );

    private static SKBitmap Paint(Widget widget)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new Gui.Tests.Animations.MockTickerProvider());
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();

        var rootRo = element.RenderObject!;
        rootRo.MarkNeedsLayout();

        var bmp = new SKBitmap((int)W, (int)H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        using var pipeline = new FramePipeline(_ => { });
        RenderObject.AdvanceFrame();
        canvas.Clear(SKColors.Transparent);
        pipeline.Run(MakeArgs(element, canvas, owner));
        return bmp;
    }

    private static int CountColor(SKBitmap bmp, Vector4 c)
    {
        var count = 0;
        for (var y = 0; y < bmp.Height; y++)
        {
            for (var x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (p.Red > 150 && p.Blue < 100 && p.Green < 100)
                    count++;
            }
        }
        return count;
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
            WindowSize = new Vector2(W, H),
            LayoutSize = new Vector2(W, H),
            UiScale = 1f,
            LocalElapsedMs = 0.0,
            WindowConfig = new WindowConfig
            {
                Size = new Vector2(W, H),
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
            ScreenW = W,
            ScreenH = H,
            PaintResizePendingOverlay = _ => { },
            IsResizePending = false,
            SyncHitBounds = () => { }
        };
}

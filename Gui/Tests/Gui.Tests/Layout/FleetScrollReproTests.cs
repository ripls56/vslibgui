using System.Text;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Layout;

/// <summary>
///     Repro harness for the "Fleet page paints nothing inside its scroll view" report:
///     replicates the lhgui FleetPage tree (Column[header, Expanded(SingleChildScrollView(
///     Padding(Column[roster, imperial])))]) with staggered-reveal wrappers around cards,
///     lays it out at page size, and checks that the scroll content actually paints.
/// </summary>
[TestFixture]
public class FleetScrollReproTests
{
    private sealed class NoopTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static Widget Card(float height, Vector4 color) => new Container
    (
        style: new BoxStyle { Height = height, Color = color }
    );

    /// <summary>Structural copy of AtlasCard's interactive wrapper chain.</summary>
    private static Widget InteractiveCard(Widget body)
    {
        Widget surface = new AnimatedContainer
        (
            duration: TimeSpan.FromMilliseconds(190),
            curve: Curves.EaseOutCubic,
            style: new BoxStyle
            {
                Color = new Vector4(0.09f, 0.13f, 0.19f, 1f),
                BorderThickness = 1f,
                BorderColor = new Vector4(1f, 0.84f, 0.55f, 0.35f),
                CornerRadius = new Vector4(6f),
                ClipBehavior = ClipBehavior.AntiAlias,
            },
            child: body
        );

        return new RepaintBoundary
        (
            new MouseRegion
            (
                cursor: MouseCursor.LinkSelect,
                child: new GestureDetector
                (
                    onTap: _ => { },
                    onEnter: _ => { },
                    onExit: _ => { },
                    child: new AnimatedScale
                    (
                        scale: 1f,
                        duration: TimeSpan.FromMilliseconds(140),
                        curve: Curves.EaseOut,
                        alignment: Alignment.Center,
                        child: surface
                    )
                )
            )
        );
    }

    /// <summary>Verbatim copy of AtlasUi.StaggeredReveal from lhgui.</summary>
    private static Widget StaggeredReveal(AnimationController reveal, int index, Widget child)
    {
        var begin = Math.Min(0.08 * index, 0.6);
        var fade = new Interval(begin, Math.Min(begin + 0.4, 1.0), Curves.EaseOutCubic);
        var pop = new Interval(begin, Math.Min(begin + 0.4, 1.0), Curves.EaseOutBack);

        return new AnimatedBuilder
        (
            animation: reveal,
            builder: _ =>
            {
                var raw = reveal.Value;
                var o = (float)fade.Transform(raw);
                var s = 0.94f + 0.06f * (float)pop.Transform(raw);
                return new Opacity
                (
                    o,
                    child: new AnimatedScale
                    (
                        scale: s,
                        duration: TimeSpan.Zero,
                        alignment: Alignment.Center,
                        child: child
                    )
                );
            }
        );
    }

    private static Widget BuildFleetTree(AnimationController reveal, bool interactiveCards)
    {
        Widget Wrap(Widget card) => interactiveCards ? InteractiveCard(card) : card;

        Widget RosterCard(int i, float h) => StaggeredReveal
        (
            reveal, i,
            Wrap(Card(h, new Vector4(0.9f, 0.75f, 0.4f, 1f)))
        );

        Widget ImperialRow(int i) => StaggeredReveal
        (
            reveal, i,
            Wrap(Card(36f, new Vector4(0.5f, 0.6f, 0.8f, 1f)))
        );

        var roster = new Column
        (
            spacing: 12f,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children: [RosterCard(0, 108f), RosterCard(1, 108f)]
        );

        var imperial = new Column
        (
            spacing: 8f,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Row
                (
                    spacing: 8f,
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children:
                    [
                        new Container(style: new BoxStyle { Width = 80, Height = 15, Color = new Vector4(1, 1, 1, 1) }),
                        new Expanded
                        (
                            new Container(style: new BoxStyle { Height = 1, Color = new Vector4(1, 1, 1, 1) })
                        ),
                    ]
                ),
                ImperialRow(2),
                ImperialRow(3),
            ]
        );

        return new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Container(style: new BoxStyle { Width = 120, Height = 15, Color = new Vector4(1, 1, 1, 1) }),
                new Expanded
                (
                    new SingleChildScrollView
                    (
                        child: new Padding
                        (
                            EdgeInsets.Only(top: 12f),
                            new Column
                            (
                                spacing: 24f,
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children: [roster, imperial]
                            )
                        )
                    )
                ),
            ]
        );
    }

    private static (Element Root, AnimationController Reveal, BuildOwner Owner, TickerScheduler Scheduler) MountFleetLive(bool interactiveCards)
    {
        var owner = new BuildOwner();
        var scheduler = new TickerScheduler();
        owner.SetTickerProvider(scheduler);

        var reveal = new AnimationController(TimeSpan.FromMilliseconds(520), scheduler);
        reveal.Forward();

        var root = BuildFleetTree(reveal, interactiveCards).CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();
        return (root, reveal, owner, scheduler);
    }

    private static (Element Root, AnimationController Reveal) MountFleet(bool interactiveCards)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new NoopTickerProvider());

        var reveal = new AnimationController(TimeSpan.FromMilliseconds(520), new NoopTickerProvider());
        // Steady state after the reveal finished.
        reveal.Forward(from: 1.0);

        var root = BuildFleetTree(reveal, interactiveCards).CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();
        return (root, reveal);
    }

    private static string Dump(RenderObject ro, int depth, StringBuilder sb)
    {
        sb.AppendLine
        (
            $"{new string(' ', depth * 2)}{ro.GetType().Name} pos=({ro.X:F1},{ro.Y:F1}) size=({ro.Size.X:F1},{ro.Size.Y:F1})"
        );
        foreach (var child in ro.Children) Dump(child, depth + 1, sb);
        return sb.ToString();
    }

    [Test]
    public void FleetScrollContent_PaintsCards()
    {
        AssertPaintsMoreThan(MountFleet(interactiveCards: false), 5000, "plain");
    }

    [Test]
    public void FleetScrollContent_InteractiveCards_PaintsCards()
    {
        AssertPaintsMoreThan(MountFleet(interactiveCards: true), 5000, "interactive");
    }

    [Test]
    public void FleetScrollContent_LiveRevealAnimation_PaintsCards()
    {
        var (root, reveal, owner, scheduler) = MountFleetLive(interactiveCards: true);
        var ro = root.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(940, 780));

        // Pump frames like GuiBase.OnRenderGUI: scheduler tick → dirty builds → relayout.
        var frame = TimeSpan.FromMilliseconds(16);
        for (var i = 0; i < 80; i++)
        {
            RenderObject.AdvanceFrame();
            scheduler.Update(frame);
            owner.BuildDirtyElements();
            ro.Layout(LayoutConstraints.Tight(940, 780));
        }

        Console.WriteLine($"reveal.Value={reveal.Value} status={reveal.Status}");

        using var bmp = new SKBitmap(940, 780, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        ro.Paint(new PaintingContext(canvas, 0.0));

        int painted = 0;
        for (var y = 40; y < 780; y++)
        {
            for (var x = 0; x < 940; x++)
            {
                if (bmp.GetPixel(x, y).Alpha > 8) painted++;
            }
        }

        using var fs = File.Create(Path.Combine(Path.GetTempPath(), "fleet-repro-live.png"));
        bmp.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);

        Assert.That(painted, Is.GreaterThan(5000),
            $"live reveal ended at value={reveal.Value} but painted only {painted}px.\n{Dump(ro, 0, new StringBuilder())}");
    }

    private static void AssertPaintsMoreThan((Element Root, AnimationController Reveal) mounted, int minPixels, string name)
    {
        var ro = mounted.Root.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(940, 780));

        var tree = Dump(ro, 0, new StringBuilder());
        Console.WriteLine(tree);

        RenderObject.AdvanceFrame();
        using var bmp = new SKBitmap(940, 780, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        ro.Paint(new PaintingContext(canvas, 0.0));

        // Count painted pixels below the header (content area).
        int painted = 0;
        for (var y = 40; y < 780; y++)
        {
            for (var x = 0; x < 940; x++)
            {
                if (bmp.GetPixel(x, y).Alpha > 8) painted++;
            }
        }

        using var fs = File.Create
        (
            Path.Combine(Path.GetTempPath(), $"fleet-repro-{name}.png")
        );
        bmp.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);

        Assert.That(painted, Is.GreaterThan(minPixels), $"[{name}] scroll content painted only {painted}px.\n{tree}");
    }
}

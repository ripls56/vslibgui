using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Gestures;

/// <summary>
///     A follower (<see cref="AnchoredOverlay" />, <see cref="CompositedTransformFollower" />)
///     is a placement wrapper, so it must stay out of the pointer path its own subtree
///     declined, and it must anchor against the size the target actually paints at.
///     <para>
///         Both matter for read-only overlays over a zoomable canvas: the overlay stack
///         hit-tests its topmost entry first and stops at the first hit, so a follower that
///         claims the pointer starves whatever it covers — the trigger included, which
///         reads as hover flickering on and off as the card opens and closes.
///     </para>
/// </summary>
[TestFixture]
public class FollowerHitTestTests
{
    private const float Scale = 2.5f;
    private const float TriggerSize = 40f;

    private static (Element root, BuildOwner owner) Mount
    (
        Widget widget,
        float w,
        float h
    )
    {
        var owner = new BuildOwner();
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        el.RenderObject!.Layout(LayoutConstraints.Tight(w, h));
        return (el, owner);
    }

    [Test]
    public void IgnoringFollower_LetsPointerReachWidgetBehindIt()
    {
        var button = new MockTarget();

        // Overlay-shaped tree: the entry on top is a read-only card, the one below is the
        // content. They overlap at (60, 60).
        var (root, _) = Mount
        (
            new Stack
            ([
                new Positioned
                (
                    left: 40f,
                    top: 40f,
                    width: 60f,
                    height: 60f,
                    child: button
                ),
                new AnchoredOverlay
                (
                    anchorX: 70f,
                    triggerGlobalY: 40f,
                    triggerHeight: 0f,
                    verticalGap: 0f,
                    preferredSide: OverlaySide.Bottom,
                    child: new IgnorePointer(child: new SizedBox(120f, 120f))
                )
            ]),
            200f,
            200f
        );

        var result = new HitTestResult();
        var hit = root.HitTest(result, new Vector2(60f, 60f));

        Assert.Multiple
        (() =>
            {
                Assert.That(hit, Is.True, "nothing was hit under the card");
                Assert.That
                (
                    result.Path.Any(e => e.Element.Widget == button),
                    Is.True,
                    "the read-only card swallowed the pointer meant for the widget behind it"
                );
            }
        );
    }

    [Test]
    public void AnchorsBelowPaintedBottom_WhenTargetSitsInsideAScaledViewport()
    {
        var link = new LayerLink();

        // A trigger inside a zoomed canvas: its layout box stays TriggerSize, but it paints
        // at TriggerSize * Scale.
        var host = new ScaleHost { Scale = Scale };
        var target = new RenderTargetProbe
        {
            Size = new Vector2(TriggerSize, TriggerSize), X = 20f, Y = 20f
        };
        host.AddChild(target);
        link.Target = target;

        const float gap = 10f;
        var (root, _) = Mount
        (
            new Stack
            ([
                new AnchoredOverlay
                (
                    anchorX: 0f,
                    triggerGlobalY: 0f,
                    triggerHeight: 0f,
                    gap,
                    OverlaySide.Bottom,
                    HorizontalAnchor.Center,
                    link,
                    new SizedBox(120f, 60f)
                )
            ]),
            800f,
            600f
        );

        var ro = root.RenderObject!.Children[0];

        // Painted trigger: top at 20*Scale, height TriggerSize*Scale.
        var paintedTop = 20f * Scale;
        var paintedBottom = paintedTop + TriggerSize * Scale;

        Assert.That
        (
            ro.Y,
            Is.EqualTo(paintedBottom + gap).Within(0.1f),
            "anchored against the unscaled layout box, so the overlay lands on its own trigger"
        );
    }

    /// <summary>Scales its children the way a zoom/pan viewport does.</summary>
    private sealed class ScaleHost : RenderObject
    {
        public float Scale { get; init; } = 1f;

        protected override void PerformLayout()
        {
            Size = Constraints.Biggest;
        }

        public override Vector2 LocalToGlobal
        (
            Vector2 localPoint
        )
        {
            var point = localPoint * Scale + new Vector2(X, Y);
            return Parent?.LocalToGlobal(point) ?? point;
        }
    }

    /// <summary>Stand-in for the render object a <c>CompositedTransformTarget</c> publishes.</summary>
    private sealed class RenderTargetProbe : RenderObject
    {
        protected override void PerformLayout()
        {
        }
    }

    private sealed class MockTarget : RenderObjectWidget, IPointerClickHandler
    {
        public void OnPointerClick
        (
            PointerEvent args
        )
        {
        }

        public override RenderObject CreateRenderObject() => new RenderBox();
    }
}

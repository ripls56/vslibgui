using Gui.Core.Framework;
using Gui.Core.Overlay;
using Gui.Core.Painting;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class AnchoredOverlayTests
{
    private static RenderAnchoredOverlay Layout(
        AnchoredOverlay widget,
        float width = 800f,
        float height = 600f
    )
    {
        var owner = new BuildOwner();
        // Wrap in a Stack so that StackParentData is processed and ro.X/Y are set.
        var root = new Stack([widget]);
        var el = root.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        var stackRo = el.RenderObject!;
        stackRo.Layout(LayoutConstraints.Tight(
            width,
            height
        ));
        return (RenderAnchoredOverlay)stackRo.Children[0];
    }

    private static AnchoredOverlay Box(
        float triggerY,
        float triggerHeight,
        float contentW,
        float contentH,
        float gap = 8f,
        OverlaySide side = OverlaySide.Top,
        HorizontalAnchor horizontal = HorizontalAnchor.Center,
        float anchorX = 400f
    ) =>
        new(
            anchorX,
            triggerY,
            triggerHeight,
            gap,
            side,
            horizontal,
            child: new SizedBox(
                contentW,
                contentH
            )
        );

    [Test]
    public void PlacesAbove_WhenRoomAboveSufficient()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 50
        ));
        Assert.That(
            ro.Y,
            Is.EqualTo(300f - 8f - 50f)
        );
    }

    [Test]
    public void FlipsBelow_WhenContentTooTallAbove()
    {
        var ro = Layout(Box(
            triggerY: 250f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 300
        ));
        Assert.That(
            ro.Y,
            Is.EqualTo(250f + 40f + 8f)
        );
    }

    [Test]
    public void FlipsAbove_WhenPreferredBottomButNoRoomBelow()
    {
        var ro = Layout(Box(
            triggerY: 400f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 300,
            side: OverlaySide.Bottom
        ));
        Assert.That(
            ro.Y,
            Is.EqualTo(400f - 8f - 300f)
        );
    }

    [Test]
    public void ClampsVertically_WhenContentFitsNeitherSide()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 400
        ));
        Assert.That(
            ro.Y,
            Is.GreaterThanOrEqualTo(0f)
        );
        Assert.That(
            ro.Y + ro.Size.Y,
            Is.LessThanOrEqualTo(600f)
        );
    }

    [Test]
    public void CentersHorizontally_WhenCenterAnchor()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 50,
            horizontal: HorizontalAnchor.Center,
            anchorX: 400f
        ));
        Assert.That(
            ro.X,
            Is.EqualTo(400f - 50f)
        );
    }

    [Test]
    public void AlignsToLeftEdge_WhenStartAnchor()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 50,
            horizontal: HorizontalAnchor.Start,
            anchorX: 400f
        ));
        Assert.That(
            ro.X,
            Is.EqualTo(400f)
        );
    }

    [Test]
    public void ClampsToLeftEdge_WhenAnchorNearLeft()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 200,
            contentH: 50,
            anchorX: 20f
        ));
        Assert.That(
            ro.X,
            Is.EqualTo(0f)
        );
    }

    [Test]
    public void ClampsToRightEdge_WhenAnchorNearRight()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 200,
            contentH: 50,
            anchorX: 780f
        ));
        Assert.That(
            ro.X,
            Is.EqualTo(800f - 200f)
        );
    }

    [Test]
    public void RespectsVerticalGap()
    {
        var ro = Layout(Box(
            triggerY: 300f,
            triggerHeight: 40f,
            contentW: 100,
            contentH: 50,
            gap: 20f
        ));
        Assert.That(
            ro.Y,
            Is.EqualTo(300f - 20f - 50f)
        );
    }

    [Test]
    public void FollowsLinkTarget_WhenTargetMoves()
    {
        var link = new LayerLink();
        var target = new RenderProxyBox { Size = new Vector2(100, 40) };
        link.Target = target;

        target.X = 400f;
        target.Y = 300f;

        var root = new Stack(
        [
            new AnchoredOverlay(
                anchorX: 400f,
                triggerGlobalY: 300f,
                triggerHeight: 40f,
                link: link,
                child: new SizedBox(100, 50)
            )
        ]);
        var owner = new BuildOwner();
        var el = root.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();

        var stackRo = el.RenderObject!;
        var constraints = LayoutConstraints.Tight(800, 600);
        stackRo.Layout(constraints);
        var ro = (RenderAnchoredOverlay)stackRo.Children[0];

        // Snapshot triggerY and live target agree here → above fits: Y = 300 - 8 - 50.
        Assert.That(ro.Y, Is.EqualTo(242f).Within(0.1f));

        // Scroll: push the live target down. The snapshot stays at 300, so a
        // snapshot-driven follower would be stuck at 242. A correct follower
        // tracks the live target: Y = 560 - 8 - 50 = 502.
        target.Y = 560f;
        stackRo.NeedsLayout = true;
        stackRo.Layout(constraints);

        Assert.That(ro.Y, Is.EqualTo(502f).Within(0.1f));
    }

    [Test]
    public void HitTest_SucceedsOnShiftedOverlay()
    {
        var owner = new BuildOwner();
        var widget = new Stack(
        [
            new AnchoredOverlay(
                anchorX: 400f,
                triggerGlobalY: 300f,
                triggerHeight: 40f,
                child: new Container(
                    style: new BoxStyle { HitTestBehavior = HitTestBehavior.Opaque },
                    child: new SizedBox(
                        100,
                        50
                    )
                )
            )
        ]);
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();

        var ro = el.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(
            800,
            600
        ));

        // AnchoredOverlay positions itself.
        // It should be at global Y = 242 (300 - 8 - 50), X = 350 (400 - 100/2).
        var overlayRo = ro.Children[0];
        Assert.That(
            overlayRo.X,
            Is.EqualTo(350f).Within(0.1f)
        );
        Assert.That(
            overlayRo.Y,
            Is.EqualTo(242f).Within(0.1f)
        );

        var result = new HitTestResult();
        var hit = el.HitTest(
            result,
            new Vector2(
                375,
                250
            )
        ); // Center of child (350+25, 242+8)

        Assert.That(
            hit,
            Is.True,
            "HitTest should succeed on the child"
        );
    }
}

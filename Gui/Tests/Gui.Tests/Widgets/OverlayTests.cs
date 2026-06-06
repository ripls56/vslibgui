using Gui.Core.Framework;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class OverlayTests
{
    private class ClickTarget : RenderObjectWidget, IPointerClickHandler
    {
        private readonly float _w, _h;

        public ClickTarget(float w, float h)
        {
            _w = w;
            _h = h;
        }

        public void OnPointerClick(PointerEvent args)
        {
        }

        public override RenderObject CreateRenderObject() => new FixedBox(_w, _h);

        // A leaf RenderObject that always reports a fixed size regardless of constraints,
        // used in tests to represent an interactive widget with known dimensions.
        private class FixedBox : RenderObject
        {
            private readonly Vector2 _size;

            public FixedBox(float w, float h)
            {
                _size = new Vector2(w, h);
            }

            protected override void PerformLayout() => Size = _size;
        }
    }

    // Stack with only Positioned children should fill available constraints,
    // not collapse to (0,0). This is required for correct hit-testing and
    // Right/Bottom positioning.
    [Test]
    public void Stack_AllPositioned_ShouldFillAvailableConstraints()
    {
        var buildOwner = new BuildOwner();
        var widget = new Stack([
            new Positioned(10, 20, width: 50, child: new Container())
        ]);

        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(800, 600));

        Assert.That(ro.Size.X, Is.EqualTo(800),
            "Stack should fill max width when all children are Positioned");
        Assert.That(ro.Size.Y, Is.EqualTo(600),
            "Stack should fill max height when all children are Positioned");
    }

    // A Stack whose only child is a Positioned widget must propagate hit tests
    // into that Positioned child. This is the core Overlay/dropdown scenario.
    [Test]
    public void Stack_AllPositioned_HitTestReachesPositionedChild()
    {
        var buildOwner = new BuildOwner();
        var target = new ClickTarget(100, 50);
        var widget = new Stack([
            new Positioned(200, 300, width: 100, child: target)
        ]);

        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(800, 600));

        // Click inside the positioned panel (210, 320) → local (10, 20) within panel
        var result = new HitTestResult();
        var hit = element.HitTest(result, new Vector2(210, 320));

        Assert.That(hit, Is.True, "Hit test should succeed for a click inside a Positioned child");
        Assert.That(
            result.Path.Any(e => e.Element.Widget == target),
            Is.True,
            "ClickTarget should appear in the hit path"
        );
    }

    // A click outside the positioned panel must NOT hit the panel.
    [Test]
    public void Stack_AllPositioned_MissesWhenOutsidePositionedChild()
    {
        var buildOwner = new BuildOwner();
        var target = new ClickTarget(100, 50);
        var widget = new Stack([
            new Positioned(200, 300, width: 100, child: target)
        ]);

        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(800, 600));

        // Click far outside the panel (10, 10)
        var result = new HitTestResult();
        var hit = element.HitTest(result, new Vector2(10, 10));

        Assert.That(hit, Is.False, "Click outside the Positioned child should not hit");
    }

    // Full Overlay scenario: root app fills the screen (entry 0), and an overlay
    // entry with a Positioned panel (entry 1) is stacked on top.
    // Clicking on the panel should hit the panel, not the root app.
    // Clicking outside the panel should fall through to the root app.
    [Test]
    public void Overlay_PositionedEntry_HitTestReachesPanelAndFallsThrough()
    {
        var buildOwner = new BuildOwner();
        var rootTarget = new ClickTarget(800, 600);
        var panelTarget = new ClickTarget(200, 150);

        var rootEntry = new OverlayEntry(rootTarget);
        var panelEntry = new OverlayEntry(
            new Stack([new Positioned(100, 100, width: 200, child: panelTarget)])
        );

        var overlay = new Overlay([rootEntry]);
        var element = overlay.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();

        // Insert the panel entry
        var overlayState = Overlay.Of(new BuildContext(overlay, element))!;
        overlayState.Insert(panelEntry);
        buildOwner.BuildDirtyElements();

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(800, 600));

        // Click inside the panel (150, 130) — panel bounds: x∈[100,300], y∈[100,250]
        var panelResult = new HitTestResult();
        var hitPanel = element.HitTest(panelResult, new Vector2(150, 130));
        Assert.That(hitPanel, Is.True, "Click inside overlay panel should hit");
        Assert.That(
            panelResult.Path.Any(e => e.Element.Widget == panelTarget),
            Is.True,
            "Panel ClickTarget should be in hit path"
        );

        // Click outside the panel (10, 10) — should reach root app
        var rootResult = new HitTestResult();
        var hitRoot = element.HitTest(rootResult, new Vector2(10, 10));
        Assert.That(hitRoot, Is.True, "Click outside panel should fall through to root app");
        Assert.That(
            rootResult.Path.Any(e => e.Element.Widget == rootTarget),
            Is.True,
            "Root ClickTarget should be in hit path when clicking outside panel"
        );
    }
}

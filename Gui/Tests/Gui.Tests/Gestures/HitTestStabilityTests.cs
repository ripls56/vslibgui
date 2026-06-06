using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Gestures;

[TestFixture]
public class HitTestStabilityTests
{
    private class MockInteractiveWidget : RenderObjectWidget, IPointerClickHandler
    {
        private readonly float _w, _h;

        public MockInteractiveWidget(float w, float h)
        {
            _w = w;
            _h = h;
        }

        public void OnPointerClick(PointerEvent args)
        {
        }

        public override RenderObject CreateRenderObject() =>
            new RenderBox { PreferredSize = new Vector2(_w, _h) };
    }

    [Test]
    public void HitTest_ShouldAccountForParentOffsets()
    {
        // Setup: Parent at (100, 100), Padding 50, Child 100x100.
        // Child top-left relative to parent root: (50, 50)
        var buildOwner = new BuildOwner();

        var child = new MockInteractiveWidget(100, 100);
        var parent = new Container(
            new BoxStyle { Width = 300, Height = 300 },
            new Padding(EdgeInsets.All(50), child)
        );

        var parentElement = parent.CreateElement();
        parentElement.AssignOwner(buildOwner);
        parentElement.Mount(null);

        // 1. Rebuild
        buildOwner.BuildDirtyElements();

        // 2. Layout (Simulation of GuiBase.OnRenderGUI)
        var rootRo = parentElement.RenderObject!;
        rootRo.Layout(LayoutConstraints.Tight(300, 300));

        // Simulate root being at (100, 100)
        rootRo.X = 100;
        rootRo.Y = 100;

        var result = new HitTestResult();

        // Point (175, 175) absolute.
        // Screen(175, 175) -> RootLocal(175-100, 175-100) = (75, 75)
        // Root(0,0) -> Padding(0,0) -> Child(50,50) size 100x100. (75,75) is inside child.

        float screenX = 175;
        float screenY = 175;
        var rootLocalX = screenX - rootRo.X;
        var rootLocalY = screenY - rootRo.Y;

        var hit = parentElement.HitTest(result, new Vector2(rootLocalX, rootLocalY));

        Assert.That(hit, Is.True,
            $"Should hit something at ({screenX}, {screenY}) absolute / ({rootLocalX}, {rootLocalY}) root-local");
        Assert.That(result.Path.Any(e => e.Element.Widget == child), Is.True,
            "Child widget should be in hit path");
    }
}

using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Layout;

public class LayoutWidgetsTests
{
    [Test]
    public void AspectRatio_WidthContrained_HeightCalculated()
    {
        var child = new TestRenderBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderAspectRatio { AspectRatio = 2.0f }; // Width = 2 * Height
        ro.AddChild(child);

        // Max Width 200, Max Height 500
        // AspectRatio 2.0 -> Width 200, Height 100
        ro.Layout(LayoutConstraints.Loose(200, 500));

        Assert.That(ro.Size.X, Is.EqualTo(200).Within(0.01f));
        Assert.That(ro.Size.Y, Is.EqualTo(100).Within(0.01f));
        Assert.That(child.Size, Is.EqualTo(ro.Size));
    }

    [Test]
    public void AspectRatio_HeightContrained_WidthCalculated()
    {
        var child = new TestRenderBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderAspectRatio { AspectRatio = 0.5f }; // Width = 0.5 * Height
        ro.AddChild(child);

        // Max Width 500, Max Height 200
        // AspectRatio 0.5 -> Width 100, Height 200
        ro.Layout(LayoutConstraints.Loose(500, 200));

        Assert.That(ro.Size.X, Is.EqualTo(100).Within(0.01f));
        Assert.That(ro.Size.Y, Is.EqualTo(200).Within(0.01f));
    }


    [Test]
    public void ConstrainedBox_MinimumHeight_Enforced()
    {
        var child = new TestRenderBox { IntrinsicSize = new Vector2(100, 10) };
        var ro = new RenderConstrainedBox(new LayoutConstraints(minHeight: 50));
        ro.AddChild(child);

        // Loose 500x500
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Child wanted height 10, but ConstrainedBox forced minHeight 50
        Assert.That(ro.Size.Y, Is.EqualTo(50).Within(0.01f));
        Assert.That(child.Size.Y, Is.EqualTo(50).Within(0.01f));
    }

    [Test]
    public void ConstrainedBox_MaximumWidth_Enforced()
    {
        var child = new TestRenderBox { IntrinsicSize = new Vector2(1000, 100) };
        var ro = new RenderConstrainedBox(new LayoutConstraints(maxWidth: 200));
        ro.AddChild(child);

        // Loose 500x500
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Child wanted width 1000, but ConstrainedBox forced maxWidth 200
        Assert.That(ro.Size.X, Is.EqualTo(200).Within(0.01f));
        Assert.That(child.Size.X, Is.EqualTo(200).Within(0.01f));
    }


    [Test]
    public void FittedBox_Contain_ScaleAndAlignment()
    {
        // Child is 200x100
        var child = new TestRenderBox { IntrinsicSize = new Vector2(200, 100) };
        var ro = new RenderFittedBox { Fit = BoxFit.Contain, Alignment = Alignment.Center };
        ro.AddChild(child);

        // FittedBox constrained to 100x100
        // Scale = min(100/200, 100/100) = 0.5
        // Child effectively becomes 100x50, centered in 100x100 -> Offset (0, 25)
        ro.Layout(LayoutConstraints.Tight(100, 100));

        Assert.That(ro.Size, Is.EqualTo(new Vector2(100, 100)));

        // Check coordinate transformation (Hit Testing)
        // Local point (50, 50) in FittedBox (center)
        // In child space: (center of 200x100) -> (100, 50)
        var localCenter = new Vector2(50, 50);
        var childPoint = ro.GlobalToChild(child, localCenter);

        Assert.That(childPoint.X, Is.EqualTo(100).Within(0.01f));
        Assert.That(childPoint.Y, Is.EqualTo(50).Within(0.01f));
    }

    [Test]
    public void FittedBox_HitTesting_PassesThroughTransform()
    {
        var childWidget = new TestInteractiveWidget(new Vector2(100, 100));
        var fittedBox = new FittedBox(childWidget);

        var owner = new BuildOwner();
        var rootElement = fittedBox.CreateElement();
        rootElement.AssignOwner(owner);
        rootElement.Mount(null);

        var ro = rootElement.RenderObject as RenderFittedBox;
        Assert.That(ro, Is.Not.Null);
        // 50x50 size for 100x100 child -> Scale 0.5
        ro!.Layout(LayoutConstraints.Tight(50, 50));

        var result = new HitTestResult();
        // (25, 25) in FittedBox -> (50, 50) in child
        var hit = rootElement.HitTest(result, new Vector2(25, 25));

        Assert.That(hit, Is.True);
        Assert.That(result.Path.Any(e => e.Element.Widget == childWidget), Is.True);

        // HitTest outside child area
        ro.Layout(LayoutConstraints.Tight(100, 50)); // Scale 0.5, child 50x50, offset (25, 0)

        result = new HitTestResult();
        // (10, 25) is in the left "letterbox" area (x: 0..25)
        hit = rootElement.HitTest(result, new Vector2(10, 25));

        // It should NOT hit the child.
        Assert.That(result.Path.Any(e => e.Element.Widget == childWidget), Is.False);
    }

    private class TestRenderBox : RenderProxyBox
    {
        public override bool IsHitTestTarget => true;
        public Vector2 IntrinsicSize { get; set; } = Vector2.Zero;

        protected override void PerformLayout() => Size = IntrinsicSize;
    }

    private class TestInteractiveWidget : SingleChildWidget, IPointerDownHandler
    {
        private readonly Vector2 _intrinsicSize;

        public TestInteractiveWidget(Vector2 size)
        {
            _intrinsicSize = size;
        }

        public void OnPointerDown(PointerEvent e)
        {
        }

        public override RenderObject CreateRenderObject() =>
            new TestRenderBox { IntrinsicSize = _intrinsicSize };

        public override void UpdateRenderObject(RenderObject renderObject)
        {
        }
    }
}

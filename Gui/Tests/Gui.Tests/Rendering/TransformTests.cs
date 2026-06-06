using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Rendering;

public class TransformTests
{
    [Test]
    public void Transform_Layout_PassesThroughConstraints()
    {
        // A 100x100 child inside a Transform should still report 100x100.
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform { Matrix = SKMatrix.CreateRotation(0.5f) };
        ro.AddChild(child);

        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size.X, Is.EqualTo(100).Within(0.01f));
        Assert.That(ro.Size.Y, Is.EqualTo(100).Within(0.01f));
        Assert.That(child.Size.X, Is.EqualTo(100).Within(0.01f));
        Assert.That(child.Size.Y, Is.EqualTo(100).Within(0.01f));
    }

    [Test]
    public void Transform_Layout_TightConstraints_ChildFillsBox()
    {
        var child = new FixedBox { IntrinsicSize = new Vector2(50, 50) };
        var ro = new RenderTransform { Matrix = SKMatrix.Identity };
        ro.AddChild(child);

        ro.Layout(LayoutConstraints.Tight(200, 200));

        // RenderProxyBox clamps Size to constraints: max(50,200)→200 clamped to Tight(200)=200
        Assert.That(ro.Size.X, Is.EqualTo(200).Within(0.01f));
    }


    [Test]
    public void Transform_Scale2x_HitCoordinatesMappedCorrectly()
    {
        // Scale by 2: a point at (60, 60) in Transform space maps to (30, 30) in child space.
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform { Matrix = SKMatrix.CreateScale(2, 2) };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(60, 60));

        Assert.That(childPos.X, Is.EqualTo(30).Within(0.01f));
        Assert.That(childPos.Y, Is.EqualTo(30).Within(0.01f));
    }

    [Test]
    public void Transform_Rotate90_HitCoordinatesMappedCorrectly()
    {
        // SKMatrix.CreateRotation(PI/2) produces a 90° clockwise rotation in screen space
        // (Y-axis points down). The forward transform maps local (x,y) to visual (-y, x).
        // Its inverse (90° CCW) maps a visual point (vx,vy) to local (vy, -vx).
        // Example: visual point (0, 50) → local (50, 0).
        var angle = (float)(Math.PI / 2); // 90° clockwise in screen space
        var ro = new RenderTransform { Matrix = SKMatrix.CreateRotation(angle) };
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(0, 50));

        Assert.That(childPos.X, Is.EqualTo(50).Within(0.1f));
        Assert.That(childPos.Y, Is.EqualTo(0).Within(0.1f));
    }

    [Test]
    public void Transform_DegenerateScale_HitTestReturnsFalse()
    {
        // Scale to zero → matrix is non-invertible → GlobalToChild returns (-inf, -inf).
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform { Matrix = SKMatrix.CreateScale(0, 0) };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(50, 50));

        Assert.That(float.IsNegativeInfinity(childPos.X), Is.True);
        Assert.That(float.IsNegativeInfinity(childPos.Y), Is.True);
    }


    [Test]
    public void Transform_Scale_CenterAlignment_PivotAtCenter()
    {
        // Scale by 2 with Center alignment: the widget centre stays fixed.
        // Child is 100x100, centre is (50, 50).
        // Point at (50, 50) in local space should map to (50, 50) in child space
        // (the centre is the fixed point of the transform).
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform
        {
            Matrix = SKMatrix.CreateScale(2, 2), Alignment = Alignment.Center
        };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(50, 50));

        Assert.That(childPos.X, Is.EqualTo(50).Within(0.1f));
        Assert.That(childPos.Y, Is.EqualTo(50).Within(0.1f));
    }

    [Test]
    public void Transform_Scale_TopLeftAlignment_PivotAtOrigin()
    {
        // Scale by 2 with TopLeft alignment (default): (0,0) is the fixed point.
        // Point at (60, 40) → inverse scale 0.5 → (30, 20).
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform
        {
            Matrix = SKMatrix.CreateScale(2, 2), Alignment = Alignment.TopLeft
        };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(60, 40));

        Assert.That(childPos.X, Is.EqualTo(30).Within(0.1f));
        Assert.That(childPos.Y, Is.EqualTo(20).Within(0.1f));
    }


    [Test]
    public void Transform_HitTest_Scale_HitsChildThroughInverseMatrix()
    {
        // Scale by 2, no alignment (pivot at origin). The layout size is still 100x100
        // (transform is paint-only). A pointer at (50, 50) — inside the layout box —
        // maps to (25, 25) in child space via inverse Scale(0.5), which is inside the child.
        var childWidget = new FixedWidget(new Vector2(100, 100));
        var transformWidget = Transform.Scale(childWidget, 2f);

        var owner = new BuildOwner();
        var root = transformWidget.CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();

        root.RenderObject!.Layout(LayoutConstraints.Loose(500, 500));

        var result = new HitTestResult();
        var hit = root.HitTest(result, new Vector2(50, 50));

        Assert.That(hit, Is.True);
        Assert.That(result.Path.Any(e => e.Element.Widget == childWidget), Is.True);
    }

    [Test]
    public void Transform_HitTest_Scale_MissesOutsideOriginalBounds()
    {
        // The coarse bounds check uses the untransformed layout size (100x100).
        // A pointer at (150, 50) is outside that box, so it should not hit.
        var childWidget = new FixedWidget(new Vector2(100, 100));
        var transformWidget = Transform.Scale(childWidget, 2f);

        var owner = new BuildOwner();
        var root = transformWidget.CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();

        root.RenderObject!.Layout(LayoutConstraints.Loose(500, 500));

        var result = new HitTestResult();
        var hit = root.HitTest(result, new Vector2(150, 50));

        // Pointer outside the layout box → entire hit test returns false.
        Assert.That(hit, Is.False);
        Assert.That(result.Path.Any(e => e.Element.Widget == childWidget), Is.False);
    }


    [Test]
    public void Transform_FactoryRotate_CreatesCorrectMatrix()
    {
        var ro = new RenderTransform { Matrix = SKMatrix.CreateRotation(1f) };
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Verify that the render object from the factory method produces identical behaviour.
        var widget = Transform.Rotate(null, 1f);
        var roFromWidget = (RenderTransform)widget.CreateRenderObject();
        var child2 = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        roFromWidget.AddChild(child2);
        roFromWidget.Layout(LayoutConstraints.Loose(500, 500));

        var pos1 = ro.GlobalToChild(child, new Vector2(30, 20));
        var pos2 = roFromWidget.GlobalToChild(child2, new Vector2(30, 20));

        Assert.That(pos1.X, Is.EqualTo(pos2.X).Within(0.001f));
        Assert.That(pos1.Y, Is.EqualTo(pos2.Y).Within(0.001f));
    }

    [Test]
    public void Transform_Translate_ShiftsHitTestOrigin()
    {
        // Translate by (20, 30): pointer at (70, 80) maps to (50, 50) in child space.
        var child = new FixedBox { IntrinsicSize = new Vector2(100, 100) };
        var ro = new RenderTransform { Matrix = SKMatrix.CreateTranslation(20, 30) };
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        var childPos = ro.GlobalToChild(child, new Vector2(70, 80));

        Assert.That(childPos.X, Is.EqualTo(50).Within(0.01f));
        Assert.That(childPos.Y, Is.EqualTo(50).Within(0.01f));
    }

    // A fixed-size render box that is always a hit-test target.
    private class FixedBox : RenderProxyBox
    {
        public override bool IsHitTestTarget => true;
        public Vector2 IntrinsicSize { get; set; } = new(100, 100);

        protected override void PerformLayout() => Size = IntrinsicSize;
    }

    private class FixedWidget : SingleChildWidget, IPointerDownHandler
    {
        private readonly Vector2 _size;

        public FixedWidget(Vector2 size)
        {
            _size = size;
        }

        public void OnPointerDown(PointerEvent e)
        {
        }

        public override RenderObject CreateRenderObject() => new FixedBox { IntrinsicSize = _size };

        public override void UpdateRenderObject(RenderObject ro)
        {
        }
    }
}

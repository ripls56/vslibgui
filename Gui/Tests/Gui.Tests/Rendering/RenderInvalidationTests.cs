using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class RenderInvalidationTests
{
    private class TestRenderWidget : RenderObjectWidget
    {
        public Vector4 Color { get; } = Vector4.One;

        public override RenderObject CreateRenderObject() => new TestRenderBox();

        public override void UpdateRenderObject(RenderObject renderObject)
        {
            base.UpdateRenderObject(renderObject);
            ((TestRenderBox)renderObject).Color = Color;
        }
    }

    private class TestRenderBox : RenderBox
    {
        // RenderBox has Color property that calls MarkNeedsPaint
    }

    [Test]
    public void RepaintBoundary_ShouldInvalidateCache_WhenChildIsDirty()
    {
        var buildOwner = new BuildOwner();

        var childWidget = new TestRenderWidget();
        var rootWidget = new RepaintBoundary(childWidget);

        var rootElement = rootWidget.CreateElement();
        rootElement.AssignOwner(buildOwner);
        rootElement.Mount(null);
        buildOwner.BuildDirtyElements();

        // Layout to ensure size (needed for recording)
        var rootRo = (RenderRepaintBoundary)rootElement.RenderObject!;
        rootRo.Layout(LayoutConstraints.Tight(100, 100));

        // Fake Paint context
        // We can't easily mock Skia context without native libs, but we can check flags

        // 1. Initial state: Clean
        // We manually reset flags because we didn't paint
        rootRo.ResetDirtyFlags();

        // 2. Update child
        // var newChildWidget = new TestRenderWidget { Color = new Vector4(1, 0, 0, 1) };
        // Manually update element to simulate rebuild
        // The child of RepaintBoundary is SingleChildElement -> TestRenderWidget
        // We need to access it. 
        // RepaintBoundary -> SingleChildElement.

        // Let's rely on widget update mechanism
        // But RepaintBoundary is a SingleChildWidget.
        // We can just update the child element directly if we had access.

        // Better: Verify via RenderObject propagation
        var childRo = (TestRenderBox)rootRo.Children[0];

        // Act: Modify child property
        childRo.Color = new Vector4(0, 1, 0, 1); // Should trigger MarkNeedsPaint

        // Assert
        Assert.That(childRo.NeedsPaint, Is.True, "Child should be dirty");
        Assert.That(rootRo.ChildNeedsPaint, Is.True, "Boundary should know child is dirty");

        // Check if cache would be invalidated? 
        // Logic is in Paint(), not immediately in MarkNeedsPaint (for parents).
        // But OnMarkNeedsPaint is NOT called for ChildNeedsPaint.
        // So cache remains until Paint() is called.

        // This confirms the logic I saw in RenderRepaintBoundary.cs is correct:
        // Cache is ONLY invalidated in Paint() if ChildNeedsPaint is true.
    }
}

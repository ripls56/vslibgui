using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

public class RenderObjectTests
{
    [Test]
    public void Layout_ShouldCallPerformLayout_WhenNeedsLayoutIsTrue()
    {
        var renderObject = new MockRenderObject();
        var constraints = LayoutConstraints.Tight(100, 100);

        renderObject.Layout(constraints);

        Assert.Multiple(() =>
        {
            Assert.That(renderObject.PerformLayoutCallCount, Is.EqualTo(1));
            Assert.That(renderObject.Size, Is.EqualTo(new Vector2(100, 100)));
            Assert.That(renderObject.NeedsLayout, Is.False);
        });
    }

    [Test]
    public void Layout_ShouldNotRecompute_IfSameConstraintsAndNotDirty()
    {
        var renderObject = new MockRenderObject();
        var constraints = LayoutConstraints.Tight(100, 100);

        renderObject.Layout(constraints);
        Assert.That(renderObject.PerformLayoutCallCount, Is.EqualTo(1));

        renderObject.Layout(constraints);
        Assert.That(renderObject.PerformLayoutCallCount, Is.EqualTo(1));
    }

    [Test]
    public void MarkNeedsLayout_ShouldBubbleUpToParent()
    {
        var parent = new MockRenderObject();
        var child = new MockRenderObject();
        parent.AddChild(child);

        parent.Layout(LayoutConstraints.Loose(500, 500));
        child.Layout(LayoutConstraints.Loose(100, 100));

        Assert.That(parent.NeedsLayout, Is.False);
        Assert.That(child.NeedsLayout, Is.False);

        child.MarkNeedsLayout();

        Assert.That(child.NeedsLayout, Is.True);
        Assert.That(parent.NeedsLayout, Is.False); // Parent itself doesn't need re-layout
        Assert.That(parent.ChildNeedsLayout, Is.True); // But its child does
    }

    [Test]
    public void LocalToGlobal_ShouldCalculateCorrectCoordinates()
    {
        var root = new MockRenderObject { X = 10, Y = 10 };
        var child = new MockRenderObject { X = 20, Y = 20 };
        root.AddChild(child);

        var globalPos = child.LocalToGlobal(new Vector2(5, 5));

        // Root(10,10) + Child(20,20) + Local(5,5) = (35,35)
        Assert.That(globalPos, Is.EqualTo(new Vector2(35, 35)));
    }

    [Test]
    public void GlobalToLocal_ShouldCalculateCorrectCoordinates()
    {
        var root = new MockRenderObject { X = 10, Y = 10 };
        var child = new MockRenderObject { X = 20, Y = 20 };
        root.AddChild(child);

        var localPos = child.GlobalToLocal(new Vector2(35, 35));

        Assert.That(localPos, Is.EqualTo(new Vector2(5, 5)));
    }

    private class MockRenderObject : RenderObject
    {
        public int PerformLayoutCallCount { get; private set; }
        public LayoutConstraints LastConstraints { get; private set; }

        protected override void PerformLayout()
        {
            PerformLayoutCallCount++;
            LastConstraints = Constraints;
            Size = new Vector2(Constraints.MinWidth, Constraints.MinHeight);
        }
    }
}

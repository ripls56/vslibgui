using Gui.Core.Animations;
using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedSizeTests
{
    private class SizeableBox : RenderBox
    {
        public Vector2 DesiredSize { get; set; } = new(100, 100);

        protected override void PerformLayout() => Size = Constraints.Constrain(DesiredSize);
    }

    [Test]
    public void RenderAnimatedSize_AnimatesSizeChange()
    {
        var vsync = new MockTickerProvider();
        var ro = new RenderAnimatedSize(
            TimeSpan.FromMilliseconds(200), Curves.Linear, vsync);

        var child = new SizeableBox();
        ro.AddChild(child);
        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size.X, Is.EqualTo(100).Within(0.1f));
        Assert.That(ro.Size.Y, Is.EqualTo(100).Within(0.1f));

        // Change child size and trigger re-layout.
        child.DesiredSize = new Vector2(200, 200);
        child.MarkNeedsLayout();
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Advance halfway through the animation.
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size.X, Is.EqualTo(150).Within(5f));
        Assert.That(ro.Size.Y, Is.EqualTo(150).Within(5f));

        // Advance past the end.
        vsync.Advance(TimeSpan.FromMilliseconds(150));
        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size.X, Is.EqualTo(200).Within(0.1f));
        Assert.That(ro.Size.Y, Is.EqualTo(200).Within(0.1f));

        ro.Dispose();
    }

    [Test]
    public void AnimatedSize_Widget_MountsSuccessfully()
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);

        var widget = new AnimatedSize(
            TimeSpan.FromMilliseconds(300));
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();

        Assert.That(element.RenderObject, Is.InstanceOf<RenderAnimatedSize>());
    }
}

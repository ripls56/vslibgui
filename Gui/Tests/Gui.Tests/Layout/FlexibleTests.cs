using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Layout;

public class FlexibleTests
{
    // Wraps a FixedBox inside a RenderProxyBox (acts like the widget layer Flexible/Expanded).
    private static RenderProxyBox MakeFlexChild(
        Vector2 intrinsicSize,
        int flex = 1,
        FlexFit fit = FlexFit.Loose)
    {
        var inner = new FixedBox { IntrinsicSize = intrinsicSize };
        var proxy = new RenderProxyBox();
        proxy.AddChild(inner);
        proxy.ParentData = new FlexParentData { Flex = flex, Fit = fit };
        return proxy;
    }


    [Test]
    public void Flexible_Loose_Horizontal_ShrinkWrapsToChild()
    {
        var child = MakeFlexChild(new Vector2(50, 30), fit: FlexFit.Loose);
        var flex = new RenderFlex { Direction = FlexDirection.Horizontal };
        flex.AddChild(child);

        flex.Layout(LayoutConstraints.Tight(200, 100));

        // Child must NOT be forced to 200px; it should stay at its intrinsic width.
        Assert.That(child.Size.X, Is.EqualTo(50).Within(0.01f));
        // Container still fills the available 200px.
        Assert.That(flex.Size.X, Is.EqualTo(200).Within(0.01f));
    }

    [Test]
    public void Flexible_Loose_Vertical_ShrinkWrapsToChild()
    {
        var child = MakeFlexChild(new Vector2(40, 60), fit: FlexFit.Loose);
        var flex = new RenderFlex { Direction = FlexDirection.Vertical };
        flex.AddChild(child);

        flex.Layout(LayoutConstraints.Tight(100, 200));

        Assert.That(child.Size.Y, Is.EqualTo(60).Within(0.01f));
        Assert.That(flex.Size.Y, Is.EqualTo(200).Within(0.01f));
    }

    [Test]
    public void Flexible_Loose_MainAxisAlignment_Center_PositionsCorrectly()
    {
        var child = MakeFlexChild(new Vector2(50, 30), fit: FlexFit.Loose);
        var flex = new RenderFlex
        {
            Direction = FlexDirection.Horizontal, MainAxisAlignment = MainAxisAlignment.Center
        };
        flex.AddChild(child);

        flex.Layout(LayoutConstraints.Tight(200, 100));

        // 50px child centered in 200px container → X = (200 - 50) / 2 = 75
        Assert.That(child.X, Is.EqualTo(75).Within(0.01f));
    }

    [Test]
    public void Flexible_Loose_MainAxisAlignment_End_PositionsCorrectly()
    {
        var child = MakeFlexChild(new Vector2(50, 30), fit: FlexFit.Loose);
        var flex = new RenderFlex
        {
            Direction = FlexDirection.Horizontal, MainAxisAlignment = MainAxisAlignment.End
        };
        flex.AddChild(child);

        flex.Layout(LayoutConstraints.Tight(200, 100));

        // 50px child at end of 200px container → X = 150
        Assert.That(child.X, Is.EqualTo(150).Within(0.01f));
    }


    [Test]
    public void Flexible_Tight_ForcesChildToFillAllocatedSpace()
    {
        var child = MakeFlexChild(new Vector2(50, 30), fit: FlexFit.Tight);
        var flex = new RenderFlex { Direction = FlexDirection.Horizontal };
        flex.AddChild(child);

        flex.Layout(LayoutConstraints.Tight(200, 100));

        // Tight fit: child must fill the full 200px slot.
        Assert.That(child.Size.X, Is.EqualTo(200).Within(0.01f));
    }


    [Test]
    public void Flexible_Loose_TwoChildren_SplitSpace()
    {
        var a = MakeFlexChild(new Vector2(30, 10));
        var b = MakeFlexChild(new Vector2(30, 10), 2);
        var flex = new RenderFlex { Direction = FlexDirection.Horizontal };
        flex.AddChild(a);
        flex.AddChild(b);

        // 300px split 1:2 → a gets 100px slot, b gets 200px slot.
        // Both children want only 30px, so they shrink-wrap.
        flex.Layout(LayoutConstraints.Tight(300, 50));

        Assert.That(a.Size.X, Is.EqualTo(30).Within(0.01f));
        Assert.That(b.Size.X, Is.EqualTo(30).Within(0.01f));
    }

    [Test]
    public void Flexible_LooseAndTight_Mixed()
    {
        var loose = MakeFlexChild(new Vector2(30, 10));
        var tight = MakeFlexChild(new Vector2(30, 10), 1, FlexFit.Tight);
        var flex = new RenderFlex { Direction = FlexDirection.Horizontal };
        flex.AddChild(loose);
        flex.AddChild(tight);

        // 200px split 1:1 → each gets 100px slot.
        flex.Layout(LayoutConstraints.Tight(200, 50));

        // Loose child shrinks to its intrinsic 30px.
        Assert.That(loose.Size.X, Is.EqualTo(30).Within(0.01f));
        // Tight child fills its 100px slot.
        Assert.That(tight.Size.X, Is.EqualTo(100).Within(0.01f));
    }


    [Test]
    public void Expanded_RemainsEquivalentToTightFlexible()
    {
        var flexChild = MakeFlexChild(new Vector2(30, 10), 1, FlexFit.Tight);
        var flex = new RenderFlex { Direction = FlexDirection.Horizontal };
        flex.AddChild(flexChild);

        flex.Layout(LayoutConstraints.Tight(200, 50));

        Assert.That(flexChild.Size.X, Is.EqualTo(200).Within(0.01f));
    }

    // Sizes itself to IntrinsicSize regardless of constraints.
    // Layout() will then clamp the reported size to the incoming constraints.
    private class FixedBox : RenderProxyBox
    {
        public Vector2 IntrinsicSize { get; set; }

        protected override void PerformLayout() => Size = IntrinsicSize;
    }
}

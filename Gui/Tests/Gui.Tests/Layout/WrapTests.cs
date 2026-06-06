using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Layout;

public class WrapTests
{
    private static FixedBox MakeChild(float w, float h) =>
        new() { IntrinsicSize = new Vector2(w, h) };

    [Test]
    public void Wrap_SingleRun_BehavesLikeRow()
    {
        var wrap = new RenderWrap();
        wrap.AddChild(MakeChild(30, 20));
        wrap.AddChild(MakeChild(40, 20));
        wrap.AddChild(MakeChild(30, 20));

        wrap.Layout(LayoutConstraints.Tight(200, 100));

        // All fit in one run: 30+40+30 = 100 < 200
        Assert.That(wrap.Children[0].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].X, Is.EqualTo(30).Within(0.01f));
        Assert.That(wrap.Children[2].X, Is.EqualTo(70).Within(0.01f));
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[2].Y, Is.EqualTo(0).Within(0.01f));
    }

    [Test]
    public void Wrap_MultipleRuns_WrapsToNextLine()
    {
        var wrap = new RenderWrap();
        wrap.AddChild(MakeChild(60, 20));
        wrap.AddChild(MakeChild(60, 20));
        wrap.AddChild(MakeChild(60, 20));

        wrap.Layout(LayoutConstraints.Tight(100, 200));

        // Run 1: child0 (60). child1 (60) doesn't fit (60+60=120>100) → new run
        // Run 2: child1 (60). child2 (60) doesn't fit → new run
        // Run 3: child2 (60)
        Assert.That(wrap.Children[0].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(20).Within(0.01f));
        Assert.That(wrap.Children[2].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[2].Y, Is.EqualTo(40).Within(0.01f));
    }

    [Test]
    public void Wrap_Spacing_AffectsWrapDecision()
    {
        var wrap = new RenderWrap { Spacing = 10 };
        wrap.AddChild(MakeChild(50, 20));
        wrap.AddChild(MakeChild(50, 20));

        wrap.Layout(LayoutConstraints.Tight(100, 200));

        // 50 + 10 + 50 = 110 > 100 → wraps
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(20).Within(0.01f));
    }

    [Test]
    public void Wrap_Spacing_WithinRun()
    {
        var wrap = new RenderWrap { Spacing = 10 };
        wrap.AddChild(MakeChild(30, 20));
        wrap.AddChild(MakeChild(30, 20));

        wrap.Layout(LayoutConstraints.Tight(200, 100));

        // 30 + 10 + 30 = 70 < 200 → single run
        Assert.That(wrap.Children[0].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].X, Is.EqualTo(40).Within(0.01f)); // 30 + 10
    }

    [Test]
    public void Wrap_RunSpacing_SpaceBetweenRuns()
    {
        var wrap = new RenderWrap { RunSpacing = 5 };
        wrap.AddChild(MakeChild(60, 20));
        wrap.AddChild(MakeChild(60, 25));
        wrap.AddChild(MakeChild(60, 15));

        wrap.Layout(LayoutConstraints.Tight(100, 200));

        // Run 1: child0 (h=20), Run 2: child1 (h=25), Run 3: child2 (h=15)
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(25).Within(0.01f)); // 20 + 5
        Assert.That(wrap.Children[2].Y, Is.EqualTo(55).Within(0.01f)); // 20 + 5 + 25 + 5
    }

    [Test]
    public void Wrap_MainAxisAlignment_Center()
    {
        var wrap = new RenderWrap { MainAxisAlignment = MainAxisAlignment.Center };
        wrap.AddChild(MakeChild(30, 20));
        wrap.AddChild(MakeChild(30, 20));

        wrap.Layout(LayoutConstraints.Tight(100, 100));

        // Single run, total = 60, free = 40, offset = 20
        Assert.That(wrap.Children[0].X, Is.EqualTo(20).Within(0.01f));
        Assert.That(wrap.Children[1].X, Is.EqualTo(50).Within(0.01f));
    }

    [Test]
    public void Wrap_CrossAxisAlignment_Center()
    {
        var wrap = new RenderWrap { CrossAxisAlignment = CrossAxisAlignment.Center };
        wrap.AddChild(MakeChild(40, 10));
        wrap.AddChild(MakeChild(40, 30));

        wrap.Layout(LayoutConstraints.Tight(200, 100));

        // Single run, run height = 30
        // child0 (h=10): centered → y = (30-10)/2 = 10
        // child1 (h=30): centered → y = 0
        Assert.That(wrap.Children[0].Y, Is.EqualTo(10).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(0).Within(0.01f));
    }

    [Test]
    public void Wrap_RunAlignment_Center()
    {
        var wrap = new RenderWrap { RunAlignment = MainAxisAlignment.Center };
        wrap.AddChild(MakeChild(60, 20));
        wrap.AddChild(MakeChild(60, 20));

        wrap.Layout(LayoutConstraints.Tight(100, 100));

        // 2 runs, total cross = 40, free = 60, offset = 30
        Assert.That(wrap.Children[0].Y, Is.EqualTo(30).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(50).Within(0.01f));
    }

    [Test]
    public void Wrap_Vertical_WrapsColumns()
    {
        var wrap = new RenderWrap { Direction = FlexDirection.Vertical };
        wrap.AddChild(MakeChild(20, 60));
        wrap.AddChild(MakeChild(20, 60));
        wrap.AddChild(MakeChild(20, 60));

        wrap.Layout(LayoutConstraints.Tight(200, 100));

        // Run 1: child0 (h=60). child1 (60+60=120>100) → new run
        // Run 2: child1. child2 doesn't fit → new run
        // Run 3: child2
        Assert.That(wrap.Children[0].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[1].X, Is.EqualTo(20).Within(0.01f));
        Assert.That(wrap.Children[1].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[2].X, Is.EqualTo(40).Within(0.01f));
        Assert.That(wrap.Children[2].Y, Is.EqualTo(0).Within(0.01f));
    }

    [Test]
    public void Wrap_EmptyChildren_ZeroSize()
    {
        var wrap = new RenderWrap();
        wrap.Layout(LayoutConstraints.Loose(200, 100));

        Assert.That(wrap.Size.X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Size.Y, Is.EqualTo(0).Within(0.01f));
    }

    [Test]
    public void Wrap_SingleChild_NoWrap()
    {
        var wrap = new RenderWrap();
        wrap.AddChild(MakeChild(50, 30));

        wrap.Layout(LayoutConstraints.Tight(200, 100));

        Assert.That(wrap.Children[0].X, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[0].Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(wrap.Children[0].Size.X, Is.EqualTo(50).Within(0.01f));
        Assert.That(wrap.Children[0].Size.Y, Is.EqualTo(30).Within(0.01f));
    }

    private class FixedBox : RenderProxyBox
    {
        public Vector2 IntrinsicSize { get; set; }

        protected override void PerformLayout() => Size = IntrinsicSize;
    }
}

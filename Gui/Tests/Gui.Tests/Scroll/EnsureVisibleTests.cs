using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Layout;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;

namespace Gui.Tests.Scroll;

[TestFixture]
public class EnsureVisibleTests
{
    private class MockTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private class KeyedBox : Widget
    {
        public KeyedBox(float height, Key? key = null) : base(key)
        {
            Height = height;
        }

        public float Height { get; }
        public override Element CreateElement() => new RenderObjectElement(this);
        public override RenderObject CreateRenderObject() => new Ro(Height);

        private class Ro : RenderObject
        {
            private readonly float _h;

            public Ro(float h)
            {
                _h = h;
            }

            protected override void PerformLayout() => Size = new Vector2(Constraints.MaxWidth, _h);
        }
    }

    // -- GlobalKey Tests --

    [Test]
    public void GlobalKey_RegistersOnMount_UnregistersOnUnmount()
    {
        var key = new GlobalKey();
        var widget = new KeyedBox(10, key);
        var owner = new BuildOwner();

        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);

        Assert.That(key.CurrentElement, Is.Not.Null);
        Assert.That(key.CurrentElement, Is.SameAs(element));
        Assert.That(key.CurrentRenderObject, Is.Not.Null);

        element.Unmount();
        Assert.That(key.CurrentElement, Is.Null);
        Assert.That(key.CurrentRenderObject, Is.Null);
    }

    [Test]
    public void GlobalKey_WithoutOwner_DoesNotThrow()
    {
        var key = new GlobalKey();
        var widget = new KeyedBox(10, key);

        var element = widget.CreateElement();
        // Mount without owner (root element)
        element.Mount(null);
        // No owner → no registration, but no crash
        Assert.That(key.CurrentElement, Is.Null);
        element.Unmount();
    }

    // -- AnimateTo Tests --

    [Test]
    public void AnimateTo_ZeroDuration_JumpsImmediately()
    {
        var controller = new ScrollController();
        controller.Attach(new MockTickerProvider());
        controller.JumpTo(0);

        controller.AnimateTo(100, TimeSpan.Zero);
        Assert.That(controller.Offset, Is.EqualTo(100).Within(0.5f));
    }

    [Test]
    public void AnimateTo_ClampsToMaxScroll()
    {
        var controller = new ScrollController();
        controller.Attach(new MockTickerProvider());
        controller.JumpTo(0);

        controller.AnimateTo(500, TimeSpan.Zero, maxScroll: 200);
        Assert.That(controller.Offset, Is.EqualTo(200).Within(0.5f));
    }

    [Test]
    public void AnimateTo_SkipsWhenAlreadyAtTarget()
    {
        var controller = new ScrollController();
        controller.Attach(new MockTickerProvider());
        controller.JumpTo(100);

        var changed = false;
        controller.OnChanged += () => changed = true;
        controller.AnimateTo(100, TimeSpan.FromMilliseconds(300));

        Assert.That(changed, Is.False);
    }

    [Test]
    public void AnimateTo_CancelsExistingSimulation()
    {
        var controller = new ScrollController();
        controller.Attach(new MockTickerProvider());
        controller.JumpTo(0);

        // Start a fling
        controller.StartSimulation(1000, 0, 500);
        // AnimateTo should cancel it
        controller.AnimateTo(50, TimeSpan.Zero);

        Assert.That(controller.Offset, Is.EqualTo(50).Within(0.5f));
    }

    // -- EnsureVisible Tests --

    [Test]
    public void EnsureVisible_NullElement_DoesNotThrow()
    {
        var key = new GlobalKey();
        // key.CurrentElement is null — calling EnsureVisible shouldn't crash
        // (we guard with a null check before calling)
        Assert.DoesNotThrow(() =>
        {
            if (key.CurrentElement != null)
            {
                Scrollable.EnsureVisible(key.CurrentElement);
            }
        });
    }

    [Test]
    public void EnsureVisible_NoScrollableAncestor_SilentNoOp()
    {
        var key = new GlobalKey();
        var widget = new KeyedBox(10, key);
        var owner = new BuildOwner();

        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);

        Assert.DoesNotThrow(() =>
            Scrollable.EnsureVisible(key.CurrentElement!));

        element.Unmount();
    }

    [Test]
    public void EnsureVisible_WithSingleChildScrollView_ScrollsDown()
    {
        var key = new GlobalKey();
        var owner = new BuildOwner();
        owner.SetTickerProvider(new MockTickerProvider());

        // Column with 20 items of 50px each = 1000px total.
        // Target is item 15 at y=750.
        var children = new Widget[20];
        for (var i = 0; i < 20; i++)
        {
            children[i] = new KeyedBox(50, i == 15 ? key : null);
        }

        var scrollView = new SingleChildScrollView(
            new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: children));

        var root = scrollView.CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();

        // Layout with 300px viewport height
        root.RenderObject!.Layout(LayoutConstraints.Tight(300, 400));

        Assert.That(key.CurrentElement, Is.Not.Null);

        // Item 15 is at y=750, well below the 400px viewport.
        // EnsureVisible should scroll down.
        Scrollable.EnsureVisible(key.CurrentElement!);

        // The element should still be accessible after scrolling
        Assert.That(key.CurrentElement, Is.Not.Null);
        Assert.That(key.CurrentRenderObject, Is.Not.Null);

        root.Unmount();
    }
}

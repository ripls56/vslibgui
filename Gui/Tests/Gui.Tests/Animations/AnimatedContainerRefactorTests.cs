using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedContainerRefactorTests
{
    private (StatefulElement element, BuildOwner owner, MockTickerProvider vsync)
        Mount(BoxStyle style, TimeSpan duration)
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var widget = new AnimatedContainer(
            style, duration);
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner, vsync);
    }

    private RenderObject? FindContainerRenderBox(Element element)
    {
        RenderObject? found = null;

        void Visit(Element el)
        {
            if (found != null)
            {
                return;
            }

            if (el.RenderObject is RenderBox)
            {
                found = el.RenderObject;
            }

            el.VisitChildren(Visit);
        }

        Visit(element);
        return found;
    }

    [Test]
    public void StyleChange_AnimatesColor()
    {
        var redStyle = new BoxStyle { Color = new Vector4(1, 0, 0, 1) };
        var blueStyle = new BoxStyle { Color = new Vector4(0, 0, 1, 1) };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount(redStyle, duration);

        // Update to blue style
        element.Update(new AnimatedContainer(
            blueStyle, duration));
        owner.BuildDirtyElements();

        // Advance halfway
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = FindContainerRenderBox(element);
        Assert.That(ro, Is.Not.Null);
    }

    [Test]
    public void SameStyle_DoesNotAnimate()
    {
        var style = new BoxStyle { Color = new Vector4(1, 0, 0, 1) };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount(style, duration);

        // Update with same style
        element.Update(new AnimatedContainer(
            new BoxStyle { Color = new Vector4(1, 0, 0, 1) },
            duration));
        owner.BuildDirtyElements();

        // Advance time — should not crash
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = FindContainerRenderBox(element);
        Assert.That(ro, Is.Not.Null);
    }

    [Test]
    public void StyleChange_AnimatesBoxShadow()
    {
        var startStyle = new BoxStyle
        {
            BoxShadows = new[]
            {
                new BoxShadow
                (
                    new Vector4(0, 0, 0, 1),
                    new Vector2(5, 5),
                    5
                )
            }
        };
        var endStyle = new BoxStyle
        {
            BoxShadows = new[]
            {
                new BoxShadow
                (
                    new Vector4(0, 0, 0, 1),
                    new Vector2(15, 15),
                    15
                )
            }
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount(startStyle, duration);

        // Update to end style
        element.Update(new AnimatedContainer(endStyle, duration));
        owner.BuildDirtyElements();

        // Advance halfway
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = (RenderBox)FindContainerRenderBox(element)!;
        Assert.That(ro.Shadows, Is.Not.Null);
        Assert.That(ro.Shadows!.Length, Is.EqualTo(1));

        // Expected offset halfway: (10, 10)
        Assert.That(ro.Shadows[0].Offset.X, Is.EqualTo(10).Within(0.01));
        Assert.That(ro.Shadows[0].Offset.Y, Is.EqualTo(10).Within(0.01));
        Assert.That(ro.Shadows[0].BlurRadius, Is.EqualTo(10).Within(0.01));
    }

    [Test]
    public void StyleChange_AnimatesBoxShadowListLength()
    {
        var startStyle = new BoxStyle { BoxShadows = null };
        var endStyle = new BoxStyle
        {
            BoxShadows = new[]
            {
                new BoxShadow
                (
                    new Vector4(0, 0, 0, 1),
                    new Vector2(10, 10),
                    10
                )
            }
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount(startStyle, duration);

        element.Update(new AnimatedContainer(endStyle, duration));
        owner.BuildDirtyElements();

        // Advance halfway
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = (RenderBox)FindContainerRenderBox(element)!;
        Assert.That(ro.Shadows, Is.Not.Null);
        Assert.That(ro.Shadows!.Length, Is.EqualTo(1));

        // Expected alpha halfway: 0.5
        Assert.That(ro.Shadows[0].Color.W, Is.EqualTo(0.5f).Within(0.01));
        // Offset and blur should also be halfway if we lerp from "transparent version of end"
        Assert.That(ro.Shadows[0].Offset.X, Is.EqualTo(5).Within(0.01));
        Assert.That(ro.Shadows[0].BlurRadius, Is.EqualTo(5).Within(0.01));
    }
}

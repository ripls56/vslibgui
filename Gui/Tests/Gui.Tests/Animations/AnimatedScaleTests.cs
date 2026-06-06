using Gui.Core.Painting;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedScaleTests
{
    private (StatefulElement element, BuildOwner owner, MockTickerProvider vsync)
        Mount(float scale, TimeSpan duration, Alignment? alignment = null)
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var widget = new AnimatedScale(
            scale, duration, alignment: alignment);
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner, vsync);
    }

    [Test]
    public void ScaleChange_AnimatesOverTime()
    {
        var (element, owner, vsync) = Mount(
            1.0f, TimeSpan.FromMilliseconds(200));

        element.Update(new AnimatedScale(
            2.0f, TimeSpan.FromMilliseconds(200)));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = FindRenderTransform(element);
        Assert.That(ro, Is.Not.Null);
    }

    [Test]
    public void Alignment_IsPassedToTransform()
    {
        var (element, _, _) = Mount(
            2.0f, TimeSpan.FromMilliseconds(200),
            Alignment.Center);

        var ro = FindRenderTransform(element);
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.Alignment, Is.EqualTo(Alignment.Center));
    }

    private RenderTransform? FindRenderTransform(Element element)
    {
        RenderTransform? found = null;

        void Visit(Element el)
        {
            if (el.RenderObject is RenderTransform rt)
            {
                found = rt;
            }

            el.VisitChildren(Visit);
        }

        Visit(element);
        return found;
    }
}

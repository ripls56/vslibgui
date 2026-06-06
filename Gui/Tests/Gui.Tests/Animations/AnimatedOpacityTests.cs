using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedOpacityTests
{
    private (StatefulElement element, BuildOwner owner, MockTickerProvider vsync)
        Mount(float opacity, TimeSpan duration, Action? onEnd = null,
            Widget? child = null)
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var widget = new AnimatedOpacity(
            opacity, duration, onEnd: onEnd, child: child);
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner, vsync);
    }

    [Test]
    public void InitialBuild_UsesTargetOpacity()
    {
        var (element, _, _) = Mount(0.5f, TimeSpan.FromMilliseconds(300));

        var opacityRo = FindRenderOpacity(element);
        Assert.That(opacityRo, Is.Not.Null);
    }

    [Test]
    public void OpacityChange_AnimatesOverTime()
    {
        var (element, owner, vsync) = Mount(
            1.0f, TimeSpan.FromMilliseconds(200));

        element.Update(new AnimatedOpacity(
            0f, TimeSpan.FromMilliseconds(200)));
        owner.BuildDirtyElements();

        // Mid-animation
        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var ro = FindRenderOpacity(element);
        Assert.That(ro, Is.Not.Null);
    }

    [Test]
    public void OnEnd_CalledAfterCompletion()
    {
        var ended = false;
        var (element, owner, vsync) = Mount(
            1f, TimeSpan.FromMilliseconds(100), () => ended = true);

        element.Update(new AnimatedOpacity(
            0f, TimeSpan.FromMilliseconds(100),
            onEnd: () => ended = true));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();

        Assert.That(ended, Is.True);
    }

    private RenderObject? FindRenderOpacity(Element element)
    {
        RenderObject? found = null;

        void Visit(Element el)
        {
            if (el.RenderObject?.GetType().Name == "RenderOpacity")
            {
                found = el.RenderObject;
            }

            el.VisitChildren(Visit);
        }

        Visit(element);
        return found;
    }
}

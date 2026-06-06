using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedPaddingTests
{
    private (StatefulElement element, BuildOwner owner, MockTickerProvider vsync)
        Mount(EdgeInsets padding, TimeSpan duration, Action? onEnd = null)
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var widget = new AnimatedPadding(
            padding, duration, onEnd: onEnd);
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner, vsync);
    }

    [Test]
    public void PaddingChange_AnimatesOverTime()
    {
        var (element, owner, vsync) = Mount(
            EdgeInsets.All(0), TimeSpan.FromMilliseconds(200));

        element.Update(new AnimatedPadding(
            EdgeInsets.All(20),
            TimeSpan.FromMilliseconds(200)));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        RenderObject? found = null;

        void Visit(Element el)
        {
            if (el.RenderObject is RenderPadding)
            {
                found = el.RenderObject;
            }

            el.VisitChildren(Visit);
        }

        Visit(element);
        Assert.That(found, Is.Not.Null);
    }

    [Test]
    public void OnEnd_CalledAfterCompletion()
    {
        var ended = false;
        var (element, owner, vsync) = Mount(
            EdgeInsets.All(0), TimeSpan.FromMilliseconds(100),
            () => ended = true);

        element.Update(new AnimatedPadding(
            EdgeInsets.All(20),
            TimeSpan.FromMilliseconds(100),
            onEnd: () => ended = true));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(200));
        owner.BuildDirtyElements();

        Assert.That(ended, Is.True);
    }
}

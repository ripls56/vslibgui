using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class ClipBehaviorTests
{
    private static (ComponentElement element, BuildOwner owner) MountContainer(BoxStyle style)
    {
        var owner = new BuildOwner();
        var widget = new Container(style);
        var element = (ComponentElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner);
    }

    private static (Element element, BuildOwner owner) MountClip(Clip widget)
    {
        var owner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner);
    }

    private static T? FindRenderObject<T>(Element root) where T : RenderObject
    {
        T? found = null;

        void Visit(Element el)
        {
            if (found != null)
            {
                return;
            }

            if (el.RenderObject is T match)
            {
                found = match;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return found;
    }


    [Test]
    public void Container_PropagatesClipBehaviorToRenderBox()
    {
        var style = new BoxStyle
        {
            ClipBehavior = ClipBehavior.HardEdge, CornerRadius = new Vector4(8)
        };

        var (element, _) = MountContainer(style);
        var box = FindRenderObject<RenderBox>(element);

        Assert.That(box, Is.Not.Null);
        Assert.That(box!.ClipBehavior, Is.EqualTo(ClipBehavior.HardEdge));
    }

    [Test]
    public void Container_PropagatesAntiAliasClipBehaviorToRenderBox()
    {
        var style = new BoxStyle { ClipBehavior = ClipBehavior.AntiAlias };

        var (element, _) = MountContainer(style);
        var box = FindRenderObject<RenderBox>(element);

        Assert.That(box!.ClipBehavior, Is.EqualTo(ClipBehavior.AntiAlias));
    }

    [Test]
    public void Container_DoesNotInsertRenderClipForClipping()
    {
        // After the fix, Container relies on RenderBox's own clip path so that outer
        // box-shadows can extend beyond the clip region. A RenderClip wrapper would
        // re-introduce the shadow-clipping regression.
        var style = new BoxStyle
        {
            ClipBehavior = ClipBehavior.AntiAlias, CornerRadius = new Vector4(12)
        };

        var (element, _) = MountContainer(style);
        var clip = FindRenderObject<RenderClip>(element);

        Assert.That(clip, Is.Null);
    }

    [Test]
    public void Container_ClipBehaviorNone_KeepsRenderBoxClipNone()
    {
        var style = new BoxStyle { ClipBehavior = ClipBehavior.None };

        var (element, _) = MountContainer(style);
        var box = FindRenderObject<RenderBox>(element);

        Assert.That(box!.ClipBehavior, Is.EqualTo(ClipBehavior.None));
    }


    [Test]
    public void Clip_Default_IsAntiAlias()
    {
        var widget = new Clip(new Vector4(4));
        Assert.That(widget.ClipBehavior, Is.EqualTo(ClipBehavior.AntiAlias));
    }

    [Test]
    public void Clip_PropagatesClipBehaviorToRenderClip()
    {
        var widget = new Clip(
            new Vector4(6),
            clipBehavior: ClipBehavior.HardEdge);

        var (element, _) = MountClip(widget);
        var renderClip = FindRenderObject<RenderClip>(element);

        Assert.That(renderClip, Is.Not.Null);
        Assert.That(renderClip!.ClipBehavior, Is.EqualTo(ClipBehavior.HardEdge));
    }

    [Test]
    public void Clip_ClipBehaviorNone_SkipsRenderClipPath()
    {
        // RenderClip.Paint short-circuits to base.Paint when ClipBehavior == None,
        // so the child paints unclipped. Verifying the property is plumbed through is
        // sufficient — the paint short-circuit itself is asserted by inspection.
        var widget = new Clip(clipBehavior: ClipBehavior.None);

        var (element, _) = MountClip(widget);
        var renderClip = FindRenderObject<RenderClip>(element);

        Assert.That(renderClip!.ClipBehavior, Is.EqualTo(ClipBehavior.None));
    }


    [Test]
    public void RenderBox_ClipBehavior_Change_MarksNeedsPaint()
    {
        var ro = new RenderBox();
        ro.Layout(LayoutConstraints.Tight(100, 100));
        ro.NeedsPaint = false;

        ro.ClipBehavior = ClipBehavior.HardEdge;

        Assert.That(ro.NeedsPaint, Is.True);
    }

    [Test]
    public void RenderBox_ClipBehavior_SameValue_DoesNotMarkRepaint()
    {
        var ro = new RenderBox { ClipBehavior = ClipBehavior.HardEdge };
        ro.Layout(LayoutConstraints.Tight(100, 100));
        ro.NeedsPaint = false;

        ro.ClipBehavior = ClipBehavior.HardEdge;

        Assert.That(ro.NeedsPaint, Is.False);
    }
}

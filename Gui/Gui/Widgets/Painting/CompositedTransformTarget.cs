using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Painting;

/// <summary>
///     Marks its child as the anchor for a <see cref="CompositedTransformFollower" />.
///     The follower reads this widget's render object position during layout so that
///     overlay content tracks the target even when the target scrolls.
///     <code>
/// var link = new LayerLink();
/// 
/// // In the main tree (inside a scroll view, etc.):
/// new CompositedTransformTarget(link: link, child: triggerWidget)
/// 
/// // In the overlay entry:
/// new CompositedTransformFollower(link: link, child: tooltipContent)
/// </code>
/// </summary>
public class CompositedTransformTarget : SingleChildWidget
{
    public CompositedTransformTarget(
        LayerLink link,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        Link = link;
    }

    public LayerLink Link { get; }

    public override RenderObject CreateRenderObject()
    {
        var ro = new RenderTarget();
        ro.Link = Link;
        return ro;
    }

    public override void UpdateRenderObject(
        RenderObject renderObject
    ) =>
        ((RenderTarget)renderObject).Link = Link;
}

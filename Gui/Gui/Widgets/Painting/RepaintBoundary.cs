using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Painting;

/// <summary>
///     Isolates <paramref name="child" /> into its own composited paint layer. When
///     anything inside repaints, only this layer is re-recorded — the enclosing layers,
///     up to and including the window root, are replayed as-is.
///     <para>
///         Worth wrapping: subtrees that redraw on their own schedule while their
///         surroundings stay still — hover/press feedback, animations, list items,
///         overlays. Not worth wrapping: static content (a layer that never goes dirty
///         only adds an indirection), or a subtree that always repaints together with
///         its parent.
///     </para>
/// </summary>
public class RepaintBoundary : SingleChildWidget
{
    public RepaintBoundary
    (
        Widget child
    ) : base(child)
    {
    }

    public override RenderObject CreateRenderObject() => new RenderRepaintBoundary();
}

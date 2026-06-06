using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;

namespace Gui.Widgets.Painting;

/// <summary>
///     Positions its child to follow a <see cref="CompositedTransformTarget" />
///     linked via a shared <see cref="LayerLink" />. The position is recomputed
///     every frame during <b>paint</b>, so it correctly tracks targets inside
///     scrollable containers (where scroll only triggers repaint, not relayout).
///     <para>
///         Must be placed inside a <see cref="Stack" /> (typically the
///         <see cref="Overlay" />'s root stack). It writes to
///         <see cref="StackParentData" /> like <see cref="Positioned" />.
///     </para>
///     <code>
/// // In the overlay entry:
/// new CompositedTransformFollower(
///   link: link,
///   offset: new Vector2(0, -8), // 8px above the target
///   child: tooltipContent
/// )
/// </code>
/// </summary>
public class CompositedTransformFollower : SingleChildWidget
{
    public CompositedTransformFollower(
        LayerLink link,
        Vector2? offset = null,
        Widget? child = null,
        bool showAbove = false,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        Link = link;
        Offset = offset ?? Vector2.Zero;
        ShowAbove = showAbove;
    }

    /// <summary>Link to the target whose position we follow.</summary>
    public LayerLink Link { get; }

    /// <summary>
    ///     Offset from the target's top-left corner in global coordinates.
    ///     Default is (0, 0) — the follower's top-left aligns with the target's.
    /// </summary>
    public Vector2 Offset { get; }

    /// <summary>
    ///     When true the follower's bottom edge aligns with the target's top edge
    ///     so the child appears above the target. Hit-test bounds reflect this position.
    /// </summary>
    public bool ShowAbove { get; }

    public override RenderObject CreateRenderObject()
    {
        var ro = new RenderFollower();
        ro.Link = Link;
        ro.Offset = Offset;
        ro.ShowAbove = ShowAbove;
        ro.SyncParentData();
        return ro;
    }

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderFollower)renderObject;
        ro.Link = Link;
        ro.Offset = Offset;
        ro.ShowAbove = ShowAbove;
        ro.SyncParentData();
    }
}

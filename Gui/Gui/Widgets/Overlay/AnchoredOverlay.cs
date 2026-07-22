using Gui.Core.Framework;
using Gui.Core.Overlay;
using Gui.Core.Painting;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Overlay;

/// <summary>Preferred vertical side for an <see cref="AnchoredOverlay" />.</summary>
public enum OverlaySide
{
    /// <summary>Above the trigger.</summary>
    Top,

    /// <summary>Below the trigger.</summary>
    Bottom
}

/// <summary>Horizontal alignment of the overlay relative to its anchor X.</summary>
public enum HorizontalAnchor
{
    /// <summary>The overlay's left edge aligns with the anchor X.</summary>
    Start,

    /// <summary>The overlay centers on the anchor X (default).</summary>
    Center
}

/// <summary>
///     Positions its child relative to an anchor point, flipping between the top
///     and bottom of a trigger and clamping on both axes so the child stays within
///     the overlay bounds. Placement accounts for the child's actual size, so it
///     only flips when there genuinely is not enough room on the preferred side.
///     <para>
///         Internally behaves like a smart follower that tracks a
///         <see cref="CompositedTransformTarget" /> linked via <see cref="LayerLink" />.
///         <b>Do not</b> wrap this in a <c>CompositedTransformFollower</c>.
///     </para>
/// </summary>
public sealed class AnchoredOverlay : SingleChildWidget
{
    public AnchoredOverlay(
        float anchorX,
        float triggerGlobalY,
        float triggerHeight,
        float verticalGap = 8f,
        OverlaySide preferredSide = OverlaySide.Top,
        HorizontalAnchor horizontal = HorizontalAnchor.Center,
        LayerLink? link = null,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        AnchorX = anchorX;
        TriggerGlobalY = triggerGlobalY;
        TriggerHeight = triggerHeight;
        VerticalGap = verticalGap;
        PreferredSide = preferredSide;
        Horizontal = horizontal;
        Link = link;
    }

    /// <summary>
    ///     Anchor X in overlay coordinates (the point the child centers on or
    ///     aligns its left edge to). Used to clamp the child within the overlay's
    ///     horizontal bounds.
    /// </summary>
    public float AnchorX { get; }

    /// <summary>Global Y of the trigger's top edge, in overlay coordinates.</summary>
    public float TriggerGlobalY { get; }

    /// <summary>Height of the trigger widget.</summary>
    public float TriggerHeight { get; }

    /// <summary>Pixel gap kept between the trigger and the overlay content.</summary>
    public float VerticalGap { get; }

    /// <summary>Side preferred when there is room on both sides.</summary>
    public OverlaySide PreferredSide { get; }

    /// <summary>How the child aligns horizontally to <see cref="AnchorX" />.</summary>
    public HorizontalAnchor Horizontal { get; }

    /// <summary>Optional link to track a trigger widget's position dynamically.</summary>
    public LayerLink? Link { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        var ro = new RenderAnchoredOverlay
        {
            AnchorX = AnchorX,
            TriggerGlobalY = TriggerGlobalY,
            TriggerHeight = TriggerHeight,
            VerticalGap = VerticalGap,
            PreferredSide = PreferredSide,
            Horizontal = Horizontal,
            Link = Link
        };
        ro.SyncParentData();
        return ro;
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderAnchoredOverlay)renderObject;
        ro.AnchorX = AnchorX;
        ro.TriggerGlobalY = TriggerGlobalY;
        ro.TriggerHeight = TriggerHeight;
        ro.VerticalGap = VerticalGap;
        ro.PreferredSide = PreferredSide;
        ro.Horizontal = Horizontal;
        ro.Link = Link;
        ro.SyncParentData();
    }
}

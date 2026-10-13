using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;

namespace Gui.Core.Painting;

/// <summary>
///     Render object that registers itself as the target in a <see cref="LayerLink" />.
///     Used by <c>CompositedTransformTarget</c> to publish its position for followers.
/// </summary>
internal class RenderTarget : RenderProxyBox
{
    private Vector2? _lastPosition;
    private LayerLink? _link;

    public LayerLink? Link
    {
        get => _link;
        set
        {
            if (_link != null)
            {
                _link.Target = null;
            }

            _link = value;
            if (_link != null)
            {
                _link.Target = this;
            }

            // Force a fresh NotifyMoved on next paint for the new link.
            _lastPosition = null;
        }
    }

    public override void Paint
    (
        PaintingContext context
    )
    {
        base.Paint(context);

        // The owning window only repaints on scroll/animation, so pinging here
        // nudges followers in other windows exactly when our position may have
        // changed — keeping their repaint event-driven instead of per-frame.
        // Only notify when the global position actually changed, otherwise every
        // paint force-invalidates repaint boundaries in the follower's subtree.
        var position = LocalToGlobal(Vector2.Zero) + GetScreenOffset();
        if (_lastPosition is { } last && last == position)
        {
            return;
        }

        _lastPosition = position;
        _link?.NotifyMoved();
    }

    public override void Dispose()
    {
        if (_link != null)
        {
            _link.Target = null;
        }

        base.Dispose();
    }
}

using Gui.Core.Framework;
using Gui.Rendering;

namespace Gui.Core.Painting;

/// <summary>
///     Render object that registers itself as the target in a <see cref="LayerLink" />.
///     Used by <c>CompositedTransformTarget</c> to publish its position for followers.
/// </summary>
internal class RenderTarget : RenderProxyBox
{
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
        }
    }

    public override void Paint(
        PaintingContext context
    )
    {
        base.Paint(context);

        // The owning window only repaints on scroll/animation, so pinging here
        // nudges followers in other windows exactly when our position may have
        // changed — keeping their repaint event-driven instead of per-frame.
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

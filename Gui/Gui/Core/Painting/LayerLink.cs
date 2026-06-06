using Gui.Core.Framework;

namespace Gui.Core.Painting;

/// <summary>
///     A mutable link between a <see cref="RenderObject" /> target and a follower.
///     The target writes its render object during mount; the follower reads the
///     target's global position during layout so that it always tracks correctly —
///     even inside scrollable containers.
/// </summary>
public class LayerLink
{
    internal RenderObject? Target { get; set; }
}

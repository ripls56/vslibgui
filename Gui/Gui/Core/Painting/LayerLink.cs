using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Core.Painting;

/// <summary>
///     A mutable link between a <see cref="RenderObject" /> target and a follower.
///     The target writes its render object during mount; the follower reads the
///     target's global position during layout so that it always tracks correctly —
///     even inside scrollable containers. A <see cref="ChangeNotifier" />: the
///     target calls <see cref="NotifyMoved" /> when it repaints, so a follower
///     living in a different window can repaint and keep tracking it through scrolls.
/// </summary>
public class LayerLink : ChangeNotifier
{
    internal RenderObject? Target { get; set; }

    /// <summary>Raises <see cref="ChangeNotifier.NotifyListeners" />.</summary>
    public void NotifyMoved() => NotifyListeners();
}

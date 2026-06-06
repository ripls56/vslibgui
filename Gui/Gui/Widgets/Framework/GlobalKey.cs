using Gui.Core.Framework;
using Gui.Widgets.Scroll;

namespace Gui.Widgets.Framework;

/// <summary>
///     A key that is unique across the entire widget tree owned by a single
///     <see cref="BuildOwner" />. Unlike <see cref="ValueKey{T}" />, a
///     <c>GlobalKey</c> provides access to the <see cref="Element" /> and
///     <see cref="RenderObject" /> associated with the widget it is
///     attached to, enabling cross-subtree operations like
///     <see cref="Scrollable.EnsureVisible" />.
///     <para>
///         Allocate once (e.g. in <see cref="State.InitState" />) and reuse across
///         rebuilds. Never create a new <c>GlobalKey</c> inside <c>Build()</c>.
///     </para>
/// </summary>
public class GlobalKey : Key
{
    public Element? CurrentElement { get; internal set; }

    public RenderObject? CurrentRenderObject => CurrentElement?.RenderObject;

    /// <summary>
    ///     Returns the <see cref="State" /> of the element's widget, cast to
    ///     <typeparamref name="TState" />. Returns <c>null</c> if the element is
    ///     not a <see cref="StatefulElement" /> or the state is not of the
    ///     requested type.
    /// </summary>
    public TState? CurrentState<TState>() where TState : State =>
        (CurrentElement as StatefulElement)?.State as TState;
}

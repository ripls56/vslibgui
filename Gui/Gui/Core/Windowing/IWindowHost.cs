namespace Gui.Core.Windowing;

/// <summary>
///     The real window that hosts a widget tree. <see cref="Gui.GuiBase" /> implements this
///     interface and registers itself on the <see cref="Gui.Widgets.Framework.BuildOwner" />,
///     so a widget deep inside the tree can change the window rectangle instead of only its
///     own painted size.
///     <para>
///         A widget needs this when its visible content becomes smaller than the window: the
///         VS engine hit-tests dialogs through <c>GuiComposer.Bounds</c>, and those bounds
///         follow the window rectangle. A widget that only collapses its own subtree leaves an
///         invisible rectangle behind that still swallows clicks aimed at the windows below.
///     </para>
/// </summary>
public interface IWindowHost
{
    /// <summary>
    ///     <c>true</c> while the host window is collapsed to a title-bar-only rectangle.
    /// </summary>
    bool IsHostCollapsed { get; }

    /// <summary>
    ///     Collapses or expands the host window.
    ///     <para>
    ///         <paramref name="owner" /> identifies the widget state that made the request. The
    ///         host honours one owner at a time: a request from a different owner is ignored
    ///         while another owner holds the collapse, so a title bar that is removed from the
    ///         tree cannot expand a window that a different title bar collapsed.
    ///     </para>
    /// </summary>
    /// <param name="owner">The requesting object, usually the widget's <c>State</c>.</param>
    /// <param name="collapsed"><c>true</c> to collapse the window to the title bar.</param>
    /// <param name="collapsedHeight">
    ///     Window height in logical pixels to use while collapsed. Ignored on expand.
    /// </param>
    void SetHostCollapsed
    (
        object owner,
        bool collapsed,
        float collapsedHeight
    );
}

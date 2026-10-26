using System;
using OpenTK.Mathematics;

namespace Gui.Core.Windowing;

/// <summary>
///     Pure bookkeeping for a window that is collapsed to its title bar.
///     <para>
///         While collapsed the window rectangle is exactly the bar: the width is frozen at the
///         value it had when the collapse started and the height is locked to
///         <see cref="CollapsedHeight" />. One owner holds the collapse at a time; requests from
///         any other owner are ignored until the owner expands the window again.
///     </para>
/// </summary>
internal sealed class WindowCollapseState
{
    private float _frozenWidth;
    private object? _owner;

    /// <summary><c>true</c> while the window is collapsed to the title bar.</summary>
    public bool IsCollapsed { get; private set; }

    /// <summary>Window height in logical pixels while collapsed.</summary>
    public float CollapsedHeight { get; private set; }

    /// <summary>Window size recorded at the moment the window was collapsed.</summary>
    public Vector2 ExpandedSize { get; private set; }

    /// <summary>
    ///     Collapses or expands the window on behalf of <paramref name="owner" />.
    /// </summary>
    /// <param name="owner">The requesting object; only one owner may hold the collapse.</param>
    /// <param name="collapsed"><c>true</c> to collapse, <c>false</c> to expand.</param>
    /// <param name="collapsedHeight">Bar height to use while collapsed; ignored on expand.</param>
    /// <param name="currentSize">The window size right now, recorded on collapse.</param>
    /// <returns><c>true</c> only when the state actually changed.</returns>
    public bool SetCollapsed
    (
        object owner,
        bool collapsed,
        float collapsedHeight,
        Vector2 currentSize
    )
    {
        if (collapsed)
        {
            if (IsCollapsed)
            {
                return false;
            }

            _owner = owner;
            IsCollapsed = true;
            CollapsedHeight = collapsedHeight;
            ExpandedSize = currentSize;
            _frozenWidth = currentSize.X;
            return true;
        }

        if (!IsCollapsed || !ReferenceEquals(_owner, owner))
        {
            return false;
        }

        _owner = null;
        IsCollapsed = false;
        return true;
    }

    /// <summary>
    ///     Applies the height lock to a size: while collapsed returns
    ///     <c>(size.X, CollapsedHeight)</c>, otherwise <paramref name="size" /> unchanged.
    /// </summary>
    public Vector2 Apply
    (
        Vector2 size
    ) =>
        IsCollapsed
            ? new Vector2(size.X, CollapsedHeight)
            : size;

    /// <summary>
    ///     Removes the vertical resize edges while collapsed: the width stays resizable, the
    ///     height is locked to the bar.
    /// </summary>
    public ResizeEdge FilterEdges
    (
        ResizeEdge edge
    ) =>
        IsCollapsed
            ? edge & ~(ResizeEdge.Top | ResizeEdge.Bottom)
            : edge;

    /// <summary>
    ///     Minimum window height to clamp against. While collapsed the bar height wins over a
    ///     larger configured minimum, so the default 50 px minimum cannot stretch a 28 px bar.
    /// </summary>
    public float EffectiveMinHeight
    (
        float configMinHeight
    ) =>
        IsCollapsed
            ? Math.Min(configMinHeight, CollapsedHeight)
            : configMinHeight;

    /// <summary>
    ///     Returns the tight layout size <c>(frozenWidth, CollapsedHeight)</c> while collapsed.
    ///     The size is tight for a fixed-size window and for a shrink-wrap window alike, so a
    ///     collapsed shrink-wrap window does not re-measure its content.
    /// </summary>
    /// <param name="size">The collapsed size, or <see cref="Vector2.Zero" /> when expanded.</param>
    /// <returns><c>true</c> while collapsed.</returns>
    public bool TryGetCollapsedSize
    (
        out Vector2 size
    )
    {
        if (!IsCollapsed)
        {
            size = Vector2.Zero;
            return false;
        }

        size = new Vector2(_frozenWidth, CollapsedHeight);
        return true;
    }

    /// <summary>
    ///     Updates the frozen width after the user resizes a collapsed window horizontally.
    /// </summary>
    public void UpdateFrozenWidth
    (
        float width
    )
    {
        if (IsCollapsed)
        {
            _frozenWidth = width;
        }
    }
}

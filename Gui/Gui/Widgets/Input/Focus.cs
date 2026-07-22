using System.Collections.Generic;
using Gui.Widgets.Framework;
using Gui.Widgets.Scroll;

namespace Gui.Widgets.Input;

/// <summary>
/// Manages focus state for a single focusable node in the widget tree.
/// Extends <see cref="ChangeNotifier"/> so listeners are notified when focus changes.
/// </summary>
public class FocusNode : ChangeNotifier
{
    private FocusManager? _manager;

    private bool _showsFocusIndicator;

    /// <summary>The element that owns this focus node.</summary>
    public Element? Owner { get; set; }

    /// <summary>Whether this node currently holds primary focus.</summary>
    public bool HasFocus { get; private set; }

    /// <summary>When false, this node is excluded from focus traversal and cannot receive focus via Tab.</summary>
    public bool CanRequestFocus { get; set; } = true;

    /// <summary>When true, Tab traversal skips this node, but it can still be focused directly.</summary>
    public bool SkipTraversal { get; set; } = false;

    /// <summary>Optional explicit traversal order override. When null, document (DFS) order is used.</summary>
    public float? TraversalOrder { get; set; }

    /// <summary>
    /// Whether the focus indicator (outline) should be drawn for this node. True only when
    /// focus was reached via keyboard traversal; pointer or programmatic focus leaves it
    /// false, so the element still receives input but shows no outline (:focus-visible).
    /// </summary>
    public bool ShowsFocusIndicator
    {
        get => _showsFocusIndicator;
        set
        {
            if (_showsFocusIndicator == value)
            {
                return;
            }

            _showsFocusIndicator = value;
            NotifyListeners();
        }
    }

    internal void SetHasFocus
    (
        bool value,
        FocusManager? manager
    )
    {
        if (HasFocus == value)
        {
            return;
        }

        HasFocus = value;
        _manager = value
            ? manager
            : null;
        if (!value)
        {
            ShowsFocusIndicator = false;
        }
        NotifyListeners();
    }

    /// <summary>Requests primary focus for this node via its owning <see cref="FocusManager"/>.</summary>
    public void RequestFocus()
    {
        var manager = _manager ?? Owner?.Owner?.FocusManager;
        if (manager != null)
        {
            manager.RequestFocus(this);
        }
        else
        {
            SetHasFocus
            (
                true,
                null
            );
        }
    }

    /// <summary>Removes focus from this node.</summary>
    public void Unfocus()
    {
        var manager = _manager ?? Owner?.Owner?.FocusManager;
        if (HasFocus && manager != null)
        {
            manager.RequestFocus(null);
        }
        else
        {
            SetHasFocus
            (
                false,
                null
            );
        }
    }

    /// <summary>
    /// Resolves the <see cref="FocusNode"/> owned by <paramref name="element"/>, checking
    /// both the widget (when it implements <see cref="IFocusable"/>) and the state of a
    /// <see cref="StatefulElement"/>. Returns null when neither exposes a node.
    /// </summary>
    public static FocusNode? Resolve
    (
        Element element
    )
    {
        if (element.Widget is IFocusable widget && widget.FocusNode is { } widgetNode)
        {
            return widgetNode;
        }

        if (element is StatefulElement stateful && stateful.State is IFocusable statefulFocus
                                                && statefulFocus.FocusNode is { } stateNode)
        {
            return stateNode;
        }

        return null;
    }
}

/// <summary>
/// A <see cref="FocusNode"/> that marks a scope boundary for focus traversal.
/// Tab navigation is contained within the scope holding the currently focused node.
/// </summary>
public class FocusScopeNode : FocusNode
{
}

/// <summary>
/// Manages which <see cref="FocusNode"/> holds primary focus and drives Tab traversal.
/// </summary>
public class FocusManager
{
    /// <summary>The node that currently holds primary focus, or null if nothing is focused.</summary>
    public FocusNode? PrimaryFocus { get; private set; }

    /// <summary>The policy used by <see cref="FocusNext"/> and <see cref="FocusPrevious"/> to order focusable nodes.</summary>
    public FocusTraversalPolicy TraversalPolicy { get; set; } = new ReadingOrderTraversalPolicy();

    /// <summary>
    /// Grants primary focus without showing the focus indicator (pointer/programmatic
    /// focus). Equivalent to <see cref="RequestFocus(FocusNode?, bool)" /> with
    /// <c>showIndicator: false</c>.
    /// </summary>
    public void RequestFocus
    (
        FocusNode? node
    ) =>
        RequestFocus
        (
            node,
            showIndicator: false
        );

    /// <summary>
    /// Grants primary focus to <paramref name="node" /> and controls whether the focus
    /// indicator is shown. Pass <paramref name="showIndicator" /> true for keyboard
    /// traversal, false for pointer or programmatic focus.
    /// </summary>
    public void RequestFocus
    (
        FocusNode? node,
        bool showIndicator
    )
    {
        if (PrimaryFocus == node)
        {
            if (node != null)
            {
                node.ShowsFocusIndicator = showIndicator;
            }

            return;
        }

        var oldFocus = PrimaryFocus;
        PrimaryFocus = node;

        oldFocus?.SetHasFocus
        (
            false,
            null
        );

        if (node != null)
        {
            node.ShowsFocusIndicator = showIndicator;
            node.SetHasFocus
            (
                true,
                this
            );
        }

        // Bring the newly focused node into view (no-op when it has no scrollable ancestor).
        if (node?.Owner is { } focused)
        {
            Scrollable.EnsureVisible(focused);
        }
    }

    /// <summary>Moves focus to the next traversable node within <paramref name="root"/>'s element subtree.</summary>
    public void FocusNext(Element root)
    {
        var nodes = TraversalPolicy.GetTraversalOrder(root, this);
        MoveFocus(nodes, forward: true);
    }

    /// <summary>Moves focus to the previous traversable node within <paramref name="root"/>'s element subtree.</summary>
    public void FocusPrevious(Element root)
    {
        var nodes = TraversalPolicy.GetTraversalOrder(root, this);
        MoveFocus(nodes, forward: false);
    }

    private void MoveFocus(IReadOnlyList<FocusNode> nodes, bool forward)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        var idx = -1;
        for (var i = 0; i < nodes.Count; i++)
        {
            if (ReferenceEquals(nodes[i], PrimaryFocus))
            {
                idx = i;
                break;
            }
        }

        int next;
        if (idx < 0)
        {
            next = forward ? 0 : nodes.Count - 1;
        }
        else
        {
            next = forward
                ? (idx + 1) % nodes.Count
                : (idx - 1 + nodes.Count) % nodes.Count;
        }

        RequestFocus
        (
            nodes[next],
            showIndicator: true
        );
    }
}

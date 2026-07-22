using System.Collections.Generic;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

/// <summary>Defines the strategy for ordering focusable nodes during Tab traversal.</summary>
public abstract class FocusTraversalPolicy
{
    /// <summary>
    /// Returns the ordered list of traversable <see cref="FocusNode"/> instances within
    /// <paramref name="root"/>'s subtree.
    /// </summary>
    public abstract IReadOnlyList<FocusNode> GetTraversalOrder(Element root, FocusManager manager);
}

/// <summary>
/// Traversal policy that follows document (depth-first) order.
/// Respects <see cref="FocusScopeNode"/> scope boundaries: traversal is limited to the
/// scope containing the currently focused node.
/// </summary>
public sealed class ReadingOrderTraversalPolicy : FocusTraversalPolicy
{
    /// <inheritdoc />
    public override IReadOnlyList<FocusNode> GetTraversalOrder(Element root, FocusManager manager)
    {
        var activeScope = FindScopeAncestor(manager.PrimaryFocus?.Owner);
        var result = new List<FocusNode>();
        CollectNodes(root, activeScope, result, insideActiveScope: activeScope == null);
        result.Sort
        ((a, b) =>
            {
                if (a.TraversalOrder.HasValue && b.TraversalOrder.HasValue)
                {
                    return a.TraversalOrder.Value.CompareTo(b.TraversalOrder.Value);
                }
                if (a.TraversalOrder.HasValue)
                {
                    return -1;
                }
                if (b.TraversalOrder.HasValue)
                {
                    return 1;
                }
                return 0;
            }
        );
        return result;
    }

    private static FocusScopeNode? FindScopeAncestor(Element? element)
    {
        var current = element?.Parent;
        while (current != null)
        {
            var scopeNode = GetScopeNode(current);
            if (scopeNode != null)
            {
                return scopeNode;
            }

            current = current.Parent;
        }

        return null;
    }

    private static FocusScopeNode? GetScopeNode(Element element)
    {
        if (element.Widget is IFocusable f && f.FocusNode is FocusScopeNode sn)
        {
            return sn;
        }

        if (element is StatefulElement se && se.State is FocusScopeState fss)
        {
            return fss.ScopeNode;
        }

        return null;
    }

    private static void CollectNodes
    (
        Element element,
        FocusScopeNode? activeScope,
        List<FocusNode> result,
        bool insideActiveScope = false
    )
    {
        var scopeNode = GetScopeNode(element);
        if (scopeNode != null)
        {
            var enterScope = activeScope != null && scopeNode == activeScope;
            if (!enterScope)
            {
                return;
            }

            element.VisitChildren
                (child => CollectNodes(child, activeScope, result, insideActiveScope: true));
            return;
        }

        var inScope = activeScope == null || insideActiveScope;

        if (inScope
            && FocusNode.Resolve(element) is { } node
            && node.CanRequestFocus
            && !node.SkipTraversal
            && !result.Contains(node))
        {
            result.Add(node);
        }

        element.VisitChildren(child => CollectNodes(child, activeScope, result, insideActiveScope));
    }
}

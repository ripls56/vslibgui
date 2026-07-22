using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

/// <summary>
/// A widget that establishes a <see cref="FocusScopeNode"/> focus scope boundary.
/// Tab traversal is contained within the scope that holds the currently focused node.
/// </summary>
public class FocusScope : StatefulWidget
{
    /// <summary>The child widget rendered inside this scope.</summary>
    public required Widget Child { get; init; }

    /// <summary>
    /// Optional externally-managed scope node. When null an internal node is created automatically.
    /// </summary>
    public FocusScopeNode? FocusNode { get; init; }

    /// <inheritdoc />
    public override State CreateState() => new FocusScopeState();
}

internal sealed class FocusScopeState : State<FocusScope>
{
    private FocusScopeNode? _ownedNode;

    /// <summary>The active scope node for this boundary (external or internally created).</summary>
    internal FocusScopeNode ScopeNode => Widget.FocusNode ?? _ownedNode!;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        if (Widget.FocusNode == null)
        {
            _ownedNode = new FocusScopeNode();
        }

        ScopeNode.Owner = Element;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        ScopeNode.Owner = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => Widget.Child;
}

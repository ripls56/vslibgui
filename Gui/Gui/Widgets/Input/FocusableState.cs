using Gui.Widgets.Framework;

namespace Gui.Widgets.Input;

/// <summary>
///     Base for <see cref="State{T}" /> of interactive widgets that own a
///     <see cref="FocusNode" /> and participate in Tab focus traversal. It creates and
///     disposes an internal node, links it to the owning element, and rebuilds the subtree
///     when focus changes. Deriving states are <see cref="IFocusable" /> through this class,
///     so they are discovered by <see cref="ReadingOrderTraversalPolicy" /> and receive focus
///     on click without each widget duplicating the node lifecycle.
/// </summary>
public abstract class FocusableState<T> : State<T>, IFocusable where T : StatefulWidget
{
    private FocusNode? _focusNode;

    /// <summary>The focus node owned by this state.</summary>
    public FocusNode FocusNode => _focusNode ??= new FocusNode();

    FocusNode? IFocusable.FocusNode => FocusNode;

    public override void InitState()
    {
        base.InitState();
        FocusNode.Owner = Element;
        FocusNode.AddListener(OnFocusChanged);
    }

    /// <summary>
    ///     Invoked when <see cref="FocusNode" /> gains or loses focus. The default
    ///     implementation schedules a rebuild so subclasses can react to
    ///     <see cref="FocusNode.HasFocus" /> in <see cref="State.Build" />.
    /// </summary>
    protected virtual void OnFocusChanged() => SetState(() => { });

    public override void Dispose()
    {
        FocusNode.RemoveListener(OnFocusChanged);
        _focusNode?.Dispose();
        base.Dispose();
    }
}

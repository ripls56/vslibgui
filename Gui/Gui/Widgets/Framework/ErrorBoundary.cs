using System;

namespace Gui.Widgets.Framework;

/// <summary>
///     A widget that catches exceptions thrown by its child subtree during build and
///     replaces the failed subtree with a fallback widget instead of propagating the
///     error up to the root.
/// </summary>
public class ErrorBoundary : StatefulWidget
{
    /// <summary>The child subtree to protect with this error boundary.</summary>
    public required Widget Child { get; init; }

    /// <summary>
    ///     Optional factory that produces a custom fallback widget from the caught exception.
    ///     When <c>null</c>, falls back to <see cref="ErrorWidget.Builder" />.
    /// </summary>
    public Func<Exception, Widget>? FallbackBuilder { get; init; }

    /// <inheritdoc />
    public override State CreateState() => new ErrorBoundaryState();
}

/// <summary>
///     Mutable state for <see cref="ErrorBoundary" />. Holds the error flag and
///     exposes <see cref="MarkError" /> so the framework can signal a caught exception.
/// </summary>
internal class ErrorBoundaryState : State<ErrorBoundary>
{
    private bool _hasError;
    private Exception? _error;

    /// <summary>
    ///     Called by <see cref="BuildOwner" /> when a descendant element throws during rebuild.
    ///     Triggers a <see cref="State.SetState" /> transition to the error display.
    /// </summary>
    internal void MarkError(
        Exception ex
    ) =>
        SetState(() =>
        {
            _hasError = true;
            _error = ex;
        });

    /// <summary>
    ///     Resets the error state so the boundary can attempt to render its child again.
    ///     Call this after the underlying cause has been resolved.
    /// </summary>
    internal void ClearError() =>
        SetState(() =>
        {
            _hasError = false;
            _error = null;
        });

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        if (_hasError && _error != null)
        {
            return Widget.FallbackBuilder?.Invoke(_error)
                ?? ErrorWidget.Builder(_error);
        }

        return Widget.Child;
    }
}

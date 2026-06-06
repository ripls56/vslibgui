using System;

namespace Gui.Shared;

/// <summary>
///     Minimal contract to track and close dialogs.
/// </summary>
public interface IClosableDialog
{
    /// <summary>Raised when the dialog has fully closed.</summary>
    event Action? Closed;

    /// <summary>Closes the dialog. Equivalent to <c>GuiBase.TryClose()</c>.</summary>
    bool TryClose();
}

using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Gui.Shared;

/// <summary>
///     Tracks open <see cref="IClosableDialog" /> instances keyed by <see cref="BlockPos" />.
///     Auto-removes entries when the dialog raises its <see cref="IClosableDialog.Closed" />
///     event. The auto-remove handler is identity-checked, so closing the previous entry on
///     <see cref="Add" /> for an already-occupied position does NOT remove the new entry.
/// </summary>
public sealed class ActiveDialogs
{
    private readonly Dictionary<BlockPos, IClosableDialog> _map = new();

    /// <summary>Number of active dialogs.</summary>
    public int Count => _map.Count;

    /// <summary>Returns true if a dialog is registered for <paramref name="pos" />.</summary>
    public bool Contains(BlockPos pos) => _map.ContainsKey(pos);

    /// <summary>Tries to get the dialog at <paramref name="pos" />.</summary>
    public bool TryGet(BlockPos pos, out IClosableDialog? dlg) => _map.TryGetValue(pos, out dlg);

    /// <summary>
    ///     Adds <paramref name="dlg" /> at <paramref name="pos" />. If a different dialog is
    ///     already there, the previous one is closed via <see cref="IClosableDialog.TryClose" />
    ///     first, then overwritten. Subscribes to <see cref="IClosableDialog.Closed" /> with an
    ///     identity-checked auto-remove handler.
    /// </summary>
    public void Add(BlockPos pos, IClosableDialog dlg)
    {
        if (_map.TryGetValue(pos, out var existing) && !ReferenceEquals(existing, dlg))
        {
            existing.TryClose();
        }

        _map[pos] = dlg;
        dlg.Closed += () =>
        {
            if (_map.TryGetValue(pos, out var current) && ReferenceEquals(current, dlg))
            {
                _map.Remove(pos);
            }
        };
    }
}

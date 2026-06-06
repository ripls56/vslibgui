using System;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;

namespace Gui.Debugging;

/// <summary>
///     A labelled checkbox row used by the debug panel to toggle a <see cref="DebugSettings" />
///     flag.
/// </summary>
public class DebugToggleRow : StatelessWidget
{
    /// <summary>Initializes a new <see cref="DebugToggleRow" />.</summary>
    public DebugToggleRow(string label, bool value, Action<bool> onChanged)
    {
        Label = label;
        Value = value;
        OnChanged = onChanged;
    }

    /// <summary>Label displayed beside the checkbox.</summary>
    public string Label { get; }

    /// <summary>Current checked state.</summary>
    public bool Value { get; }

    /// <summary>Invoked when the user toggles the checkbox.</summary>
    public Action<bool> OnChanged { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => new Checkbox(Value, OnChanged, Label);
}

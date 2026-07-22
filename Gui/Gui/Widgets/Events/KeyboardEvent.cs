namespace Gui.Widgets.Events;

public enum KeyEventType
{
    KeyDown,
    KeyUp,
    KeyChar
}

public class KeyboardEvent
{
    public KeyboardEvent
    (
        KeyEventType type,
        int keyCode = 0,
        char keyChar = '\0',
        bool shift = false,
        bool ctrl = false,
        bool alt = false
    )
    {
        Type = type;
        KeyCode = keyCode;
        KeyChar = keyChar;
        Shift = shift;
        Ctrl = ctrl;
        Alt = alt;
    }

    public KeyEventType Type { get; }
    public int KeyCode { get; }
    public char KeyChar { get; }
    public bool Shift { get; }
    public bool Ctrl { get; }
    public bool Alt { get; }
    public bool Handled { get; set; }
}

public interface IKeyDownHandler
{
    void OnKeyDown
    (
        KeyboardEvent e
    );
}

public interface IKeyUpHandler
{
    void OnKeyUp
    (
        KeyboardEvent e
    );
}

public interface IKeyCharHandler
{
    void OnKeyChar
    (
        KeyboardEvent e
    );
}

/// <summary>
///     Implemented by widgets (or their <c>State</c>) that can be activated by the
///     keyboard. When the focused element is <see cref="IActivatable" />, pressing
///     Enter or Space invokes <see cref="Activate" />.
/// </summary>
public interface IActivatable
{
    /// <summary>Performs the same action as a click/tap.</summary>
    void Activate();
}

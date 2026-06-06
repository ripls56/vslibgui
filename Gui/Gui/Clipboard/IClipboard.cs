namespace Gui.Clipboard;

/// <summary>Provides read/write access to the system clipboard.</summary>
public interface IClipboard
{
    /// <summary>Returns the current clipboard text, or empty string if unavailable.</summary>
    string GetText();

    /// <summary>Writes <paramref name="text" /> to the system clipboard.</summary>
    void SetText(string text);
}

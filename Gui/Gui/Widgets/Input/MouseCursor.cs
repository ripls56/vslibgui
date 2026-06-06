namespace Gui.Widgets.Input;

/// <summary>
///     Describes a cursor to display inside a <see cref="MouseRegion" />.
///     Uses Vintage Story's string-based cursor system via
///     <c>GuiDialog.MouseOverCursor</c>.
/// </summary>
public sealed class MouseCursor
{
    public static readonly MouseCursor LinkSelect = new("linkselect");
    public static readonly MouseCursor TextSelect = new("textselect");

    private MouseCursor(
        string name
    )
    {
        Name = name;
    }

    /// <summary>
    ///     The cursor name recognized by Vintage Story (e.g. "linkselect",
    ///     "textselect"). Null means "default arrow".
    /// </summary>
    public string Name { get; }

    /// <summary>Creates a cursor with a custom VS cursor name.</summary>
    public static MouseCursor Custom(
        string name
    ) =>
        new(name);
}

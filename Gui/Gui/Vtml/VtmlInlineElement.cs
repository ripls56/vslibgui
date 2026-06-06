using Gui.Rendering.Text;

namespace Gui.Vtml;

/// <summary>
///     Base class for inline elements produced by <see cref="VtmlConverter" />
///     from a parsed VTML token tree.
/// </summary>
public abstract class VtmlInlineElement
{
}

/// <summary>
///     A run of styled text, optionally wrapped in a link.
/// </summary>
public class VtmlTextRun : VtmlInlineElement
{
    /// <summary>The text content of this run.</summary>
    public string Text { get; set; } = "";

    /// <summary>Visual style applied to this text run.</summary>
    public TextStyle Style { get; set; }

    /// <summary>
    ///     If non-null, this text run is a clickable link with the given href.
    ///     Supports protocols: http(s), handbook://, chattype:///, command:///, hotkey://.
    /// </summary>
    public string? Href { get; set; }
}

/// <summary>
///     An inline icon element — either a named built-in icon or an SVG path.
/// </summary>
public class VtmlIconRun : VtmlInlineElement
{
    /// <summary>Built-in icon name (e.g. "dice", "wpCircle").</summary>
    public string? Name { get; set; }

    /// <summary>SVG asset path (e.g. "icons/checkmark.svg").</summary>
    public string? Path { get; set; }

    /// <summary>Desired icon size in pixels. Defaults to surrounding font size.</summary>
    public float Size { get; set; } = 24f;
}

/// <summary>
///     An inline or floated item stack rendering element.
/// </summary>
public class VtmlItemStackRun : VtmlInlineElement
{
    /// <summary>
    ///     Item or block code(s), pipe-separated for slideshow (e.g. "plank-oak|plank-birch").
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>"item" or "block".</summary>
    public string ItemType { get; set; } = "block";

    /// <summary>Float behavior: None, Inline, Left, Right.</summary>
    public VtmlFloat FloatType { get; set; } = VtmlFloat.Inline;

    /// <summary>Size multiplier (default 1.0, applied on top of base 1.3x).</summary>
    public float RSize { get; set; } = 1f;

    /// <summary>Horizontal pixel offset.</summary>
    public float OffX { get; set; }

    /// <summary>Vertical pixel offset.</summary>
    public float OffY { get; set; }

    /// <summary>Render size derived from surrounding font height.</summary>
    public float FontHeight { get; set; } = 14f;
}

/// <summary>
///     Displays the currently mapped key binding for a hotkey command.
///     Rendered as styled text showing the key name.
/// </summary>
public class VtmlHotkeyRun : VtmlInlineElement
{
    /// <summary>The hotkey command code (e.g. "sprint", "sneak").</summary>
    public string HotkeyCode { get; set; } = "";

    /// <summary>Style inherited from the surrounding font context.</summary>
    public TextStyle Style { get; set; }
}

/// <summary>Forces a line break in the inline flow.</summary>
public class VtmlLineBreak : VtmlInlineElement
{
}

/// <summary>Clears floated elements (analogous to CSS clear:both).</summary>
public class VtmlClearFloat : VtmlInlineElement
{
}

/// <summary>Float positioning for item stacks.</summary>
public enum VtmlFloat
{
    /// <summary>No float, takes no space (unused in practice).</summary>
    None,

    /// <summary>Placed inline with text flow.</summary>
    Inline,

    /// <summary>Floated to the left; text wraps around the right side.</summary>
    Left,

    /// <summary>Floated to the right; text wraps around the left side.</summary>
    Right
}

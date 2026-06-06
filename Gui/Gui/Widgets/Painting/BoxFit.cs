namespace Gui.Widgets.Painting;

public enum BoxFit
{
    /// <summary>Stretch to fill the widget. Distorts aspect ratio.</summary>
    Fill,

    /// <summary>
    ///     Scale down uniformly to fit within the widget. Preserves aspect ratio.
    ///     May letterbox if aspect ratios differ.
    /// </summary>
    Contain,

    /// <summary>
    ///     Scale up uniformly to fill the widget. Preserves aspect ratio.
    ///     Crops the portion that overflows.
    /// </summary>
    Cover,

    /// <summary>
    ///     Scale to match the widget width. Preserves aspect ratio.
    ///     May overflow vertically.
    /// </summary>
    FitWidth,

    /// <summary>
    ///     Scale to match the widget height. Preserves aspect ratio.
    ///     May overflow horizontally.
    /// </summary>
    FitHeight,

    /// <summary>No scaling. Displays at natural pixel size, centered. May clip.</summary>
    None,

    /// <summary>Like Contain but never upscales; displays at natural size if it fits.</summary>
    ScaleDown
}

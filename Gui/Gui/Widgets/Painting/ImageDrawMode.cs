namespace Gui.Widgets.Painting;

/// <summary>
///     Controls how a textured widget renders its bitmap.
/// </summary>
public enum ImageDrawMode
{
    /// <summary>
    ///     The entire bitmap is stretched to fill the widget bounds. No 9-slice math.
    /// </summary>
    Simple,

    /// <summary>
    ///     9-slice (or 3-slice): corners are drawn at their source-pixel size;
    ///     edges and the center are <b>stretched</b> to fill the remaining space.
    /// </summary>
    Sliced,

    /// <summary>
    ///     9-slice (or 3-slice): corners are drawn at their source-pixel size;
    ///     edges and the center are <b>tiled</b> (repeated) to fill the remaining space.
    /// </summary>
    Tiled
}

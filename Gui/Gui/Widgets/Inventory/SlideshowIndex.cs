namespace Gui.Widgets.Inventory;

/// <summary>
///     Computes the currently-shown element index for a time-cycling slideshow,
///     advancing one element per second (vanilla parity:
///     <c>(capi.ElapsedMilliseconds / 1000) % count</c>).
/// </summary>
public static class SlideshowIndex
{
    /// <summary>Returns the index to display, or 0 when there is nothing to cycle.</summary>
    /// <param name="elapsedMs">Elapsed client time in milliseconds.</param>
    /// <param name="count">Number of elements in the slideshow.</param>
    public static int At(long elapsedMs, int count)
    {
        if (count <= 1)
        {
            return 0;
        }

        return (int)(elapsedMs / 1000 % count);
    }
}

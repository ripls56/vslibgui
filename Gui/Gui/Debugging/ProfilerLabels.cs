using System.Collections.Generic;
using Gui.Core.Framework;

namespace Gui.Debugging;

/// <summary>
///     Builds human-readable labels for render objects from their owning widgets, so
///     profiler output names the author's widgets instead of internal render types.
/// </summary>
public static class ProfilerLabels
{
    /// <summary>
    ///     Returns the owning widget's type name (plus key when present), falling back
    ///     to the render object's own type name when no owner element is attached.
    /// </summary>
    public static string Describe
    (
        RenderObject ro
    )
    {
        var element = ro.OwnerElement;
        if (element == null)
        {
            return ro.GetType().Name;
        }

        var name = element.Widget.GetType().Name;
        var key = element.Widget.Key;
        return key == null
            ? name
            : $"{name}[{key}]";
    }

    /// <summary>
    ///     Builds a " &gt; "-joined path of enclosing widget type names, from the
    ///     outermost ancestor down to the owning widget. Falls back to the render
    ///     object's own type name when no owner element is attached.
    /// </summary>
    public static string DescribePath
    (
        RenderObject ro
    )
    {
        var element = ro.OwnerElement;
        if (element == null)
        {
            return ro.GetType().Name;
        }

        var parts = new List<string>();
        for (var current = element; current != null; current = current.Parent)
        {
            parts.Add(current.Widget.GetType().Name);
        }

        parts.Reverse();
        return string.Join(" > ", parts);
    }
}

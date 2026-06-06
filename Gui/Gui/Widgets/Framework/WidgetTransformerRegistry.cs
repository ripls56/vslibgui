using System.Collections.Generic;

namespace Gui.Widgets.Framework;

/// <summary>
///     Global registry for <see cref="IWidgetTransformer" /> instances.
///     Mods register transformers keyed by the target widget's <see cref="Key" />.
///     The framework applies registered transformers in <see cref="Element.UpdateChild" />
///     before inflating any keyed widget.
/// </summary>
/// <remarks>
///     Register transformers during mod initialization, before the game loop begins.
///     The registry is not thread-safe.
/// </remarks>
public static class WidgetTransformerRegistry
{
    private static readonly Dictionary<Key, List<(int Priority, IWidgetTransformer Transformer)>>
        Map = new();

    /// <summary>
    ///     Registers a transformer for widgets whose <see cref="Widget.Key" /> equals
    ///     <paramref name="key" />. Lower <paramref name="priority" /> runs first.
    ///     Transformers with equal priority run in registration order.
    /// </summary>
    /// <param name="key">The widget key to match against.</param>
    /// <param name="transformer">The transformer to register.</param>
    /// <param name="priority">Execution order; lower values run first. Default 0.</param>
    public static void Register(Key key, IWidgetTransformer transformer, int priority = 0)
    {
        if (!Map.TryGetValue(key, out var list))
        {
            Map[key] = list = new List<(int, IWidgetTransformer)>();
        }

        list.Add((priority, transformer));
        list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    ///     Applies all registered transformers for <paramref name="widget" />'s key in
    ///     priority order. Returns the same instance if no transformers are registered or
    ///     the widget has no key.
    /// </summary>
    /// <param name="widget">The widget to transform.</param>
    /// <returns>
    ///     The widget after all applicable transformers have run, or the original instance
    ///     if no transformers are registered for its key.
    /// </returns>
    internal static Widget Apply(Widget widget)
    {
        if (widget.Key == null)
        {
            return widget;
        }

        if (!Map.TryGetValue(widget.Key, out var list))
        {
            return widget;
        }

        foreach (var (_, transformer) in list)
        {
            widget = transformer.Transform(widget);
        }

        return widget;
    }

    /// <summary>Removes all registered transformers. For testing only.</summary>
    internal static void Clear() => Map.Clear();
}

namespace Gui.Widgets.Framework;

/// <summary>
///     Intercepts a widget before it is inflated into an element.
///     Return the same <paramref name="widget" /> to leave it unchanged,
///     or return a new widget of the same type and key to replace it.
/// </summary>
public interface IWidgetTransformer
{
    /// <summary>
    ///     Transforms <paramref name="widget" /> before it is inflated into an element.
    ///     Return the same instance to leave it unchanged, or a new widget
    ///     (preserving <see cref="Widget.Key" />) to replace it.
    /// </summary>
    /// <param name="widget">The widget to inspect or replace.</param>
    /// <returns>The original widget or a replacement with the same key.</returns>
    Widget Transform(Widget widget);
}

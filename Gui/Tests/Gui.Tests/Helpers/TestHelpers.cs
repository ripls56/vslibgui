using Gui.Core.Basic;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;

namespace Gui.Tests.Helpers;

internal static class TestHelpers
{
    /// <summary>
    ///     Creates a <see cref="BuildOwner" /> with a no-op ticker provider so widgets
    ///     that schedule animations can mount outside of a running <c>GuiBase</c>.
    /// </summary>
    internal static BuildOwner NewBuildOwner()
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        return owner;
    }

    /// <summary>Mounts a widget and flushes its initial build.</summary>
    internal static Element Mount(Widget widget, ThemeData? theme = null)
    {
        widget = theme == null ? widget : new Theme(theme, widget);
        var owner = NewBuildOwner();
        var root = widget.CreateElement();
        root.AssignOwner(owner);
        root.Mount(null);
        owner.BuildDirtyElements();
        return root;
    }

    /// <summary>Finds the first widget of type <typeparamref name="T"/>.</summary>
    internal static T? FindWidget<T>(Element root) where T : Widget
    {
        return FindElement<T>(root)?.Widget as T;
    }

    /// <summary>Finds the first element whose widget has type <typeparamref name="T"/>.</summary>
    internal static Element? FindElement<T>(Element root) where T : Widget
    {
        if (root.Widget is T)
        {
            return root;
        }

        Element? found = null;
        root.VisitChildren(child =>
        {
            if (found == null)
            {
                found = FindElement<T>(child);
            }
        });
        return found;
    }

    /// <summary>
    ///     Walks the element tree depth-first and returns the first
    ///     <see cref="RenderRichText" /> render object found, or null.
    /// </summary>
    internal static RenderRichText? FindRenderRichText(Element el)
    {
        if (el.RenderObject is RenderRichText r)
        {
            return r;
        }

        RenderRichText? found = null;
        el.VisitChildren(child =>
        {
            if (found != null)
            {
                return;
            }

            found = FindRenderRichText(child);
        });
        return found;
    }

    private sealed class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }
}

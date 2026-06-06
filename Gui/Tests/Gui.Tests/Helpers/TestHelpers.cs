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

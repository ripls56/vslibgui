using Gui.Clipboard;
using Gui.Sound;
using Gui.Widgets.Animations;

namespace Gui.Widgets.Framework;

public readonly struct BuildContext
{
    public Widget Widget { get; }
    public Element Element { get; }

    public BuildContext(
        Widget widget,
        Element element
    )
    {
        Widget = widget;
        Element = element;
    }

    public ITickerProvider GetTickerProvider()
    {
        // Resolves through the element's BuildOwner, which holds the ITickerProvider
        // registered when the GUI screen was opened. This is the same provider used
        // by AnimationController and ScrollController throughout this widget subtree.
        return Element.Owner!.GetTickerProvider();
    }

    public IClipboard GetClipboard() => Element.Owner!.GetClipboard();

    public ISoundPlayer GetSoundPlayer() => Element.Owner!.GetSoundPlayer();

    /// <summary>
    ///     Returns the nearest ancestor <see cref="InheritedWidget" /> of exact type
    ///     <typeparamref name="T" /> and registers a dependency so this element
    ///     rebuilds when the inherited data changes.
    /// </summary>
    public T? DependOnInheritedWidgetOfExactType<T>() where T : InheritedWidget =>
        Element.DependOnInheritedWidgetOfExactType<T>();
}

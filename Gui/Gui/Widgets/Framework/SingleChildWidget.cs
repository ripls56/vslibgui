using Gui.Core.Framework;

namespace Gui.Widgets.Framework;

/// <summary>
///     A <see cref="RenderObjectWidget" /> that owns exactly one optional child widget.
///     The Library threads the child's <see cref="RenderObject" /> into this widget's
///     render object automatically via <c>SingleChildElement</c>.
/// </summary>
public abstract class SingleChildWidget : RenderObjectWidget, ISingleChildWidget
{
    /// <summary>The single optional child widget.</summary>
    public readonly Widget? Child;

    protected SingleChildWidget(
        Widget? child = null,
        Key? key = null
    ) : base(key)
    {
        Child = child;
    }

    Widget? ISingleChildWidget.Child => Child;

    public override void Dispose()
    {
        Child?.Dispose();
        base.Dispose();
    }

    public override Element CreateElement() => new SingleChildElement(this);
}

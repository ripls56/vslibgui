namespace Gui.Widgets.Framework;

public interface ISingleChildWidget : IWidget
{
    Widget? Child { get; }
}

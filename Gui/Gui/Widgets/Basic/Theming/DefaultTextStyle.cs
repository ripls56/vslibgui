using Gui.Rendering.Text;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Basic.Theming;

public class DefaultTextStyle : InheritedWidget
{
    public DefaultTextStyle
    (
        TextStyle style,
        Widget child,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Style = style;
    }

    public TextStyle Style { get; }

    public static TextStyle Of(BuildContext context)
    {
        var widget = context.DependOnInheritedWidgetOfExactType<DefaultTextStyle>();
        return widget?.Style ?? new TextStyle();
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Style.Equals(((DefaultTextStyle)oldWidget).Style);
    }
}

using Gui.Widgets.Framework;

namespace Gui.Widgets.Basic.Theming;

public class IconTheme : InheritedWidget
{
    public IconTheme
    (
        IconThemeData data,
        Widget child,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Data = data;
    }

    public IconThemeData Data { get; }

    public static IconThemeData Of(BuildContext context)
    {
        var widget = context.DependOnInheritedWidgetOfExactType<IconTheme>();
        return widget?.Data ?? new IconThemeData();
    }

    public override bool UpdateShouldNotify
        (InheritedWidget oldWidget) => Data != ((IconTheme)oldWidget).Data;
}

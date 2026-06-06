using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>Lays out children vertically in a flex column.</summary>
public class Column : Flex
{
    /// <summary>Creates a vertical flex layout.</summary>
    public Column(
        float spacing = 0,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Start,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        IEnumerable<Widget>? children = null,
        Framework.Key? key = null
    )
        : base(
            FlexDirection.Vertical,
            spacing,
            mainAxisAlignment,
            crossAxisAlignment,
            mainAxisSize,
            children,
            key
        )
    {
    }
}

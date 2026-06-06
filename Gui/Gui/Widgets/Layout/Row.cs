using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>Lays out children horizontally in a flex row.</summary>
public class Row : Flex
{
    /// <summary>Creates a horizontal flex layout.</summary>
    public Row(
        float spacing = 0,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Start,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        IEnumerable<Widget>? children = null,
        Framework.Key? key = null
    )
        : base(
            FlexDirection.Horizontal,
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

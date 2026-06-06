using System.Collections.Generic;

namespace Gui.Widgets.Framework;

public interface IMultiChildWidget : IWidget
{
    IReadOnlyList<Widget> Children { get; }
}

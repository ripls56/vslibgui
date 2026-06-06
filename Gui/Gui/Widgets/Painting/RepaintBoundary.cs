using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Painting;

public class RepaintBoundary : SingleChildWidget
{
    public RepaintBoundary(
        Widget child
    ) : base(child)
    {
    }

    public override RenderObject CreateRenderObject() => new RenderRepaintBoundary();
}

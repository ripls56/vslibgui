using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class IntrinsicWidth : SingleChildWidget
{
    public IntrinsicWidth(
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject() => new RenderIntrinsicWidth();
}

using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class IntrinsicHeight : SingleChildWidget
{
    public IntrinsicHeight(
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject() => new RenderIntrinsicHeight();
}

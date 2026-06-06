using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Center : SingleChildWidget
{
    public Center(
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject() =>
        new RenderPositionedBox { Alignment = Alignment.Center };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderPositionedBox box)
        {
            box.Alignment = Alignment.Center;
        }
    }
}

using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Align : SingleChildWidget
{
    public Align(
        Alignment alignment,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Alignment = alignment;
    }

    public Alignment Alignment { get; }

    public override RenderObject CreateRenderObject() =>
        new RenderPositionedBox { Alignment = Alignment };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderPositionedBox box)
        {
            box.Alignment = Alignment;
        }
    }
}

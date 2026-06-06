using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Padding : SingleChildWidget
{
    public Padding(
        EdgeInsets edgeInsets,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
        EdgeInsets = edgeInsets;
    }

    public EdgeInsets EdgeInsets { get; }

    public override RenderObject CreateRenderObject() => new RenderPadding { Padding = EdgeInsets };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderPadding box)
        {
            box.Padding = EdgeInsets;
        }
    }
}

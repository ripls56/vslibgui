using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Expanded : SingleChildWidget
{
    public Expanded(
        Widget child,
        int flex = 1,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Flex = flex;
    }

    public int Flex { get; }

    public override RenderObject CreateRenderObject() => new RenderProxyBox();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject.ParentData is not FlexParentData)
        {
            renderObject.ParentData = new FlexParentData();
        }

        var flexData = (FlexParentData)renderObject.ParentData;
        flexData.Flex = Flex;
        flexData.Fit = FlexFit.Tight;
    }
}

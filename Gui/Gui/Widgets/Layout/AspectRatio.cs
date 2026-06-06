using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class AspectRatio : SingleChildWidget
{
    public AspectRatio(
        Widget child,
        float aspectRatio,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Ratio = aspectRatio;
    }

    public float Ratio { get; }

    public override RenderObject CreateRenderObject() =>
        new RenderAspectRatio { AspectRatio = Ratio };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        ((RenderAspectRatio)renderObject).AspectRatio = Ratio;
    }
}

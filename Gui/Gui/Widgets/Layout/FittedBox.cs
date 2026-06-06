using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;

namespace Gui.Widgets.Layout;

public class FittedBox : SingleChildWidget
{
    public FittedBox(
        Widget child,
        BoxFit fit = BoxFit.Contain,
        Alignment? alignment = null,
        ClipBehavior clipBehavior = ClipBehavior.None,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Fit = fit;
        Alignment = alignment ?? Alignment.Center;
        ClipBehavior = clipBehavior;
    }

    public BoxFit Fit { get; }
    public Alignment Alignment { get; }
    public ClipBehavior ClipBehavior { get; }

    public override RenderObject CreateRenderObject()
    {
        return new RenderFittedBox
        {
            Fit = Fit, Alignment = Alignment, ClipBehavior = ClipBehavior
        };
    }

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        var ro = (RenderFittedBox)renderObject;
        ro.Fit = Fit;
        ro.Alignment = Alignment;
        ro.ClipBehavior = ClipBehavior;
    }
}

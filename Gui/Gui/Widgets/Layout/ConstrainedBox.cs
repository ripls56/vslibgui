using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>
///     A widget that imposes additional constraints on its child.
/// </summary>
public class ConstrainedBox : SingleChildWidget
{
    public ConstrainedBox(
        LayoutConstraints constraints,
        Widget? child = null,
        Framework.Key? key = null
    )
        : base(
            child,
            key
        )
    {
        Constraints = constraints;
    }

    /// <summary>
    ///     The additional constraints to impose on the child.
    /// </summary>
    public LayoutConstraints Constraints { get; }

    public override RenderObject CreateRenderObject() => new RenderConstrainedBox(Constraints);

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        var ro = (RenderConstrainedBox)renderObject;
        ro.AdditionalConstraints = Constraints;
    }
}

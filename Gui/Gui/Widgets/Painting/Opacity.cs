using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Painting;

/// <summary>Applies a fractional opacity to its child.</summary>
public class Opacity : SingleChildWidget
{
    public Opacity(
        float opacity,
        Widget? child = null
    ) : base(child)
    {
        OpacityValue = opacity;
    }

    public float OpacityValue { get; }

    public override RenderObject CreateRenderObject() => new RenderOpacity();

    public override void UpdateRenderObject(
        RenderObject renderObject
    ) =>
        ((RenderOpacity)renderObject).Opacity = OpacityValue;
}

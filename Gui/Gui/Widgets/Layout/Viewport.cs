using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Layout;

public class Viewport : SingleChildWidget
{
    public Viewport(
        Vector2 offset,
        Widget? child = null,
        Axis scrollDirection = Axis.Vertical,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Offset = offset;
        ScrollDirection = scrollDirection;
    }

    public Vector2 Offset { get; }

    /// <summary>The axis along which this viewport scrolls.</summary>
    public Axis ScrollDirection { get; }

    public override RenderObject CreateRenderObject() =>
        new RenderViewport { Offset = Offset, ScrollDirection = ScrollDirection };

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderViewport)renderObject;
        ro.Offset = Offset;
        ro.ScrollDirection = ScrollDirection;
    }
}

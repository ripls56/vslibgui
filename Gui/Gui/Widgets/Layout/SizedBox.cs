using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class SizedBox : SingleChildWidget
{
    public SizedBox(
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    )
        : base(
            child,
            key
        )
    {
        Width = width;
        Height = height;
    }

    public float? Width { get; }
    public float? Height { get; }

    public override RenderObject CreateRenderObject()
    {
        return new RenderConstrainedBox
        {
            MinWidth = Width, MaxWidth = Width, MinHeight = Height, MaxHeight = Height
        };
    }

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderConstrainedBox box)
        {
            box.MinWidth = Width;
            box.MaxWidth = Width;
            box.MinHeight = Height;
            box.MaxHeight = Height;
        }
    }
}

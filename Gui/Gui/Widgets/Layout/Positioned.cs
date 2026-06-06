using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

public class Positioned : SingleChildWidget
{
    public Positioned(
        float? left = null,
        float? top = null,
        float? right = null,
        float? bottom = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Width = width;
        Height = height;
    }

    public float? Top { get; }
    public float? Left { get; }
    public float? Right { get; }
    public float? Bottom { get; }
    public float? Width { get; }
    public float? Height { get; }

    public override RenderObject CreateRenderObject() => new RenderPositioned();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderPositioned)renderObject;
        ro.Top = Top;
        ro.Left = Left;
        ro.Right = Right;
        ro.Bottom = Bottom;
        ro.Width = Width;
        ro.Height = Height;
        ro.SyncParentData();
    }
}

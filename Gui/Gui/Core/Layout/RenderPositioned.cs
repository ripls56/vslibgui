using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Core.Layout;

/// <summary>
///     Render object for a positioned child within a <see cref="RenderStack" />.
///     Writes <see cref="StackParentData" /> so the stack can position this child.
/// </summary>
internal class RenderPositioned : RenderProxyBox
{
    public float? Top { get; set; }
    public float? Left { get; set; }
    public float? Right { get; set; }
    public float? Bottom { get; set; }
    public float? Width { get; set; }
    public float? Height { get; set; }

    // Called eagerly from Positioned.UpdateRenderObject so that RenderStack
    // can identify this child as positioned before the first Layout call.
    internal void SyncParentData()
    {
        if (ParentData is not StackParentData spd)
        {
            spd = new StackParentData();
            ParentData = spd;
        }

        var changed = spd.Top != Top || spd.Left != Left
                                     || spd.Right != Right || spd.Bottom != Bottom
                                     || spd.Width != Width || spd.Height != Height;

        spd.Top = Top;
        spd.Left = Left;
        spd.Right = Right;
        spd.Bottom = Bottom;
        spd.Width = Width;
        spd.Height = Height;

        if (changed)
        {
            Parent?.MarkNeedsLayout();
        }
    }

    public override void Layout(
        LayoutConstraints constraints
    )
    {
        SyncParentData();
        base.Layout(constraints);
    }
}

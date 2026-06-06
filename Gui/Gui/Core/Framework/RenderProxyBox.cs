using OpenTK.Mathematics;

namespace Gui.Core.Framework;

/// <summary>
///     A RenderBox that sizes itself to its child's size.
/// </summary>
public class RenderProxyBox : RenderBox
{
    public override bool IsHitTestTarget => true;

    protected override void PerformLayout()
    {
        if (Children.Count > 0)
        {
            var child = Children[0];
            child.X = 0;
            child.Y = 0;
            child.Layout(Constraints);

            Size = Constraints.Constrain(child.Size);
        }
        else
        {
            Size = Constraints.Constrain(Vector2.Zero);
        }
    }

    public override float GetMinIntrinsicWidth(
        float height
    )
    {
        return Children.Count > 0
            ? Children[0].GetMinIntrinsicWidth(height)
            : 0f;
    }

    public override float GetMaxIntrinsicWidth(
        float height
    )
    {
        return Children.Count > 0
            ? Children[0].GetMaxIntrinsicWidth(height)
            : 0f;
    }

    public override float GetMinIntrinsicHeight(
        float width
    )
    {
        return Children.Count > 0
            ? Children[0].GetMinIntrinsicHeight(width)
            : 0f;
    }

    public override float GetMaxIntrinsicHeight(
        float width
    )
    {
        return Children.Count > 0
            ? Children[0].GetMaxIntrinsicHeight(width)
            : 0f;
    }
}

using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Painting;

/// <summary>
///     Translates the child's paint position by a fraction of the child's own size.
///     For example, <c>Translation = new Vector2(-0.5f, -1f)</c> shifts the child
///     left by half its width and up by its full height.
///     <para>
///         Layout is unaffected — only the paint offset and hit-test position change.
///     </para>
/// </summary>
public class RenderFractionalTranslation : RenderProxyBox
{
    private Vector2 _translation;

    /// <summary>
    ///     Translation as a fraction of child size. (−0.5, −1) means shift left 50%
    ///     and up 100% of the child's measured size.
    /// </summary>
    public Vector2 Translation
    {
        get => _translation;
        set => SetProperty(ref _translation, value, relayout: true);
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (Children.Count > 0)
        {
            var child = Children[0];
            child.X = child.Size.X * _translation.X;
            child.Y = child.Size.Y * _translation.Y;
        }
    }

    /// <inheritdoc />
    public override bool HitTest(
        HitTestResult result,
        Vector2 position,
        Element element
    )
    {
        var offset = Children.Count > 0
            ? new Vector2(
                Children[0].Size.X * _translation.X,
                Children[0].Size.Y * _translation.Y
            )
            : Vector2.Zero;
        return base.HitTest(
            result,
            position - offset,
            element
        );
    }
}

using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Painting;

/// <summary>
///     Translates the child's paint position by a fraction of the child's own size.
///     For example, <c>translation: new Vector2(-0.5f, -1f)</c> shifts the child
///     left by half its width and up by its full height.
///     <para>
///         The child is laid out normally; only the paint offset and hit-test position
///         are affected. This widget does not expand its own size to encompass the
///         translated child — it should be used inside an unclipped context such as
///         the root overlay stack.
///     </para>
/// </summary>
public class FractionalTranslation : SingleChildWidget
{
    public FractionalTranslation(
        Vector2 translation,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        Translation = translation;
    }

    /// <summary>
    ///     Translation as a fraction of child size. (−0.5, −1) means shift left 50%
    ///     and up 100% of the child's measured size.
    /// </summary>
    public Vector2 Translation { get; }

    public override RenderObject CreateRenderObject() =>
        new RenderFractionalTranslation { Translation = Translation };

    public override void UpdateRenderObject(
        RenderObject renderObject
    ) =>
        ((RenderFractionalTranslation)renderObject).Translation = Translation;
}

using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Core.Layout;

/// <summary>
///     Render object for <see cref="Gui.Widgets.Layout.LayoutBuilder" />.
///     Lays out its single child with the incoming constraints and notifies
///     the owning <see cref="LayoutBuilderElement" /> when constraints change
///     so the child widget tree can be rebuilt next frame.
/// </summary>
public class RenderLayoutBuilder : RenderBox
{
    private LayoutConstraints? _lastConstraints;

    /// <summary>
    ///     The <see cref="LayoutBuilderElement" /> that owns this render object. Set immediately
    ///     after mount by <see cref="LayoutBuilderElement" /> so constraint changes trigger a
    ///     rebuild. Distinct from the base <see cref="RenderObject.OwnerElement" /> which holds
    ///     the generic element back-reference used by profiling tools.
    /// </summary>
    internal LayoutBuilderElement? LayoutBuilderOwner { get; set; }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        var constraintsChanged =
            _lastConstraints == null || !_lastConstraints.Value.Equals(Constraints);

        if (constraintsChanged)
        {
            _lastConstraints = Constraints;
            LayoutBuilderOwner?.MarkNeedsRebuild();
        }

        if (Children.Count > 0)
        {
            var child = Children[0];
            child.Layout(Constraints);
            child.X = 0;
            child.Y = 0;
            Size = child.Size;
        }
        else
        {
            Size = Constraints.Constrain
            (
                new Vector2(0, 0)
            );
        }
    }
}

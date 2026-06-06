using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>
///     A widget that controls how a child of a Row, Column, or Flex flexes.
///     Using a Flexible widget gives a child of a Row, Column, or Flex the flexibility
///     to expand to fill the available space in the main axis (e.g., horizontally for a
///     Row or vertically for a Column), but, unlike Expanded, Flexible does not require
///     the child to fill the available space.
///     A Flexible widget must be a descendant of a Row, Column, or Flex, and the path
///     from the Flexible widget to its enclosing Row, Column, or Flex must contain only
///     StatelessWidgets or StatefulWidgets (not other kinds of widgets, like
///     RenderObjectWidgets).
/// </summary>
public class Flexible : SingleChildWidget
{
    public Flexible(
        Widget child,
        int flex = 1,
        FlexFit fit = FlexFit.Loose,
        Framework.Key? key = null
    ) : base(child, key)
    {
        Flex = flex;
        Fit = fit;
    }

    /// <summary>The flex factor for distributing remaining main-axis space.</summary>
    public int Flex { get; }

    /// <summary>
    ///     Whether the child is forced to fill its allocated flex space (Tight) or may
    ///     be smaller (Loose). Defaults to <see cref="FlexFit.Loose" />.
    /// </summary>
    public FlexFit Fit { get; }

    public override RenderObject CreateRenderObject() => new RenderProxyBox();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject.ParentData is not FlexParentData)
        {
            renderObject.ParentData = new FlexParentData();
        }

        var flexData = (FlexParentData)renderObject.ParentData;
        flexData.Flex = Flex;
        flexData.Fit = Fit;
    }
}

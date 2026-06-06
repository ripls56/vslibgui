using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>
///     Base class for directional flex-layout widgets.
///     <see cref="Row" /> and <see cref="Column" /> are thin subclasses
///     that only set the <see cref="Direction" />.
/// </summary>
public abstract class Flex : MultiChildWidget
{
    /// <summary>
    ///     Creates a flex layout widget with the given direction and
    ///     alignment parameters.
    /// </summary>
    protected Flex(
        FlexDirection direction,
        float spacing = 0,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Start,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        IEnumerable<Widget>? children = null,
        Framework.Key? key = null
    ) : base(children, key)
    {
        Direction = direction;
        Spacing = spacing;
        MainAxisAlignment = mainAxisAlignment;
        CrossAxisAlignment = crossAxisAlignment;
        MainAxisSize = mainAxisSize;
    }

    /// <summary>Flex layout direction.</summary>
    public FlexDirection Direction { get; }

    /// <summary>Spacing between children along the main axis.</summary>
    public float Spacing { get; }

    /// <summary>Alignment of children along the main axis.</summary>
    public MainAxisAlignment MainAxisAlignment { get; }

    /// <summary>Alignment of children along the cross axis.</summary>
    public CrossAxisAlignment CrossAxisAlignment { get; }

    /// <summary>Whether the flex fills all available main-axis space or shrinks to content.</summary>
    public MainAxisSize MainAxisSize { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderFlex
        {
            Direction = Direction,
            Spacing = Spacing,
            MainAxisAlignment = MainAxisAlignment,
            CrossAxisAlignment = CrossAxisAlignment,
            MainAxisSize = MainAxisSize
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderFlex box)
        {
            box.Direction = Direction;
            box.Spacing = Spacing;
            box.MainAxisAlignment = MainAxisAlignment;
            box.CrossAxisAlignment = CrossAxisAlignment;
            box.MainAxisSize = MainAxisSize;
        }
    }
}

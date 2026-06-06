using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>
///     A widget that lays out its children in horizontal or vertical runs,
///     wrapping to the next line when a child would overflow the available
///     main-axis space.
/// </summary>
public class Wrap : MultiChildWidget
{
    /// <summary>Creates a new Wrap widget.</summary>
    public Wrap(
        FlexDirection direction = FlexDirection.Horizontal,
        float spacing = 0,
        float runSpacing = 0,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Start,
        MainAxisAlignment runAlignment = MainAxisAlignment.Start,
        IEnumerable<Widget>? children = null,
        Framework.Key? key = null
    ) : base(children, key)
    {
        Direction = direction;
        Spacing = spacing;
        RunSpacing = runSpacing;
        MainAxisAlignment = mainAxisAlignment;
        CrossAxisAlignment = crossAxisAlignment;
        RunAlignment = runAlignment;
    }

    /// <summary>Primary axis direction. Default: Horizontal.</summary>
    public FlexDirection Direction { get; }

    /// <summary>Spacing between children on the main axis within a run.</summary>
    public float Spacing { get; }

    /// <summary>Spacing between runs on the cross axis.</summary>
    public float RunSpacing { get; }

    /// <summary>How children are aligned on the main axis within each run.</summary>
    public MainAxisAlignment MainAxisAlignment { get; }

    /// <summary>How children are aligned on the cross axis within each run.</summary>
    public CrossAxisAlignment CrossAxisAlignment { get; }

    /// <summary>How runs are aligned on the cross axis of the container.</summary>
    public MainAxisAlignment RunAlignment { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject()
    {
        return new RenderWrap
        {
            Direction = Direction,
            Spacing = Spacing,
            RunSpacing = RunSpacing,
            MainAxisAlignment = MainAxisAlignment,
            CrossAxisAlignment = CrossAxisAlignment,
            RunAlignment = RunAlignment
        };
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        if (renderObject is RenderWrap wrap)
        {
            wrap.Direction = Direction;
            wrap.Spacing = Spacing;
            wrap.RunSpacing = RunSpacing;
            wrap.MainAxisAlignment = MainAxisAlignment;
            wrap.CrossAxisAlignment = CrossAxisAlignment;
            wrap.RunAlignment = RunAlignment;
        }
    }
}

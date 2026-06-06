using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Scroll;

/// <summary>
///     Render object that lays out visible grid cells and reports viewport metrics.
/// </summary>
internal class RenderGridContent : RenderObject
{
    public RenderGridContent(
        Action<float> onViewportLayout,
        Action<float> onViewportWidth
    )
    {
        OnViewportLayout = onViewportLayout;
        OnViewportWidth = onViewportWidth;
    }

    public SliverGridGeometry Geometry { get; set; }
    public float TotalHeight { get; set; }
    public float ViewportHeight { get; private set; }
    public float ViewportWidth { get; private set; }

    public Action<float> OnViewportLayout { get; set; }
    public Action<float> OnViewportWidth { get; set; }

    public override bool IsHitTestTarget => true;

    protected override void PerformLayout()
    {
        ViewportWidth = Constraints.MaxWidth;
        ViewportHeight = Parent?.Size.Y ?? 0;

        OnViewportLayout(ViewportHeight);
        OnViewportWidth(ViewportWidth);

        Size = new Vector2(
            Constraints.MaxWidth,
            TotalHeight
        );

        if (Geometry.CellWidth > 0 && Geometry.CellHeight > 0)
        {
            var cellConstraints = LayoutConstraints.Tight(
                Geometry.CellWidth,
                Geometry.CellHeight
            );
            foreach (var child in Children)
            {
                child.Layout(cellConstraints);
            }
        }
    }
}

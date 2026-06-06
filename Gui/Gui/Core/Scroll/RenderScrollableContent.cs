using System;
using Gui.Core.Framework;
using Gui.Widgets.Framework;

namespace Gui.Core.Scroll;

/// <summary>
///     Render object that measures its single child with unbounded height and
///     reports the resulting content and viewport sizes to the scroll controller.
/// </summary>
internal class RenderScrollableContent : RenderProxyBox
{
    private readonly Action<float, float> _onLayout;

    public RenderScrollableContent(
        Action<float, float> onLayout
    )
    {
        _onLayout = onLayout;
    }

    protected override void PerformLayout()
    {
        if (Children.Count > 0)
        {
            var child = Children[0];
            child.Layout(
                new LayoutConstraints(
                    Constraints.MinWidth,
                    Constraints.MaxWidth
                )
            );
            Size = child.Size;
        }
        else
        {
            Size = Constraints.Smallest;
        }

        var viewportHeight = Parent?.Size.Y ?? 0;
        _onLayout(
            viewportHeight,
            Size.Y
        );
    }
}

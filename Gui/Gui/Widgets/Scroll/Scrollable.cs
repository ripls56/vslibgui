using System;
using Gui.Core.Framework;
using Gui.Core.Scroll;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;

namespace Gui.Widgets.Scroll;

/// <summary>
///     Implemented by the <see cref="State" /> of any scrollable widget
///     (<see cref="SingleChildScrollView" />, <see cref="ListView" />) so that
///     <see cref="Scrollable.EnsureVisible" /> can drive scrolling without
///     knowing the concrete widget type.
/// </summary>
internal interface IScrollableContext
{
    ScrollController ScrollController { get; }
    float ViewportHeight { get; }
    float ContentHeight { get; }
}

/// <summary>
///     Utility methods for programmatic scrolling.
/// </summary>
public static class Scrollable
{
    /// <summary>
    ///     Scrolls the nearest scrollable ancestor of <paramref name="target" />
    ///     by the minimum amount needed to make the target fully visible in
    ///     the viewport.
    /// </summary>
    /// <param name="target">The element to scroll into view.</param>
    /// <param name="duration">
    ///     Animation duration. <c>null</c> or <see cref="TimeSpan.Zero" /> for
    ///     an instant jump.
    /// </param>
    /// <param name="curve">Easing curve. Defaults to <see cref="Curves.EaseOut" />.</param>
    public static void EnsureVisible(
        Element target,
        TimeSpan? duration = null,
        Curve? curve = null
    )
    {
        var targetRo = target.RenderObject;
        if (targetRo == null)
        {
            return;
        }

        // Walk up the element tree to find the nearest scrollable ancestor.
        IScrollableContext? scrollCtx = null;
        var cursor = target.Parent;
        while (cursor != null)
        {
            if (cursor is StatefulElement se && se.State is IScrollableContext ctx)
            {
                scrollCtx = ctx;
                break;
            }

            cursor = cursor.Parent;
        }

        if (scrollCtx == null)
        {
            return;
        }

        // Find the RenderViewport by walking up the target's RO parent chain.
        RenderViewport? viewport = null;
        var roCursor = targetRo.Parent;
        while (roCursor != null)
        {
            if (roCursor is RenderViewport rv)
            {
                viewport = rv;
                break;
            }

            roCursor = roCursor.Parent;
        }

        if (viewport == null || viewport.Children.Count == 0)
        {
            return;
        }

        // Compute the target's Y in content space by summing Y offsets from
        // the target up to (but not including) the viewport itself.
        var contentY = ComputeContentSpaceY(
            targetRo,
            viewport
        );
        if (float.IsNaN(contentY))
        {
            return;
        }

        var itemHeight = targetRo.Size.Y;
        var viewTop = scrollCtx.ScrollController.Offset;
        var viewBottom = viewTop + scrollCtx.ViewportHeight;
        var itemTop = contentY;
        var itemBottom = contentY + itemHeight;

        var newOffset = viewTop;
        if (itemBottom > viewBottom)
        {
            newOffset = itemBottom - scrollCtx.ViewportHeight;
        }

        if (itemTop < viewTop)
        {
            newOffset = itemTop;
        }

        var maxScroll = Math.Max(
            0,
            scrollCtx.ContentHeight - scrollCtx.ViewportHeight
        );
        newOffset = Math.Clamp(
            newOffset,
            0,
            maxScroll
        );

        if (Math.Abs(newOffset - viewTop) < 0.5f)
        {
            return;
        }

        if (duration == null || duration.Value <= TimeSpan.Zero)
        {
            scrollCtx.ScrollController.JumpTo(newOffset);
        }
        else
        {
            scrollCtx.ScrollController.AnimateTo(
                newOffset,
                duration.Value,
                curve,
                0,
                maxScroll
            );
        }
    }

    private static float ComputeContentSpaceY(
        RenderObject descendant,
        RenderObject ancestor
    )
    {
        var y = 0f;
        var ro = descendant;
        while (ro != null && ro != ancestor)
        {
            y += ro.Y;
            ro = ro.Parent;
        }

        return ro == ancestor
            ? y
            : float.NaN;
    }
}

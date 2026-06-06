using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Scroll;

/// <summary>
///     Render object that draws a scrollbar track and a proportional thumb.
/// </summary>
internal class RenderScrollbarTrack : RenderProxyBox
{
    private readonly Action<float, RenderObject> _onLayout;

    /// <summary>Creates a new scrollbar track render object.</summary>
    public RenderScrollbarTrack(
        Action<float, RenderObject> onLayout
    )
    {
        _onLayout = onLayout;
    }

    /// <summary>Whether the thumb position is inverted (reverse mode).</summary>
    public bool Reverse
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Current scroll offset in pixels.</summary>
    public float ScrollOffset
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Total content size in pixels.</summary>
    public float ContentSize
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Visible viewport size in pixels.</summary>
    public float ViewportSize
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Background color of the scrollbar track.</summary>
    public Vector4 TrackColor
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Color of the draggable thumb.</summary>
    public Vector4 ThumbColor
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    }

    /// <summary>Corner radius of the thumb.</summary>
    public float ThumbRadius
    {
        get;
        set => SetProperty(
            ref field,
            value,
            true
        );
    } = 4;

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        Size = new Vector2(
            Constraints.MaxWidth,
            Constraints.MaxHeight
        );

        foreach (var child in Children)
        {
            child.Layout(
                LayoutConstraints.Tight(
                    Size.X,
                    Size.Y
                )
            );
        }

        _onLayout(
            Size.Y,
            this
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        context.DrawBox(
            Vector2.Zero,
            Size,
            TrackColor,
            Vector4.Zero,
            0,
            Vector4.Zero
        );

        var maxOffset = Math.Max(
            0,
            ContentSize - ViewportSize
        );
        if (maxOffset > 0 && ContentSize > 0)
        {
            var thumbHeight = Math.Max(
                20,
                ViewportSize / ContentSize * Size.Y
            );
            var maxTravel = Size.Y - thumbHeight;
            var ratio = maxTravel > 0
                ? ScrollOffset / maxOffset
                : 0;
            if (Reverse)
            {
                ratio = 1 - ratio;
            }

            var thumbY = ratio * maxTravel;

            context.DrawBox(
                new Vector2(
                    2,
                    thumbY
                ),
                new Vector2(
                    Math.Max(0f, Size.X - 4),
                    thumbHeight
                ),
                ThumbColor,
                new Vector4(ThumbRadius),
                0,
                Vector4.Zero
            );
        }
    }
}

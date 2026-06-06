using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Core.Input;

/// <summary>
///     Render object that draws a slider track with an active fill and a draggable thumb.
/// </summary>
internal class RenderSliderTrack : RenderProxyBox
{
    private readonly Action<float, RenderObject> _onLayout;
    private Vector4 _activeColor;
    private Vector4 _inactiveColor;
    private float _ratio;
    private Vector4 _thumbColor;
    private float _thumbHeight = 24;
    private float _thumbRadius = 2;
    private float _thumbWidth = 12;
    private float _trackHeight = 8;

    public RenderSliderTrack(
        Action<float, RenderObject> onLayout
    )
    {
        _onLayout = onLayout;
    }

    public float Ratio
    {
        get => _ratio;
        set => SetProperty(
            ref _ratio,
            value,
            true
        );
    }

    public Vector4 ActiveColor
    {
        get => _activeColor;
        set => SetProperty(
            ref _activeColor,
            value,
            true
        );
    }

    public Vector4 InactiveColor
    {
        get => _inactiveColor;
        set => SetProperty(
            ref _inactiveColor,
            value,
            true
        );
    }

    public Vector4 ThumbColor
    {
        get => _thumbColor;
        set => SetProperty(
            ref _thumbColor,
            value,
            true
        );
    }

    public float TrackHeight
    {
        get => _trackHeight;
        set => SetProperty(
            ref _trackHeight,
            value,
            relayout: true
        );
    }

    public float ThumbWidth
    {
        get => _thumbWidth;
        set => SetProperty(
            ref _thumbWidth,
            value,
            relayout: true
        );
    }

    public float ThumbHeight
    {
        get => _thumbHeight;
        set => SetProperty(
            ref _thumbHeight,
            value,
            relayout: true
        );
    }

    public float ThumbRadius
    {
        get => _thumbRadius;
        set => SetProperty(
            ref _thumbRadius,
            value,
            true
        );
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        var width = Constraints.MaxWidth;
        var height = float.IsPositiveInfinity(Constraints.MaxHeight)
            ? _thumbHeight
            : Constraints.MaxHeight;
        Size = new Vector2(
            width,
            height
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
            Size.X,
            this
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(
        PaintingContext context
    )
    {
        var centerY = Size.Y / 2f;
        var trackTop = centerY - _trackHeight / 2f;
        var trackRadius = _trackHeight / 2f;

        var usable = Size.X - _thumbWidth;
        var thumbX = usable * _ratio;

        context.DrawBox(
            new Vector2(
                0,
                trackTop
            ),
            new Vector2(
                Size.X,
                _trackHeight
            ),
            _inactiveColor,
            new Vector4(trackRadius),
            0,
            Vector4.Zero
        );

        var activeWidth = thumbX + _thumbWidth / 2f;
        if (activeWidth > 0)
        {
            context.DrawBox(
                new Vector2(
                    0,
                    trackTop
                ),
                new Vector2(
                    activeWidth,
                    _trackHeight
                ),
                _activeColor,
                new Vector4(trackRadius),
                0,
                Vector4.Zero
            );
        }

        var thumbTop = centerY - _thumbHeight / 2f;
        context.DrawBox(
            new Vector2(
                thumbX,
                thumbTop
            ),
            new Vector2(
                _thumbWidth,
                _thumbHeight
            ),
            _thumbColor,
            new Vector4(_thumbRadius),
            1f,
            new Vector4(
                _thumbColor.X * 0.7f,
                _thumbColor.Y * 0.7f,
                _thumbColor.Z * 0.7f,
                _thumbColor.W
            )
        );
    }
}

using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     Clips its child to a rounded rectangle.
/// </summary>
public class RenderClip : RenderProxyBox
{
    private readonly SKRoundRect _roundRect = new();
    private Vector4 _borderRadius;

    /// <summary>
    ///     Corner radii as a Vector4: X = top-right, Y = bottom-right,
    ///     Z = top-left, W = bottom-left.
    /// </summary>
    public Vector4 BorderRadius
    {
        get => _borderRadius;
        set => SetProperty(ref _borderRadius, value, true);
    }

    /// <inheritdoc />
    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null || Children.Count == 0 || ClipBehavior == ClipBehavior.None)
        {
            base.Paint(context);
            return;
        }

        var rect = Size.ToSkRect(Vector2.Zero);
        _roundRect.SetRectRadii(
            rect,
            new[]
            {
                new SKPoint(
                    _borderRadius.Z,
                    _borderRadius.Z
                ), // Top-Left
                new SKPoint(
                    _borderRadius.X,
                    _borderRadius.X
                ), // Top-Right
                new SKPoint(
                    _borderRadius.Y,
                    _borderRadius.Y
                ), // Bottom-Right
                new SKPoint(
                    _borderRadius.W,
                    _borderRadius.W
                ) // Bottom-Left
            }
        );

        using (context.Canvas.SaveScope())
        {
            context.Canvas.ClipRoundRect(
                _roundRect,
                SKClipOperation.Intersect,
                ClipBehavior == ClipBehavior.AntiAlias
            );
            base.Paint(context);
        }
    }
}

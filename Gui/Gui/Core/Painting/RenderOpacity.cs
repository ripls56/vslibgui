using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     Paints its child with a fractional opacity. Opacity 0 skips painting entirely;
///     opacity 1 delegates directly to the child without a SaveLayer.
/// </summary>
public class RenderOpacity : RenderProxyBox
{
    /// <summary>Opacity in [0, 1]. Changes trigger a repaint.</summary>
    public float Opacity
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1.0f;

    /// <inheritdoc />
    public override void Paint(
        PaintingContext context
    )
    {
        if (Opacity <= 0.001f)
        {
            // Clear dirty flags so future MarkNeedsPaint() can propagate when opacity returns above zero.
            MarkPainted();
            NeedsPaint = false;
            ChildNeedsPaint = false;
            return;
        }

        if (Opacity >= 0.999f)
        {
            base.Paint(context);
            return;
        }

        if (context.Canvas != null)
        {
            var paint = context.SharedPaint;
            paint.Color = new Vector4(1, 1, 1, Opacity).ToSkColor();
            paint.BlendMode = SKBlendMode.SrcOver;
            paint.Shader = null;
            paint.ImageFilter = null;
            context.Canvas.SaveLayer(paint);
            base.Paint(context);
            context.Canvas.Restore();
        }
    }
}

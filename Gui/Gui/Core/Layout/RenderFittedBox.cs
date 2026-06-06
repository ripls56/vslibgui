using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Core.Layout;

public class RenderFittedBox : RenderProxyBox
{
    private Alignment _alignment = Alignment.Center;
    private BoxFit _fit = BoxFit.Contain;
    private Vector2 _offset = Vector2.Zero;
    private Vector2 _scale = Vector2.One;
    private SKMatrix _transform = SKMatrix.Identity;

    public BoxFit Fit
    {
        get => _fit;
        set => SetProperty(
            ref _fit,
            value,
            relayout: true
        );
    }

    public Alignment Alignment
    {
        get => _alignment;
        set => SetProperty(
            ref _alignment,
            value,
            relayout: true
        );
    }

    protected override void PerformLayout()
    {
        if (Children.Count > 0)
        {
            var child = Children[0];
            // Measure child with no constraints to get its natural size
            child.Layout(new LayoutConstraints(0));

            Size = Constraints.Constrain(child.Size);

            // Calculate transform
            CalculateTransform(
                child.Size,
                Size,
                out _scale,
                out _offset
            );
            _transform = SKMatrix.CreateScale(
                    _scale.X,
                    _scale.Y
                )
                .PostConcat(
                    SKMatrix.CreateTranslation(
                        _offset.X,
                        _offset.Y
                    )
                );

            // Position child at 0,0 locally; transform will handle the rest
            child.X = 0;
            child.Y = 0;
        }
        else
        {
            Size = Constraints.Constrain(Vector2.Zero);
            _transform = SKMatrix.Identity;
            _scale = Vector2.One;
            _offset = Vector2.Zero;
        }
    }

    private void CalculateTransform(
        Vector2 contentSize,
        Vector2 containerSize,
        out Vector2 scale,
        out Vector2 offset
    )
    {
        scale = Vector2.One;
        offset = Vector2.Zero;

        if (contentSize.X <= 0 || contentSize.Y <= 0 || containerSize.X <= 0 ||
            containerSize.Y <= 0)
        {
            return;
        }

        var scaleX = 1.0f;
        var scaleY = 1.0f;

        switch (_fit)
        {
            case BoxFit.Fill:
                scaleX = containerSize.X / contentSize.X;
                scaleY = containerSize.Y / contentSize.Y;
                break;
            case BoxFit.Contain:
                scaleX = scaleY = Math.Min(
                    containerSize.X / contentSize.X,
                    containerSize.Y / contentSize.Y
                );
                break;
            case BoxFit.Cover:
                scaleX = scaleY = Math.Max(
                    containerSize.X / contentSize.X,
                    containerSize.Y / contentSize.Y
                );
                break;
            case BoxFit.FitWidth:
                scaleX = scaleY = containerSize.X / contentSize.X;
                break;
            case BoxFit.FitHeight:
                scaleX = scaleY = containerSize.Y / contentSize.Y;
                break;
            case BoxFit.None:
                scaleX = scaleY = 1.0f;
                break;
            case BoxFit.ScaleDown:
                scaleX = scaleY = Math.Min(
                    1.0f,
                    Math.Min(
                        containerSize.X / contentSize.X,
                        containerSize.Y / contentSize.Y
                    )
                );
                break;
        }

        var renderedSize = new Vector2(
            contentSize.X * scaleX,
            contentSize.Y * scaleY
        );
        offset = _alignment.CalculateOffset(
            containerSize,
            renderedSize
        );
        scale = new Vector2(
            scaleX,
            scaleY
        );
    }

    public override void Paint(
        PaintingContext context
    )
    {
        if (context.Canvas == null || Children.Count == 0)
        {
            base.Paint(context);
            return;
        }

        context.Canvas.Save();

        var matrix = _transform;
        context.Canvas.Concat(in matrix);

        base.Paint(context);

        context.Canvas.Restore();
    }

    public override Vector2 GlobalToChild(
        RenderObject child,
        Vector2 position
    )
    {
        // For FittedBox, all children (usually just one) are transformed by the same matrix
        // Inverse of T * S is S^-1 * T^-1
        // 1. Subtract offset
        // 2. Divide by scale
        var x = (position.X - _offset.X) / _scale.X;
        var y = (position.Y - _offset.Y) / _scale.Y;
        return new Vector2(
            x,
            y
        );
    }
}

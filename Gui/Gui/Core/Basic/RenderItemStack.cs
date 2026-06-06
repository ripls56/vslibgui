using System;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Common;

namespace Gui.Core.Basic;

/// <summary>
///     A leaf render object that paints a cached <see cref="SKImage" /> of an
///     <see cref="ItemStack" />. The image is obtained from
///     <see cref="ItemStackRenderer" /> which renders items via an offscreen GL
///     framebuffer and caches the result.
///     <para>
///         While a new visual state is still rendering, the slot falls back to the most recent
///         cached image for the same collectible (<see cref="ItemStackRenderer.GetCachedAny" />), so a
///         cooling item or perishing food never blanks out. The fallback is a live, cache-owned image
///         drawn the same frame — the slot never holds an <see cref="SKImage" /> across frames, which
///         would dangle once the cache evicts the entry and recycles its GL texture.
///     </para>
/// </summary>
public class RenderItemStack : RenderBox
{
    private float? _height;
    private ItemStack? _itemStack;
    private int _renderSize = 48;
    private bool _waitingForBitmap;
    private float? _width;

    /// <summary>Gets or sets the item stack to render.</summary>
    public ItemStack? ItemStack
    {
        get => _itemStack;
        set
        {
            SetProperty(
                ref _itemStack,
                value,
                true,
                true
            );
            _waitingForBitmap = false;
            MarkNeedsPaint();
        }
    }

    /// <summary>Gets or sets the pixel size used for the offscreen FBO render.</summary>
    public int RenderSize
    {
        get => _renderSize;
        set
        {
            if (SetProperty(
                    ref _renderSize,
                    value,
                    relayout: true
                ))
            {
                _waitingForBitmap = false;
            }
        }
    }

    /// <summary>Gets or sets the fixed width override. Defaults to <see cref="RenderSize" />.</summary>
    public float? Width
    {
        get => _width;
        set => SetProperty(
            ref _width,
            value,
            relayout: true
        );
    }

    /// <summary>Gets or sets the fixed height override. Defaults to <see cref="RenderSize" />.</summary>
    public float? Height
    {
        get => _height;
        set => SetProperty(
            ref _height,
            value,
            relayout: true
        );
    }

    /// <inheritdoc />
    public override bool IsHitTestTarget =>
        HitTestBehavior == HitTestBehavior.Opaque ||
        (HitTestBehavior == HitTestBehavior.Defer && _itemStack != null);

    /// <summary>The stack currently assigned to this slot, or null when empty.</summary>
    internal ItemStack? CurrentStack => _itemStack;

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        var w = _width ?? _renderSize;
        var h = _height ?? _renderSize;
        Size = Constraints.Constrain(
            new Vector2(
                w,
                h
            )
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(
        PaintingContext context
    )
    {
        if (context.Canvas == null)
        {
            return;
        }

        if (Color.W > 0 || BorderThickness > 0)
        {
            context.Canvas.DrawBox(
                Vector2.Zero,
                Size,
                Color,
                CornerRadii,
                BorderThickness,
                BorderColor,
                context.SharedPaint,
                context.SharedRoundRect,
                context.SharedBorderRoundRect
            );
        }

        var image = ResolveImage();
        if (image == null)
        {
            return;
        }

        float imgW = image.Width;
        float imgH = image.Height;
        var scale = Math.Min(
            Size.X / imgW,
            Size.Y / imgH
        );
        var drawW = imgW * scale;
        var drawH = imgH * scale;
        var offsetX = (Size.X - drawW) / 2f;
        var offsetY = (Size.Y - drawH) / 2f;

        var src = new SKRect(0, 0, imgW, imgH);
        var dst = new SKRect(offsetX, offsetY, offsetX + drawW, offsetY + drawH);

        context.SharedPaint.Style = SKPaintStyle.Fill;
        context.SharedPaint.Color = SKColors.White;
        context.SharedPaint.BlendMode = SKBlendMode.SrcOver;
        context.SharedPaint.Shader = null;
        context.SharedPaint.ImageFilter = null;
        context.Canvas.DrawImage(
            image,
            src,
            dst,
            new SKSamplingOptions(
                SKFilterMode.Linear,
                SKMipmapMode.None
            ),
            context.SharedPaint
        );
    }

    /// <summary>
    ///     Called by <see cref="ItemStackRenderer.ProcessQueue" /> after new
    ///     images are rendered. Schedules a repaint so the new image is picked up.
    /// </summary>
    internal void OnBitmapReady()
    {
        if (!_waitingForBitmap)
        {
            return;
        }

        _waitingForBitmap = false;
        MarkNeedsPaint();
    }

    /// <summary>
    ///     Called by <see cref="ItemStackRenderer" /> when a glowing item has cooled into a new
    ///     temperature bucket, so the slot repaints and picks up the next glow step even while idle
    ///     (not hovered) — preventing a stale hot frame that would otherwise pop on the next interaction.
    /// </summary>
    internal void InvalidateGlow() => MarkNeedsPaint();

    private SKImage? ResolveImage()
    {
        var renderer = GuiModSystem.Instance?.ItemStackRenderer;
        if (renderer == null || _itemStack == null)
        {
            return null;
        }

        renderer.TrackGlowingIfHot(this);

        var image = renderer.GetOrQueue(_itemStack, _renderSize);
        if (image != null)
        {
            _waitingForBitmap = false;
            return image;
        }

        if (!_waitingForBitmap)
        {
            _waitingForBitmap = true;
            renderer.RegisterPending(this);
        }

        return renderer.GetCachedAny(_itemStack, _renderSize);
    }
}

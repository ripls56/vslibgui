using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Renders the visual contents of an inventory slot: hover highlight, item icon,
///     stack size text, and durability bar. Reads hover animation from
///     <see cref="ItemSlotHoverData" /> when available.
/// </summary>
public class ItemSlotOverlay : StatelessWidget
{
    /// <summary>Creates an overlay for <paramref name="slot" /> sized to <paramref name="size" />.</summary>
    public ItemSlotOverlay(
        ItemSlot? slot,
        float size,
        Vector4? hoverColor = null,
        EdgeInsets? padding = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Slot = slot;
        Size = size;
        HoverColor = hoverColor;
        Padding = padding;
    }

    /// <summary>The inventory slot to display. May be null for an empty placeholder.</summary>
    public ItemSlot? Slot { get; }

    /// <summary>Side length of the slot in pixels.</summary>
    public float Size { get; }

    /// <summary>Overrides the theme hover overlay color when set.</summary>
    public Vector4? HoverColor { get; }

    /// <summary>Overrides the theme item padding when set.</summary>
    public EdgeInsets? Padding { get; }

    /// <summary>When false, the hover spotlight border is not drawn for this slot.</summary>
    public bool EnableSpotlight { get; init; } = true;

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var itemStack = Slot?.Itemstack;
        var hoverAnim = ItemSlotHoverData.Of(context)?.HoverAnimation;
        var hoverOpacity = (float)(hoverAnim?.Value ?? 0) * 0.08f;
        var theme = Theme.Of(context);
        var slotStyle = theme.ItemSlotStyle;
        var hoverOverlayColor = HoverColor ?? slotStyle.HoverColor ?? new Vector4(1);
        var pad = Padding ?? slotStyle.Padding ?? EdgeInsets.Zero;

        var hoverDataCtx = ItemSlotHoverData.Of(context);
        var punchScale = hoverDataCtx?.PunchScale ?? 1f;
        var onPunchEnd = hoverDataCtx?.OnPunchEnd;

        var fontSize = (int)MathF.Max(12f, Size * 0.16f);

        Widget itemIcon;
        if (itemStack != null)
        {
            var iconChildren = new List<Widget>
            {
                new ItemStackDisplay(itemStack, Size, Size, (int)Size)
            };

            if (itemStack.Collectible != null)
            {
                var itemStackStackSize = itemStack.StackSize;
                var stackSizeText = itemStackStackSize.ToString();
                var containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
                if (containableProps != null)
                {
                    var itemsPerLitre = containableProps?.ItemsPerLitre;
                    var num = (itemsPerLitre.HasValue
                        ? new float?(itemStackStackSize / itemsPerLitre.GetValueOrDefault())
                        : null) ?? 1f;
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (num < 0.1)
                    {
                        stackSizeText = Lang.Get("{0} mL", (int)(num * 1000.0));
                    }
                    else
                    {
                        stackSizeText = Lang.Get("{0:0.##} L", num);
                    }
                }

                if (itemStackStackSize > 1 || containableProps != null)
                {
                    iconChildren.Add(
                        new Positioned(
                            right: 2,
                            bottom: 2,
                            child: new Text(
                                stackSizeText,
                                new TextStyle
                                {
                                    FontSize = fontSize,
                                    Color = new Vector4(1, 1, 1, .95f),
                                    OutlineWidth = 0.1f,
                                    OutlineColor = new Vector4(0, 0, 0, .85f)
                                }
                            )
                        )
                    );
                }
            }

            itemIcon = new Stack(iconChildren);
        }
        else
        {
            itemIcon = new SizedBox();
        }

        itemIcon = new AnimatedScale(
            punchScale,
            TimeSpan.FromMilliseconds(150),
            Curves.EaseOut,
            punchScale != 1f ? onPunchEnd : null,
            child: itemIcon
        );

        var backgroundIcon = Slot?.BackgroundIcon;

        var outerChildren = new List<Widget>
        {
            // Flat hover fill disabled for preview — spotlight border is the hover affordance.
            // new Opacity(
            //     hoverOpacity,
            //     new Container(new BoxStyle { Color = hoverOverlayColor })
            // ),
        };

        if (!string.IsNullOrEmpty(backgroundIcon) && itemStack == null)
        {
            var bgIconColor = theme.ColorScheme.Secondary with { W = 0.45f };
            outerChildren.Add(
                new Center(
                    new VsIcon(backgroundIcon, Size * 0.7f, bgIconColor)
                )
            );
        }

        outerChildren.Add(new Center(new Padding(pad, itemIcon)));

        if (itemStack?.Collectible != null)
        {
            var maxDur = itemStack.Collectible.GetMaxDurability(itemStack);
            if (maxDur > 0)
            {
                var remaining = itemStack.Collectible.GetRemainingDurability(itemStack);
                if (maxDur != remaining)
                {
                    var dur = new DurabilityBar((float)remaining / maxDur, Size);
                    outerChildren.Add(
                        new Positioned(bottom: 3, child: new Align(Alignment.BottomCenter, dur))
                    );
                }
            }
        }

        if (Slot?.DrawUnavailable == true)
        {
            outerChildren.Add(new UnavailableSlashOverlay());
        }

        var hoverIntensity = (float)(hoverAnim?.Value ?? 0);
        if (EnableSpotlight && hoverIntensity > 0f)
        {
            var color = Slot?.HexBackgroundColor?.FromHex() ?? theme.ColorScheme.Primary;
            outerChildren.Add(
                new SpotlightBorder(
                    hoverIntensity,
                    color,
                    hoverDataCtx?.PointerPosition
                )
            );
        }

        return new Stack(outerChildren);
    }
}

/// <summary>
///     Draws a semi-transparent red diagonal slash indicating the slot is unavailable.
/// </summary>
internal sealed class UnavailableSlashOverlay : RenderObjectWidget
{
    /// <inheritdoc />
    public override RenderObject CreateRenderObject() => new RenderUnavailableSlash();

    /// <inheritdoc />
    public override void UpdateRenderObject(RenderObject renderObject)
    {
    }
}

internal sealed class RenderUnavailableSlash : RenderBox
{
    /// <inheritdoc />
    public override bool IsHitTestTarget => false;

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(
            new Vector2(Constraints.MaxWidth, Constraints.MaxHeight)
        );
    }

    /// <inheritdoc />
    protected override void PaintInternal(PaintingContext context)
    {
        if (context.Canvas == null)
        {
            return;
        }

        var paint = context.SharedPaint;
        var strokeW = MathF.Max(2f, Size.X * 0.06f);
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = new SKColor(220, 50, 50, 180);
        paint.StrokeWidth = strokeW;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Shader = null;
        paint.ImageFilter = null;
        var pad = strokeW * 0.5f;
        context.Canvas.DrawLine(pad, pad, Size.X - pad, Size.Y - pad, paint);
    }
}

/// <summary>
///     Positioned bar indicating remaining durability at the bottom of a slot.
///     Color interpolates red → yellow → green based on <paramref name="ratio" />.
/// </summary>
internal class DurabilityBar : StatelessWidget
{
    public DurabilityBar(float ratio, float slotSize)
    {
        Ratio = Math.Clamp(ratio, 0f, 1f);
        SlotSize = slotSize;
    }

    public float Ratio { get; }
    public float SlotSize { get; }

    public override Widget Build(BuildContext context)
    {
        const float margin = 4f;
        const float barH = 3f;
        var trackWidth = SlotSize - margin * 2;
        var fillWidth = Math.Max(trackWidth * Ratio, barH);

        var barColor = Ratio > 0.5f
            ? Vector4.Lerp(
                new Vector4(1f, 0.72f, 0f, 1f),
                new Vector4(0.2f, 0.88f, 0.25f, 1f),
                (Ratio - 0.5f) * 2f)
            : Vector4.Lerp(
                new Vector4(0.88f, 0.15f, 0.15f, 1f),
                new Vector4(1f, 0.72f, 0f, 1f),
                Ratio * 2f);

        return new Stack([
            new Container(new BoxStyle
            {
                // Width = trackWidth,
                Height = barH,
                Color = new Vector4(0f, 0f, 0f, 0.55f),
                CornerRadius = new Vector4(barH / 2f)
            }),
            new Container(new BoxStyle
            {
                Width = fillWidth,
                Height = barH,
                Color = barColor,
                CornerRadius = new Vector4(barH / 2f)
            })
        ]);
    }
}

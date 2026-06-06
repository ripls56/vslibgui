using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     An inventory slot widget with a flat (solid color + animated border) background.
///     Composes <see cref="ItemSlotGestureLayer" />, a theme-driven container, and
///     <see cref="ItemSlotOverlay" />.
/// </summary>
public class FlatItemSlot : StatelessWidget
{
    /// <summary>Creates a flat-background inventory slot.</summary>
    public FlatItemSlot(
        ItemSlot? slot = null,
        SlotController? controller = null,
        ItemSlotStyle? style = null,
        ValueNotifier<bool>? isHighlighted = null,
        IInventory? outerInventory = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Slot = slot;
        Controller = controller;
        Style = style ?? ItemSlotStyle.Default;
        IsHighlighted = isHighlighted;
        OuterInventory = outerInventory;
    }

    /// <summary>The inventory slot to display. May be null for an empty placeholder.</summary>
    public ItemSlot? Slot { get; }

    /// <summary>Handles click, wheel, and drag interactions. May be null.</summary>
    public SlotController? Controller { get; }

    /// <summary>Visual style tokens: size, background color, border colors.</summary>
    public ItemSlotStyle Style { get; }

    /// <summary>
    ///     When set, the slot shows a persistent highlight border whenever <c>Value</c> is <c>true</c>.
    ///     Independent of pointer hover.
    /// </summary>
    public ValueNotifier<bool>? IsHighlighted { get; }

    /// <summary>
    ///     When set, forwarded to <see cref="ItemSlotGestureLayer.OuterInventory" />
    ///     so clicks route through the parent inventory's <c>ActivateSlot</c>.
    /// </summary>
    public IInventory? OuterInventory { get; }

    /// <inheritdoc />
    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        return new ItemSlotGestureLayer(
            slot: Slot,
            controller: Controller,
            isHighlighted: IsHighlighted,
            outerInventory: OuterInventory,
            childBuilder: () => new FlatBackground(Style, Slot)
        );
    }

    private class FlatBackground : StatelessWidget
    {
        private readonly ItemSlot? _slot;
        private readonly ItemSlotStyle _style;

        public FlatBackground(ItemSlotStyle style, ItemSlot? slot)
        {
            _style = style;
            _slot = slot;
        }


        public override Widget Build(BuildContext context)
        {
            var hoverData = ItemSlotHoverData.Of(context);
            var hoverAnim = hoverData?.HoverAnimation;
            var highlighted = hoverData?.IsHighlighted ?? false;
            var theme = Theme.Of(context);
            var slotStyle = theme.ItemSlotStyle;

            var bgColor = _style.BackgroundColor
                          ?? slotStyle.BackgroundColor
                          ?? new Vector4(0, 0, 0, 0.4f);
            var borderColor = _slot?.HexBackgroundColor?.FromHex()
                              ?? _style.BorderColor
                              ?? slotStyle.BorderColor
                              ?? new Vector4(1, 1, 1, 0.2f);
            var borderHoverColor = _slot?.HexBackgroundColor?.FromHex().Lighten(0.2f)
                                   ?? _style.BorderHoverColor
                                   ?? slotStyle.BorderHoverColor
                                   ?? new Vector4(1, 0.8f, 0, 0.8f);

            var hoverT = highlighted ? 1f : (float)(hoverAnim?.Value ?? 0);

            var borderThickness = highlighted ? 2 : 1;
            return new Container(
                new BoxStyle
                {
                    Width = _style.Size,
                    Height = _style.Size,
                    Color = bgColor,
                    BorderThickness = borderThickness,
                    BorderColor = Vector4.Lerp(borderColor, borderHoverColor, hoverT),
                    CornerRadius = new Vector4(2)
                },
                new ItemSlotOverlay(_slot, _style.Size, _style.HoverColor, _style.Padding)
            );
        }
    }
}

using System;
using System.Collections.Generic;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using SkiaSharp;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Lays out a grid of inventory slots arranged in rows and columns.
///     Accepts an array of <see cref="ItemSlot" /> objects and an optional
///     <see cref="SlotController" /> for full inventory interaction.
///     When <see cref="SlotBackground" /> is provided, each slot is rendered
///     with a <see cref="NineSliceItemSlot" />; otherwise a <see cref="FlatItemSlot" />.
/// </summary>
public class SlotGrid : StatelessWidget
{
    /// <summary>
    ///     Creates a new <see cref="SlotGrid" />.
    /// </summary>
    public SlotGrid(
        IReadOnlyList<ItemSlot?> slots,
        SlotController? controller = null,
        int columns = 10,
        float slotSize = 48,
        float spacing = 4,
        SKBitmap? slotBackground = null,
        EdgeInsets? slotBackgroundSlice = null
    )
    {
        Slots = slots;
        Controller = controller;
        Columns = columns;
        SlotSize = slotSize;
        Spacing = spacing;
        SlotBackground = slotBackground;
        SlotBackgroundSlice = slotBackgroundSlice;
    }

    /// <summary>The inventory slots to display.</summary>
    public IReadOnlyList<ItemSlot?> Slots { get; }

    /// <summary>Optional controller for click/wheel interactions.</summary>
    public SlotController? Controller { get; }

    /// <summary>Number of columns in the grid.</summary>
    public int Columns { get; }

    /// <summary>Side length of each slot in pixels.</summary>
    public float SlotSize { get; }

    /// <summary>Spacing between slots.</summary>
    public float Spacing { get; }

    /// <summary>Optional nine-slice bitmap for slot backgrounds.</summary>
    public SKBitmap? SlotBackground { get; }

    /// <summary>Nine-slice insets for <see cref="SlotBackground" />.</summary>
    public EdgeInsets? SlotBackgroundSlice { get; }

    public override Widget Build(
        BuildContext context
    )
    {
        var rows = new List<Widget>();
        var rowCount = (int)Math.Ceiling(
            Slots.Count / (double)Columns
        );


        var theme = Theme.Of(context);
        var slotStyle = theme.ItemSlotStyle with { Size = SlotSize };
        for (var r = 0; r < rowCount; r++)
        {
            var rowChildren = new List<Widget>();
            for (var c = 0; c < Columns; c++)
            {
                var index = r * Columns + c;
                if (index < Slots.Count)
                {
                    Widget slot = SlotBackground != null
                        ? new NineSliceItemSlot(
                            SlotBackground,
                            SlotBackgroundSlice ?? EdgeInsets.All(16),
                            SlotSize,
                            Slots[index],
                            Controller
                        )
                        : new FlatItemSlot(
                            Slots[index],
                            Controller,
                            slotStyle
                        );
                    rowChildren.Add(slot);
                }
                else
                {
                    rowChildren.Add(
                        new Container(
                            new BoxStyle { Width = SlotSize, Height = SlotSize }
                        )
                    );
                }
            }

            rows.Add(
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: Spacing,
                    children: rowChildren
                )
            );
        }

        return new Column(
            mainAxisSize: MainAxisSize.Min,
            spacing: Spacing,
            children: rows
        );
    }
}

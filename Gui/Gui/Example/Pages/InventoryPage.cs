using System;
using System.Collections.Generic;
using System.Linq;
using Gui.Example.Shared;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Inventory;
using Gui.Widgets.Layout;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Example.Pages;

internal class InventoryPage : StatefulWidget
{
    public InventoryPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override State CreateState() => new PageState();

    private class PageState : State<InventoryPage>
    {
        private readonly ValueNotifier<bool> _highlight = new(false);
        private readonly List<ItemSlot?> _slots = [];
        private SlotController _ctrl = null!;
        private IInventory? _hotbar;
        private SKBitmap? _tileTex;

        public override void InitState()
        {
            base.InitState();
            _ctrl = new SlotController(Widget.Capi);
            _hotbar = Widget.Capi.World.Player.InventoryManager.GetOwnInventory("hotbar");
            if (_hotbar != null)
            {
                _ctrl.WatchInventory(_hotbar);
                for (var i = 0; i < Math.Min(8, _hotbar.Count); i++)
                {
                    _slots.Add(_hotbar[i]);
                }
            }

            _tileTex = new SkiaAssetLoader(Widget.Capi).LoadBitmap("gui", "textures/tile000.png");
        }

        public override void Dispose()
        {
            if (_hotbar != null)
            {
                _ctrl.UnwatchInventory(_hotbar);
            }

            _ctrl.Dispose();
            _highlight.Dispose();
            _tileTex?.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var colors = Theme.Of(context).ColorScheme;
            var slot0 = _slots.Count > 0 ? _slots[0] : null;
            var slot1 = _slots.Count > 1 ? _slots[1] : null;
            var slot2 = _slots.Count > 2 ? _slots[2] : null;

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("Inventory",
                        new TextStyle
                        {
                            FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                        }),

                    new DemoCard(
                        "FlatItemSlot",
                        description:
                        "Interactive slot: click, right-click, shift-click, scroll-wheel transfers.",
                        demo: new Row(4, children: _slots
                            .Take(4)
                            .Select(s => (Widget)new FlatItemSlot(s, _ctrl))
                            .ToList()),
                        code: """
                              new FlatItemSlot(
                                slot:       hotbarSlot,
                                controller: _ctrl
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "NineSliceItemSlot",
                        description:
                        "Slot with a nine-slice bitmap background. Corners stay sharp at any size.",
                        demo: _tileTex != null
                            ? new Row(4, children: _slots
                                .Take(5)
                                .Select(s =>
                                {
                                    var slotId = s?.Inventory.GetSlotId(s) ?? 1;
                                    return (Widget)new NineSliceItemSlot(
                                        _tileTex,
                                        EdgeInsets.All(16),
                                        padding: EdgeInsets.All(6 * slotId),
                                        size: 48 * slotId,
                                        slot: s,
                                        controller: _ctrl,
                                        scale: 1f * slotId
                                    );
                                })
                                .ToList())
                            : new Text("Texture not loaded",
                                new TextStyle { Color = colors.Error }),
                        code: """
                              new NineSliceItemSlot(
                                texture:    tileBitmap,
                                slice:      EdgeInsets.All(16),
                                size:       64,
                                slot:       hotbarSlot,
                                controller: _ctrl,
                                scale:      1f
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "SlotGrid",
                        description:
                        "Arranges slots in a configurable grid. Columns and slot size are independent.",
                        demo: new SlotGrid(
                            _slots,
                            _ctrl,
                            4
                        ),
                        code: """
                              new SlotGrid(
                                slots:      hotbarSlots,
                                controller: _ctrl,
                                columns:    4,
                                slotSize:   48
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "IsHighlighted",
                        description:
                        "ValueNotifier<bool> drives a persistent gold highlight border, independent of hover.",
                        demo: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            spacing: 12,
                            children:
                            [
                                new FlatItemSlot(
                                    slot0,
                                    _ctrl,
                                    isHighlighted: _highlight
                                ),
                                new Button(
                                    new Text(
                                        _highlight.Value ? "Clear" : "Highlight",
                                        new TextStyle { Color = colors.OnPrimary }
                                    ),
                                    onTap: _ => SetState(() => _highlight.Value = !_highlight.Value)
                                )
                            ]
                        ),
                        code: """
                              var _highlight = new ValueNotifier<bool>(false);

                              new FlatItemSlot(
                                slot:          slot,
                                controller:    _ctrl,
                                isHighlighted: _highlight
                              )
                              // Toggle: _highlight.Value = !_highlight.Value;
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "ItemSlotStyle — sizes",
                        description:
                        "ItemSlotStyle.Size scales the slot widget (32 / 48 / 64 px shown).",
                        demo: new Row(
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            spacing: 8,
                            children:
                            [
                                new FlatItemSlot(slot0, _ctrl, new ItemSlotStyle { Size = 32 }),
                                new FlatItemSlot(slot1, _ctrl, new ItemSlotStyle { Size = 48 }),
                                new FlatItemSlot(slot2, _ctrl, new ItemSlotStyle { Size = 64 })
                            ]
                        ),
                        code: """
                              new FlatItemSlot(
                                slot:  slot,
                                style: new ItemSlotStyle { Size = 64 }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "HexBackgroundColor",
                        description:
                        "Slot.HexBackgroundColor tints the border — used in VS for dye slots, crafting output, and special inputs.",
                        demo: new Row(8, children:
                        [
                            new FlatItemSlot(new TintedSlot("#ff4444"),
                                style: new ItemSlotStyle { Size = 48 }),
                            new FlatItemSlot(new TintedSlot("#44aaff"),
                                style: new ItemSlotStyle { Size = 48 }),
                            new FlatItemSlot(new TintedSlot("#44ff88"),
                                style: new ItemSlotStyle { Size = 48 }),
                            new FlatItemSlot(new TintedSlot("#ffcc44"),
                                style: new ItemSlotStyle { Size = 48 })
                        ]),
                        code: """
                              class TintedSlot : DummySlot
                              {
                                public TintedSlot(string hex) { HexBackgroundColor = hex; }
                              }

                              new FlatItemSlot(slot: new TintedSlot("#ff4444"))
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "DrawUnavailable",
                        description:
                        "Slot.DrawUnavailable = true overlays a red diagonal slash — e.g. locked crafting inputs.",
                        demo: new Row(4, children:
                        [
                            new FlatItemSlot(new UnavailableSlot(),
                                style: new ItemSlotStyle { Size = 48 }),
                            new FlatItemSlot(new UnavailableSlot(),
                                style: new ItemSlotStyle { Size = 48 }),
                            new FlatItemSlot(new UnavailableSlot(),
                                style: new ItemSlotStyle { Size = 48 })
                        ]),
                        code: """
                              class UnavailableSlot : DummySlot
                              {
                                public override bool DrawUnavailable => true;
                              }

                              new FlatItemSlot(slot: new UnavailableSlot())
                              """,
                        capi: Widget.Capi
                    )
                ]
            );
        }
    }

    private sealed class UnavailableSlot : DummySlot
    {
        public override bool DrawUnavailable => true;
    }

    private sealed class TintedSlot : DummySlot
    {
        public TintedSlot(string hex)
        {
            HexBackgroundColor = hex;
        }
    }
}

# Complex Widgets

## TabView

A tabbed panel with a header row and content area.

```csharp
new TabView(
    tabs:
    [
        new TabItem { Label = "Overview", Content = new OverviewPanel() },
        new TabItem { Label = "Settings", Content = new SettingsPanel() },
    ],
    position: TabPosition.Top,    // Top (default) or Left
    initialIndex: 0
)
```

`TabView` manages its own active tab index via `SetState` — no external state needed.

## FlatItemSlot

Inventory slot with a solid-color background and animated glowing border on hover.

```csharp
var controller = new SlotController(capi);
controller.WatchInventory(hotbarInv);    // auto-update on external inventory changes

new FlatItemSlot(
    slot: inventory[0],
    controller: controller,
    style: new ItemSlotStyle
    {
        Size = 48f,
        BackgroundColor = new Vector4(0, 0, 0, 0.5f),
        BorderColor = new Vector4(0.5f, 0.5f, 0.5f, 1f),
        BorderHoverColor = new Vector4(1f, 0.8f, 0f, 1f)
    }
)
```

## NineSliceItemSlot

Inventory slot rendered with a nine-slice bitmap background instead of a flat box.

```csharp
new NineSliceItemSlot(
    texture: slotTex,
    slice: EdgeInsets.All(16),    // fixed corner insets for nine-slice
    size: 48f,
    slot: inventory[0],
    controller: controller
)
```

## ItemStackDisplay

Renders a single `ItemStack` icon at a given size using the vanilla item-stack renderer.
Hovering the icon shows the vanilla-style item tooltip; pass `showTooltip: false` to disable
it (e.g. when the icon already sits inside a slot that owns a tooltip).

```csharp
new ItemStackDisplay(stack, width: 40f, height: 40f, renderSize: 80)
new ItemStackDisplay(stack, width: 40f, height: 40f, showTooltip: false)
```

Use `RawItemStackDisplay` for the bare render-object widget with no tooltip wrapper.

## ItemStackSlideshow

Cycles through a list of `ItemStack` icons, advancing one element per second.

```csharp
new ItemStackSlideshow(stacks: myList)
new ItemStackSlideshow(stacks: myList, size: 32f)
```

## SlotGrid

A grid of inventory slots for game inventory UIs.

```csharp
new SlotGrid(
    slots: [slot1, slot2, null, slot3],    // null = empty slot
    controller: controller,                 // optional SlotController
    columns: 10,
    slotSize: 48f,
    spacing: 4f,
    slotBackground: slotTextureBitmap,      // optional nine-slice background
    slotBackgroundSlice: EdgeInsets.All(16)
)
```

## AnchoredOverlay

A utility widget for smart positioning of floating content (tooltips, dropdowns, color pickers).
It calculates available screen space and automatically flips between top and bottom sides
while clamping the child within screen bounds.

```csharp
new AnchoredOverlay(
    anchorX: globalPos.X + width / 2, // global X of anchor point
    triggerGlobalY: globalPos.Y,      // global Y of trigger top
    triggerHeight: height,            // height of trigger
    verticalGap: 8f,                  // space between trigger and child
    preferredSide: OverlaySide.Top,   // default side
    horizontal: HorizontalAnchor.Center, // alignment to anchorX
    link: myLayerLink,                // LayerLink to track a CompositedTransformTarget
    child: new MyPopup()
)
```

## Overlay

Manages a dynamic stack of layers (for dropdowns, tooltips, dialogs). `GuiBase.TryOpen()`
automatically wraps your `Build()` result in an `Overlay`.

```csharp
// Insert an entry dynamically from within a widget:
var entry = new OverlayEntry(
    new Positioned(left: 100, top: 100,
        child: new Container(style: new BoxStyle { Width = 200, Height = 100 },
                             child: new Text("Tooltip")))
);
Overlay.Of(context)!.Insert(entry);

// Remove it:
entry.Remove();   // or entry.Dispose()
```

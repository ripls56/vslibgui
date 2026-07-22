# Scrolling Widgets

## ListView

Virtualized scrollable list. Only visible items are rendered.

**Fixed height — builder:**
```csharp
new ListView(
    itemCount: 1000,
    itemHeight: 60f,
    itemBuilder: (ctx, i) => new Text($"Item {i}")
)
```

**Variable height** (items can have different heights):
```csharp
new ListView(
    itemBuilder: (ctx, i) => new IntrinsicHeight(
        child: new MyVariableHeightWidget(items[i])
    ),
    itemCount: items.Count,
    estimatedItemHeight: 44f,
    variableHeight: true,
    stickToBottom: true // auto-scroll to bottom on append
)
```

Parameters:
- `estimatedItemHeight` — initial height estimate for unmeasured items (affects scrollbar accuracy)
- `variableHeight: true` — enables per-item measurement and height caching
- `stickToBottom: true` — auto-scrolls to bottom when new items are appended (useful for chat)
- `reverse: true` — anchor at bottom; offset 0 shows last items

Items are measured on first layout and heights are cached. Scroll offset is corrected
automatically when measured heights differ from estimates.

See [Scrolling](../scrolling.md).

## IntrinsicHeight / IntrinsicWidth

Sizes itself to the child's intrinsic height (or width). Useful for items in a
variable-height `ListView` where the child needs to determine its own natural size.

```csharp
new IntrinsicHeight(
    child: new Column(children: [ new Text("Hello"), new Text("World") ])
)
```

See [Layout](../layout.md).

## GridView

Virtualized scrollable grid with built-in scroll, two layout delegates, and two construction
modes.

**Builder mode** (lazy, virtualized — preferred for large data):

```csharp
GridView.Builder(
    itemCount: 200,
    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 4,
        crossAxisSpacing: 8,
        mainAxisSpacing: 8,
        childAspectRatio: 1.0f     // width / height ratio per cell
    ),
    itemBuilder: (ctx, i) => new Container(
        style: new BoxStyle { Color = new Vector4(0.2f, 0.2f, 0.2f, 1) },
        child: new Center(child: new Text($"{i}"))
    )
)
```

### Grid delegates

| Class | Key parameter | Column count |
|-------|--------------|--------------|
| `SliverGridDelegateWithFixedCrossAxisCount` | `crossAxisCount` | Fixed |
| `SliverGridDelegateWithMaxCrossAxisExtent` | `maxCrossAxisExtent` | Auto: `floor(width / maxExtent)` |

Both delegates accept `mainAxisSpacing`, `crossAxisSpacing`, `childAspectRatio`, and
`fixedItemHeight` (overrides aspect ratio). Both construction modes accept `reverse: true`.

See [Scrolling](../scrolling.md).

## SingleChildScrollView

Non-virtualized scrollable container.

```csharp
new SingleChildScrollView(
    child: new Column(children: manyWidgets)
)
```

Accepts `reverse: true` to anchor scroll origin at the bottom.

## Scrollbar

Standalone wrapper that adds a scrollbar alongside any scrollable child. The scrollbar
and the scrollable widget must share the same `ScrollController`.

```csharp
var controller = new ScrollController();

new Scrollbar(
    controller: controller,
    child: new ListView(
        itemCount: 1000,
        itemHeight: 60,
        controller: controller,
        itemBuilder: (ctx, i) => new Text($"Item {i}")
    )
)
```

Parameters:
- `controller` — shared `ScrollController` (required)
- `child` — the scrollable child widget
- `position` — `ScrollbarPosition.Right` (default) or `ScrollbarPosition.Left`
- `width` — track width in pixels (default 12)
- `trackColor` / `thumbColor` — optional color overrides (theme-based by default)
- `thumbRadius` — corner radius of the thumb (default 4)

Supports thumb dragging and click-to-jump on the track. See [Scrolling](../scrolling.md).

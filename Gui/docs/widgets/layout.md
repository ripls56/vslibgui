# Layout Widgets

## Row

Horizontal flexbox. Children are arranged left-to-right.

```csharp
new Row(
    spacing: 8,                                    // gap between children
    mainAxisAlignment: MainAxisAlignment.Start,    // default
    crossAxisAlignment: CrossAxisAlignment.Start,  // default
    mainAxisSize: MainAxisSize.Max,                // default: fill available width
    children: [ ... ]
)
```

Use `MainAxisSize.Min` to shrink-wrap the row to its children's total width.

## Column

Vertical flexbox. Children are arranged top-to-bottom.

```csharp
new Column(
    spacing: 12,
    mainAxisAlignment: MainAxisAlignment.Center,
    crossAxisAlignment: CrossAxisAlignment.Stretch,
    mainAxisSize: MainAxisSize.Min,                // shrink to content height
    children: [ ... ]
)
```

Use `MainAxisSize.Max` (default) to fill all available height.

## Wrap

Lays out children in runs, wrapping to the next line when a child would overflow the
available main-axis space. Similar to CSS `flex-wrap`. Unlike Row/Column, Wrap handles
overflow by creating new runs instead of clipping or overflowing.

```csharp
new Wrap(
    direction: FlexDirection.Horizontal,  // or Vertical
    spacing: 8,                           // gap between children in a run
    runSpacing: 4,                        // gap between runs
    mainAxisAlignment: MainAxisAlignment.Start,
    crossAxisAlignment: CrossAxisAlignment.Start,
    runAlignment: MainAxisAlignment.Start, // how runs are distributed
    children: [
        new Chip("Tag1"),
        new Chip("Tag2"),
        new Chip("LongTag3"),
        // wraps to next line if no room
    ]
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `direction` | `FlexDirection` | `Horizontal` | Primary axis direction |
| `spacing` | `float` | `0` | Space between children within a run |
| `runSpacing` | `float` | `0` | Space between runs |
| `mainAxisAlignment` | `MainAxisAlignment` | `Start` | Child alignment within a run |
| `crossAxisAlignment` | `CrossAxisAlignment` | `Start` | Child cross-axis alignment within a run |
| `runAlignment` | `MainAxisAlignment` | `Start` | Run distribution across the cross axis |

## Stack

Layers children on top of each other. Sizes to unpositioned children; fills parent if all
children are positioned.

```csharp
new Stack(
[
    new Container(style: new BoxStyle { Color = Colors.Background }),
    new Positioned(right: 8, top: 8, child: new CloseButton()),
])
```

## Positioned

Places a child at absolute coordinates within a `Stack`. Only valid as a direct child of
`Stack`.

```csharp
new Positioned(
    left: 10,           // offset from left edge (optional)
    top: 20,            // offset from top edge (optional)
    right: null,        // offset from right edge (optional)
    bottom: null,       // offset from bottom edge (optional)
    width: 200,         // explicit width (optional)
    height: 100,        // explicit height (optional)
    child: new MyWidget()
)
```

At least one of `left`/`right` and at least one of `top`/`bottom` (or explicit `width`/`height`)
should be set. Conflicting constraints (e.g., both `left` and `right`) define an exact size.

## Expanded

Tells its `Row`/`Column` parent to give it all remaining main-axis space. Must be a direct
child of `Row` or `Column`.

```csharp
new Row(children:
[
    new Text("Name:"),
    new Expanded(flex: 1, child: new TextField()),   // gets remaining space
])
```

Multiple `Expanded` children share remaining space by their `flex` ratio.

## SizedBox

Imposes explicit size constraints. With no child, acts as a spacer.

```csharp
new SizedBox(width: 200, height: 100, child: new Text("Fixed size"))
new SizedBox(height: 16)    // 16px vertical spacer in Column
new SizedBox(width: 8)      // 8px horizontal spacer in Row
```

## Padding

Adds inset space around its child.

```csharp
new Padding(EdgeInsets.All(16), child: new Text("Padded"))
new Padding(EdgeInsets.Only(left: 12, top: 8), child: new Text("..."))
```

## Align

Positions a child at a point within the parent using a normalized `(-1,-1)` to `(1,1)` space.

```csharp
new Align(alignment: Alignment.BottomRight, child: new Text("BR"))
new Align(alignment: new Alignment(0, -0.5f), child: new Text("Custom"))
```

Predefined constants: `TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `Center`,
`CenterRight`, `BottomLeft`, `BottomCenter`, `BottomRight`.

## Center

Shorthand for `Align(Alignment.Center)`.

```csharp
new Center(child: new Text("Centered"))
```

# Scrolling

## ListView — Virtualized Scrolling

`ListView` is the primary widget for scrollable lists. It **virtualizes** rendering: only
items visible in the viewport (plus 1 buffer item each side) are created. A list of 10,000
items only maintains ~15 live elements at any time.

```csharp
new ListView(
    itemCount: 1000,
    itemHeight: 60,          // all items must have the same height
    itemBuilder: (ctx, index) => new Container(
        style: new BoxStyle
        {
            Color = new Vector4(0.15f, 0.15f, 0.15f, 0.8f),
            CornerRadius = new Vector4(4),
            Padding = EdgeInsets.All(10)
        },
        child: new Text($"Item #{index}")
    )
)
```

For short, fixed lists the static-children constructor is simpler — no builder or count needed:

```csharp
new ListView(
    children: myWidgets,     // IEnumerable<Widget>
    itemHeight: 40f
)
```

### Virtualization details

The visible index range is computed each rebuild:
```
firstVisible = floor(scrollOffset / itemHeight) - 1
lastVisible  = ceil((scrollOffset + viewportHeight) / itemHeight) + 1
```

Items outside this range are **unmounted** (their elements and render objects are destroyed).
Items that enter the range are freshly created from `itemBuilder`. The built widgets are cached
by index to avoid calling `itemBuilder` repeatedly for the same index in the same frame.

### External scroll controller

```csharp
var ctrl = new ScrollController();
ctrl.Attach(tickerProvider);

// Jump to position:
ctrl.JumpTo(500f);

// Start kinetic scroll:
ctrl.StartSimulation(velocity: 800f, min: 0, max: 6000);

new ListView(itemCount: 100, itemHeight: 60, controller: ctrl,
    itemBuilder: (ctx, i) => new Text($"Item {i}"))
```

## GridView — Virtualized Scrollable Grid

`GridView` arranges items in a multi-column grid with built-in vertical scrolling and
virtualization. Like `ListView`, only cells visible in the viewport are created.

### Builder mode

```csharp
GridView.Builder(
    itemCount: 500,
    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 5,
        crossAxisSpacing: 8,
        mainAxisSpacing: 8,
        childAspectRatio: 1.0f   // cell width / cell height
    ),
    itemBuilder: (ctx, i) => new Container(
        style: new BoxStyle
        {
            Color = new Vector4(0.15f, 0.15f, 0.15f, 0.9f),
            CornerRadius = new Vector4(4)
        },
        child: new Center(child: new Text($"{i}"))
    )
)
```

### Static mode

```csharp
new GridView(
    children: items.Select(item => new ItemCard(item)).ToList(),
    gridDelegate: new SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 160,
        crossAxisSpacing: 6,
        mainAxisSpacing: 6,
        fixedItemHeight: 100
    )
)
```

Both modes accept `reverse: true` to anchor the scroll at the bottom.

### Grid delegates

| Parameter | `FixedCrossAxisCount` | `MaxCrossAxisExtent` |
|-----------|----------------------|---------------------|
| Column count | Fixed `crossAxisCount` | `floor(width / maxExtent)` |
| `crossAxisSpacing` | Column gap (px) | Column gap (px) |
| `mainAxisSpacing` | Row gap (px) | Row gap (px) |
| `childAspectRatio` | `cellWidth / cellHeight` | `cellWidth / cellHeight` |
| `fixedItemHeight` | Overrides aspect ratio | Overrides aspect ratio |

### Virtualization details

`GridView.Builder` uses the same index-keyed approach as `ListView`:

```
rowHeight  = CellHeight + MainAxisSpacing
firstRow   = floor(scrollOffset / rowHeight) - 1   (one-row buffer)
lastRow    = ceil((scrollOffset + viewportHeight) / rowHeight)
firstIndex = firstRow  × crossAxisCount
lastIndex  = lastRow   × crossAxisCount + (crossAxisCount - 1), clamped to itemCount - 1
```

Cells outside the visible band are **unmounted**. Cells that scroll into view call
`itemBuilder` for fresh widgets. Static mode (`children: [...]`) uses the same range
calculation but reads from the supplied list instead of calling a builder.

### External scroll controller

```csharp
var ctrl = new ScrollController();
ctrl.Attach(tickerProvider);

GridView.Builder(
    itemCount: 1000,
    gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(4),
    controller: ctrl,
    itemBuilder: (ctx, i) => new Text($"{i}")
)
```

## SingleChildScrollView — Non-Virtualized Scrolling

For small, non-virtualized scrollable content. The entire child is built regardless of
visibility. Use `ListView` instead when the content has many uniform items.

```csharp
new SingleChildScrollView(
    child: new Column(children: manyItems)
)

// Reverse: offset 0 = bottom of content
new SingleChildScrollView(
    reverse: true,
    child: new Column(children: manyItems)
)
```

`SingleChildScrollView` uses kinetic physics (same as `ListView`) for wheel-driven scrolling.

## ScrollController

`ScrollController` is the shared state between a scroll widget and any external controller.
It manages the scroll offset and runs physics simulations for kinetic (fling) scrolling.

```csharp
var ctrl = new ScrollController();
ctrl.Attach(tickerProvider);   // must be attached before use

// React to scroll changes:
ctrl.OnChanged += () => Console.WriteLine($"Offset: {ctrl.Offset}");

// Programmatic control:
ctrl.JumpTo(0f);                                     // instant snap
ctrl.StartSimulation(velocity: 600f, min: 0f, max: maxScroll);  // fling
ctrl.AnimateTo(200f, TimeSpan.FromMilliseconds(300), Curves.EaseOut);  // animated

// Cleanup:
ctrl.Dispose();
```

### Properties

| Property | Description |
|----------|-------------|
| `Offset` | Current scroll position in pixels |
| `ViewportSize` | Visible viewport height (set by the scrollable widget during layout) |
| `ContentSize` | Total content height (set by the scrollable widget during layout) |
| `MaxScrollExtent` | `Max(0, ContentSize - ViewportSize)` — maximum scrollable offset |
| `OnChanged` | Event fired when `Offset`, `ViewportSize`, or `ContentSize` changes |

## Scrollable.EnsureVisible

Scrolls the nearest scrollable ancestor so that a target element (identified by
`GlobalKey`) becomes fully visible in the viewport. Supports both instant and
animated scrolling.

```csharp
// 1. Allocate a GlobalKey (once, e.g. in InitState)
private GlobalKey _targetKey = new GlobalKey();

// 2. Attach the key to the widget you want to scroll to
new MyWidget(key: _targetKey, ...)

// 3. Call EnsureVisible when needed
Scrollable.EnsureVisible(_targetKey.CurrentElement!);

// With animation:
Scrollable.EnsureVisible(
    _targetKey.CurrentElement!,
    duration: TimeSpan.FromMilliseconds(150),
    curve: Curves.EaseOut);
```

The algorithm walks up the element tree to find the nearest `SingleChildScrollView`
or `ListView`, computes the target's Y position in content space by walking the
render-object parent chain, then applies the minimal scroll offset to bring the
element into the viewport.

## Kinetic Scrolling Physics

`ListView` uses `FrictionSimulation` for inertia scrolling. The simulation models exponential
velocity decay:

```
position(t) = x0 + v0 * (1 - e^(-drag * t)) / drag
velocity(t) = v0 * e^(-drag * t)
```

The default physics preset is `ClampingScrollPhysics` (drag `25.0`, min velocity `100 px/s`) —
snappy deceleration suited to game UI. Wheel events accumulate velocity: multiple rapid
scroll events combine into a faster fling. Reversing direction (scrolling down while still
moving up) starts a new simulation from the current position.

## Scrollbar

`Scrollbar` is a standalone wrapper widget that displays a scrollbar alongside its child.
It is **not** built into `ListView`, `SingleChildScrollView`, or `GridView` — you wrap them
yourself to get a scrollbar. The scrollbar and the scrollable widget share a
`ScrollController`.

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

### Interaction

- **Thumb drag** — click and drag the thumb to scroll
- **Click-to-jump** — click on the track outside the thumb to jump to that position
- **Theme colors** — track and thumb colors default to `ColorScheme.OnSurface` with
  alpha 0.1 (track) and 0.4 (thumb)

## Viewport

`RenderViewport` (used by both `ListView` and `SingleChildScrollView`) clips its child to its
own bounds and translates by the negative scroll offset, creating the scroll effect:

```
canvas.ClipRect([0, 0, Size.X, Size.Y])
canvas.Translate(0, -scrollOffset)
// draw child
```

Hit testing compensates for the translation: pointer positions are offset by `+scrollOffset`
before testing child bounds.

## Layout Within Scroll Widgets

`Viewport` passes child constraints of `Loose(viewportWidth, 100000)` — the child can be any
height. The child sizes to fit its content. The viewport clips everything beyond its own
bounds.

This means `Expanded` children inside a scroll view will not work correctly: they need an
explicit parent height to expand into, but the scroll view provides unbounded height.

```csharp
// Wrong: Expanded needs bounded height
new SingleChildScrollView(
    child: new Column(children: [new Expanded(child: new Text("..."))])
)

// Correct: give explicit height instead
new SingleChildScrollView(
    child: new Column(children:
    [
        new SizedBox(height: 200, child: new Text("...")),
        new Text("More content")
    ])
)
```

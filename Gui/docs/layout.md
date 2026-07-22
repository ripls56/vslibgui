# Layout

## LayoutConstraints

`LayoutConstraints` is a value struct that describes the minimum and maximum size a
`RenderObject` is allowed to be.

```csharp
public struct LayoutConstraints
{
    public float MinWidth, MaxWidth, MinHeight, MaxHeight;
}
```

### Factories

| Factory | Meaning |
|---------|---------|
| `LayoutConstraints.Tight(w, h)` | `min == max` — forces exact size |
| `LayoutConstraints.Loose(maxW, maxH)` | `min == 0` — child can be any size up to max |
| `LayoutConstraints.TightFor(width?, height?)` | Tight on specified axes, infinite on others |
| `constraints.Loosen()` | Zero the minimums, keep the maximums |
| `constraints.Enforce(other)` | Intersect two constraint ranges |
| `constraints.Constrain(size)` | Clamp a `Vector2` to the box |

### Constraint propagation

Each `RenderObject.PerformLayout()` decides what constraints to pass to its children. The root
receives constraints based on the `WindowConfig`: `Tight(windowSize)` for fixed-size windows,
or `Loose(screenWidth, screenHeight)` for shrink-wrap windows. Children receive constraints
derived from the parent's own constraints minus any padding, borders, or fixed sizes.

A child cannot exceed the `MaxWidth`/`MaxHeight` it receives. A child that reports a size
smaller than `MinWidth`/`MinHeight` will be clamped up by `Layout()` after `PerformLayout()`.

## Row and Column (RenderFlex)

`Row` and `Column` implement a flexbox algorithm. Both use `RenderFlex` internally.

```csharp
// Row — horizontal flex
new Row(
    spacing: 8,
    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
    crossAxisAlignment: CrossAxisAlignment.Center,
    children: [ ... ]
)

// Column — vertical flex
new Column(
    spacing: 12,
    mainAxisAlignment: MainAxisAlignment.Start,
    crossAxisAlignment: CrossAxisAlignment.Stretch,
    children: [ ... ]
)
```

### MainAxisAlignment

Controls how children are distributed along the main axis (horizontal for Row, vertical for
Column) after all children have been sized.

| Value | Behavior |
|-------|----------|
| `Start` | Pack children to the start edge |
| `End` | Pack children to the end edge |
| `Center` | Center children as a group |
| `SpaceBetween` | First/last children at edges; equal gaps between |
| `SpaceAround` | Equal gaps around each child (half gap at edges) |
| `SpaceEvenly` | Equal gaps between all children and at edges |

### CrossAxisAlignment

Controls child positioning on the perpendicular axis.

| Value | Behavior |
|-------|----------|
| `Start` | Align to the start edge of the cross axis |
| `Center` | Center on the cross axis |
| `End` | Align to the end edge of the cross axis |
| `Stretch` | Force child to fill the full cross-axis extent |

### MainAxisSize

Controls how much main-axis space the container itself occupies.

| Value             | Behavior                                            |
|-------------------|-----------------------------------------------------|
| `Max` *(default)* | Container fills all available main-axis space       |
| `Min`             | Container shrinks to the total size of its children |

```csharp
// Shrink-wrap: Row is exactly as wide as its children + spacing
new Row(
    mainAxisSize: MainAxisSize.Min,
    children: [ new Icon("star"), new Text("Label") ]
)

// Fill: Row stretches to the full width it was given (default)
new Row(
    mainAxisSize: MainAxisSize.Max,
    children: [ new Text("Left"), new Text("Right") ]
)
```

> **Note:** `MainAxisSize` only has a visible effect when the container has no `Expanded`
> children. When `Expanded` children are present, `MainAxisSize.Max` forces the container to
> claim the full available space (so flex fractions are computed correctly), while
> `MainAxisSize.Min` makes the container report the actual consumed size instead.

### Expanded — taking remaining space

`Expanded` tells its flex parent to give it all remaining main-axis space after fixed children
are sized. Multiple `Expanded` children share space proportionally via their `flex` factor:

```csharp
new Row(children:
[
    new Text("Label"),          // fixed width
    new Expanded(               // gets 2/3 of remaining space
        flex: 2,
        child: new TextField()
    ),
    new Expanded(               // gets 1/3 of remaining space
        flex: 1,
        child: new NumericField()
    ),
])
```

`Expanded` only works as a **direct child** of `Row` or `Column`. Using it elsewhere has no
effect.

### Layout algorithm (three passes)

1. **Fixed pass** — all non-`Expanded` children are laid out with the full cross-axis constraint
   (or tight cross if `Stretch`). Their main-axis sizes are accumulated.
2. **Flex pass** — remaining main-axis space is divided among `Expanded` children by flex factor.
3. **Position pass** — children are placed using `MainAxisAlignment` and `CrossAxisAlignment`.

## Stack and Positioned (RenderStack)

`Stack` layers children on top of each other. Unpositioned children are placed at `(0, 0)` and
the stack's size is determined by their bounding box. `Positioned` children are placed
absolutely using offset values.

```csharp
new Stack(
[
    // Unpositioned — fills the stack (or sizes naturally)
    new Container(style: new BoxStyle { Color = new Vector4(0.1f, 0.1f, 0.1f, 1) }),

    // Positioned — 10px from left, 20px from top, 100px wide
    new Positioned(
        left: 10, top: 20, width: 100,
        child: new Text("Overlay label")
    ),

    // Positioned from the right edge
    new Positioned(
        right: 8, top: 8,
        child: new CloseButton()
    ),
])
```

`Positioned` only has meaning as a **direct child** of `Stack`. It stores its offset values in
`StackParentData` on its child's `RenderObject`.

### Stack sizing rules

- If all children are positioned, the stack sizes itself to its own parent's constraints
  (fills available space).
- If there are any unpositioned children, the stack sizes to their combined bounding box, then
  positioned children are placed relative to that size.

## Sizing Widgets

### SizedBox — explicit size or spacer

```csharp
// Fixed-size box
new SizedBox(width: 200, height: 50, child: new Text("..."))

// Spacer in a Row/Column
new SizedBox(width: 16)      // horizontal spacer
new SizedBox(height: 8)      // vertical spacer
```

If both `width` and `height` are null, `SizedBox` is equivalent to a transparent `Container`
that sizes to its child.

### Container — visual box with optional size

```csharp
new Container(
    style: new BoxStyle
    {
        Width = 300,        // fixed width (optional)
        Height = 200,       // fixed height (optional)
        Padding = EdgeInsets.All(16),
        Color = new Vector4(0.1f, 0.1f, 0.1f, 0.9f),
        CornerRadius = new Vector4(8),
        BorderThickness = 1,
        BorderColor = new Vector4(1, 1, 1, 0.3f),
        ClipBehavior = ClipBehavior.AntiAlias,
    },
    child: new Text("Content")
)
```

`Container` internally composes `Padding` + a constrained box. When `Width`/`Height` are set,
it imposes tight constraints on its child via `RenderConstrainedBox`.

### BoxStyle.CornerRadius

`CornerRadius` is a `Vector4` where each component maps to a corner:

| Component | Corner |
|-----------|--------|
| `X` | Top-right |
| `Y` | Bottom-right |
| `Z` | Top-left |
| `W` | Bottom-left |

Use `new Vector4(r)` to set all corners uniformly to radius `r`.

## Padding and EdgeInsets

```csharp
new Padding(EdgeInsets.All(16), child: new Text("Padded"))
```

`EdgeInsets` factory methods:

```csharp
EdgeInsets.All(16)                                   // 16px on all sides
EdgeInsets.Only(left: 8, top: 4, right: 8, bottom: 4)
EdgeInsets.Symmetric(vertical: 12, horizontal: 20)
EdgeInsets.Ltrb(left, top, right, bottom)
EdgeInsets.Zero                                      // no padding
```

## Alignment

### Align and Center

`Align` positions a child at any point within its parent using a normalized coordinate system
where `(-1, -1)` is top-left and `(1, 1)` is bottom-right:

```csharp
new Align(alignment: Alignment.Center, child: myWidget)
new Align(alignment: Alignment.BottomRight, child: myWidget)
new Align(alignment: new Alignment(0, -0.5f), child: myWidget) // custom
```

`Center` is shorthand for `Align(Alignment.Center)`:

```csharp
new Center(child: myWidget)
```

`Align` sizes itself to fill its parent when the parent provides finite constraints. When
infinite space is available (e.g., inside a scroll view), it sizes to the child.

### Alignment constants

`TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `Center`, `CenterRight`, `BottomLeft`,
`BottomCenter`, `BottomRight`.

## Clipping

`ClipBehavior` can be set on any `Container` (via `BoxStyle.ClipBehavior`) or applied
explicitly with the `Clip` widget:

```csharp
new Clip(
    borderRadius: new Vector4(12),   // all corners
    child: new Image("domain", "path/image.png", fit: BoxFit.Cover)
)
```

| `ClipBehavior` | Behavior |
|---------------|----------|
| `None` | No clipping (default) |
| `HardEdge` | Clip to rect; no anti-aliasing on clip edge |
| `AntiAlias` | Clip to rounded rect with anti-aliased edge |

## Baseline Alignment

### CrossAxisAlignment.Baseline

When laying out a horizontal `Row`, `CrossAxisAlignment.Baseline` co-aligns children
along their alphabetic text baseline instead of their top/center/bottom edges. This is
useful for rows mixing text of different font sizes.

```csharp
new Row(
    crossAxisAlignment: CrossAxisAlignment.Baseline,
    children: [
        new Text("Label", style: new TextStyle { FontSize = 12 }),
        new Text("Value", style: new TextStyle { FontSize = 20 }),
    ]
)
```

The row height is computed as `maxBaseline + maxBelow`, where `maxBaseline` is the
largest distance-to-baseline among all children and `maxBelow` is the largest
distance from baseline to the child's bottom edge. Each child is offset vertically
by `maxBaseline - childBaseline`.

For children without an intrinsic baseline (non-text render objects), `Size.Y` is used
as the fallback baseline distance.

**Vertical `Column` with `CrossAxisAlignment.Baseline`** falls back to `Start` behavior.

### TextBaseline enum

```csharp
TextBaseline.Alphabetic   // Latin baseline (default for CrossAxisAlignment.Baseline)
TextBaseline.Ideographic  // CJK baseline (CapHeight approximation)
```

`RenderText` and `RenderRichText` implement `GetDistanceToBaseline(TextBaseline)` using
`SKFontMetrics`. Custom render objects can override this method to participate in
baseline alignment.

## LayoutBuilder

`LayoutBuilder` builds its child tree based on the constraints it receives from its
parent. Use it when a widget's structure depends on available space.

```csharp
new LayoutBuilder(
    builder: (ctx, constraints) =>
    {
        if (constraints.MaxWidth < 300)
            return new Column(children: [ /* compact layout */ ]);
        else
            return new Row(children: [ /* wide layout */ ]);
    }
)
```

### Rebuild timing

`LayoutBuilder` uses a one-frame-lag approach:

- **Frame N**: element builds with last known constraints (default on first frame).
  `RenderLayoutBuilder` lays out the child and, if constraints changed, marks the
  element dirty.
- **Frame N+1**: `BuildOwner` rebuilds the element with the new constraints. Layout
  runs again with the correctly built child.

This avoids re-entrancy between the build and layout phases. In practice, the lag
is invisible because the initial constraints are often the same as subsequent ones
(e.g., a fixed-size parent).

### GetDistanceToBaseline

Any render object can participate in baseline alignment by overriding:

```csharp
public override float? GetDistanceToBaseline(TextBaseline baseline)
{
    // return distance from top of this RO to its baseline, or null
}
```

Returning `null` causes `RenderFlex` to use `Size.Y` as the fallback baseline.

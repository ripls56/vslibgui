# Key Types & Misc

## Key and ValueKey\<T\>

Keys provide stable element identity across rebuilds. Use them when list items can be
reordered:

```csharp
new Column(children: items.Select(item =>
    new ItemRow(key: new ValueKey<int>(item.Id), item: item)
))
```

## GlobalKey

A key that is unique across the entire widget tree, providing cross-subtree access to the
element and its render object. Used by `Scrollable.EnsureVisible` and other imperative APIs.

## Scrollable.EnsureVisible

Scrolls the nearest scrollable ancestor (`SingleChildScrollView` or `ListView`) so that the
target element is fully visible. Supports instant jump and animated scroll.

## ScrollController.AnimateTo

Smoothly animates the scroll offset to a target value over a given duration.

## EdgeInsets

```csharp
EdgeInsets.Zero
EdgeInsets.All(16)
EdgeInsets.Only(left: 8, top: 4, right: 8, bottom: 4)
EdgeInsets.Symmetric(vertical: 12, horizontal: 20)
EdgeInsets.LTRB(left, top, right, bottom)
```

## Alignment

Normalized 2D position: `(-1, -1)` = top-left, `(0, 0)` = center, `(1, 1)` = bottom-right.

---

## LayoutBuilder

`Gui.Widgets.Layout.LayoutBuilder`

Builds its child tree based on the incoming `LayoutConstraints`. The `builder` callback
receives the actual constraints at layout time and returns a widget.

```csharp
new LayoutBuilder(
    builder: (ctx, constraints) =>
        constraints.MaxWidth >= 400
            ? new Row(children: [ new Icon("icon"), new Text("Wide") ])
            : new Text("Narrow")
)
```

**Behavior:**
- Child is rebuilt the frame after constraints change (one-frame lag).
- On the first frame the builder is called with the default `LayoutConstraints` (all zeros);
  on the next frame it receives the real parent constraints.
- The `RenderLayoutBuilder` sizes itself to its child.

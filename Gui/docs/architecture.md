# Architecture

The Library is a faithful C# port of Flutter's three-tree architecture, running on top of
Vintage Story's game engine with Skia (SkiaSharp) as the GPU backend.

## The Three Trees

```
┌─────────────────────────────────────────────────────────┐
│  Widget Tree  (immutable, recreated each rebuild)       │
│  Widget → StatelessWidget, StatefulWidget, ...          │
└───────────────────────┬─────────────────────────────────┘
                        │ CreateElement()
                        ▼
┌─────────────────────────────────────────────────────────┐
│  Element Tree  (mutable, reconciled, long-lived)        │
│  Element → ComponentElement, StatefulElement, ...       │
│  Holds: State objects, BuildOwner reference, Depth      │
└───────────────────────┬─────────────────────────────────┘
                        │ CreateRenderObject() / UpdateRenderObject()
                        ▼
┌─────────────────────────────────────────────────────────┐
│  RenderObject Tree  (mutable layout + paint)            │
│  RenderObject → RenderBox, RenderFlex, RenderStack, ... │
│  Owns: Size, X/Y, dirty flags, Skia draw calls          │
└───────────────────────┬─────────────────────────────────┘
                        │ PaintInternal()
                        ▼
┌─────────────────────────────────────────────────────────┐
│  PaintingContext → SkiaRenderer → OpenGL framebuffer    │
└─────────────────────────────────────────────────────────┘
```

### Widget Tree

Widgets are **immutable value objects** — pure configuration with no mutable state and no GPU
resources. They are cheap to create and can be discarded and recreated on every rebuild.

```csharp
// Widget instances are created fresh in every Build() call:
new Text("Hello", new TextStyle { FontSize = 14 })
// ↑ This object may be thrown away after one frame — that is normal.
```

The key method on every widget is `CreateElement()`, which produces the corresponding element
type. `Widget.CanUpdate(oldWidget, newWidget)` determines whether an existing element can be
reused: it returns `true` if both widgets have the same runtime type **and** the same `Key`.

### Element Tree

Elements are **long-lived** objects that persist across rebuilds. They are the mutable layer
that bridges immutable widgets and the render tree. The element tree:

- Holds `State<T>` objects for stateful widgets
- Tracks parent-child relationships and tree depth
- Drives reconciliation: when a widget is new but compatible with an existing element (same
  type + key), the element is **updated** (`element.Update(newWidget)`) rather than destroyed

Key element types:

| Element | Used by | Behavior |
|---------|---------|----------|
| `ComponentElement` | `StatelessWidget` | Calls `widget.Build()`, stores result as `_child` |
| `StatefulElement` | `StatefulWidget` | Holds `State`, calls `state.Build()` |
| `RenderObjectElement` | Leaf render widgets | Creates and owns a `RenderObject` |
| `SingleChildElement` | Single-child render widgets | Threads child's RO into parent RO |
| `MultiChildElement` | Multi-child render widgets | Keeps ordered list of child elements |

#### Element Lifecycle

```
widget.CreateElement()
        │
        ▼
element.Mount(parent)       ← attach to tree; InitState() for stateful
        │
        ▼ (on rebuild)
element.Update(newWidget)   ← parent rebuilt with a compatible new widget
        │
        ▼ (on removal)
element.Unmount()           ← detach; State.Dispose(); RenderObject.Dispose()
```

### RenderObject Tree

`RenderObject` nodes own:
- **Layout**: `Size`, position (`X`, `Y`), `LayoutConstraints`, dirty flags `NeedsLayout` /
  `ChildNeedsLayout`
- **Paint**: `NeedsPaint` / `ChildNeedsPaint`, the actual Skia draw calls in `PaintInternal()`

Setting any property that affects layout or paint calls `MarkNeedsLayout()` or
`MarkNeedsPaint()`, which propagates `ChildNeeds*` flags up the parent chain. The frame loop
checks only the root's flags before deciding whether to relayout or repaint.

## Reconciliation

The `UpdateChild(child, newWidget)` method is the heart of reconciliation. Called during
every rebuild, it decides what to do with a child slot:

```
newWidget == null           → unmount child, return null
child == null               → create fresh element from newWidget
child.Widget == newWidget   → same reference, skip (no-op)
CanUpdate(child, newWidget) → reuse element: child.Update(newWidget)
otherwise                   → unmount child, create fresh element
```

`CanUpdate` checks **type + Key equality**. Without a `Key`, identity is purely positional —
the element at position N is reused if the new widget at position N has the same type. Use
`ValueKey<T>` when list items can be reordered:

```csharp
new Row(children: items.Select(item =>
    new Container(key: new ValueKey<int>(item.Id), ...)
))
```

## BuildOwner

`BuildOwner` is the central scheduler for the widget rebuild pass. It maintains a
`HashSet<Element>` of dirty elements. When `State.SetState()` or `Element.MarkNeedsBuild()`
is called, the element is added to this set.

`BuildDirtyElements()` runs once per frame:

1. Snapshot the dirty set and clear it
2. Sort by `Depth` (parents before children) — guarantees a parent rebuild can clear a child's
   dirty flag before the child is processed, preventing redundant rebuilds
3. Skip elements whose `IsDirty` flag has been cleared by a parent rebuild
4. Call `element.Rebuild()` on each remaining dirty element
5. Loop until no new dirty elements remain (rebuilds can cascade)

## Frame Loop

`SkiaRenderer.Begin/End` are called by the render pipeline stages (`PreSkiaPipeline` /
`PostSkiaPipeline`) — once per frame, wrapping all windows. Inside that window,
`GuiBase.OnRenderGUI` runs for each open window:

```
[PreSkiaPipeline — once per frame]
  SkiaRenderer.Begin(width, height)     // acquire/resize Skia GL surface

[GuiBase.OnRenderGUI — per open window]
1. RenderObject.AdvanceFrame()           // increment global frame ID
2. _tickerScheduler.Update(elapsed)      // fire active animation tickers
                                         //   → AnimationController.OnValueChanged
                                         //   → AnimatedBuilder.SetState → dirty elements
3. new/reset PaintingContext(canvas)
4. BuildOwner.BuildDirtyElements()       // rebuild dirty widget subtrees
5. if (rootRo.NeedsLayout || ChildNeedsLayout)
       rootRo.Layout(window constraints) // Tight(windowSize) or Loose(screenSize)
6. canvas.Translate(WindowPos)           // position window on screen
   canvas.ClipRect(windowBounds)         // clip to window area
7. rootRo.Paint(context)                 // recursive paint
8. DebugPainter (if enabled)

[PostSkiaPipeline — once per frame]
  SkiaRenderer.End()                    // flush Skia, restore GL state
  prevShader.Use()                      // restore game shader
```

The key insight is **step 4 before step 5**: widget rebuilds happen first, potentially
changing the render tree's structure, and only then does layout and paint run.

Each `GuiBase` instance runs this loop independently, so multiple windows are fully isolated
rendering passes. Window position, drag, and resize are handled before the paint pass via
mouse event processing in `HandleMouseDown` / `HandleMouseMove` / `HandleMouseUp`.

## State Reactivity Flow

```
State.SetState(fn)
  → fn() mutates state fields
  → Element.MarkNeedsBuild()
    → BuildOwner.ScheduleBuildFor(element)
      → next frame: BuildDirtyElements()
        → state.Build(ctx) returns new widget tree
          → UpdateChild reconciles with existing elements
            → changed render objects call MarkNeedsLayout() / MarkNeedsPaint()
              → Layout + Paint pass re-runs on dirty branches only
```

## Animation Reactivity Flow

```
Ticker.Tick(elapsed)                     // TickerScheduler fires each frame
  → AnimationController.OnTick(delta)
    → _value += deltaPercent
    → OnValueChanged?.Invoke(_value)
      → AnimatedBuilder: SetState(() => {})
        → Element marked dirty
          → next BuildDirtyElements: builder(ctx) called
            → new widget tree with updated animated values
```

## Key Design Decisions

**Widget identity by type + key, not reference.** Two widgets are considered the same slot if
their runtime type and `Key` are equal — content is irrelevant to this check. When a parent
rebuilds and passes a new `Text("world")` where `Text("hello")` was, the existing element and
RenderObject are reused and updated in place (triggering repaint), not reconstructed. Only a
type or key mismatch causes the element to be unmounted and replaced.

**Dirty flags propagate upward, not downward.** `MarkNeedsLayout()` walks up setting
`ChildNeedsLayout` on ancestors. The root can then decide in O(1) whether any relayout is
needed at all.

**`BuildOwner` sorts dirty elements by depth.** This single sort prevents O(n²) redundant
rebuilds when a parent and its descendants are both dirty.

**Animations are purely event-driven.** There is no polling or per-frame check outside the
ticker system. If nothing is animating, `BuildDirtyElements()` is a no-op.

**`RepaintBoundary` isolates subtrees.** Subtrees wrapped in `RepaintBoundary` are cached as
`SKPicture` objects. On frames where nothing in that subtree changed, the picture is replayed
without re-executing any paint logic. See [Rendering](rendering.md).

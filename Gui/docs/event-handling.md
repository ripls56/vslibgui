# Event Handling

## GestureDetector

`GestureDetector` is the primary widget for responding to pointer (mouse) events. It wraps a
child and fires user callbacks when events occur:

```csharp
new GestureDetector(
    onTap:     e => Console.WriteLine("clicked"),
    onEnter:   e => SetState(() => _hovered = true),
    onExit:    e => SetState(() => _hovered = false),
    onPress:   e => Console.WriteLine("mouse down"),
    onRelease: e => Console.WriteLine("mouse up"),
    onMove:    e => Console.WriteLine($"moved to {e.X}, {e.Y}"),
    onWheel:   e => Console.WriteLine($"scroll delta: {e.Delta}"),
    child: new Container(
        style: new BoxStyle
        {
            Color = _hovered
                ? new Vector4(0.4f, 0.6f, 1, 1)
                : new Vector4(0.2f, 0.3f, 0.6f, 1)
        },
        child: new Text("Hover me")
    )
)
```

All callbacks are optional — pass only the ones you need. When a callback fires, the event's
`Handled` flag is set to `true`, preventing it from being received by elements underneath.

### PointerEvent properties

| Property | Type | Description |
|----------|------|-------------|
| `X` | `float` | Local X coordinate (relative to this element's top-left) |
| `Y` | `float` | Local Y coordinate |
| `Delta` | `float` | Scroll wheel delta (positive = scroll down) |
| `Button` | `PointerButton` | `Left`, `Right`, `Middle`, or `None` |
| `Handled` | `bool` | Set to `true` to stop event propagation |

## MouseRegion

`MouseRegion` detects pointer enter/exit/hover events and optionally changes the mouse
cursor shape while the pointer is over it. Unlike `GestureDetector`, it does not handle
clicks, presses, or scroll — it is purpose-built for hover tracking and cursor management.

```csharp
new MouseRegion(
    cursor:  MouseCursor.LinkSelect,
    onEnter: e => SetState(() => _hovered = true),
    onExit:  e => SetState(() => _hovered = false),
    child: new Text("Link-style text")
)
```

### Cursor types

Uses Vintage Story's string-based cursor system (`GuiDialog.MouseOverCursor`).

| Cursor | VS cursor name | Description |
|--------|---------------|-------------|
| `MouseCursor.LinkSelect` | `"linkselect"` | Link/button select cursor |
| `MouseCursor.TextSelect` | `"textselect"` | Text selection cursor |
| `MouseCursor.Custom("name")` | any string | Custom VS cursor name |

### Nesting

When `MouseRegion` widgets are nested, the **innermost** region's cursor wins. A region
with `cursor: null` inherits from the nearest ancestor that specifies one (or defaults to
the standard arrow).

## Hit Testing and Event Dispatch

The event system performs a **depth-first hit-test** of the element tree when a pointer event
arrives. Children are tested in reverse order (top-most visually = last in list = tested first).

1. `GuiBase` receives the OS event and converts screen coordinates to window-local coordinates
   (accounting for `WindowPos` and UI scale)
2. Drag/resize interactions are handled first — if the pointer is on a resize edge or drag
   handle, the event is consumed by the window system
3. `EventDispatcher.DispatchPointerDown(root, e)` walks the element tree
3. `Element.HitTest(result, position)` recursively tests whether the pointer falls within each
   element's bounds, returning the deepest hit
4. `FindTarget(result)` scans the hit path for the first element that implements a pointer
   interface
5. The event is dispatched to the target and bubbles upward through ancestors

### Pointer capture

When a `PointerDown` event is handled, the target element is **captured**. All subsequent
`PointerMove` events are routed directly to the captured element, even if the pointer moves
outside its bounds. The capture is released on `PointerUp`. This is what makes drag-and-drop
and slider-drag feel correct.

### Click detection

A click (`onTap`) fires on `PointerUp` only if the hit-test at mouse-up resolves to the
**same element** that received `PointerDown`. Moving the mouse off an element before releasing
does not fire `onTap`.

## Implementing Custom Pointer Handlers

You can implement pointer interfaces directly on a `StatefulWidget`'s `State` class:

```csharp
class _MyInteractiveState : State<_MyWidget>,
    IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    bool _pressed = false;

    public void OnPointerDown(PointerEvent e)
    {
        SetState(() => _pressed = true);
        e.Handled = true;
    }

    public void OnPointerClick(PointerEvent e)
    {
        Console.WriteLine("clicked!");
        e.Handled = true;
    }

    public void OnPointerEnter(PointerEvent e) { /* hover start */ }
    public void OnPointerExit(PointerEvent e)  { /* hover end   */ }

    public override Widget Build(BuildContext ctx) =>
        new Text(_pressed ? "PRESSED" : "Click me");
}
```

The dispatcher checks both `element.Widget` and `element.State` for interface implementations,
so you can place handlers on either.

## Keyboard Events

Keyboard events are routed through the **focus system**. Only the focused element (and its
ancestors) receives keyboard events.

### FocusNode and FocusManager

```csharp
class _TextWidgetState : State<_TextWidget>, IFocusable,
    IKeyDownHandler, IKeyCharHandler
{
    public FocusNode FocusNode { get; } = new();

    public void OnKeyDown(KeyboardEvent e)
    {
        if (e.KeyCode == (int)GlKeys.Escape)
        {
            Element.Owner.FocusManager.RequestFocus(null);   // clear focus
            e.Handled = true;
        }
    }

    public void OnKeyChar(KeyboardEvent e)
    {
        SetState(() => _text += e.KeyChar);
        e.Handled = true;
    }

    public override Widget Build(BuildContext ctx)
    {
        bool focused = FocusNode.HasFocus;
        return new GestureDetector(
            onTap: _ => Element.Owner.FocusManager.RequestFocus(FocusNode),
            child: new Container(
                style: new BoxStyle
                {
                    BorderThickness = focused ? 2 : 1,
                    BorderColor = focused
                        ? new Vector4(0.4f, 0.7f, 1, 1)
                        : new Vector4(1, 1, 1, 0.3f)
                },
                child: new Text(_text)
            )
        );
    }
}
```

`FocusNode` extends `ChangeNotifier`. Widgets can listen to `FocusNode.AddListener(...)` to
react when focus is gained or lost.

### KeyboardEvent properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `KeyEventType` | `KeyDown`, `KeyUp`, or `KeyChar` |
| `KeyCode` | `int` | Raw key code (compare with `(int)GlKeys.Enter` etc.) |
| `KeyChar` | `char` | Character for `KeyChar` events |
| `Shift` | `bool` | Shift modifier held |
| `Ctrl` | `bool` | Ctrl modifier held |
| `Alt` | `bool` | Alt modifier held |
| `Handled` | `bool` | Set to stop propagation |

### Keyboard interfaces

| Interface | Method | When called |
|-----------|--------|-------------|
| `IKeyDownHandler` | `OnKeyDown(KeyboardEvent)` | Key pressed (repeats while held) |
| `IKeyUpHandler` | `OnKeyUp(KeyboardEvent)` | Key released |
| `IKeyCharHandler` | `OnKeyChar(KeyboardEvent)` | Printable character typed |

### Focus routing

When `RequestFocus(node)` is called:
1. The old focused element's `FocusNode.SetHasFocus(false)` is called, notifying listeners
2. The new focused element's `FocusNode.SetHasFocus(true)` is called
3. `FocusManager.PrimaryFocus` is updated to the new node

On the next keyboard event, `EventDispatcher` reads `PrimaryFocus.Owner` to find the focused
element, then dispatches up the ancestor chain.

Clicking moves focus to the innermost focusable element under the pointer — or clears focus
(`RequestFocus(null)`) when the click lands on nothing focusable. This applies to every
focusable widget without each one opting in.

### Keyboard activation (Enter/Space)

`Button`, `IconButton`, `Checkbox`, `RadioButton`, `Dropdown`, and `NumericField` are
focusable and keyboard-activatable: they participate in Tab traversal and fire their
click/toggle action when focused and Enter or Space is pressed. Two pieces provide this
without per-widget boilerplate:

- **`FocusableState<T>`** — a `State<T>` base that owns a `FocusNode`, links it to the element
  on mount, and rebuilds on focus change. Derive a widget's state from it to make the widget
  focusable (it implements `IFocusable` for you).
- **`IActivatable`** — implement `Activate()` to perform the same action as a click. While
  focused, Enter/Space invokes `Activate()` before any `IKeyDownHandler` runs, so a
  `TextField` (which is not `IActivatable`) still receives Enter for text submission.

The focus outline follows `:focus-visible` semantics: it is drawn only when focus was reached
via keyboard traversal (`Tab`), not after a mouse click or programmatic focus. `FocusNode`
exposes both `HasFocus` (focused at all, receives input) and `ShowsFocusIndicator` (draw the
outline). Ring color/width come from `ThemeData.FocusOutlineColor` / `FocusOutlineWidth`. To
make a custom interactive widget keyboard-accessible:

```csharp
class _MyButtonState : FocusableState<MyButton>, IActivatable
{
    public void Activate() => Widget.OnTap?.Invoke();

    public override Widget Build(BuildContext ctx) =>
        /* read FocusNode.HasFocus to draw the focus outline */ new Container();
}
```

## ScrollWheel Events

The `onWheel` callback on `GestureDetector` receives the wheel delta. Positive delta = scroll
down. The `ListView` and `SingleChildScrollView` handle wheel events internally; use `onWheel`
for custom scroll behavior:

```csharp
new GestureDetector(
    onWheel: e =>
    {
        SetState(() => _zoom += e.Delta * 0.1f);
        e.Handled = true;
    },
    child: new ZoomableView(zoom: _zoom)
)
```

### VS Engine wheel dispatch and SyncHitBounds

The Vintage Story `GuiManager` dispatches `OnMouseWheel` in three consecutive loops:

1. **CaptureAllInputs loop** — only dialogs returning `true` from `CaptureAllInputs()`.
2. **Bounds loop (priority)** — dialogs whose `Composers` dictionary contains at least one
   composer whose `Bounds.PointInside(mouseX, mouseY)` is true. If a dialog is hit here, its
   `OnMouseWheel` is called before other low-priority dialogs.
3. **Catch-all loop** — every open dialog with `ShouldReceiveMouseEvents() == true`, in
   registration order. Once any dialog sets `args.IsHandled`, no subsequent dialog receives
   the event.

`GuiBase` does not use VS `GuiComposer` at all — `Composers` is always empty. This means it
is **skipped by loop 2** and falls into loop 3, where another open dialog may intercept the
wheel event first.

**Fix:** `GuiBase.SyncHitBounds()` registers a zero-element `GuiComposer` whose `Bounds`
match the current window rect. This makes the engine find the dialog in loop 2 (the priority
path) so `OnMouseWheel` is delivered with higher priority. `SyncHitBounds` is called on open,
on drag, and on resize to keep the bounds in sync.

> **Note:** This is an implementation detail of `GuiBase` and is fully automatic. You do not
> need to call `SyncHitBounds` yourself. It only matters if you are debugging why wheel events
> are not arriving at a custom `GuiBase` subclass — check that the dialog's `WindowPos` and
> `WindowSize` are set correctly at open time.

## Hover across windows

`GuiManager.OnMouseMove` walks `LoadedGuis` in order and **stops at the first dialog that
sets `args.Handled`**. One dialog owns each move event, so a window in front starves every
window behind it. Hover state in a starved window then goes stale: the item slot keeps its
highlight, and a `Tooltip` that was already open stays on screen because its `onExit` never
fires. This is most visible with a HUD that draws item slots and a dialog opened on top of
them.

`GuiBase` closes the gap in two places:

1. **Blocked move.** When `IsBlockedByFrontDialog` reports that a focused dialog in front
   covers the cursor, `OnMouseMove` cancels hover instead of updating it.
2. **Starved window.** `PointerWatchdog` records the cursor position hover was last synced
   for. `OnRenderGUI` (which the engine calls on every open dialog, every frame) compares it
   with `capi.Input.MouseX/MouseY`. If the pointer moved without this window being told, and
   the pointer is now outside the window or over a window in front, hover is cancelled.

The watchdog only cancels — it never claims hover for a window that did not receive the move
event. It also stands down while the window is being dragged or resized, and while a widget
holds pointer capture (an active drag keeps owning the pointer outside its bounds).

`Tooltip` adds a widget-level guard on top of that: while it is armed or visible, a ticker
compares `GlobalPointer.LogicalPosition` with the trigger's screen rect
(`LocalToGlobal(0) + GetScreenOffset()`, size `RenderObject.Size`) and closes the tooltip when
the cursor is no longer over the trigger. The tooltip panel itself is not part of that rect —
it is non-interactive (`RenderFollower.IsHitTestTarget` is `false`), so moving the cursor onto
the bubble closes it, exactly as an exit event would.

`GlobalPointer.LogicalPosition` is the only supported way to read the cursor without a pointer
event. It returns logical (UI-scaled) screen units, the same space as `WindowPos`, and `null`
when the game input is unavailable — callers treat `null` as "unknown" and keep their
event-driven behavior.

## HitTestBehavior

`BoxStyle.HitTestBehavior` controls how a container participates in hit-testing:

| Value | Behavior |
|-------|----------|
| `Defer` | Default: the container is a hit target only if it has visible content (color, border, or texture) |
| `Opaque` | Always a hit target, regardless of visual content |
| `Translucent` | Never a hit target; pointer events pass through to elements behind it |

```csharp
// Invisible overlay that still catches all clicks:
new Container(
    style: new BoxStyle { HitTestBehavior = HitTestBehavior.Opaque }
)

// Transparent container that doesn't block clicks:
new Container(
    style: new BoxStyle { HitTestBehavior = HitTestBehavior.Translucent },
    child: new Text("Clickable text underneath")
)
```

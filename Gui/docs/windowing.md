# Windowing

Each `GuiBase` subclass renders as an independent window that can be positioned, dragged, and
resized. Multiple windows can be open simultaneously.

## WindowConfig

Override `CreateWindowConfig()` to configure window behavior:

```csharp
protected override WindowConfig CreateWindowConfig() => new()
{
    Size = new Vector2(800, 600),     // fixed size; null = shrink-wrap to content
    Position = new Vector2(100, 50),  // initial position; null = centered on screen
    MinSize = new Vector2(200, 100),  // resize clamp
    MaxSize = new Vector2(1600, 900), // resize clamp
    Draggable = true,                 // drag by title bar / top handle zone
    Resizable = true,                 // resize by edges and corners
    DragHandleHeight = 24f,           // height of the drag zone at the top
    ResizeHandleSize = 8f,            // edge grip width for resize detection
    Clip = true,                      // false = let content overflow window bounds
};
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Position` | `Vector2?` | `null` | Initial position in logical pixels. `null` = centered |
| `Size` | `Vector2?` | `null` | Fixed window size. `null` = shrink-wrap to content |
| `MinSize` | `Vector2` | `(100, 50)` | Minimum size for resize clamping |
| `MaxSize` | `Vector2` | `(inf, inf)` | Maximum size for resize clamping |
| `Draggable` | `bool` | `true` | Enable drag by the top handle zone |
| `Resizable` | `bool` | `true` | Enable edge/corner resize |
| `DragHandleHeight` | `float` | `24` | Height of the drag handle at the top |
| `ResizeHandleSize` | `float` | `8` | Width of the edge resize grip |
| `Clip` | `bool` | `true` | Clip content to window bounds. `false` = oversized content overflows |
| `IsShrinkWrap` | `bool` | (computed) | `true` when `Size == null` |

### Fixed size vs shrink-wrap

- **Fixed size** (`Size = new Vector2(w, h)`): The root widget receives `Tight(w, h)` constraints.
  Content fills the window.
- **Shrink-wrap** (`Size = null`): The root widget receives `Loose(screenW, screenH)` constraints.
  The window sizes to its content.

## Drag and Resize

When `Draggable` is `true`, clicking and dragging within the top `DragHandleHeight` pixels
moves the window.

When `Resizable` is `true`, hovering over the window edges (within `ResizeHandleSize` pixels)
shows resize cursors and allows dragging to resize. Corner regions resize on both axes
simultaneously.

### Resize cursors

The Library automatically sets the OS cursor when hovering resize zones:

| Zone | Cursor |
|------|--------|
| Left / Right edge | `ResizeEW` (horizontal arrows) |
| Top / Bottom edge | `ResizeNS` (vertical arrows) |
| Top-left / Bottom-right corner | `ResizeNWSE` (diagonal) |
| Top-right / Bottom-left corner | `ResizeNESW` (diagonal) |

### Resize throttle

Resizing defers relayout by a short delay (50ms) to avoid FPS drops during active resize.
The visual window size updates immediately (for clipping), but the widget tree relayout is
batched. On mouse-up, the final layout is committed immediately.

### ResizeEdge flags

```csharp
[Flags]
public enum ResizeEdge
{
    None   = 0,
    Left   = 1,
    Right  = 2,
    Top    = 4,
    Bottom = 8,
}
```

Corner detection uses flag composition: `Left | Top` = top-left corner.

## Multi-Window Support

`GuiModSystem` tracks all open windows:

```csharp
// All open windows:
IReadOnlyList<GuiBase> windows = modSystem.OpenWindows;

// Currently active (focused) window:
GuiBase? active = modSystem.ActiveWindow;
```

Each `GuiBase` calls `SkiaRenderer.Begin()` / `End()` independently in its own `OnRenderGUI`,
so windows are fully isolated rendering passes.

When windows overlap, pointer events go to the top-most window under the cursor. A window
drawn behind another will not receive clicks or hover events even if its bounds overlap.

## WindowTitleBar

A title bar widget with title text, minimize (collapse), and close buttons. Add it as the
first child in a `Column`:

```csharp
protected override Widget Build() =>
    new Column(children:
    [
        new WindowTitleBar(
            title: "My Window",
            onClose: () => TryClose(),
            onMinimizeChanged: minimized => { /* show/hide content */ },
        ),
        new Expanded(child: myContent),
    ]);
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | `""` | Title text |
| `height` | `float` | `28` | Title bar height |
| `onClose` | `Action?` | `null` | Close button callback |
| `onMinimizeChanged` | `Action<bool>?` | `null` | Minimize toggle callback |
| `backgroundColor` | `Vector4?` | dark gray | Title bar background |
| `textColor` | `Vector4?` | light gray | Title and button text color |
| `buttonHoverColor` | `Vector4?` | medium gray | Minimize button hover color |
| `closeHoverColor` | `Vector4?` | red | Close button hover color |
| `fontSize` | `float` | `14` | Title font size |
| `showMinimize` | `bool` | `true` | Show minimize button |
| `showClose` | `bool` | `true` | Show close button |

## WindowFrame

A convenience widget that wraps `WindowTitleBar` + content and handles minimize state
internally:

```csharp
protected override Widget Build() =>
    new WindowFrame(
        title: "My Window",
        onClose: () => TryClose(),
        child: myContent,
    );
```

When minimized, only the title bar is visible; the child is removed from the tree.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | `""` | Title text |
| `child` | `Widget?` | `null` | Window content |
| `onClose` | `Action?` | `null` | Close button callback |
| `titleBarHeight` | `float` | `28` | Title bar height |
| `backgroundColor` | `Vector4?` | dark gray | Title bar background |
| `textColor` | `Vector4?` | light gray | Text color |
| `fontSize` | `float` | `14` | Title font size |
| `showMinimize` | `bool` | `true` | Show minimize button |
| `showClose` | `bool` | `true` | Show close button |

## Complete Example

```csharp
public class MyToolWindow : GuiBase
{
    public MyToolWindow(ICoreClientAPI capi) : base(capi) { }

    protected override WindowConfig CreateWindowConfig() => new()
    {
        Size = new Vector2(400, 300),
        Draggable = true,
        Resizable = true,
        MinSize = new Vector2(200, 100),
    };

    protected override Widget Build() =>
        new WindowFrame(
            title: "Tool Window",
            onClose: () => TryClose(),
            child: new Padding(
                EdgeInsets.All(12),
                child: new Text("Window content here")
            )
        );
}
```

## Dialog Type

To switch between interactive dialog and HUD element, override `DialogType` on your
`GuiBase` subclass:

```csharp
public override EnumDialogType DialogType => EnumDialogType.HUD;
```

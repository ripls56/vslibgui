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

Color and size parameters left at `null` fall back to `style`, then to
`Theme.Of(context).WindowTitleBarStyle`. A parameter you pass explicitly always wins.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | `""` | Title text |
| `height` | `float?` | style (`28`) | Title bar height |
| `onClose` | `Action?` | `null` | Close button callback |
| `onMinimizeChanged` | `Action<bool>?` | `null` | Minimize toggle callback |
| `backgroundColor` | `Vector4?` | style (`Surface`) | Title bar background |
| `textColor` | `Vector4?` | style (`OnSurface`) | Title and button text color |
| `buttonHoverColor` | `Vector4?` | style | Minimize button hover and pressed fill |
| `closeHoverColor` | `Vector4?` | style (`Error`) | Close button hover color |
| `fontSize` | `float?` | style (`14`) | Title and glyph font size (close glyph is drawn 2 px larger) |
| `showMinimize` | `bool` | `true` | Show minimize button |
| `showClose` | `bool` | `true` | Show close button |
| `style` | `WindowTitleBarStyle?` | theme | Full style tokens, see [WindowTitleBarStyle](#windowtitlebarstyle) |
| `leading` | `Widget?` | `null` | Placed immediately before the button group |
| `trailing` | `Widget?` | `null` | Placed immediately after the button group |
| `collapseWindow` | `bool` | `true` | Minimizing shrinks the real window to the bar height |

With the default `ButtonPlacement.End` the row reads
`title | leading | buttons | trailing`; with `ButtonPlacement.Start` it reads
`leading | buttons | trailing | title`.

### Minimize collapses the window

Minimizing does more than hide content. The title bar looks up the hosting window
(`IWindowHost`, implemented by `GuiBase`) through `BuildOwner.GetWindowHost()` and calls
`SetHostCollapsed(owner, true, barHeight)`. The window rectangle and the engine hit bounds
(`GuiComposer.Bounds`) shrink to the title bar, so the empty area below no longer swallows
clicks meant for windows underneath. Restoring, or removing the title bar from the tree,
expands the window again.

Pass `collapseWindow: false` to keep the old behaviour, where only the widget subtree
changes and the window keeps its size. Trees mounted without a host (headless tests) skip
the call.

### IWindowHost

`IWindowHost` (`Gui.Core.Windowing`) is the real window behind a widget tree. `GuiBase`
implements it and registers itself on the `BuildOwner`, so any widget can change the actual
window rectangle — not just its own painted area. Reach it from a `State` through
`Element.Owner`:

```csharp
var host = Element.Owner?.GetWindowHost();  // null in headless tests
host?.SetHostCollapsed(this, true, 40f);
```

| Member | Description |
|--------|-------------|
| `IsHostCollapsed` | `true` while the host window is collapsed to a title-bar-only rectangle |
| `SetHostCollapsed(owner, collapsed, collapsedHeight)` | Collapse or expand the window on behalf of `owner` |

One owner holds the collapse at a time: while one owner is collapsed, requests from any
other owner are ignored, so a title bar removed from the tree cannot expand a window that
a different title bar collapsed. While collapsed, the height is locked to
`collapsedHeight`, the width is frozen at its pre-collapse value (horizontal resize keeps
working), the top and bottom resize edges and their cursors are disabled, and
`WindowConfig.MinSize` cannot stretch the bar.

## WindowTitleBarStyle

`WindowTitleBarStyle` is a `readonly struct` of style tokens on
`ThemeData.WindowTitleBarStyle`. `WindowTitleBarStyle.Default(colors, text)` reproduces
the legacy look. Set it for a whole subtree through `Theme`, or per widget through `style:`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Height` | `float` | `28` | Bar height |
| `Padding` | `EdgeInsets` | left `8`, right `2` | Inner padding of the bar |
| `ButtonSize` | `float` | `0` | Button width and height; `0` = `Height - 4` |
| `ButtonSpacing` | `float` | `0` | Gap between buttons |
| `ButtonCornerRadius` | `float` | `3` | Corner radius of each button |
| `CornerRadius` | `Vector4` | `Vector4.Zero` | Per-corner radii of the bar (TL, TR, BR, BL) |
| `BorderThickness` | `float` | `0` | Bar border width |
| `BorderColor` | `Vector4` | `Border` | Bar border color |
| `TitleAlignment` | `TitleBarAlignment` | `Left` | `Left`, `Center` or `Right` |
| `ButtonPlacement` | `TitleBarButtonPlacement` | `End` | `Start` (left) or `End` (right) |
| `ReverseButtonOrder` | `bool` | `false` | Close before minimize inside the group |
| `BackgroundColor` | `Vector4` | `Surface` | Bar fill |
| `BackgroundGradient` | `Gradient?` | `null` | Gradient painted over the fill |
| `ButtonColor` | `Vector4` | transparent | Idle button fill |
| `ButtonHoverColor` | `Vector4` | `Surface + 0.12` | Minimize hover fill |
| `ButtonPressedColor` | `Vector4` | `Surface + 0.12` | Minimize pressed fill |
| `CloseHoverColor` | `Vector4` | `Error` | Close hover and pressed fill |
| `TitleTextStyle` | `TextStyle` | 14 px, `OnSurface` | Title text, including `FontFamily` |
| `ButtonTextStyle` | `TextStyle` | 14 px, `OnSurface` | Glyph text; the close glyph is 2 px larger |
| `MinimizeGlyph` | `string` | `"–"` (U+2013) | Minimize glyph while expanded |
| `RestoreGlyph` | `string` | `"+"` | Minimize glyph while collapsed |
| `CloseGlyph` | `string` | `"×"` (U+00D7) | Close glyph |

```csharp
var colors = ColorScheme.Default();
var macStyle = WindowTitleBarStyle.Default(colors, TextTheme.Default(colors)) with
{
    Height = 32,
    ButtonPlacement = TitleBarButtonPlacement.Start,
    ReverseButtonOrder = true,
    TitleAlignment = TitleBarAlignment.Center,
    CornerRadius = new Vector4(6, 6, 0, 0),
    ButtonCornerRadius = 8,
    ButtonSpacing = 4,
    CloseGlyph = "x",
    TitleTextStyle = new TextStyle { FontFamily = "monospace", FontSize = 13, Color = colors.OnSurface },
};

new WindowFrame("Inventory", content, onClose: () => TryClose())
{
    Style = macStyle
};

// or for every title bar in a subtree:
new Theme(new ThemeData { WindowTitleBarStyle = macStyle }, child: content);
```

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

When minimized, only the title bar is visible; the child is removed from the tree and the
host window collapses to the bar height (unless `collapseWindow: false`).

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | `""` | Title text |
| `child` | `Widget?` | `null` | Window content |
| `onClose` | `Action?` | `null` | Close button callback |
| `titleBarHeight` | `float?` | style (`28`) | Title bar height |
| `titleBarColor` | `Vector4?` | style (`Surface`) | Title bar background |
| `textColor` | `Vector4?` | style (`OnSurface`) | Text color |
| `fontSize` | `float?` | style (`14`) | Title font size |
| `showMinimize` | `bool` | `true` | Show minimize button |
| `showClose` | `bool` | `true` | Show close button |
| `fillHeight` | `bool` | `false` | Child fills the remaining window height |
| `style` | `WindowTitleBarStyle?` | theme | Title bar style tokens |
| `leading` | `Widget?` | `null` | Forwarded to the title bar |
| `trailing` | `Widget?` | `null` | Forwarded to the title bar |
| `collapseWindow` | `bool` | `true` | Minimizing shrinks the real window to the bar height |
| `onMinimizeChanged` | `Action<bool>?` | `null` | Minimize toggle callback |

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

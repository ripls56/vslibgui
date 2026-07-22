# Visual Widgets

## Container

A visual box with background, border, corner radii, optional fixed size, and optional child.

```csharp
new Container(
    style: new BoxStyle
    {
        Color = new Vector4(0.1f, 0.1f, 0.1f, 0.95f),
        Color2 = new Vector4(0.15f, 0.15f, 0.15f, 0.95f),  // gradient end (optional)
        CornerRadius = new Vector4(8),     // uniform radius; or per-corner Vector4
        BorderThickness = 1f,
        BorderColor = new Vector4(1, 1, 1, 0.3f),
        Padding = EdgeInsets.All(12),
        Width = 300f,                      // optional fixed width
        Height = 200f,                     // optional fixed height
        Texture = myBitmap,                // optional SKBitmap background
        ClipBehavior = ClipBehavior.AntiAlias,
        HitTestBehavior = HitTestBehavior.Defer
    },
    child: new Text("Content")
)
```

### BoxStyle.CornerRadius convention

`Vector4` components map to corners: `X` = top-right, `Y` = bottom-right, `Z` = top-left,
`W` = bottom-left. Use `new Vector4(r)` for uniform radius.

### BoxStyle.BoxShadows

CSS-like box shadows — outer (drop shadow) and inner (inset). Multiple shadows are supported
and painted in array order (first = bottommost).

```csharp
new Container(
    style: new BoxStyle
    {
        Color = new Vector4(0.15f, 0.15f, 0.2f, 1f),
        CornerRadius = new Vector4(12),
        BoxShadows = new[]
        {
            // Outer drop shadow
            new BoxShadow(
                Color: new Vector4(0, 0, 0, 0.6f),
                Offset: new Vector2(0, 4f),
                BlurRadius: 16f,
                SpreadRadius: 2f),
            // Subtle outer glow
            new BoxShadow(
                Color: new Vector4(0.3f, 0.5f, 1f, 0.15f),
                Offset: Vector2.Zero,
                BlurRadius: 24f),
            // Inner shadow (inset) for depth
            new BoxShadow(
                Color: new Vector4(0, 0, 0, 0.4f),
                Offset: new Vector2(0, 2f),
                BlurRadius: 8f,
                SpreadRadius: 1f,
                Inset: true),
        }
    },
    child: new Text("Shadowed card")
)
```

Each `BoxShadow` has:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Color` | `Vector4` | required | RGBA shadow color |
| `Offset` | `Vector2` | required | Displacement (X=right, Y=down) |
| `BlurRadius` | `float` | `0` | Gaussian blur sigma; 0 = hard edge |
| `SpreadRadius` | `float` | `0` | Expands (+) or contracts (−) the shadow shape |
| `Inset` | `bool` | `false` | `true` = inner shadow, `false` = outer shadow |

Outer shadows are drawn **before** the fill (behind the box). Inner shadows are drawn
**after** the fill but **before** children. Outer shadows are clipped out of the box area
so they don't bleed through transparent backgrounds.

::: info
When `ClipBehavior != None`, the clip wraps the entire box including outer
shadows. This matches CSS `overflow: hidden` behavior — outer shadows will be clipped.
:::

### BoxStyle.Border (per-side)

`BorderThickness` / `BorderColor` apply one uniform stroke to all four sides. For an
asymmetric border — an underline, a tab indicator, or a different color per edge — set
`Border` to a `Border` value built from four `BorderSide` (width + color) edges. When set,
it overrides `BorderThickness` / `BorderColor`.

```csharp
// Thick bottom edge only (e.g. selected tab)
new Container(
    style: new BoxStyle
    {
        Color = new Vector4(0.15f, 0.15f, 0.2f, 1f),
        CornerRadius = new Vector4(8),
        Border = Border.Only(bottom: new BorderSide(3f, new Vector4(1, 0.6f, 0.2f, 1f)))
    },
    child: new Text("Tab")
)

// Distinct width per side, shared color
Border.Symmetric(vertical: 1f, horizontal: 4f, color: new Vector4(1, 1, 1, 0.5f))

// Four independent edges
new Border(
    Left:   new BorderSide(2f, new Vector4(1, 0, 0, 1)),
    Top:    new BorderSide(4f, new Vector4(0, 1, 0, 1)),
    Right:  new BorderSide(2f, new Vector4(0, 0, 1, 1)),
    Bottom: new BorderSide(4f, new Vector4(1, 1, 0, 1)))
```

| Member                                          | Description                                                                |
|-------------------------------------------------|----------------------------------------------------------------------------|
| `BorderSide(Width, Color)`                      | One edge; `Width == 0` means that side is absent (`BorderSide.None`)       |
| `Border.All(w, color)`                          | Uniform border on all four sides                                           |
| `Border.Symmetric(vertical, horizontal, color)` | Different top/bottom vs left/right widths                                  |
| `Border.Only(left:, top:, right:, bottom:)`     | Individual sides; omitted sides default to none                            |
| `Border.IsUniform`                              | `true` when all four sides are equal (uses the cheaper single-stroke path) |

Borders are painted inside the box bounds (from the outer edge inward by each side's width)
and do not affect layout. Rounded corners are preserved: each corner's inner radius is
reduced by the smaller of the two widths meeting there.

## Text

Renders a string with configurable style.

```csharp
new Text("Hello, World!", new TextStyle
{
    FontFamily = "sans-serif",
    FontSize = 14f,
    Color = Vector4.One,
    Weight = FontWeight.Normal,      // Normal, Bold, Italic
    Align = TextAlignment.Left,      // Left, Center, Right
    Overflow = TextOverflow.Clip,    // Clip, Ellipsis
    Boldness = 0.1f,                 // -0.5 (thin) to 0.5 (bold)
    OutlineWidth = 0.03f,            // fraction of font size
    OutlineColor = new Vector4(0, 0, 0, 1),
    GlowWidth = 0.2f,                // blur radius fraction of font size
    GlowColor = new Vector4(0.5f, 0.8f, 1, 0.5f)
})
```

Multi-line text is supported via `\n` in the content string.

## VtmlText

Renders VTML (Vintage Story Text Markup Language) content with full support for styled text,
links, icons, item stacks (with float layout), and hotkey display.

```csharp
new VtmlText(
    vtml: "Hello <strong>bold</strong> and <i>italic</i><br>"
        + "<font color=\"#ff0000\" size=\"20\">Red big text</font><br>"
        + "<a href=\"handbook://page\">Click me</a><br>"
        + "<icon name=\"dice\"></icon> inline icon<br>"
        + "<itemstack type=\"block\" code=\"plank-oak\" floattype=\"left\"></itemstack>"
        + "Text wraps around the floated item stack.",
    baseStyle: new TextStyle { FontSize = 14f, Color = Vector4.One },
    onLinkClick: href => HandleLink(href)
)
```

### Supported VTML tags

| Tag | Description |
|-----|-------------|
| `<br>` | Line break |
| `<strong>`, `<b>` | Bold text |
| `<i>` | Italic text |
| `<font size/color/weight/opacity/family/align>` | Styled text span |
| `<code>` | Monospace text |
| `<a href="...">` | Clickable link (handbook://, http://, chattype:///, etc.) |
| `<icon name="..." path="...">` | Inline SVG icon |
| `<itemstack type="block\|item" code="..." floattype="inline\|left\|right" rsize="1" offx="0" offy="0">` | Item/block rendering |
| `<hk>`, `<hotkey>` | Display current key binding |
| `<clear>` | Clear floated elements |

### Architecture

- **VtmlConverter** (`Gui.Vtml`) — converts `VtmlToken[]` from the game's `VtmlParser.Tokenize()`
  into a flat list of `VtmlInlineElement` objects (text runs, icons, item stacks, hotkeys, etc.).
- **VtmlSpanBuilder** (`Gui.Vtml`) — converts `VtmlInlineElement` list into an `InlineSpan` tree
  (`TextSpan`, `WidgetSpan`, `ClearSpan`) suitable for `RichText`.
- **VtmlText** (`Gui.Widgets`) — `StatefulWidget` that parses VTML, builds an `InlineSpan` tree,
  and renders via `RichText` composition (delegates to `RenderRichText`).

## Image

Displays a game asset bitmap with configurable fit and alignment.

```csharp
new Image(
    domain: "game",                  // asset domain
    path: "textures/gui/icon.png",   // path within domain
    fit: BoxFit.Contain,
    alignment: Alignment.Center,     // optional, defaults to Center
    width: 64f,                      // optional explicit width
    height: 64f                      // optional explicit height
)
```

### BoxFit values

| Value | Behavior |
|-------|----------|
| `Fill` | Stretch to fill; distorts aspect ratio |
| `Contain` | Scale uniformly to fit within bounds; may letterbox |
| `Cover` | Scale uniformly to fill bounds; may crop edges |
| `FitWidth` | Scale to match width; may overflow vertically |
| `FitHeight` | Scale to match height; may overflow horizontally |
| `None` | Natural pixel size, positioned by alignment |
| `ScaleDown` | Like `Contain` but never upscales |

## Clip

Clips its child to a rounded rectangle. Useful for rounded images.

```csharp
new Clip(
    borderRadius: new Vector4(12),   // same Vector4 convention as CornerRadius
    child: new Image("game", "textures/banner.png", fit: BoxFit.Cover, width: 200, height: 100)
)
```

## Opacity

Renders its child at a specified alpha.

```csharp
new Opacity(0.5f, child: new Text("Half-transparent"))
```

- Values ≤ `0.001` skip painting entirely
- Values ≥ `0.999` skip `SaveLayer` for performance
- Values in between use `SKCanvas.SaveLayer`

## RepaintBoundary

Caches its entire subtree as an `SKPicture`. Prevents repaints from propagating into the
cached subtree when siblings change. See [Rendering](../rendering.md).

```csharp
new RepaintBoundary(child: new StaticPanel())
```

## ShaderMask

Paints its child normally, then overlays an `SKShader` across the child's bounds using a
configurable `SKBlendMode`. The shader is **not** owned by the widget — the caller manages
its lifecycle (create / dispose).

| Parameter   | Type          | Default    | Description                    |
|-------------|---------------|------------|--------------------------------|
| `shader`    | `SKShader?`   | `null`     | The shader to overlay          |
| `blendMode` | `SKBlendMode` | `SrcOver`  | Blend mode for the overlay     |
| `child`     | `Widget?`     | `null`     | Child widget painted underneath|
| `offsetX`   | `float`       | `0`        | Horizontal shader scroll offset|
| `offsetY`   | `float`       | `0`        | Vertical shader scroll offset  |

```csharp
// Perlin noise overlay on a colored box
new ShaderMask(
    shader: SKShader.CreatePerlinNoiseTurbulence(0.015f, 0.03f, 4, 0f),
    blendMode: SKBlendMode.Multiply,
    child: new Container(
        style: new BoxStyle { Color = new Vector4(0.8f, 0.2f, 0.1f, 0.9f) }
    )
)
```

**Blend mode pitfall:** With `Multiply`, every pixel of the child is multiplied by the
shader. Decorative elements like borders or shadows inside the child will be distorted.
Place borders and shadows in a separate layer **outside** the ShaderMask:

```csharp
// Correct — border is a separate layer on top
new Stack([
    new ShaderMask(
        shader: noise, blendMode: SKBlendMode.Multiply,
        child: new Container(style: new BoxStyle { Color = fillColor })
    ),
    new Container(style: new BoxStyle {
        BorderThickness = 2f, BorderColor = gray
    })
])
```

## CompositedTransformTarget / CompositedTransformFollower

Links a trigger widget to an overlay widget so the overlay tracks the trigger's
position during layout — even inside scrollable containers. Used internally by
`Tooltip` and `Dropdown`.

```csharp
var link = new LayerLink();

// In the main widget tree (e.g., inside a scroll view):
new CompositedTransformTarget(link: link, child: triggerWidget)

// In the overlay entry:
new CompositedTransformFollower(
    link: link,
    offset: new Vector2(0, 40), // below the trigger
    child: overlayContent
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `link` | `LayerLink` | required | Shared link between target and follower |
| `offset` | `Vector2?` | (0, 0) | Offset from the target's top-left corner |

The follower computes its position in the **layout** phase (not build), so it
reads the target's post-scroll global position. It writes `StackParentData` and
must be placed inside a `Stack` (typically the `Overlay` root stack).

## FractionalTranslation

Translates the child's paint position by a fraction of the child's own size.
Useful for centering a widget of unknown size relative to an anchor point.

```csharp
new FractionalTranslation(
    translation: new Vector2(-0.5f, -1f), // center horizontally, shift up by full height
    child: tooltipContent
)
```

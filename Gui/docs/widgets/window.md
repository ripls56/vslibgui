# Window Widgets

## WindowTitleBar

A title bar with title text, minimize (collapse), and close buttons. Typically the first
child in a `Column`.

```csharp
new WindowTitleBar(
    title: "My Window",
    height: 28f,
    onClose: () => TryClose(),
    onMinimizeChanged: minimized => SetState(() => _minimized = minimized),
    showMinimize: true,
    showClose: true,
    fontSize: 14f,
    backgroundColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
    textColor: new Vector4(0.85f, 0.85f, 0.85f, 1f),
    buttonHoverColor: new Vector4(0.3f, 0.3f, 0.35f, 1f),
    closeHoverColor: new Vector4(0.8f, 0.2f, 0.2f, 1f)
)
```

The minimize button toggles internal state and fires `onMinimizeChanged`. The close button
fires `onClose`. Both buttons show hover and pressed highlighting.

Minimizing also collapses the real window: the title bar calls
`IWindowHost.SetHostCollapsed` on the hosting `GuiBase`, so the window rectangle and its
engine hit bounds shrink to the bar height and stop swallowing clicks for windows below.
Pass `collapseWindow: false` to opt out and only change the widget subtree. See
[IWindowHost](../windowing.md#iwindowhost) for the interface itself.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | `""` | Title text |
| `height` | `float?` | style (`28`) | Bar height |
| `onClose` | `Action?` | `null` | Close button callback |
| `onMinimizeChanged` | `Action<bool>?` | `null` | Minimize toggle callback |
| `backgroundColor` | `Vector4?` | style | Bar background |
| `textColor` | `Vector4?` | style | Title and glyph color |
| `buttonHoverColor` | `Vector4?` | style | Minimize hover / pressed fill |
| `closeHoverColor` | `Vector4?` | style | Close hover fill |
| `fontSize` | `float?` | style (`14`) | Title and glyph font size |
| `showMinimize` | `bool` | `true` | Show minimize button |
| `showClose` | `bool` | `true` | Show close button |
| `style` | `WindowTitleBarStyle?` | theme | Style tokens |
| `leading` | `Widget?` | `null` | Immediately before the button group |
| `trailing` | `Widget?` | `null` | Immediately after the button group |
| `collapseWindow` | `bool` | `true` | Collapse the host window on minimize |

Explicit parameters win over `style`, which wins over `Theme.Of(context).WindowTitleBarStyle`.

A customized bar with a help button in `leading`:

```csharp
var colors = ColorScheme.Default();
var style = WindowTitleBarStyle.Default(colors, TextTheme.Default(colors)) with
{
    Height = 32,
    ButtonPlacement = TitleBarButtonPlacement.Start,
    TitleAlignment = TitleBarAlignment.Center,
    CornerRadius = new Vector4(6, 6, 0, 0),
    ButtonSpacing = 4,
    CloseGlyph = "x",
};

new WindowTitleBar(
    title: "Inventory",
    style: style,
    leading: new IconButton(
        new Icon("gui", "textures/icons/search.svg"),
        ButtonVariant.Ghost,
        onTap: _ => ShowHelp()),
    onClose: () => TryClose()
)
```

## WindowTitleBarStyle

Style tokens for the title bar: `Height`, `Padding`, `ButtonSize`, `ButtonSpacing`,
`ButtonCornerRadius`, `CornerRadius`, `BorderThickness`, `BorderColor`, `TitleAlignment`
(`Left` / `Center` / `Right`), `ButtonPlacement` (`Start` / `End`), `ReverseButtonOrder`,
`BackgroundColor`, `BackgroundGradient`, `ButtonColor`, `ButtonHoverColor`,
`ButtonPressedColor`, `CloseHoverColor`, `TitleTextStyle`, `ButtonTextStyle`,
`MinimizeGlyph`, `RestoreGlyph`, `CloseGlyph`.

`TitleTextStyle` and `ButtonTextStyle` are full `TextStyle` values, so the font family,
size, weight and color of the title and of the glyphs are all customizable. The close glyph
is drawn 2 px larger than `ButtonTextStyle.FontSize`.

### TitleBarAlignment

Horizontal placement of the title in the free space between the side groups.

| Value | Result |
|-------|--------|
| `Left` | Title hugs the left edge (default) |
| `Center` | Title is centred in the free space |
| `Right` | Title hugs the right edge |

### TitleBarButtonPlacement

Which side of the bar holds the minimize / close group.

| Value | Row order |
|-------|-----------|
| `End` | `title \| leading \| buttons \| trailing` (default) |
| `Start` | `leading \| buttons \| trailing \| title` |

`ReverseButtonOrder = true` swaps close and minimize inside the group.

```csharp
var colors = ColorScheme.Default();
var style = WindowTitleBarStyle.Default(colors, TextTheme.Default(colors)) with
{
    ButtonPlacement = TitleBarButtonPlacement.Start,
    TitleAlignment = TitleBarAlignment.Center,
    CloseGlyph = "x",
};

new WindowFrame("My Window", content)
{
    Style = style
};
```

Register it for a subtree with `new ThemeData { WindowTitleBarStyle = style }`. Full table in
[Windowing](../windowing.md#windowtitlebarstyle).

## WindowFrame

Convenience widget that wraps `WindowTitleBar` + content in a `Column` and handles minimize
state automatically. When minimized, only the title bar is visible and the host window
collapses to the bar height (unless `collapseWindow: false`).

The constructor takes the 3.x arguments (`title`, `child`, `onClose`, `titleBarHeight`,
`titleBarColor`, `textColor`, `fontSize`, `showMinimize`, `showClose`, `fillHeight`) and is
frozen: it is part of the binary contract with mods compiled against 3.x. Newer options are set
in an object initializer and forwarded to the `WindowTitleBar`: `Style`, `Leading`,
`Trailing`, `CollapseWindow`, `OnMinimizeChanged`.

```csharp
new WindowFrame(
    title: "My Window",
    onClose: () => TryClose(),
    child: new Padding(
        EdgeInsets.All(12),
        child: new Text("Content")
    )
)
```

See [Windowing](../windowing.md) for full details.

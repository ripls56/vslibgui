# Theming

The library ships with a JSON-based theme system. Themes control color tokens used throughout
the widget tree. Switching themes requires no code change — only a JSON file.

## Using theme tokens in widgets

`Theme` is accessible via `BuildContext`:

```csharp
public override Widget Build(BuildContext ctx)
{
    var theme = Theme.Of(ctx);
    return new Container(
        style: new BoxStyle
        {
            Color = theme.Surface,
            BorderColor = theme.Border,
        },
        child: new Text("Hello", style: new TextStyle { Color = theme.OnSurface })
    );
}
```

Using `Theme.Of(ctx)` instead of hard-coded colors means the widget respects any active theme
without modification.

## Text roles

`theme.TextTheme` provides a seven-step typographic scale. Each role is a `TextStyle`
(size, weight, color) derived from the active `ColorScheme`, so text recolors automatically
when the theme changes.

| Role       | Size | Weight   | Color            | Use for                       |
|------------|------|----------|------------------|-------------------------------|
| `Display`  | 30   | Bold     | OnBackground     | Hero / splash titles          |
| `Headline` | 24   | Bold     | OnBackground     | Screen and dialog headers     |
| `Title`    | 18   | SemiBold | OnBackground     | Card and section titles       |
| `Subtitle` | 16   | Normal   | OnBackground     | Secondary headings            |
| `Body`     | 14   | Normal   | OnBackground     | Running text                  |
| `Label`    | 12   | Normal   | OnBackground     | Captions, badges, form fields |
| `Caption`  | 11   | Normal   | OnSurfaceVariant | Hints and metadata            |

Pick a role directly:

```csharp
var theme = Theme.Of(ctx);
new Text("Inventory", theme.TextTheme.Headline);
```

When a role needs a non-default color, mutate a local copy (`TextStyle` is a mutable
struct, so this leaves the theme untouched):

```csharp
var header = theme.TextTheme.Headline;
header.Color = theme.ColorScheme.Primary;
new Text("Inventory", header);
```

## Component style tokens

`ThemeData` also carries one style struct per customizable widget. Each is a
`readonly struct` of `{ get; init; }` tokens with a `Default(...)` factory, so you copy the
default and override only what you need:

| Property | Applies to |
|----------|------------|
| `ProgressBarStyle` | `ProgressBar` |
| `ItemSlotStyle` | `FlatItemSlot`, `NineSliceItemSlot` |
| `HotkeyBadgeStyle` | `HotkeyBadge` |
| `DropdownStyle` | `Dropdown<T>` |
| `CheckboxStyle` | `Checkbox` |
| `RadioButtonStyle` | `RadioButton<T>` |
| `SliderStyle` | `Slider` |
| `ButtonStyle` | `Button` |
| `IconButtonStyle` | `IconButton` |
| `TextFieldStyle` | `TextField` |
| `WindowTitleBarStyle` | `WindowTitleBar`, `WindowFrame` |

Pass one to `ThemeData` to restyle a whole subtree:

```csharp
var colors = ColorScheme.Default();
var bars = WindowTitleBarStyle.Default(colors, TextTheme.Default(colors)) with
{
    Height = 32,
    ButtonPlacement = TitleBarButtonPlacement.Start,
    TitleAlignment = TitleBarAlignment.Center,
    TitleTextStyle = new TextStyle
    {
        FontFamily = "monospace", FontSize = 13, Color = colors.OnSurface
    },
};

new Theme(new ThemeData { WindowTitleBarStyle = bars }, child: myContent);
```

A widget's own `style:` argument wins over the theme, and an explicit constructor argument
(such as `backgroundColor:`) wins over both. See
[WindowTitleBarStyle](./windowing.md#windowtitlebarstyle) for the full token table.

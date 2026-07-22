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

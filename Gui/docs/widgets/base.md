# Base Classes

All widgets are in the `Gui.Widgets` namespace unless noted.

---

## Widget

All widgets extend `Widget`. It is immutable — widgets describe UI, they hold no state and
create no GPU resources.

```csharp
public abstract class Widget
{
    public Key? Key { get; }
    public abstract Element CreateElement();
    public static bool CanUpdate(Widget old, Widget @new); // type + key equality
}
```

## StatelessWidget

A widget whose appearance is fully determined by its constructor arguments. Override `Build`
to describe the UI.

```csharp
class MyButton : StatelessWidget
{
    public string Label { get; }
    public Action? OnPressed { get; }

    public MyButton(string label, Action? onPressed = null)
    {
        Label = label;
        OnPressed = onPressed;
    }

    public override Widget Build(BuildContext ctx) =>
        new GestureDetector(
            onTap: _ => OnPressed?.Invoke(),
            child: new Container(
                style: new BoxStyle { Padding = EdgeInsets.All(8) },
                child: new Text(Label)
            )
        );
}
```

## StatefulWidget + State\<T\>

A widget with mutable state. `CreateState()` is called once; the `State<T>` object persists
across rebuilds. See [State Management](../state-management.md).

## InheritedWidget

A widget that propagates data down the tree efficiently. Descendants look up the
nearest ancestor `InheritedWidget` of a given type and are automatically rebuilt
when the inherited data changes.

```csharp
class MyConfig : InheritedWidget
{
    public string Locale { get; }

    public MyConfig(string locale, Widget child) : base(child)
    {
        Locale = locale;
    }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return Locale != ((MyConfig)oldWidget).Locale;
    }

    public static string Of(BuildContext context)
    {
        var w = context.DependOnInheritedWidgetOfExactType<MyConfig>();
        return w?.Locale ?? "en";
    }
}
```

::: tip
- `DependOnInheritedWidgetOfExactType<T>()` registers a dependency — the caller
  rebuilds when data changes.
- `UpdateShouldNotify` controls whether dependents are notified on update.
- `State.DidChangeDependencies()` is called on dependents when inherited data changes.
:::

## Theme

An `InheritedWidget` that provides `ThemeData` (colors + typography) to a subtree.
Use `Theme.Of(context)` to read the current theme.

```csharp
// Provide a theme at the top of the tree:
new Theme(
    data: new ThemeData(
        colorScheme: new ColorScheme { Primary = new Vector4(0.2f, 0.6f, 1f, 1f), ... },
        textTheme: new TextTheme { Body = new TextStyle { FontSize = 16 }, ... }
    ),
    child: myAppContent
)

// Read the theme inside any widget's Build():
var theme = Theme.Of(context);
var primary = theme.ColorScheme.Primary;
var bodyStyle = theme.TextTheme.Body;
```

`ThemeData` contains:
- `ColorScheme` — 17 semantic colors: Primary, OnPrimary, Secondary, OnSecondary, Surface,
  OnSurface, OnSurfaceVariant, Background, OnBackground, Border, Error, OnError, SurfaceLow,
  SurfaceHigh, StateHover, StateSelected, OutlineVariant
- `TextTheme` — 7 text roles: Display, Headline, Title, Subtitle, Body, Label, Caption
- One style struct per customizable widget: `ButtonStyle`, `IconButtonStyle`,
  `TextFieldStyle`, `DropdownStyle`, `CheckboxStyle`, `RadioButtonStyle`, `SliderStyle`,
  `ProgressBarStyle`, `ItemSlotStyle`, `HotkeyBadgeStyle`, `WindowTitleBarStyle`. See
  [Theming](../theming.md#component-style-tokens)
- `FocusOutlineColor` and `FocusOutlineWidth`

If no `Theme` ancestor exists, `Theme.Of()` returns `ThemeData.Default` (dark theme).

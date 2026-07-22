# Interactive Widgets

## GestureDetector

Wraps a child with pointer event callbacks.

```csharp
new GestureDetector(
    onTap:     e => { },    // mouse click (down + up on same element)
    onPress:   e => { },    // mouse down
    onRelease: e => { },    // mouse up
    onEnter:   e => { },    // pointer enters bounds
    onExit:    e => { },    // pointer leaves bounds
    onMove:    e => { },    // pointer moves within bounds
    onWheel:   e => { },    // scroll wheel; e.Delta > 0 = scroll down
    child: new Container(...)
)
```

All callbacks are optional. See [Event Handling](../event-handling.md).

## MouseRegion

Detects pointer enter/exit/hover and optionally changes the cursor shape. Nested regions are supported — the innermost cursor wins.

```csharp
new MouseRegion(
    cursor:  MouseCursor.LinkSelect,  // VS "linkselect" cursor
    onEnter: e => { },                // pointer enters bounds
    onExit:  e => { },                // pointer leaves bounds
    onHover: e => { },                // pointer moves within bounds
    child: new Container(...)
)

// Custom VS cursor name:
new MouseRegion(
    cursor: MouseCursor.Custom("mycursor"),
    child: someWidget
)
```

Built-in cursors: `MouseCursor.LinkSelect`, `MouseCursor.TextSelect`.
Custom names via `MouseCursor.Custom("name")`. Uses VS's `GuiDialog.MouseOverCursor`.

All callbacks and `cursor` are optional. A `MouseRegion` with only `cursor` set (no
callbacks) still participates in hit-testing.

## TextField

Single-line text input with cursor, caret scrolling, and keyboard handling.

```csharp
new TextField(
    controller: new TextEditingController("initial text"),
    focusNode: new FocusNode(),    // optional; one is created automatically
    style: new TextFieldStyle
    {
        Height = 35,
        FillColor = new Vector4(0, 0, 0, 0.3f),
        BorderThickness = 1
    }
)
```

Without an external `controller`, the text value is inaccessible from the parent.

When `style` is omitted, `TextField` falls back to `ThemeData.TextFieldStyle` (`FillColor`, `BorderColor` /
`BorderThickness`, `Height`, `CornerRadius`, `TextStyle`, `SelectionColor`). The border animates smoothly between three
states following `:focus-visible` semantics (see [Event Handling](../event-handling.md)): unfocused keeps the configured
`BorderColor` / `BorderThickness`; keyboard focus (`FocusNode.ShowsFocusIndicator`) draws the full shared
`ThemeData.FocusOutlineColor` / `FocusOutlineWidth`; pointer focus blends toward the outline at reduced intensity for a
subtle highlight without a bright ring.

Keyboard shortcuts supported out of the box:

| Shortcut | Action |
|----------|--------|
| `Ctrl+Left` / `Ctrl+Right` | Move caret to previous/next word boundary |
| `Ctrl+Backspace` | Delete word to the left of the caret |
| `Ctrl+Delete` | Delete word to the right of the caret |

## NumericField

A row of `−` button + `TextField` + `+` button. Validates input as a float.

```csharp
new NumericField(
    initialValue: 25f,
    step: 5f,                             // increment/decrement step
    onChanged: v => SetState(() => _value = v)
)
```

## Checkbox

A parchment-textured checkbox with an animated check mark.

```csharp
new Checkbox(
    value: _checked,
    onChanged: v => SetState(() => _checked = v),
    label: "Enable feature",
    size: 24f                             // checkbox size in pixels
)
```

Use the controlled pattern: the parent holds `_checked`, passes it as `value`, and updates it
in `onChanged`.

## RadioButton\<T\>

A radio button. Selected when `value == groupValue`.

```csharp
new RadioButton<int>(
    value: 1,                             // this button's value
    groupValue: _selectedMode,            // currently selected group value
    onChanged: v => SetState(() => _selectedMode = v),
    label: "Survival"
)
```

## Dropdown\<T\>

A dropdown menu that opens an overlay panel when clicked.

```csharp
new Dropdown<int>(
    _selectedValue,
    [
        new DropdownItem<int> { Value = 1, Label = "Option A" },
        new DropdownItem<int> { Value = 2, Label = "Option B" },
        new DropdownItem<int> { Value = 3, Label = "Option C" },
    ],
    v => SetState(() => _selectedValue = v)
)
```

## Slider

A horizontal slider for selecting a numeric value from a range. Supports continuous
and discrete (stepped) modes, mouse wheel, and an optional value tooltip on hover/drag.
The value tooltip uses the `Tooltip` widget internally.

```csharp
// Continuous slider (0.0 – 1.0)
new Slider(
    value: _volume,
    onChanged: v => SetState(() => _volume = v)
)

// Discrete slider with label (0 – 100, 10 steps)
new Slider(
    value: _brightness,
    onChanged: v => SetState(() => _brightness = v),
    onChangeEnd: v => SaveSetting(v),
    min: 0, max: 100, divisions: 10,
    label: "Brightness"
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `float` | required | Current value (clamped to min..max) |
| `onChanged` | `Action<float>?` | `null` | Called on every drag/click/wheel change |
| `onChangeEnd` | `Action<float>?` | `null` | Called once on pointer release |
| `min` | `float` | `0` | Minimum value |
| `max` | `float` | `1` | Maximum value |
| `divisions` | `int?` | `null` | Number of discrete steps; null = continuous |
| `label` | `string?` | `null` | Text label displayed to the left |
| `trackHeight` | `float` | `8` | Track thickness in pixels |
| `thumbWidth` | `float` | `12` | Thumb width in pixels |
| `thumbHeight` | `float` | `24` | Thumb height in pixels |
| `thumbRadius` | `float` | `2` | Thumb corner radius (2 = rectangular VS style) |
| `activeColor` | `Vector4?` | theme | Filled portion of track |
| `inactiveColor` | `Vector4?` | theme | Unfilled portion of track |
| `thumbColor` | `Vector4?` | theme | Thumb color |
| `showValueLabel` | `bool` | `true` | Whether to show a tooltip with the current value on hover/drag |
| `valueLabelBuilder` | `Func<float, Widget>?` | `null` | Custom builder for tooltip content |


## ColorPicker

A color picker with a rainbow hue×saturation square (drag to pick any fully-bright
color) and a vertical transparency strip on the right. Value/lightness is fixed at 1,
so black, white, and grey are out of range by design. Uses the same drag gesture
pattern as `Slider`. The `ColorField` editor in `Gui.Settings` opens a `ColorPicker`
in an `Overlay` popover when its swatch is clicked.

```csharp
new ColorPicker(
    value: _color,
    onChanged: c => SetState(() => _color = c),
    onChangeEnd: c => SaveColor(c)
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `Vector4` | required | Current RGBA color (components in [0, 1]) |
| `onChanged` | `Action<Vector4>?` | `null` | Called continuously while dragging inside the square or the strip |
| `onChangeEnd` | `Action<Vector4>?` | `null` | Called once on pointer release |
| `panelSize` | `float?` | `180` | Side length of the hue×saturation square in pixels |
| `barWidth` | `float?` | `16` | Width of the transparency strip in pixels |
| `disabled` | `bool` | `false` | When true, the picker ignores pointer input |

## ProgressBar

A horizontal progress bar that displays a value between 0 and 1. Renders a
background track with a filled portion and an optional border, using rounded
corners. Colors default to the current theme's `ProgressBarStyle`.

```csharp
// Basic usage
new ProgressBar(value: 0.6f)

// Custom style override
new ProgressBar(
    value: _progress,
    style: new ProgressBarStyle
    {
        Height = 16,
        CornerRadius = 8,
        BorderThickness = 2,
        FillColor = new Vector4(0.2f, 0.8f, 0.2f, 1f),
        TrackColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
        BorderColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
    }
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `float` | required | Progress (clamped to 0..1) |
| `style` | `ProgressBarStyle?` | theme | Optional style override |

## Tooltip

Wraps a child and shows a floating tooltip widget above it on hover after a configurable
delay. Uses the Overlay system for rendering above other content and fades in/out with
an `AnimationController`.

```csharp
new Tooltip(
    content: new Text("Save the document"),
    child: myButton
)

// With custom settings:
new Tooltip(
    content: new Row(children: [new Icon("info"), new Text("Details here")]),
    child: myWidget,
    waitDuration: TimeSpan.FromMilliseconds(300),
    fadeDuration: TimeSpan.FromMilliseconds(200),
    verticalGap: 12f
)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `child` | `Widget` | required | The trigger widget |
| `content` | `Widget` | required | Arbitrary widget shown inside the tooltip bubble |
| `waitDuration` | `TimeSpan?` | 500 ms | Hover delay before showing |
| `fadeDuration` | `TimeSpan?` | 150 ms | Fade-in/out animation duration |
| `verticalGap` | `float` | 8 | Pixel gap between trigger top and tooltip bottom |
| `anchorX` | `float?` | `null` | Horizontal anchor X within the child |

## IconButton

A circular button (40×40) with hover animation.

```csharp
new IconButton(
    color: new Vector4(0.8f, 0.3f, 0.3f, 1),
    onTap: _ => Console.WriteLine("clicked"),
    child: new Text("X")    // optional icon
)
```

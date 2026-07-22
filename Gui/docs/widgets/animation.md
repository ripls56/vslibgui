# Animation Widgets

## AnimatedBuilder

Rebuilds a subtree every animation frame.

```csharp
new AnimatedBuilder(
    animation: _controller,
    builder: ctx =>
    {
        float size = new FloatTween(12f, 20f).Evaluate(_curved);
        return new Text("Animated", new TextStyle { FontSize = size });
    }
)
```

See [Animations](../animations.md).

## AnimatedContainer

A `Container` that smoothly interpolates its `BoxStyle` when properties change.

```csharp
new AnimatedContainer(
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    style: new BoxStyle
    {
        Color = _active ? activeColor : inactiveColor,
        CornerRadius = new Vector4(_active ? 8 : 2),
        Width = 120,
        Height = 40
    }
)
```

## AnimatedOpacity

Automatically animates changes to opacity. Wraps `Opacity` internally.

```csharp
new AnimatedOpacity(
    opacity: _isVisible ? 1.0f : 0.0f,
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseInOut,
    child: new Text("Fades in and out")
)
```

## AnimatedPadding

Automatically animates changes to padding. Wraps `Padding` internally.

```csharp
new AnimatedPadding(
    padding: _expanded ? EdgeInsets.All(24) : EdgeInsets.All(4),
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    child: new Text("Breathing room")
)
```

## AnimatedRotation

Automatically animates rotation in radians with a configurable alignment origin. Wraps `Transform` internally.

```csharp
new AnimatedRotation(
    angle: _isOpen ? MathF.PI / 4 : 0f,  // 45 degrees
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseInOut,
    alignment: Alignment.Center,
    child: new Text("+")
)
```

## AnimatedScale

Automatically animates uniform scale with a configurable alignment origin. Wraps `Transform` internally.

```csharp
new AnimatedScale(
    scale: _isHovered ? 1.1f : 1.0f,
    duration: TimeSpan.FromMilliseconds(150),
    curve: Curves.EaseOut,
    alignment: Alignment.Center,
    child: myButton
)
```

## AnimatedSlide

Automatically animates a pixel translation offset. Wraps `Transform` internally.

```csharp
new AnimatedSlide(
    offset: _isDrawerOpen ? Vector2.Zero : new Vector2(-200, 0),
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseOut,
    child: drawerContent
)
```

## AnimatedText

A `Text` that smoothly interpolates its `TextStyle` (color, font size, outline, glow,
boldness) when properties change. The text content updates instantly.

```csharp
new AnimatedText(
    content: "Hello, world!",
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    style: new TextStyle
    {
        FontSize = _active ? 24 : 16,
        Color = _active ? activeColor : inactiveColor
    }
)
```

## AnimatedSize

Animates its layout size whenever its child's size changes. Uses a dedicated render object for efficient size transitions.

```csharp
new AnimatedSize(
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseInOut,
    child: _showDetails ? new Column(children: detailWidgets) : new SizedBox(height: 0)
)
```

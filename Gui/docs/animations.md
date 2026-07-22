# Animations

## AnimationController

`AnimationController` drives a `double` value from `0.0` to `1.0` (or in reverse) over a
specified duration. It wraps a `Ticker` that fires every game frame.

```csharp
// Create in InitState — requires a ticker provider from the UI's frame loop:
_ctrl = new AnimationController(
    TimeSpan.FromMilliseconds(500),
    Element.Owner.GetTickerProvider()
);

// Always dispose in State.Dispose():
_ctrl.Dispose();
```

### Control methods

| Method | Description |
|--------|-------------|
| `Forward(from?)` | Animate value from current position (or `from`) toward `1.0` |
| `Reverse()` | Animate value from current position toward `0.0` |
| `Reset()` | Stop and snap value to `0.0` |
| `Stop()` | Pause at current position |
| `Resume()` | Continue from current position in the current direction |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `double` | Current animation position `[0.0, 1.0]` |
| `Status` | `AnimationStatus` | `Dismissed` / `Forward` / `Reverse` / `Completed` |
| `IsAnimating` | `bool` | `true` if the ticker is currently running |

### Events

```csharp
_ctrl.OnValueChanged += value => { /* fires every tick */ };
_ctrl.OnStatusChanged += status =>
{
    if (status == AnimationStatus.Completed) _ctrl.Reverse();
    if (status == AnimationStatus.Dismissed) _ctrl.Forward();
    // ↑ Ping-pong loop
};
```

### Looping

```csharp
_ctrl.OnStatusChanged += s =>
{
    if (s == AnimationStatus.Completed) _ctrl.Reverse();
    else if (s == AnimationStatus.Dismissed) _ctrl.Forward();
};
_ctrl.Forward();   // start
```

## CurvedAnimation

`CurvedAnimation` wraps a controller and applies an easing function to its raw `[0, 1]` value:

```csharp
var curved = new CurvedAnimation(_ctrl, Curves.EaseInOut);
// curved.Value = Curves.EaseInOut.Transform(_ctrl.Value)
```

Both `AnimationController` and `CurvedAnimation` implement `IAnimation`, which means they can
both be passed to `Tween<T>.Evaluate()`.

### Available curves

| Constant | Formula | Feel |
|----------|---------|------|
| `Curves.Linear` | `t` | Constant speed |
| `Curves.EaseIn` | `t²` | Slow start, fast end |
| `Curves.EaseOut` | `t(2-t)` | Fast start, slow end |
| `Curves.EaseInOut` | `t²(3-2t)` | Slow start + slow end (smooth step) |
| `Curves.EaseInCubic` | `t³` | Slow start, fast end |
| `Curves.EaseOutCubic` | `1-(1-t)³` | Fast start, slow end |
| `Curves.EaseInOutCubic` | cubic smooth step | Slow start + slow end |
| `Curves.EaseInQuart` | `t⁴` | Slow start, fast end |
| `Curves.EaseOutQuart` | `1-(1-t)⁴` | Fast start, slow end |
| `Curves.EaseInOutQuart` | quartic smooth step | Slow start + slow end |
| `Curves.EaseInQuint` | `t⁵` | Slow start, fast end |
| `Curves.EaseOutQuint` | `1-(1-t)⁵` | Fast start, slow end |
| `Curves.EaseInOutQuint` | quintic smooth step | Slow start + slow end |
| `Curves.EaseInExpo` | `2^(10t-10)` | Near-zero then explosive |
| `Curves.EaseOutExpo` | `1-2^(-10t)` | Explosive then near-zero |
| `Curves.EaseInOutExpo` | exponential both ends | Extreme slow + fast |
| `Curves.EaseInBack` | overshoots below 0 | Spring pull-back at start |
| `Curves.EaseOutBack` | overshoots above 1 | Spring settle at end |
| `Curves.EaseInOutBack` | overshoots both ends | Spring on both sides |
| `Curves.EaseInElastic` | elastic oscillation at start | Rubber-band start |
| `Curves.EaseOutElastic` | elastic oscillation at end | Rubber-band end |
| `Curves.EaseInOutElastic` | elastic on both ends | Rubber-band both sides |
| `Curves.BounceOut` | bounces at end | Ball hitting the floor |
| `Curves.BounceIn` | bounces at start | Ball reverse |
| `Curves.BounceInOut` | bounces both ends | Ball both sides |

## Tweens

Tweens interpolate between typed values using the controller's current `Value`:

```csharp
// Float interpolation
var sizeTween = new FloatTween(12f, 24f);
float current = sizeTween.Evaluate(curved);   // curved.Value maps [0→1] to [12→24]

// Color (RGBA Vector4) interpolation
var colorTween = new ColorTween(
    new Vector4(1, 1, 1, 1),   // white
    new Vector4(1, 0.5f, 0, 1) // orange
);
Vector4 currentColor = colorTween.Evaluate(curved);

// 2D position interpolation
var offsetTween = new OffsetTween(Vector2.Zero, new Vector2(100, 50));
```

Available tween types:

| Type | `T` | Use case |
|------|-----|----------|
| `FloatTween` | `float` | Size, opacity, font size |
| `Vector4Tween` | `Vector4` | Corner radii |
| `ColorTween` | `Vector4` | Color (extends `Vector4Tween`) |
| `OffsetTween` | `Vector2` | Position, scroll offset |

## AnimatedBuilder

`AnimatedBuilder` is the primary way to drive per-frame widget rebuilds from an animation.
It subscribes to `AnimationController.OnValueChanged` in `InitState` and calls `SetState`
on every tick, re-running the `builder` lambda:

```csharp
new AnimatedBuilder(
    animation: _ctrl,
    builder: ctx =>
    {
        float size = new FloatTween(32f, 48f).Evaluate(_curved);
        Vector4 color = new ColorTween(
            new Vector4(1, 1, 1, 1),
            new Vector4(1, 0.8f, 0, 1)
        ).Evaluate(_curved);

        return new Text("Animated Title", new TextStyle
        {
            FontSize = size,
            Color = color
        });
    }
)
```

## Implicit Animations (Declarative)

Implicit animation widgets animate automatically when their properties change -- no
`AnimationController` setup required. Just change a value in `SetState` and the widget
smoothly transitions from the old value to the new one.

### ImplicitlyAnimatedWidget Base Class

All implicit animation widgets (except `AnimatedSize`) extend `ImplicitlyAnimatedWidget`:

| Parameter | Type | Description |
|-----------|------|-------------|
| `duration` | `TimeSpan` | How long the animation takes |
| `curve` | `Curve?` | Easing curve (defaults to `Curves.Linear`) |
| `onEnd` | `Action?` | Callback fired when the animation completes |

When the parent rebuilds with new property values, the widget interpolates from the
**current animated value** (not from the previous target) to the new target. This allows
mid-animation reversals without snapping.

### AnimatedOpacity

Animates opacity changes. Wraps `Opacity` internally.

```csharp
new AnimatedOpacity(
    opacity: _isVisible ? 1.0f : 0.0f,
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseInOut,
    child: new Text("Fades in and out"))
```

### AnimatedPadding

Animates padding changes. Wraps `Padding` internally.

```csharp
new AnimatedPadding(
    padding: _expanded
        ? EdgeInsets.All(24)
        : EdgeInsets.All(4),
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    child: new Text("Breathing room"))
```

### AnimatedScale

Animates uniform scale with a configurable alignment origin. Wraps `Transform` internally.

```csharp
new AnimatedScale(
    scale: _isHovered ? 1.1f : 1.0f,
    duration: TimeSpan.FromMilliseconds(150),
    curve: Curves.EaseOut,
    alignment: Alignment.Center,  // default
    child: myButton)
```

### AnimatedRotation

Animates rotation in radians with a configurable alignment origin. Wraps `Transform`
internally.

```csharp
new AnimatedRotation(
    angle: _isOpen ? MathF.PI / 4 : 0f,  // 45 degrees
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseInOut,
    alignment: Alignment.Center,  // default
    child: new Text("+"))
```

### AnimatedSlide

Animates a pixel translation offset. Wraps `Transform` internally.

```csharp
new AnimatedSlide(
    offset: _isDrawerOpen
        ? Vector2.Zero
        : new Vector2(-200, 0),
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseOut,
    child: drawerContent)
```

### AnimatedContainer

Animates `BoxStyle` property changes (color, corner radius, width, height).

```csharp
new AnimatedContainer(
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    style: new BoxStyle
    {
        Color = _isHovered
            ? new Vector4(0.4f, 0.6f, 1.0f, 1)
            : new Vector4(0.2f, 0.3f, 0.6f, 1),
        CornerRadius = new Vector4(_isHovered ? 12 : 4),
        Width = 120,
        Height = 40,
    },
    child: new Center(child: new Text("Hover me")))
```

**Animatable properties:** `Color`, `CornerRadius`, `Width`, `Height`
**Non-animatable (applied immediately):** `BorderThickness`, `BorderColor`, `Texture`,
`Padding`, `ClipBehavior`

### AnimatedText

Animates `TextStyle` property changes (color, font size, outline, glow, boldness). The
displayed text (`Content`) and non-interpolatable style fields update instantly.

```csharp
new AnimatedText(
    content: "Hello, world!",
    duration: TimeSpan.FromMilliseconds(200),
    curve: Curves.EaseOut,
    style: new TextStyle
    {
        FontSize = _isHovered ? 24 : 16,
        Color = _isHovered
            ? new Vector4(1.0f, 0.9f, 0.3f, 1)
            : new Vector4(0.8f, 0.8f, 0.8f, 1)
    })
```

**Animatable properties:** `Color`, `FontSize`, `OutlineWidth`, `OutlineColor`, `GlowWidth`,
`GlowColor`, `Boldness`
**Non-animatable (applied immediately):** `FontFamily`, `Weight`, `Align`, `Overflow`,
`Decoration`, `SoftWrap`, `MaxLines`, `Content`

### AnimatedSize

Animates the widget's layout size when its child changes size. Unlike the other implicit
widgets, `AnimatedSize` extends `SingleChildWidget` and uses a dedicated
`RenderAnimatedSize` render object with its own internal animation controller.

```csharp
new AnimatedSize(
    duration: TimeSpan.FromMilliseconds(300),
    curve: Curves.EaseInOut,
    child: _showDetails
        ? new Column(children: detailWidgets)
        : new SizedBox(height: 0))
```

### Creating Custom Implicit Animation Widgets

To animate a custom property, extend the base class:

1. Create a widget class extending `ImplicitlyAnimatedWidget`.
2. Create a state class extending `ImplicitlyAnimatedWidgetState<T>`.
3. Override `ForEachTween` to register tweens via `TweenVisitor.Visit`.
4. Override `Build` to use the interpolated values from the tweens.

```csharp
public class AnimatedFontSize : ImplicitlyAnimatedWidget
{
    public float FontSize { get; }
    public Widget? Child { get; }

    public AnimatedFontSize(float fontSize, TimeSpan duration,
        Curve? curve = null, Widget? child = null)
        : base(duration, curve)
    {
        FontSize = fontSize;
        Child = child;
    }

    public override State CreateState() => new _State();

    class _State : ImplicitlyAnimatedWidgetState<AnimatedFontSize>
    {
        FloatTween? _sizeTween;

        protected override void ForEachTween(TweenVisitor visitor)
        {
            // Visit creates the tween on first call, updates it on subsequent calls
            _sizeTween = (FloatTween)visitor.Visit(
                _sizeTween, Widget.FontSize, v => new FloatTween(v, v));
        }

        public override Widget Build(BuildContext context)
        {
            return new Text("Hello", new TextStyle
            {
                FontSize = _sizeTween!.Evaluate(Animation)
            });
        }
    }
}
```

The `TweenVisitor.Visit` method handles the lifecycle: on first call it creates a tween
where Begin=End=targetValue. On subsequent calls, if the target changed, it sets
Begin to the current interpolated value and End to the new target, so mid-animation
reversals blend smoothly.

## Opacity Animations

```csharp
// Fade in/out using AnimatedBuilder + Opacity:
new AnimatedBuilder(
    animation: _ctrl,
    builder: ctx => new Opacity((float)_curved.Value, child: new MyWidget())
)

// Or using Opacity with a raw controller value:
new Opacity((float)_ctrl.Value, child: new MyWidget())
```

`Opacity` uses `SKCanvas.SaveLayer` for values between `0.001` and `0.999`. At exactly 0 or
1, it skips the layer for performance.

# Authoring Custom Widgets

This guide shows how to write your own widget at three levels of complexity, from least to most
involved. Pick the lowest level that meets your needs — each extra level is more code and more
things that can go wrong.

| Level | Base class | When to use |
|-------|-----------|-------------|
| 1 | `StatelessWidget` / `StatefulWidget` | Composition of existing widgets. No custom painting, no custom layout. |
| 2 | `SingleChildWidget` + existing `RenderBox` subclass | You need a new visual effect or a small paint tweak but layout is already covered. |
| 3 | Custom `RenderObject` subclass | You need layout behavior that no existing `RenderObject` provides (custom flow, non-rectangular bounds, etc.). |

If you know you want level 3, read [rendering § Custom RenderObject](rendering.md#custom-renderobject)
for the lower-level details. This document is the conceptual guide.

---

## Level 1 — Composition only

Most widgets in this Library are just compositions of other
widgets. You don't need custom painting or custom layout — you need a named, reusable
configuration.

### Stateless composition

Use `StatelessWidget` when the widget has no mutable state. Everything it renders is derived from
its constructor arguments.

```csharp
public class Badge : StatelessWidget
{
    public string Text { get; }
    public Vector4 Background { get; }

    public Badge(string text, Vector4 background)
    {
        Text = text;
        Background = background;
    }

    public override Widget Build(BuildContext context) =>
        new Container(
            style: new BoxStyle
            {
                Padding = EdgeInsets.Symmetric(horizontal: 8, vertical: 2),
                Color = Background,
                CornerRadius = new Vector4(4),
            },
            child: new Text(Text));
}
```

Use it like any built-in widget:

```csharp
new Row(children: [
    new Badge("NEW", Vector4.UnitX),
    new Badge("HOT", new Vector4(1, 0.5f, 0, 1)),
])
```

`StatelessWidget.Build` is pure. It is called whenever the Library decides to rebuild this
widget (parent rebuild, inherited widget change, etc.). Don't cache anything in the widget
itself — widgets are meant to be cheap and disposable.

### Stateful composition

Use `StatefulWidget` when the widget owns values that change over time: counters, toggles,
animations, controllers, subscriptions. The widget stays immutable — the mutable data lives in a
separate `State<T>` instance that survives across widget rebuilds.

```csharp
public class Toggle : StatefulWidget
{
    public string Label { get; }
    public Action<bool> OnChanged { get; }

    public Toggle(string label, Action<bool> onChanged)
    {
        Label = label;
        OnChanged = onChanged;
    }

    public override State CreateState() => new _ToggleState();
}

class _ToggleState : State<Toggle>
{
    bool _isOn;

    public override Widget Build(BuildContext context) =>
        new GestureDetector(
            onTap: _ => {
                SetState(() => _isOn = !_isOn);
                Widget.OnChanged(_isOn);
            },
            child: new Row(children: [
                new Container(
                    width: 32, height: 16,
                    color: _isOn ? Vector4.UnitY : new Vector4(0.4f, 0.4f, 0.4f, 1)),
                new Padding(padding: EdgeInsets.Only(left: 8),
                    child: new Text(Widget.Label)),
            ]));
}
```

Key rules:

- `State` owns the mutable fields. The `Widget` is immutable — access it via the `Widget` property
  from the state.
- Call `SetState` to mutate fields. This marks the element dirty so the Library rebuilds it on
  the next frame.
- Create controllers (`AnimationController`, `TextEditingController`, etc.) in `InitState` and
  dispose them in `Dispose`.
- Don't forget to unhook listeners in `Dispose` — forgotten `RemoveListener` calls are the
  number-one leak source.

### When level 1 is enough

Any time the visual effect you want can be produced by arranging existing widgets
(`Container`, `Row`, `Column`, `Stack`, `Padding`, `Opacity`, `Transform`, `GestureDetector`, …),
level 1 is enough.

---

## Level 2 — Custom `RenderObject` wrapping an existing `RenderBox`

Sometimes you need a visual effect that no existing widget provides: a custom paint pass, a
non-standard clip shape, a debug overlay. If the layout behavior is already what `RenderBox`
gives you (take the incoming constraints, pick a size, let the Library place you) then you can
subclass `RenderBox` instead of building a whole `RenderObject` from scratch.

### Widget side

Inherit from `RenderObjectWidget` (or `SingleChildWidget` if you want a child slot). Override
`CreateRenderObject` and `UpdateRenderObject`.

```csharp
public class Dot : RenderObjectWidget
{
    public Vector4 Color { get; }
    public float Radius { get; }

    public Dot(Vector4 color, float radius) { Color = color; Radius = radius; }

    public override RenderObject CreateRenderObject() =>
        new _RenderDot { Color = Color, Radius = Radius };

    public override void UpdateRenderObject(RenderObject ro)
    {
        var dot = (_RenderDot)ro;
        dot.Color = Color;
        dot.Radius = Radius;
    }
}
```

### RenderObject side

Subclass `RenderBox` and override `PerformLayout` (if your size depends on something other than
the incoming constraints) and `PaintInternal`.

```csharp
class _RenderDot : RenderBox
{
    private Vector4 _color;
    public Vector4 Color
    {
        get => _color;
        set => SetProperty(ref _color, value, repaint: true);
    }

    private float _radius;
    public float Radius
    {
        get => _radius;
        set => SetProperty(ref _radius, value, relayout: true);
    }

    protected override void PerformLayout()
    {
        Size = Constraints.Constrain(new Vector2(_radius * 2, _radius * 2));
    }

    protected override void PaintInternal(PaintingContext context)
    {
        var paint = context.SharedPaint;
        paint.Color = _color.ToSkColor();
        paint.Style = SKPaintStyle.Fill;
        context.Canvas.DrawCircle(_radius, _radius, _radius, paint);
    }
}
```

### The critical rules

**Use `SetProperty` for all setters.** `RenderObject.SetProperty<T>` is the canonical way to
write property setters. It skips the update if the value did not change, then calls the correct
dirty flag:

```csharp
// repaint: true  → MarkNeedsPaint()  (visual change only)
// relayout: true → MarkNeedsLayout() (size or position changes)
public Vector4 Color
{
    get => _color;
    set => SetProperty(ref _color, value, repaint: true);
}

public float Radius
{
    get => _radius;
    set => SetProperty(ref _radius, value, relayout: true);
}
```

Never write the manual `if (field == value) return; field = value; MarkNeeds*();` pattern —
`SetProperty` does exactly this, with correct equality semantics for all types.

**Setters must pick the right dirty-flag.** A setter that only changes how the widget *looks*
(color, opacity, border color) passes `repaint: true`. A setter that changes size or position
passes `relayout: true`, which also propagates paint along the correct chain. Getting this wrong is one of the most common causes of wasted frame time.

**`PaintInternal` is a hot path.** It runs once per visible render object per frame. Don't
allocate inside it — no `new SKPaint`, no `new List<...>`, no `string.Format`. Use
`context.SharedPaint` or cache instances on the render object.

**Size must never be negative.** If `PerformLayout` is asked to produce a size, honor the
incoming `Constraints` — use `Constraints.Constrain(desiredSize)` so the Library's invariants
hold.

---

## Level 3 — Fully custom `RenderObject`

You need this only when you want layout behavior that no existing render object gives you:

- A non-rectangular flow (radial, diagonal).
- A widget that children are laid out relative to in an unusual way.
- A new kind of hit-testing

This is the hardest level. You're implementing a new `RenderObject` subclass from scratch,
managing the child-list protocol, and making `PerformLayout`/`PaintInternal` cooperate with the
Library's dirty flags.

Rather than duplicating the full walkthrough here, read the complete example in
[rendering § Custom RenderObject](rendering.md#custom-renderobject) — it shows the
`_RenderCircle` pattern end-to-end and is the canonical reference.

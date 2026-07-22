# State Management

## StatefulWidget and State

The primary way to hold mutable UI state is a `StatefulWidget` + `State<T>` pair.

```csharp
// 1. Declare the widget (immutable config):
class MyWidget : StatefulWidget
{
    public string Title { get; }
    public MyWidget(string title) { Title = title; }
    public override State CreateState() => new _MyWidgetState();
}

// 2. Implement State (mutable):
class _MyWidgetState : State<MyWidget>
{
    int _count = 0;

    // Called once when the element is first mounted.
    public override void InitState()
    {
        base.InitState();
        // Initialize resources here (animations, controllers, listeners)
    }

    // Builds the UI based on current state.
    public override Widget Build(BuildContext ctx)
    {
        return new Column(children:
        [
            new Text(Widget.Title),            // Widget is the current MyWidget instance
            new Text($"Count: {_count}"),
            new GestureDetector(
                onTap: _ => SetState(() => _count++),
                child: new Text("Increment")
            )
        ]);
    }

    // Called when the parent rebuilds with a new MyWidget instance.
    // Use this to react to prop changes.
    public override void UpdateWidget(MyWidget oldWidget)
    {
        // oldWidget.Title != Widget.Title — compare if needed
    }

    // Called when the element is removed from the tree.
    public override void Dispose()
    {
        // Release resources (AnimationControllers, listeners)
        base.Dispose();
    }
}
```

### SetState

`SetState(Action fn)` is the only correct way to trigger a rebuild:

```csharp
SetState(() =>
{
    _count++;
    _isEnabled = false;
    // Any number of mutations in one call
});
```

`SetState` executes `fn()` synchronously, then marks the element dirty so
`BuildOwner.BuildDirtyElements()` will call `Build()` again on the next frame. Only the
`StatefulElement` and its descendants rebuild — ancestors are unaffected.

**Do not** read widget props or state that should be reflected in the UI outside of `Build()`.
**Do not** call `SetState` from within `Build()`.

## Controlled Component Pattern

For input widgets (`Checkbox`, `RadioButton`, `Dropdown`), the parent holds the value and
passes a callback:

```csharp
class _FormState : State<_Form>
{
    bool _agreed = false;
    int _difficulty = 1;

    public override Widget Build(BuildContext ctx) =>
        new Column(children:
        [
            new Checkbox(
                value: _agreed,                         // controlled: value comes from parent
                onChanged: v => SetState(() => _agreed = v),
                label: "I agree to the terms"
            ),
            new RadioButton<int>(
                value: 1,
                groupValue: _difficulty,
                onChanged: v => SetState(() => _difficulty = v),
                label: "Easy"
            ),
            new RadioButton<int>(
                value: 2,
                groupValue: _difficulty,
                onChanged: v => SetState(() => _difficulty = v),
                label: "Hard"
            ),
            new Dropdown<int>(
                _difficulty,
                [ new DropdownItem<int> { Value = 1, Label = "Easy" },
                  new DropdownItem<int> { Value = 2, Label = "Hard" } ],
                v => SetState(() => _difficulty = v)
            ),
        ]);
}
```

The child widget **never owns state** — it only displays what it receives and fires callbacks.
The parent calls `SetState` in the callback to update the value, which propagates back down on
the next build.

## TextEditingController

`TextField` uses a `TextEditingController` to expose the current text value:

```csharp
class _FormState : State<_Form>
{
    TextEditingController _nameCtrl = new("default");

    public override void InitState()
    {
        base.InitState();
        _nameCtrl.AddListener(OnTextChanged);
    }

    void OnTextChanged()
    {
        // Called every time the user types
        Console.WriteLine(_nameCtrl.Text);
    }

    public override Widget Build(BuildContext ctx) =>
        new TextField(controller: _nameCtrl);

    public override void Dispose()
    {
        _nameCtrl.RemoveListener(OnTextChanged);
        base.Dispose();
    }
}
```

If you don't pass a controller, `TextField` creates its own internally and its value is not
accessible from the outside.

## ChangeNotifier and ValueNotifier

For observable values shared between multiple widgets, use `ChangeNotifier`:

```csharp
// A shared model:
class GameSettings : ChangeNotifier
{
    float _volume = 0.8f;
    public float Volume
    {
        get => _volume;
        set
        {
            if (Math.Abs(_volume - value) < 0.001f) return;
            _volume = value;
            NotifyListeners();    // fires all registered callbacks
        }
    }
}
```

Subscribe and unsubscribe in `State`:

```csharp
class _VolumeWidgetState : State<_VolumeWidget>
{
    readonly GameSettings _settings = GameSettings.Instance;

    public override void InitState()
    {
        base.InitState();
        _settings.AddListener(OnSettingsChanged);
    }

    void OnSettingsChanged() => SetState(() => { });

    public override Widget Build(BuildContext ctx) =>
        new Text($"Volume: {_settings.Volume:P0}");

    public override void Dispose()
    {
        _settings.RemoveListener(OnSettingsChanged);
        base.Dispose();
    }
}
```

`ValueNotifier<T>` is a `ChangeNotifier` specialized for a single value:

```csharp
var counter = new ValueNotifier<int>(0);
counter.Value = 5;   // fires listeners only if value changed
```

## Lifecycle Reference

```
CreateState()              // called once when element is created
    ↓
InitState()                // element is mounted, Owner is set, can create AnimationControllers
    ↓
Build(ctx)                 // UI is described; called again after SetState or parent rebuild
    ↓ (on parent rebuild with new widget)
UpdateWidget(oldWidget)    // Widget property has been updated; call SetState if rebuild needed
    ↓ (on unmount)
Dispose()                  // release all resources: controllers, listeners
```

## Preventing Unnecessary Rebuilds

**Use `Key` for stable identity.** Without keys, elements are matched by position. If you
insert an item at the top of a list, all elements below shift their position and may rebuild
unnecessarily:

```csharp
new Column(children: items.Select(item =>
    new ItemWidget(key: new ValueKey<int>(item.Id), item: item)
))
```

**Use `RepaintBoundary` for static subtrees.** Isolates a subtree from repaints triggered by
its siblings:

```csharp
var staticSidebar = new RepaintBoundary(new Sidebar());
```

**Hoist state as low as possible.** If only one widget needs a piece of state, put the
`StatefulWidget` right there, not at the top of the tree. Smaller dirty subtrees = fewer
rebuilds.

**Lift expensive computations out of `Build`.** `Build` may run many times per second during
animations. Heavy work (sorting lists, parsing strings) should be cached in `State` fields and
recomputed only when the data changes via `SetState`.

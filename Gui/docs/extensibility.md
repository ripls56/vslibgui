# Extending the Library from Other Mods

The library provides a **Widget Transformer Pipeline** that allows third-party mods to modify
widgets inside existing GUI screens without Harmony patches or copying source code.

## How it works

Before any widget is inflated into an element, the library checks whether any transformers
are registered for that widget's `Key`. If yes, the widget passes through the transformer chain
and the result is used instead of the original.

Only widgets with an explicit `Key` are eligible. Keyless widgets are never intercepted.

## Registering a transformer

Call `WidgetTransformerRegistry.Register` in your `ModSystem.Start()`:

```csharp
public class MyMod : ModSystem
{
  public override void Start(ICoreAPI api)
  {
    WidgetTransformerRegistry.Register(
      new ValueKey<string>("hud.status_bars"),
      new MyStatusBarTransformer()
    );
  }
}
```

Implement `IWidgetTransformer` to define what changes to make:

```csharp
class MyStatusBarTransformer : IWidgetTransformer
{
  public Widget Transform(Widget widget)
  {
    var row = (Row)widget;
    return new Row(
      key: row.Key,
      children: [..row.Children, new MyStatusBar()]
    );
  }
}
```

The transformer receives the widget produced by the library (or by the previous transformer),
and must return a valid replacement of the same type and key.

## Priority

Multiple mods can transform the same widget. Use `priority` to control order — lower value
runs first (default: `0`):

```csharp
WidgetTransformerRegistry.Register(key, transformer, priority: 10);
```

Each transformer receives the output of the previous one, so they compose cleanly.

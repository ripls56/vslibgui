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
fires `onClose`. Both buttons show hover highlighting.

## WindowFrame

Convenience widget that wraps `WindowTitleBar` + content in a `Column` and handles minimize
state automatically. When minimized, only the title bar is visible.

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

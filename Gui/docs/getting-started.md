# Getting Started

This guide walks you through creating a custom GUI screen from scratch.

## Project References

Add these references to your `.csproj`. `Gui.dll` lives wherever you downloaded the
library; the rest ship with the game and resolve via the `VINTAGE_STORY` environment variable.

```xml
<Reference Include="Gui">
  <HintPath>C:\path\to\Gui\bin\Release\Mods\mod\Gui.dll</HintPath>
  <Private>false</Private>
</Reference>

<Reference Include="OpenTK.Mathematics">
  <HintPath>$(VINTAGE_STORY)\Lib\OpenTK.Mathematics.dll</HintPath>
  <Private>false</Private>
</Reference>
<Reference Include="SkiaSharp">
  <HintPath>$(VINTAGE_STORY)\Lib\SkiaSharp.dll</HintPath>
  <Private>false</Private>
</Reference>
```

## Step 1: Create a GUI Screen

Subclass `GuiBase`, override `Build()` for the UI and `CreateWindowConfig()` for window
behavior:

```csharp
using Gui;
using Gui.Widgets;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

public class MyInventoryGui : GuiBase
{
    public MyInventoryGui(ICoreClientAPI capi) : base(capi) { }

    protected override WindowConfig CreateWindowConfig() => new()
    {
        Size = new Vector2(400, 300),
        Draggable = true,
        Resizable = true,
    };

    protected override Widget Build()
    {
        return new WindowFrame(
            title: "My Inventory",
            onClose: () => TryClose(),
            child: new Padding(
                EdgeInsets.All(16),
                child: new Text("Inventory content here")
            )
        );
    }
}
```

`Build()` is called once when the GUI opens. It returns the **immutable widget tree** that
describes the UI. The Library turns this into an element tree and a render tree automatically.

`CreateWindowConfig()` defines the window's size, position, and drag/resize behavior. See
[Windowing](windowing.md) for all options.

## Step 2: Open the GUI

```csharp
// In your ModSystem or event handler:
var gui = new MyInventoryGui(capi);
gui.TryOpen();

// Close it programmatically:
gui.TryClose();
```

Alternatively, register a hotkey:

```csharp
public override void StartClientSide(ICoreClientAPI api)
{
    api.Input.RegisterHotKey("myguiopen", "Open My GUI", GlKeys.M);
    api.Input.SetHotKeyHandler("myguiopen", _ =>
    {
        new MyInventoryGui(api).TryOpen();
        return true;
    });
}
```

## Step 3: Add Local State

Wrap the content in a `StatefulWidget` to hold mutable data:

```csharp
public class MyInventoryGui : GuiBase
{
    public MyInventoryGui(ICoreClientAPI capi) : base(capi)
    {
    }

    protected override Widget Build() => new _MyContent();

    class _MyContent : StatefulWidget
    {
        public override State CreateState() => new _MyContentState();
    }

    class _MyContentState : State<_MyContent>
    {
        TextEditingController _textController = null!;
        bool _filterEnabled = false;

        public override void InitState()
        {
            base.InitState();
            _textController = new TextEditingController("");
        }

        public override Widget Build(BuildContext ctx)
        {
            return new Center(
                child: new Container(
                    style: new BoxStyle { Width = 400, Height = 300, Padding = EdgeInsets.All(16) },
                    child: new Column(
                        spacing: 12,
                        children:
                        [
                            new Text("Search Items", new TextStyle { FontSize = 16 }),
                            new Row(children:
                                [
                                    new Expanded(child: new TextField(controller: _textController)),
                                ]
                            ),
                            new Checkbox(
                                _filterEnabled,
                                v => SetState(() => _filterEnabled = v),
                                "Enable filter"
                            ),
                            new Text($"Filter active: {_filterEnabled}",
                                new TextStyle { Color = new Vector4(0.7f, 0.7f, 0.7f, 1) }
                            )
                        ]
                    )
                )
            );
        }

        public override void Dispose()
        {
            _textController.Dispose();
            base.Dispose();
        }
    }
}

```

`SetState(fn)` executes `fn` to mutate state fields, then schedules a rebuild of only this
subtree. The parent and siblings are untouched.

## Step 4: Initialize Animations in State

If your screen needs animations, create the `AnimationController` in `InitState()` and dispose
it in `Dispose()`:

```csharp
class _MyContentState : State<_MyContent>
{
    AnimationController? _fadeCtrl;
    CurvedAnimation? _fadeAnim;

    public override void InitState()
    {
        base.InitState();
        _fadeCtrl = new AnimationController(
            TimeSpan.FromMilliseconds(400),
            Element.Owner.GetTickerProvider()   // ticker is tied to the GUI's frame loop
        );
        _fadeAnim = new CurvedAnimation(_fadeCtrl, Curves.EaseOut);
        _fadeCtrl.Forward();                    // start playing immediately
    }

    public override Widget Build(BuildContext ctx)
    {
        return new AnimatedBuilder(
            animation: _fadeCtrl!,
            builder: c => new Opacity(
                (float)_fadeAnim!.Value,
                child: new Text("Fades in on open")
            )
        );
    }

    public override void Dispose()
    {
        _fadeCtrl?.Dispose();   // stops the ticker and frees resources
        base.Dispose();
    }
}
```

## Step 5: Handle Cleanup

`GuiBase.OnGuiClosed()` calls `RootElement.Unmount()` automatically, which recursively calls
`State.Dispose()` on every stateful widget. You only need to manually dispose resources that
live inside `State`, such as `AnimationController`.

## Complete Minimal Example

```csharp
using System;
using Gui;
using Gui.Widgets;
using Gui.Rendering.Text;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

public class HelloWorldGui : GuiBase
{
    public HelloWorldGui(ICoreClientAPI capi) : base(capi) { }

    protected override WindowConfig CreateWindowConfig() => new()
    {
        Size = new Vector2(300, 180),
        Draggable = true,
        Resizable = true,
    };

    protected override Widget Build() => new WindowFrame(
        title: "Hello World",
        onClose: () => TryClose(),
        child: new _Content()
    );

    class _Content : StatefulWidget
    {
        public override State CreateState() => new _State();
    }

    class _State : State<_Content>
    {
        int _clicks = 0;

        public override Widget Build(BuildContext ctx) =>
            new Center(
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.Center,
                    spacing: 12,
                    children:
                    [
                        new Text($"Clicked {_clicks} times",
                            new TextStyle { FontSize = 18, Color = Vector4.One }),
                        new GestureDetector(
                            onTap: _ => SetState(() => _clicks++),
                            child: new Container(
                                style: new BoxStyle
                                {
                                    Color = new Vector4(0.2f, 0.5f, 0.9f, 1),
                                    CornerRadius = new Vector4(6),
                                    Padding = EdgeInsets.Symmetric(vertical: 8, horizontal: 20)
                                },
                                child: new Text("Click me", new TextStyle { FontSize = 14 })
                            )
                        )
                    ]
                )
            );
    }
}
```

See [Architecture](architecture.md) for a deep dive.

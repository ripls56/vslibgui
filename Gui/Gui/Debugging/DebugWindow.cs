using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Debugging;

/// <summary>
///     Debug tool window implemented with the widget library. Replaces the legacy
///     <c>GuiDialogGeneric</c>-based debug window. Shows overlay toggles, performance
///     metrics, and an optional widget-tree dump.
/// </summary>
public class DebugWindow : GuiBase
{
    private readonly DebugSettings _settings;

    /// <summary>Initializes a new <see cref="DebugWindow" />.</summary>
    public DebugWindow(ICoreClientAPI capi, DebugSettings settings) : base(capi)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    protected override WindowConfig CreateWindowConfig()
    {
        return new WindowConfig
        {
            Draggable = true,
            Resizable = false,
            MinSize = new Vector2(220, 100),
            MaxSize = new Vector2(520, 500)
        };
    }

    /// <inheritdoc />
    protected override Widget Build()
    {
        return new WindowFrame(
            "UI Debugger",
            onClose: () =>
            {
                _settings.ShowWindow = false;
                TryClose();
            },
            child: new ListenableBuilder(
                _settings,
                _ => new DebugPanel(_settings)
            )
        );
    }
}

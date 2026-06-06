using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Gui;

/// <summary>
///     Full-screen, non-interactive overlay for rendering information
///     (tooltips, damage numbers, floating text, notifications) on top
///     of all other UI. Passes through all input events.
///     <para>
///         Access the singleton via <see cref="Instance" />. Insert and remove
///         entries with <see cref="Insert" /> / <see cref="Remove" />.
///     </para>
/// </summary>
public sealed class GuiGlobalOverlay : GuiBase
{
    private OverlayState? _overlayState;

    /// <summary>
    ///     Creates the global overlay and sets <see cref="Instance" />.
    /// </summary>
    public GuiGlobalOverlay(
        ICoreClientAPI capi
    ) : base(capi)
    {
        Instance = this;
    }

    /// <summary>Singleton instance, available after <c>GuiModSystem.StartClientSide</c>.</summary>
    public static GuiGlobalOverlay? Instance { get; private set; }

    /// <summary>Overlays render on top of regular dialogs and HUDs.</summary>
    public override double DrawOrder => 1;

    /// <summary>HUD dialogs render above the game but below menus.</summary>
    public override EnumDialogType DialogType => EnumDialogType.HUD;

    /// <summary>Never intercepts keyboard events.</summary>
    public override bool ShouldReceiveKeyboardEvents() => false;

    /// <summary>Never intercepts mouse events.</summary>
    public override bool ShouldReceiveMouseEvents() => false;

    /// <inheritdoc />
    protected override WindowConfig CreateWindowConfig()
    {
        var scale = RuntimeEnv.GUIScale;
        var screenW = capi.Render.FrameWidth / scale;
        var screenH = capi.Render.FrameHeight / scale;
        return new WindowConfig
        {
            Draggable = false,
            Resizable = false,
            Position = Vector2.Zero,
            Size = new Vector2(
                screenW,
                screenH
            )
        };
    }

    /// <inheritdoc />
    protected override Widget Build() => new SizedBox();

    /// <summary>
    ///     Opens the overlay and captures the root <see cref="OverlayState" />
    ///     that <see cref="GuiBase.TryOpen" /> creates automatically.
    /// </summary>
    public override bool TryOpen(
        bool withFocus
    )
    {
        var result = base.TryOpen(withFocus);
        if (result && RootElement is StatefulElement { State: OverlayState os })
        {
            _overlayState = os;
        }

        return result;
    }

    /// <inheritdoc />
    public override void OnRenderGUI(
        float deltaTime
    )
    {
        // Keep stretched to full screen on every frame.
        var scale = RuntimeEnv.GUIScale;
        var screenW = capi.Render.FrameWidth / scale;
        var screenH = capi.Render.FrameHeight / scale;
        WindowPos = Vector2.Zero;
        WindowSize = new Vector2(
            screenW,
            screenH
        );
        SyncLayoutSize();

        base.OnRenderGUI(deltaTime);
    }

    /// <summary>
    ///     Inserts an <see cref="OverlayEntry" /> into the global overlay.
    /// </summary>
    public static void Insert(
        OverlayEntry entry
    ) =>
        Instance?._overlayState?.Insert(entry);

    /// <summary>
    ///     Removes an <see cref="OverlayEntry" /> from the global overlay.
    /// </summary>
    public static void Remove(
        OverlayEntry entry
    ) =>
        Instance?._overlayState?.RemoveEntry(entry);

    /// <inheritdoc />
    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        _overlayState = null;
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

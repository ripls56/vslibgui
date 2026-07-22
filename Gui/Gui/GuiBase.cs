using System;
using System.Runtime.CompilerServices;
using Gui.Clipboard;
using Gui.Core.Framework;
using Gui.Core.Windowing;
using Gui.Debugging;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

[assembly: InternalsVisibleTo("Gui.Tests")]

namespace Gui;

/// <summary>
///     Abstract base class for all GUI screens in the Library. Subclass this and override
///     <see cref="Build" /> to define your UI.
///     <para>
///         <b>Basic usage:</b>
///         <code>
/// public class MyGui : GuiBase
/// {
///     public MyGui(ICoreClientAPI capi) : base(capi) { }
///
///     protected override Widget Build() =>
///         new Center(child: new Text("Hello!"));
///
///     protected override WindowConfig CreateWindowConfig() => new()
///     {
///         Size = new Vector2(400, 300),
///         Draggable = true,
///         Resizable = true,
///     };
/// }
///
/// // Open the screen:
/// new MyGui(capi).TryOpen();
/// </code>
///     </para>
/// </summary>
public abstract class GuiBase : GuiDialog, ITickerProvider
{
    /// Dummy composer key so VS engine's GuiManager dispatches OnMouseWheel
    /// to us in the priority "point-inside-bounds" loop rather than the
    /// catch-all loop (where the hotbar intercepts it first).
    private const string HitBoundsKey = "__guibase_hitbounds";
    private readonly GlfwCursorController _cursorController = new();
    private readonly FramePipeline _framePipeline;
    private readonly DialogInputRouter _inputRouter;

    private readonly TickerScheduler _tickerScheduler = new();

    private int _elementCount;
    private WindowInteractionStateMachine _interactionSm = null!;

    /// <summary>Last known mouse position in window-local space (for OnMouseWheel).</summary>
    private Vector2? _lastMouseLocal;

    /// <summary>The committed layout size — only updated after resize settles.</summary>
    private Vector2 _layoutSize;

    private double _localElapsedMs;

    /// <summary>Whether position has been initialized (for deferred centering).</summary>
    private bool _positionInitialized;

    private Action<RenderObject, string>? _violationReporter;

    private WindowConfig _windowConfig = new();

    protected GuiBase
    (
        ICoreClientAPI capi
    ) : base(capi)
    {
        ModSystem = capi.ModLoader.GetModSystem<GuiModSystem>();
        BuildOwner.SetTickerProvider(this);
        BuildOwner.SetClipboard(new GameClipboard(capi));
        BuildOwner.SetSoundPlayer(new SoundPlayer(capi));
        BuildOwner.FocusManager = FocusManager;
        BuildOwner.OnError =
            (
                element,
                ex
            ) =>
            {
                capi.Logger.Error
                (
                    $"[GUI] Failed to rebuild element for widget {element.Widget.DebugPropertyName}: {ex}"
                );
            };
        BuildOwner.OnElementMounted = () => _elementCount++;
        BuildOwner.OnElementUnmounted = () => _elementCount--;
        _inputRouter = new DialogInputRouter
            (EventDispatcher, FocusManager, () => ModSystem.ActiveWindow = this);
        _framePipeline = new FramePipeline(ro => PerformanceMetrics.IncrementPaint());
    }

    /// <summary>The root element of the inflated widget tree. Available after <see cref="TryOpen" />.</summary>
    public Element? RootElement { get; private set; }

    /// <summary>Schedules and executes dirty element rebuilds each frame.</summary>
    public BuildOwner BuildOwner { get; } = new();

    /// <summary>Routes pointer and keyboard events through the element tree.</summary>
    public EventDispatcher EventDispatcher { get; } = new();

    /// <summary>Manages keyboard focus across all focusable elements in this screen.</summary>
    public FocusManager FocusManager { get; } = new();

    protected GuiModSystem ModSystem { get; }
    public PerformanceMetrics PerformanceMetrics { get; } = new();

    /// <summary>Per-window frametime profiler. Surfaced via <c>/ui prof</c>.</summary>
    public GuiProfiler Profiler { get; } = new();

    public override string? ToggleKeyCombinationCode => null;

    /// <summary>
    ///     Unique key used to persist this dialog's position across open/close cycles.
    ///     Stored in <c>clientsettings.json → dialogPositions</c>. Override to customise.
    /// </summary>
    public virtual string DialogCode => GetType().Name.ToLowerInvariant();

    /// <summary>
    ///     Mirrors vanilla <c>GuiDialogBlockEntity</c>: keep <c>mouseWorldInteractAnyway</c>
    ///     enabled so unhandled clicks fall through to the game world while the dialog is open.
    ///     Without this, VS sets the global <c>flag2</c> true and blocks all world interaction
    ///     even outside the window's composer bounds.
    /// </summary>
    public override bool PrefersUngrabbedMouse => false;

    /// <summary>Window position in logical (UI-scaled) pixels.</summary>
    protected Vector2 WindowPos { get; set; }

    /// <summary>Current window size in logical pixels.</summary>
    protected Vector2 WindowSize { get; set; }


    /// <summary>True while this window is being dragged or resized by the user.</summary>
    internal bool IsDraggingOrResizing => _interactionSm?.IsInteracting ?? false;

    /// <summary>
    ///     Whether this window registers a dummy hit-bounds composer so the VS engine
    ///     routes point-inside-composer events (mouse wheel, HudDropItem's outside-click
    ///     detection, etc.) against its bounds. Interactive windows need this; pass-through
    ///     overlays that never consume input must return <c>false</c> so they don't pollute
    ///     the engine's hit-testing.
    /// </summary>
    protected virtual bool RegistersHitBounds => true;

    public Ticker CreateTicker
    (
        Action<TimeSpan> onTick
    ) =>
        _tickerScheduler.CreateTicker(onTick);

    /// <summary>
    ///     Override this to return the root widget of your GUI screen. Called once in
    ///     <see cref="TryOpen" />. If you need mutable state, return a <see cref="StatefulWidget" />
    ///     here and manage state inside its <c>State</c> object.
    /// </summary>
    protected abstract Widget Build();

    /// <summary>
    ///     Override to configure window behavior: position, size, drag, resize, dialog type.
    ///     Called once during <see cref="TryOpen" />.
    /// </summary>
    protected virtual WindowConfig CreateWindowConfig() => new();

    /// <summary>
    ///     Called after layout completes for shrink-wrap windows whose
    ///     content size may have changed. Override to reposition the
    ///     window based on the new <see cref="WindowSize" />.
    /// </summary>
    protected virtual void OnShrinkWrapLayoutCompleted()
    {
    }

    public override bool TryOpen
    (
        bool withFocus
    )
    {
        if (IsOpened())
        {
            return false;
        }

        _windowConfig = CreateWindowConfig();
        _interactionSm = new WindowInteractionStateMachine(_windowConfig);
        _positionInitialized = false;

        if (_windowConfig.Size.HasValue)
        {
            WindowSize = ClampSize(_windowConfig.Size.Value);
        }
        else
        {
            WindowSize = new Vector2
            (
                400,
                300
            ); // initial estimate for shrink-wrap
        }

        _layoutSize = WindowSize;

        if (_windowConfig.Position.HasValue)
        {
            WindowPos = _windowConfig.Position.Value;
            _positionInitialized = true;
        }
        else
        {
            var saved = capi.Gui.GetDialogPosition(DialogCode);
            if (saved != null)
            {
                WindowPos = new Vector2(saved.X, saved.Y);
                _positionInitialized = true;
            }
        }

        ModSystem.RegisterWindow(this);
        _elementCount = 0;
        var rootWidget = BuildRootTree();
        RootElement = rootWidget.CreateElement();
        RootElement.AssignOwner(BuildOwner);
        RootElement.Mount(null);

        SyncHitBounds();
        return base.TryOpen(withFocus);
    }

    /// <summary>
    ///     Registers (or updates) a dummy GuiComposer whose bounds match our
    ///     window rect. The VS engine only dispatches OnMouseWheel to dialogs
    ///     whose Composers contain the mouse pointer, so without this our
    ///     Library-based dialog would never receive wheel events.
    /// </summary>
    private void SyncHitBounds()
    {
        // Pass-through windows (e.g. the full-screen global overlay) must not register a
        // hit-bounds composer: the VS engine's point-inside-composer hit-testing — most
        // notably HudDropItem's drop-on-click-outside — would otherwise treat the whole
        // screen as "inside a dialog" and never drop the cursor item.
        if (!RegistersHitBounds)
        {
            return;
        }

        var extraScale = GetUiScale() / RuntimeEnv.GUIScale;

        if (Composers.ContainsKey(HitBoundsKey))
        {
            var b = Composers[HitBoundsKey].Bounds;
            b.fixedX = WindowPos.X * extraScale;
            b.fixedY = WindowPos.Y * extraScale;
            b.fixedWidth = WindowSize.X * extraScale;
            b.fixedHeight = WindowSize.Y * extraScale;
            b.CalcWorldBounds();
        }
        else
        {
            var bounds = ElementBounds.Fixed
            (
                WindowPos.X * extraScale,
                WindowPos.Y * extraScale,
                WindowSize.X * extraScale,
                WindowSize.Y * extraScale
            );
            bounds.CalcWorldBounds();
            Composers[HitBoundsKey] = capi.Gui.CreateCompo
            (
                HitBoundsKey,
                bounds
            );
        }
    }

    protected virtual float GetUiScale() => RuntimeEnv.GUIScale;

    /// <summary>Convert raw pixel mouse coords to logical screen coords.</summary>
    private Vector2 ToLogicalScreen
    (
        int rawX,
        int rawY
    )
    {
        var uiScale = GetUiScale();
        return new Vector2
        (
            rawX / uiScale,
            rawY / uiScale
        );
    }

    /// <summary>Convert logical screen coords to window-local coords.</summary>
    private Vector2 ToWindowLocal
    (
        Vector2 screen
    ) =>
        screen - WindowPos;

    /// <summary>Check if a window-local point is inside the window bounds.</summary>
    private bool IsInsideWindow
    (
        Vector2 local
    )
    {
        return local.X >= 0 && local.X <= WindowSize.X &&
               local.Y >= 0 && local.Y <= WindowSize.Y;
    }

    /// <summary>
    ///     Keeps the window within the screen bounds so it can never end up entirely
    ///     off-screen (e.g. from a stale saved position or an over-eager drag).
    ///     Windows larger than the screen are pinned to the top-left corner.
    /// </summary>
    private void ClampWindowToScreen
    (
        float screenW,
        float screenH
    )
    {
        var maxX = Math.Max
        (
            0f,
            screenW - WindowSize.X
        );
        var maxY = Math.Max
        (
            0f,
            screenH - WindowSize.Y
        );
        var clamped = new Vector2
        (
            Math.Clamp
            (
                WindowPos.X,
                0f,
                maxX
            ),
            Math.Clamp
            (
                WindowPos.Y,
                0f,
                maxY
            )
        );
        if (clamped == WindowPos)
        {
            return;
        }

        WindowPos = clamped;
        SyncHitBounds();
    }

    private Vector2 ClampSize
    (
        Vector2 size
    )
    {
        return new Vector2
        (
            Math.Clamp
            (
                size.X,
                _windowConfig.MinSize.X,
                _windowConfig.MaxSize.X
            ),
            Math.Clamp
            (
                size.Y,
                _windowConfig.MinSize.Y,
                _windowConfig.MaxSize.Y
            )
        );
    }


    private void CommitResizeLayout()
    {
        _layoutSize = WindowSize;
        RootElement?.RenderObject?.MarkNeedsLayout();
    }

    /// <summary>
    ///     Syncs internal layout size with <see cref="WindowSize" /> and marks
    ///     root for relayout. Call when <see cref="WindowSize" /> is changed
    ///     programmatically (e.g. tracking game window resize).
    /// </summary>
    protected void SyncLayoutSize()
    {
        if (_layoutSize == WindowSize)
        {
            return;
        }

        _layoutSize = WindowSize;
        RootElement?.RenderObject?.MarkNeedsLayout();
    }


    /// <summary>
    ///     Returns true when another currently-focused dialog covers the given raw-pixel screen
    ///     point. The focused dialog always has priority — Windows-style focus model: clicking
    ///     a non-overlap area of a window first brings it to focus before its overlap region
    ///     can intercept events.
    /// </summary>
    private bool IsBlockedByFrontDialog(int rawX, int rawY)
    {
        // Self-exemption: a dragging/resizing window is never blocked by anyone.
        if (_interactionSm.IsInteracting)
        {
            return false;
        }

        // Yield to any other window actively dragging or resizing — its bounds may be stale.
        foreach (var w in ModSystem.OpenWindows)
        {
            if (w != this && w.IsDraggingOrResizing)
            {
                return true;
            }
        }

        if (Focused)
        {
            return false;
        }

        foreach (var dlg in capi.Gui.OpenedGuis)
        {
            if (dlg == this)
            {
                continue;
            }

            if (!dlg.Focused)
            {
                continue;
            }

            if (dlg.DialogType != EnumDialogType.Dialog)
            {
                continue;
            }

            foreach (var composer in dlg.Composers.Values)
            {
                if (composer.Bounds.PointInside(rawX, rawY))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override void OnMouseMove
    (
        MouseEvent args
    )
    {
        if (args.Handled && !_interactionSm.IsInteracting)
        {
            if (RootElement?.RenderObject != null)
            {
                _inputRouter.PointerLeave(RootElement);
            }

            return;
        }

        base.OnMouseMove(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        var screen = ToLogicalScreen
        (
            args.X,
            args.Y
        );

        if (_interactionSm.OnMove(screen, _localElapsedMs, out var newPos, out var newSize))
        {
            WindowPos = newPos;
            if (_interactionSm.IsResizing)
            {
                WindowSize = newSize;
            }

            SyncHitBounds();
            args.Handled = true;
            return;
        }

        var local = ToWindowLocal(screen);
        _lastMouseLocal = local;
        MouseOverCursor = null;

        var e = _inputRouter.PointerMove(RootElement, local.X, local.Y);

        if (!IsInsideWindow(local) && !_interactionSm.IsInteracting)
        {
            args.Handled = false;
            var hoverCursor = _interactionSm.HoverCursorFor(local, WindowSize);
            if (hoverCursor.HasValue)
            {
                _cursorController.SetShape(hoverCursor.Value);
            }

            return;
        }

        var blocked = IsBlockedByFrontDialog(args.X, args.Y);
        if (blocked)
        {
            args.Handled = false;
        }
        else if (e.Handled)
        {
            args.Handled = true;
        }

        var widgetCursor = _inputRouter.ResolvedCursor();
        if (widgetCursor != null && !blocked)
        {
            MouseOverCursor = widgetCursor.Name;
        }

        var cursor = _interactionSm.HoverCursorFor(local, WindowSize);
        if (cursor.HasValue)
        {
            _cursorController.SetShape(cursor.Value);
        }
    }

    public override void OnMouseDown
    (
        MouseEvent args
    )
    {
        if (args.Handled)
        {
            return;
        }

        base.OnMouseDown(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        var screen = ToLogicalScreen
        (
            args.X,
            args.Y
        );
        var local = ToWindowLocal(screen);

        var edge = _interactionSm.DetectResizeEdge(local, WindowSize);
        if (edge != ResizeEdge.None)
        {
            _interactionSm.BeginResize(edge, screen, WindowPos, WindowSize);
            args.Handled = true;
            return;
        }

        if (!IsInsideWindow(local))
        {
            // Outside our window: let the click fall through. The cursor-item
            // drop-on-click-outside is handled by the engine's HudDropItem (it works
            // again now that pass-through windows no longer register hit-bounds — see
            // RegistersHitBounds).
            _windowConfig.OnPointerDownOutside?.Invoke();
            args.Handled = false;
            return;
        }

        if (IsBlockedByFrontDialog(args.X, args.Y))
        {
            args.Handled = false;
            return;
        }

        var capturedByWidget = _inputRouter.PointerDown
            (RootElement, local.X, local.Y, (PointerButton)args.Button);
        if (capturedByWidget)
        {
            args.Handled = true;
            return;
        }

        if (_interactionSm.IsInDragZone(local, WindowSize))
        {
            _interactionSm.BeginDrag(screen, WindowPos);
            args.Handled = true;
        }

        if (_windowConfig.OpaqueHitTest)
        {
            args.Handled = true;
        }
    }

    public override void OnMouseUp
    (
        MouseEvent args
    )
    {
        base.OnMouseUp(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        var (wasDragging, wasResizing) = _interactionSm.OnUp();
        if (wasDragging || wasResizing)
        {
            if (wasDragging)
            {
                capi.Gui.SetDialogPosition
                    (DialogCode, new Vec2i((int)WindowPos.X, (int)WindowPos.Y));
            }

            if (wasResizing)
            {
                CommitResizeLayout();
            }

            args.Handled = true;
            return;
        }

        var screen = ToLogicalScreen
        (
            args.X,
            args.Y
        );
        var local = ToWindowLocal(screen);

        var e = _inputRouter.PointerUp(RootElement, local.X, local.Y, (PointerButton)args.Button);
        if (e.Handled && IsInsideWindow(local) && !IsBlockedByFrontDialog(args.X, args.Y))
        {
            args.Handled = true;
        }
        else
        {
            args.Handled = false;
        }
    }

    public override void OnMouseWheel
    (
        MouseWheelEventArgs args
    )
    {
        if (args.IsHandled)
        {
            return;
        }

        base.OnMouseWheel(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        Vector2 local;
        if (_lastMouseLocal.HasValue)
        {
            local = _lastMouseLocal.Value;
        }
        else
        {
            var screen = ToLogicalScreen
            (
                capi.Input.MouseX,
                capi.Input.MouseY
            );
            local = ToWindowLocal(screen);
        }

        if (!IsInsideWindow(local))
        {
            return;
        }

        if (IsBlockedByFrontDialog(capi.Input.MouseX, capi.Input.MouseY))
        {
            return;
        }

        if (_inputRouter.MouseWheel(RootElement, local.X, local.Y, args.deltaPrecise))
        {
            args.SetHandled();
        }
    }

    public override void OnKeyDown
    (
        KeyEvent args
    )
    {
        if (args.Handled)
        {
            return;
        }

        if (!Focused && FocusManager.PrimaryFocus == null)
        {
            return;
        }

        if (Focused)
        {
            base.OnKeyDown(args);
        }

        // Tab / Shift+Tab drive focus traversal. Handle BEFORE dispatching to widgets:
        // a focused TextField marks every non-Alt key Handled, which would otherwise swallow Tab.
        // No guard on PrimaryFocus: when nothing is focused yet, FocusNext selects the first node.
        if (RootElement != null && IsTabKey(args.KeyCode))
        {
            if (args.ShiftPressed)
            {
                FocusManager.FocusPrevious(RootElement);
            }
            else
            {
                FocusManager.FocusNext(RootElement);
            }

            args.Handled = true;
            return;
        }

        if (ModSystem.DebugSettings.IsAnyEnabled)
        {
            capi.Logger.Debug($"[GUI KeyDown] KeyCode: {args.KeyCode}");
        }

        var keyHandled = _inputRouter.KeyDown
            (args.KeyCode, args.ShiftPressed, args.CtrlPressed, args.AltPressed);

        args.Handled |= keyHandled;
    }

    internal static bool IsTabKey(int keyCode) => keyCode == (int)GlKeys.Tab;

    public override void OnKeyUp
    (
        KeyEvent args
    )
    {
        if (args.Handled)
        {
            return;
        }

        if (!Focused && FocusManager.PrimaryFocus == null)
        {
            return;
        }

        if (Focused)
        {
            base.OnKeyUp(args);
        }

        args.Handled |= _inputRouter.KeyUp
            (args.KeyCode, args.ShiftPressed, args.CtrlPressed, args.AltPressed);
    }

    public override void OnKeyPress
    (
        KeyEvent args
    )
    {
        if (args.Handled)
        {
            return;
        }

        if (!Focused && FocusManager.PrimaryFocus == null)
        {
            return;
        }

        if (Focused)
        {
            base.OnKeyPress(args);
        }

        args.Handled |= _inputRouter.KeyChar
            (args.KeyChar, args.ShiftPressed, args.CtrlPressed, args.AltPressed);
    }


    public override void OnRenderGUI
    (
        float deltaTime
    )
    {
        if (!IsOpened() || RootElement == null)
        {
            return;
        }

        _localElapsedMs += deltaTime;

        BeginFrameMetrics(deltaTime);

        RenderObject.AdvanceFrame();

        _tickerScheduler.Update(TimeSpan.FromSeconds(deltaTime));

        if (ModSystem.SkiaRenderer?.Canvas != null)
        {
            var uiScale = GetUiScale();
            var screenW = capi.Render.FrameWidth / uiScale;
            var screenH = capi.Render.FrameHeight / uiScale;

            if (!_positionInitialized)
            {
                WindowPos = new Vector2
                (
                    (screenW - WindowSize.X) / 2f,
                    (screenH - WindowSize.Y) / 2f
                );
                _positionInitialized = true;
                SyncHitBounds();
            }

            ClampWindowToScreen(screenW, screenH);

            if (ModSystem.DebugSettings.ShowViolations)
            {
                _violationReporter ??= (ro, msg) =>
                    capi.Logger.Warning($"[GUI Layout] {ro.GetType().Name}: {msg}");
            }

            _framePipeline.Run
            (
                new FrameArgs
                {
                    RootElement = RootElement,
                    Canvas = ModSystem.SkiaRenderer.Canvas!,
                    WindowPos = WindowPos,
                    WindowSize = WindowSize,
                    LayoutSize = _layoutSize,
                    UiScale = uiScale,
                    LocalElapsedMs = _localElapsedMs,
                    WindowConfig = _windowConfig,
                    PerformanceMetrics = PerformanceMetrics,
                    Profiler = Profiler,
                    BuildOwner = BuildOwner,
                    DebugSettings = ModSystem.DebugSettings,
                    ViolationReporter =
                        ModSystem.DebugSettings.ShowViolations ? _violationReporter : null,
                    IsActiveWindow = GuiModSystem.Instance?.ActiveWindow == this,
                    ShouldCommitLayout = _interactionSm.ShouldCommitLayout(_localElapsedMs),
                    OnCommitLayout = CommitResizeLayout,
                    OnShrinkWrapCompleted = newSize =>
                    {
                        WindowSize = newSize;
                        _layoutSize = newSize;
                        OnShrinkWrapLayoutCompleted();
                        return WindowPos;
                    },
                    IsPositionInitialized = _positionInitialized,
                    ScreenW = screenW,
                    ScreenH = screenH,
                    PaintResizePendingOverlay = PaintResizePendingOverlay,
                    IsResizePending = _interactionSm.IsResizing && WindowSize != _layoutSize,
                    SyncHitBounds = SyncHitBounds,
                }
            );
        }
    }

    /// <summary>
    ///     Draws a visual overlay indicating the pending resize region — the gap between
    ///     the committed layout size and the current drag target size. Only visible while
    ///     the window is being enlarged and layout has not yet caught up.
    /// </summary>
    private void PaintResizePendingOverlay
    (
        SKCanvas canvas
    )
    {
        var lw = _layoutSize.X;
        var lh = _layoutSize.Y;
        var tw = WindowSize.X;
        var th = WindowSize.Y;

        if (lw <= 0 || lh <= 0)
        {
            return;
        }

        var expandRight = tw > lw + 0.5f;
        var expandBottom = th > lh + 0.5f;
        if (!expandRight && !expandBottom)
        {
            return;
        }

        using var fill = new SKPaint();
        fill.Color = new SKColor(100, 160, 255, 45);
        fill.Style = SKPaintStyle.Fill;
        fill.IsAntialias = false;

        var hatchMatrix = SKMatrix.CreateScale
            (
                9,
                9
            )
            .PostConcat(SKMatrix.CreateRotationDegrees(45));
        using var hatchEffect = SKPathEffect.Create2DLine
        (
            1.2f,
            hatchMatrix
        );
        using var hatch = new SKPaint();
        hatch.Color = new SKColor(100, 160, 255, 80);
        hatch.Style = SKPaintStyle.Fill;
        hatch.PathEffect = hatchEffect;
        hatch.IsAntialias = false;

        if (expandRight)
        {
            var r = SKRect.Create
            (
                lw,
                0,
                tw - lw,
                th
            );
            canvas.DrawRect
            (
                r,
                fill
            );
            canvas.DrawRect
            (
                r,
                hatch
            );
        }

        if (expandBottom)
        {
            var stripW = expandRight
                ? lw
                : tw;
            var r = SKRect.Create
            (
                0,
                lh,
                stripW,
                th - lh
            );
            canvas.DrawRect
            (
                r,
                fill
            );
            canvas.DrawRect
            (
                r,
                hatch
            );
        }

        using var border = new SKPaint();
        border.Color = new SKColor(100, 160, 255, 200);
        border.Style = SKPaintStyle.Stroke;
        border.StrokeWidth = 1.5f;
        border.PathEffect = SKPathEffect.CreateDash([6f, 4f], 0);
        border.IsAntialias = true;
        canvas.DrawRect
        (
            SKRect.Create
            (
                0,
                0,
                tw,
                th
            ),
            border
        );
    }

    private void BeginFrameMetrics
    (
        float dt
    )
    {
        PerformanceMetrics.OnFrameStart(_localElapsedMs);
        PerformanceMetrics.RecordFrameTime(dt * 1000.0f);
        PerformanceMetrics.WidgetCount = _elementCount;
    }

    /// <summary>
    ///     Tears down the current widget tree and rebuilds it from scratch without
    ///     closing the dialog. Useful during development to pick up code changes
    ///     loaded via hot-reload or recompiled DLLs.
    /// </summary>
    public void ForceRebuild()
    {
        if (RootElement == null)
        {
            return;
        }

        RootElement.Unmount();

        _framePipeline.ResetCache();

        _elementCount = 0;

        var rootWidget = BuildRootTree();
        RootElement = rootWidget.CreateElement();
        RootElement.AssignOwner(BuildOwner);
        RootElement.Mount(null);
    }

    /// <summary>
    ///     Wraps the user's <see cref="Build" /> output in an <see cref="Overlay" /> and a
    ///     <see cref="ListenableBuilder" /> subscribed to <see cref="ThemeData.DefaultNotifier" />.
    ///     When the global theme changes, the builder rebuilds with a fresh <see cref="Theme" />
    ///     instance, which propagates to <see cref="Theme.Of" /> consumers via
    ///     <see cref="InheritedWidget" /> notification without tearing down the tree.
    /// </summary>
    private Widget BuildRootTree()
    {
        var content = Build();
        return new Overlay
        (
            [
                new OverlayEntry
                (
                    new ListenableBuilder
                    (
                        ThemeData.DefaultNotifier,
                        _ => new Theme(ThemeData.DefaultNotifier.Value, content)
                    )
                )
            ]
        );
    }

    public override void OnGuiClosed()
    {
        _cursorController.Reset();
        MouseOverCursor = null;
        ModSystem.UnregisterWindow(this);
        RootElement?.Unmount();
        _tickerScheduler.Dispose();
        _framePipeline.Dispose();
        base.OnGuiClosed();
    }
}

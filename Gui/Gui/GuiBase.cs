using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Gui.Clipboard;
using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Rendering;
using Gui.Sound;
using Gui.Widgets.Animations;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using Gui.Widgets.Overlay;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
    /// Resize throttle: defer relayout until resize settles
    private const double ResizeSettleMs = 150.0;

    /// Dummy composer key so VS engine's GuiManager dispatches OnMouseWheel
    /// to us in the priority "point-inside-bounds" loop rather than the
    /// catch-all loop (where the hotbar intercepts it first).
    private const string HitBoundsKey = "__guibase_hitbounds";

    private readonly TickerScheduler _tickerScheduler = new();

    /// Cursor state
    private CursorShape _currentCursorShape = CursorShape.Arrow;

    private unsafe Cursor* _customCursor = null;
    private Vector2 _dragStartPos;
    private Vector2 _dragStartWindowPos;
    private int _elementCount;

    /// Drag state
    private bool _isDragging;

    /// Resize state
    private bool _isResizing;

    /// Last known mouse position in window-local space (for OnMouseWheel)
    private Vector2? _lastMouseLocal;

    private double _lastResizeTimeMs;

    /// <summary>The committed layout size — only updated after resize settles.</summary>
    private Vector2 _layoutSize;

    private double _localElapsedMs;
    private PaintingContext? _paintingContext;


    private Action<RenderObject>? _paintReporter;

    /// <summary>Whether position has been initialized (for deferred centering).</summary>
    private bool _positionInitialized;

    private ResizeEdge _resizeEdge;
    private bool _resizeLayoutPending;
    private Vector2 _resizeStartPos;
    private Vector2 _resizeStartWindowPos;
    private Vector2 _resizeStartWindowSize;

    /// <summary>Cached SKPicture of the last paint pass, replayed on clean frames.</summary>
    private SKPicture? _rootPaintCache;

    private Action<RenderObject, string>? _violationReporter;

    private WindowConfig _windowConfig = new();

    protected GuiBase(
        ICoreClientAPI capi
    ) : base(capi)
    {
        ModSystem = capi.ModLoader.GetModSystem<GuiModSystem>();
        BuildOwner.SetTickerProvider(this);
        BuildOwner.SetClipboard(new GameClipboard(capi));
        BuildOwner.SetSoundPlayer(new SoundPlayer(capi));
        BuildOwner.FocusManager = FocusManager;
        BuildOwner.OnError = (
            element,
            ex
        ) =>
        {
            capi.Logger.Error(
                $"[GUI] Failed to rebuild element for widget {element.Widget.DebugPropertyName}: {ex}"
            );
        };
        BuildOwner.OnElementMounted = () => _elementCount++;
        BuildOwner.OnElementUnmounted = () => _elementCount--;
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
    internal bool IsDraggingOrResizing => _isDragging || _isResizing;

    public Ticker CreateTicker(
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
    ///     window based on the new <see cref="GuiDialog.WindowSize" />.
    /// </summary>
    protected virtual void OnShrinkWrapLayoutCompleted()
    {
    }

    public override bool TryOpen(
        bool withFocus
    )
    {
        if (IsOpened())
        {
            return false;
        }

        _windowConfig = CreateWindowConfig();
        _positionInitialized = false;

        if (_windowConfig.Size.HasValue)
        {
            WindowSize = ClampSize(_windowConfig.Size.Value);
        }
        else
        {
            WindowSize = new Vector2(
                400,
                300
            ); // initial estimate for shrink-wrap
        }

        _layoutSize = WindowSize;
        _resizeLayoutPending = false;

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
        var extraScale = GetUiScale() / RuntimeEnv.GUIScale;

        // Reuse existing composer if present, else create a minimal one.
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
            var bounds = ElementBounds.Fixed(
                WindowPos.X * extraScale,
                WindowPos.Y * extraScale,
                WindowSize.X * extraScale,
                WindowSize.Y * extraScale
            );
            bounds.CalcWorldBounds();
            Composers[HitBoundsKey] = capi.Gui.CreateCompo(
                HitBoundsKey,
                bounds
            );
        }
    }

    protected virtual float GetUiScale() => RuntimeEnv.GUIScale;

    /// <summary>Convert raw pixel mouse coords to logical screen coords.</summary>
    private Vector2 ToLogicalScreen(
        int rawX,
        int rawY
    )
    {
        var uiScale = GetUiScale();
        return new Vector2(
            rawX / uiScale,
            rawY / uiScale
        );
    }

    /// <summary>Convert logical screen coords to window-local coords.</summary>
    private Vector2 ToWindowLocal(
        Vector2 screen
    ) =>
        screen - WindowPos;

    /// <summary>Check if a window-local point is inside the window bounds.</summary>
    private bool IsInsideWindow(
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
    private void ClampWindowToScreen(
        float screenW,
        float screenH
    )
    {
        var maxX = Math.Max(
            0f,
            screenW - WindowSize.X
        );
        var maxY = Math.Max(
            0f,
            screenH - WindowSize.Y
        );
        var clamped = new Vector2(
            Math.Clamp(
                WindowPos.X,
                0f,
                maxX
            ),
            Math.Clamp(
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

    private Vector2 ClampSize(
        Vector2 size
    )
    {
        return new Vector2(
            Math.Clamp(
                size.X,
                _windowConfig.MinSize.X,
                _windowConfig.MaxSize.X
            ),
            Math.Clamp(
                size.Y,
                _windowConfig.MinSize.Y,
                _windowConfig.MaxSize.Y
            )
        );
    }


    private ResizeEdge DetectResizeEdge(
        Vector2 local
    )
    {
        if (!_windowConfig.Resizable)
        {
            return ResizeEdge.None;
        }

        if (!IsInsideWindow(local))
        {
            return ResizeEdge.None;
        }

        var grip = _windowConfig.ResizeHandleSize;
        var edge = ResizeEdge.None;

        if (local.X <= grip)
        {
            edge |= ResizeEdge.Left;
        }
        else if (local.X >= WindowSize.X - grip)
        {
            edge |= ResizeEdge.Right;
        }

        if (local.Y <= grip)
        {
            edge |= ResizeEdge.Top;
        }
        else if (local.Y >= WindowSize.Y - grip)
        {
            edge |= ResizeEdge.Bottom;
        }

        return edge;
    }

    private bool IsInDragZone(
        Vector2 local
    )
    {
        return _windowConfig.Draggable &&
               local.Y >= 0 && local.Y <= _windowConfig.DragHandleHeight &&
               local.X >= 0 && local.X <= WindowSize.X;
    }


    private bool HandleMouseMove(
        Vector2 screen
    )
    {
        if (_isDragging)
        {
            var delta = screen - _dragStartPos;
            WindowPos = _dragStartWindowPos + delta;
            SyncHitBounds();
            return true;
        }

        if (_isResizing)
        {
            var delta = screen - _resizeStartPos;
            var newPos = _resizeStartWindowPos;
            var newSize = _resizeStartWindowSize;

            if ((_resizeEdge & ResizeEdge.Left) != 0)
            {
                newPos.X = _resizeStartWindowPos.X + delta.X;
                newSize.X = _resizeStartWindowSize.X - delta.X;
            }

            if ((_resizeEdge & ResizeEdge.Right) != 0)
            {
                newSize.X = _resizeStartWindowSize.X + delta.X;
            }

            if ((_resizeEdge & ResizeEdge.Top) != 0)
            {
                newPos.Y = _resizeStartWindowPos.Y + delta.Y;
                newSize.Y = _resizeStartWindowSize.Y - delta.Y;
            }

            if ((_resizeEdge & ResizeEdge.Bottom) != 0)
            {
                newSize.Y = _resizeStartWindowSize.Y + delta.Y;
            }

            var clamped = ClampSize(newSize);
            // If size was clamped, adjust position so the opposite edge stays fixed.
            if ((_resizeEdge & ResizeEdge.Left) != 0)
            {
                newPos.X += newSize.X - clamped.X;
            }

            if ((_resizeEdge & ResizeEdge.Top) != 0)
            {
                newPos.Y += newSize.Y - clamped.Y;
            }

            WindowSize = clamped;
            WindowPos = newPos;
            SyncHitBounds();

            // Defer relayout — will fire after resize settles
            _lastResizeTimeMs = _localElapsedMs;
            _resizeLayoutPending = true;
            return true;
        }

        return false;
    }

    private bool HandleMouseUp()
    {
        var wasInteracting = _isDragging || _isResizing;
        if (_isDragging)
        {
            capi.Gui.SetDialogPosition(DialogCode, new Vec2i((int)WindowPos.X, (int)WindowPos.Y));
        }

        if (_isResizing)
        {
            CommitResizeLayout();
        }

        _isDragging = false;
        _isResizing = false;
        return wasInteracting;
    }

    private void CommitResizeLayout()
    {
        _layoutSize = WindowSize;
        _resizeLayoutPending = false;
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


    private static CursorShape EdgeToCursorShape(
        ResizeEdge edge
    )
    {
        var left = (edge & ResizeEdge.Left) != 0;
        var right = (edge & ResizeEdge.Right) != 0;
        var top = (edge & ResizeEdge.Top) != 0;
        var bottom = (edge & ResizeEdge.Bottom) != 0;

        if ((left && top) || (right && bottom))
        {
            return CursorShape.ResizeNWSE;
        }

        if ((right && top) || (left && bottom))
        {
            return CursorShape.ResizeNESW;
        }

        if (left || right)
        {
            return CursorShape.ResizeEW;
        }

        if (top || bottom)
        {
            return CursorShape.ResizeNS;
        }

        return CursorShape.Arrow;
    }

    private unsafe void SetCursorShape(
        CursorShape shape
    )
    {
        if (shape == _currentCursorShape)
        {
            return;
        }

        _currentCursorShape = shape;

        var window = GLFW.GetCurrentContext();
        if (window == null)
        {
            return;
        }

        if (_customCursor != null)
        {
            GLFW.DestroyCursor(_customCursor);
            _customCursor = null;
        }

        if (shape == CursorShape.Arrow)
        {
            GLFW.SetCursor(
                window,
                null
            );
        }
        else
        {
            _customCursor = GLFW.CreateStandardCursor(shape);
            GLFW.SetCursor(
                window,
                _customCursor
            );
        }
    }

    private void UpdateCursorForPosition(
        Vector2 local
    )
    {
        if (_isDragging || _isResizing)
        {
            return;
        }

        var edge = DetectResizeEdge(local);
        if (edge != ResizeEdge.None)
        {
            SetCursorShape(EdgeToCursorShape(edge));
        }
        else
        {
            SetCursorShape(CursorShape.Arrow);
        }
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
        if (_isDragging || _isResizing)
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

    public override void OnMouseMove(
        MouseEvent args
    )
    {
        if (args.Handled && !_isDragging && !_isResizing)
        {
            // Pass off-screen coords so only PointerLeave fires — real coords would
            // trigger PointerEnter on occluded widgets and corrupt hover state.
            if (RootElement?.RenderObject != null)
            {
                EventDispatcher.DispatchPointerMove(RootElement, new PointerEvent(-1, -1));
            }

            return;
        }

        base.OnMouseMove(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        var screen = ToLogicalScreen(
            args.X,
            args.Y
        );

        if (HandleMouseMove(screen))
        {
            args.Handled = true;
            return;
        }

        var local = ToWindowLocal(screen);
        _lastMouseLocal = local;
        MouseOverCursor = null;

        var e = new PointerEvent(
            local.X,
            local.Y
        );
        EventDispatcher.DispatchPointerMove(
            RootElement,
            e
        );

        if (!IsInsideWindow(local) && !_isDragging && !_isResizing)
        {
            // base may have set Handled via the hit-bounds composer; reset so other
            // dialogs still receive the move (cursor outside our window).
            args.Handled = false;
            UpdateCursorForPosition(local);
            return;
        }

        var blocked = IsBlockedByFrontDialog(args.X, args.Y);
        if (blocked)
        {
            args.Handled = false; // yield to front dialog
        }
        else if (e.Handled)
        {
            args.Handled = true; // our widget handled the move
        }

        var widgetCursor = EventDispatcher.ResolveHoveredCursor();
        if (widgetCursor != null && !blocked)
        {
            MouseOverCursor = widgetCursor.Name;
        }

        UpdateCursorForPosition(local);
    }

    public override void OnMouseDown(
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

        var screen = ToLogicalScreen(
            args.X,
            args.Y
        );
        var local = ToWindowLocal(screen);

        var edge = DetectResizeEdge(local);
        if (edge != ResizeEdge.None)
        {
            _isResizing = true;
            _resizeEdge = edge;
            _resizeStartPos = screen;
            _resizeStartWindowPos = WindowPos;
            _resizeStartWindowSize = WindowSize;
            args.Handled = true;
            return;
        }

        if (!IsInsideWindow(local))
        {
            _windowConfig.OnPointerDownOutside?.Invoke();
            // base.OnMouseDown may have set Handled via the hit-bounds composer; reset
            // so GuiManager keeps iterating to other dialogs.
            args.Handled = false;
            return;
        }

        if (IsBlockedByFrontDialog(args.X, args.Y))
        {
            // Yielding to a visually-higher dialog — must NOT consume the event,
            // otherwise GuiManager attributes the click to us and calls RequestFocus.
            args.Handled = false;
            return;
        }

        ModSystem.ActiveWindow = this;

        var e = new PointerEvent(
            local.X,
            local.Y,
            (PointerButton)args.Button
        );

        var capturedByWidget = EventDispatcher.DispatchPointerDown(
            RootElement,
            e
        );
        if (capturedByWidget)
        {
            args.Handled = true;
            return;
        }

        if (IsInDragZone(local))
        {
            _isDragging = true;
            _dragStartPos = screen;
            _dragStartWindowPos = WindowPos;
            args.Handled = true;
        }

        if (_windowConfig.OpaqueHitTest)
        {
            args.Handled = true;
        }
    }

    public override void OnMouseUp(
        MouseEvent args
    )
    {
        base.OnMouseUp(args);
        if (RootElement == null || RootElement.RenderObject == null)
        {
            return;
        }

        if (HandleMouseUp())
        {
            args.Handled = true;
            return;
        }

        var screen = ToLogicalScreen(
            args.X,
            args.Y
        );
        var local = ToWindowLocal(screen);

        var e = new PointerEvent(
            local.X,
            local.Y,
            (PointerButton)args.Button
        );
        EventDispatcher.DispatchPointerUp(
            RootElement,
            e
        );
        if (e.Handled && IsInsideWindow(local) && !IsBlockedByFrontDialog(args.X, args.Y))
        {
            args.Handled = true;
        }
        else
        {
            args.Handled = false; // base.OnMouseUp may have set it; reset since we are yielding
        }
    }

    public override void OnMouseWheel(
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

        // Use per-instance cached position (set during OnMouseMove).
        // Fall back to capi.Input.MouseX/Y for HUD-type dialogs where
        // OnMouseMove may not fire.
        Vector2 local;
        if (_lastMouseLocal.HasValue)
        {
            local = _lastMouseLocal.Value;
        }
        else
        {
            var screen = ToLogicalScreen(
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

        var e = new PointerEvent(
            local.X,
            local.Y,
            delta: args.deltaPrecise
        );

        EventDispatcher.DispatchMouseWheel(
            RootElement,
            e
        );

        if (e.Handled)
        {
            args.SetHandled();
        }
    }

    public override void OnKeyDown(
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

        if (ModSystem.DebugSettings.IsAnyEnabled)
        {
            capi.Logger.Debug($"[GUI KeyDown] KeyCode: {args.KeyCode}");
        }

        var e = new KeyboardEvent(
            KeyEventType.KeyDown,
            args.KeyCode,
            shift: args.ShiftPressed,
            ctrl: args.CtrlPressed,
            alt: args.AltPressed
        );
        EventDispatcher.DispatchKeyDown(
            FocusManager,
            e
        );
        args.Handled |= e.Handled;
    }

    public override void OnKeyUp(
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

        var e = new KeyboardEvent(
            KeyEventType.KeyUp,
            args.KeyCode,
            shift: args.ShiftPressed,
            ctrl: args.CtrlPressed,
            alt: args.AltPressed
        );
        EventDispatcher.DispatchKeyUp(
            FocusManager,
            e
        );
        args.Handled |= e.Handled;
    }

    public override void OnKeyPress(
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

        var e = new KeyboardEvent(
            KeyEventType.KeyChar,
            keyChar: args.KeyChar,
            shift: args.ShiftPressed,
            ctrl: args.CtrlPressed,
            alt: args.AltPressed
        );
        EventDispatcher.DispatchKeyChar(
            FocusManager,
            e
        );
        args.Handled |= e.Handled;
    }


    public override void OnRenderGUI(
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

            // Deferred centering: center on first render when position was not specified
            if (!_positionInitialized)
            {
                WindowPos = new Vector2(
                    (screenW - WindowSize.X) / 2f,
                    (screenH - WindowSize.Y) / 2f
                );
                _positionInitialized = true;
                SyncHitBounds();
            }

            ClampWindowToScreen(
                screenW,
                screenH
            );

            var sw = new Stopwatch();

            if (_paintingContext == null)
            {
                _paintingContext = new PaintingContext(
                    ModSystem.SkiaRenderer.Canvas!,
                    _localElapsedMs
                );
            }
            else
            {
                _paintingContext.Reset(
                    ModSystem.SkiaRenderer.Canvas!,
                    _localElapsedMs
                );
            }

            var context = _paintingContext;

            sw.Restart();
            BuildOwner.BuildDirtyElements();
            sw.Stop();
            PerformanceMetrics.RecordBuildTime((float)sw.Elapsed.TotalMilliseconds);

            if (ModSystem.DebugSettings.ShowViolations)
            {
                _violationReporter ??= (
                        ro,
                        msg
                    ) =>
                    capi.Logger.Warning($"[GUI Layout] {ro.GetType().Name}: {msg}");
                RenderObject.OnLayoutViolation = _violationReporter;
            }
            else
            {
                RenderObject.OnLayoutViolation = null;
            }

            var rootRo = RootElement.RenderObject;
            if (rootRo != null)
            {
                try
                {
                    // Check if a deferred resize has settled
                    if (_resizeLayoutPending &&
                        (_localElapsedMs - _lastResizeTimeMs) * 1000.0 >= ResizeSettleMs)
                    {
                        CommitResizeLayout();
                    }

                    // Always keep ScreenOffset in sync with WindowPos,
                    // even when no layout is needed (e.g. window was dragged).
                    rootRo.ScreenOffset = WindowPos;

                    // Layout with committed _layoutSize (not live WindowSize during resize)
                    if (rootRo.NeedsLayout || rootRo.ChildNeedsLayout)
                    {
                        PerformanceMetrics.IncrementLayout();

                        LayoutConstraints constraints;
                        if (_windowConfig.IsShrinkWrap)
                        {
                            var maxW = Math.Min(
                                screenW,
                                _windowConfig.MaxSize.X
                            );
                            var maxH = Math.Min(
                                screenH,
                                _windowConfig.MaxSize.Y
                            );
                            constraints = LayoutConstraints.Loose(
                                Math.Max(
                                    maxW,
                                    _windowConfig.MinSize.X
                                ),
                                Math.Max(
                                    maxH,
                                    _windowConfig.MinSize.Y
                                )
                            );
                        }
                        else
                        {
                            constraints = LayoutConstraints.Tight(
                                _layoutSize.X,
                                _layoutSize.Y
                            );
                        }

                        sw.Restart();
                        rootRo.Layout(constraints);
                        sw.Stop();
                        PerformanceMetrics.RecordLayoutTime((float)sw.Elapsed.TotalMilliseconds);

                        // For shrink-wrap, update window size to match content
                        if (_windowConfig.IsShrinkWrap)
                        {
                            WindowSize = ClampSize(rootRo.Size);
                            _layoutSize = WindowSize;
                            OnShrinkWrapLayoutCompleted();
                            rootRo.ScreenOffset = WindowPos;
                        }

                        SyncHitBounds();
                    }

                    // Paint at window position with clipping
                    var canvas = context.Canvas;
                    if (canvas == null)
                    {
                        return;
                    }

                    using (canvas.SaveScope())
                    {
                        var extraScale = uiScale / RuntimeEnv.GUIScale;
                        if (Math.Abs(extraScale - 1.0f) > 1e-5f)
                        {
                            canvas.Scale(extraScale, extraScale);
                        }

                        canvas.Translate(
                            WindowPos.X,
                            WindowPos.Y
                        );
                        var paintBounds = _windowConfig.Clip
                            ? new SKRect(
                                0,
                                0,
                                WindowSize.X,
                                WindowSize.Y
                            )
                            : new SKRect(
                                -WindowPos.X,
                                -WindowPos.Y,
                                capi.Render.FrameWidth / uiScale - WindowPos.X,
                                capi.Render.FrameHeight / uiScale - WindowPos.Y
                            );

                        if (_windowConfig.Clip)
                        {
                            canvas.ClipRect(paintBounds);
                        }

                        var needsRepaint = rootRo.NeedsPaint ||
                                           rootRo.ChildNeedsPaint ||
                                           _rootPaintCache == null;

                        if (needsRepaint)
                        {
                            _rootPaintCache?.Dispose();
                            using var recorder = new SKPictureRecorder();
                            var bounds = paintBounds;
                            var recCanvas = recorder.BeginRecording(bounds);
                            context.PushCanvas(recCanvas);

                            sw.Restart();
                            rootRo.Paint(context);
                            sw.Stop();
                            PerformanceMetrics.RecordPaintTime((float)sw.Elapsed.TotalMilliseconds);

                            context.PopCanvas();
                            _rootPaintCache = recorder.EndRecording();
                            canvas.DrawPicture(_rootPaintCache);
                        }
                        else
                        {
                            canvas.DrawPicture(_rootPaintCache);
                            PerformanceMetrics.RecordPaintTime(0);
                        }

                        if (ModSystem.DebugSettings.IsAnyEnabled)
                        {
                            DebugPainter.PaintDebugInfo(
                                rootRo,
                                context,
                                ModSystem.DebugSettings,
                                _localElapsedMs,
                                GuiModSystem.Instance?.ActiveWindow == this
                            );
                        }

                        if (_isResizing && (_resizeLayoutPending || WindowSize != _layoutSize))
                        {
                            PaintResizePendingOverlay(canvas);
                        }
                    }
                }
                catch (Exception ex)
                {
                    capi.Logger.Error($"[GUI] Error during paint: {ex}");
                }
            }
        }

        RenderObject.OnAnyPaint = null;
        RenderObject.OnLayoutViolation = null;
    }

    /// <summary>
    ///     Draws a visual overlay indicating the pending resize region — the gap between
    ///     the committed layout size and the current drag target size. Only visible while
    ///     the window is being enlarged and layout has not yet caught up.
    /// </summary>
    private void PaintResizePendingOverlay(
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

        var hatchMatrix = SKMatrix.CreateScale(
                9,
                9
            )
            .PostConcat(SKMatrix.CreateRotationDegrees(45));
        using var hatchEffect = SKPathEffect.Create2DLine(
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
            var r = SKRect.Create(
                lw,
                0,
                tw - lw,
                th
            );
            canvas.DrawRect(
                r,
                fill
            );
            canvas.DrawRect(
                r,
                hatch
            );
        }

        if (expandBottom)
        {
            var stripW = expandRight
                ? lw
                : tw;
            var r = SKRect.Create(
                0,
                lh,
                stripW,
                th - lh
            );
            canvas.DrawRect(
                r,
                fill
            );
            canvas.DrawRect(
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
        canvas.DrawRect(
            SKRect.Create(
                0,
                0,
                tw,
                th
            ),
            border
        );
    }

    private void BeginFrameMetrics(
        float dt
    )
    {
        PerformanceMetrics.OnFrameStart(_localElapsedMs);
        PerformanceMetrics.RecordFrameTime(dt * 1000.0f);
        PerformanceMetrics.WidgetCount = _elementCount;

        _paintReporter ??= ro => { PerformanceMetrics.IncrementPaint(); };
        RenderObject.OnAnyPaint = _paintReporter;
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

        // Dispose painting context so all cached Skia resources (blur filters,
        // shared paints, etc.) are freed. A fresh one is created on the next frame.
        _paintingContext?.Dispose();
        _paintingContext = null;

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
        return new Overlay([
            new OverlayEntry(
                new ListenableBuilder(
                    ThemeData.DefaultNotifier,
                    _ => new Theme(ThemeData.DefaultNotifier.Value, content)
                )
            )
        ]);
    }

    public override void OnGuiClosed()
    {
        SetCursorShape(CursorShape.Arrow);
        MouseOverCursor = null;
        ModSystem.UnregisterWindow(this);
        RootElement?.Unmount();
        _tickerScheduler.Dispose();
        _rootPaintCache?.Dispose();
        _rootPaintCache = null;
        base.OnGuiClosed();
    }
}

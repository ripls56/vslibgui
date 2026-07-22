using System;
using System.Diagnostics;
using Gui.Core.Framework;
using Gui.Debugging;
using Gui.Rendering;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;
using Vintagestory.API.Config;

namespace Gui.Core.Windowing;

/// <summary>
///     All data <see cref="FramePipeline.Run" /> needs for a single render frame.
///     Assembled by <c>GuiBase.OnRenderGUI</c> each frame.
/// </summary>
internal sealed class FrameArgs
{
    /// <summary>The root element of the inflated widget tree.</summary>
    internal required Element RootElement { get; init; }

    /// <summary>The Skia canvas acquired from the renderer this frame.</summary>
    internal required SKCanvas Canvas { get; init; }

    /// <summary>Window position in logical pixels.</summary>
    internal required Vector2 WindowPos { get; init; }

    /// <summary>Current window size in logical pixels.</summary>
    internal required Vector2 WindowSize { get; init; }

    /// <summary>Committed layout size (lags behind WindowSize during live resize).</summary>
    internal required Vector2 LayoutSize { get; init; }

    /// <summary>UI scale factor (from <c>GetUiScale()</c>).</summary>
    internal required float UiScale { get; init; }

    /// <summary>Accumulated elapsed time in milliseconds for <see cref="PaintingContext" />.</summary>
    internal required double LocalElapsedMs { get; init; }

    /// <summary>Window configuration snapshot for this frame.</summary>
    internal required WindowConfig WindowConfig { get; init; }

    /// <summary>Performance metrics collector for this dialog.</summary>
    internal required PerformanceMetrics PerformanceMetrics { get; init; }

    /// <summary>Frametime profiler collector for this window.</summary>
    internal required GuiProfiler Profiler { get; init; }

    /// <summary>Build owner that drives dirty-element rebuilds.</summary>
    internal required BuildOwner BuildOwner { get; init; }

    /// <summary>Active debug settings from the mod system.</summary>
    internal required DebugSettings DebugSettings { get; init; }

    /// <summary>
    ///     Optional reporter invoked when a layout constraint violation is detected.
    ///     Set only when <see cref="DebugSettings.ShowViolations" /> is enabled.
    /// </summary>
    internal required Action<RenderObject, string>? ViolationReporter { get; init; }

    /// <summary>Whether this window is the active (topmost focused) window.</summary>
    internal required bool IsActiveWindow { get; init; }

    /// <summary>
    ///     <c>true</c> when the settle-throttle fired and layout should be committed
    ///     this frame.
    /// </summary>
    internal required bool ShouldCommitLayout { get; init; }

    /// <summary>Invoked when the pipeline commits a deferred resize layout.</summary>
    internal required Action OnCommitLayout { get; init; }

    /// <summary>
    ///     Invoked after a shrink-wrap layout pass with the new content-driven window size.
    ///     Returns the window position that may have been recomputed in response to the new
    ///     size (e.g. a re-anchored HUD), so the pipeline can position and paint this frame
    ///     at the updated location instead of the pre-layout snapshot.
    /// </summary>
    internal required Func<Vector2, Vector2> OnShrinkWrapCompleted { get; init; }

    /// <summary>Whether position has already been initialised this session.</summary>
    internal required bool IsPositionInitialized { get; init; }

    /// <summary>Logical screen width in logical pixels.</summary>
    internal required float ScreenW { get; init; }

    /// <summary>Logical screen height in logical pixels.</summary>
    internal required float ScreenH { get; init; }

    /// <summary>
    ///     Draws the hatched overlay that indicates the region not yet covered by
    ///     the committed layout. Only invoked when the flag is true.
    /// </summary>
    internal required Action<SKCanvas> PaintResizePendingOverlay { get; init; }

    /// <summary>
    ///     <c>true</c> when the window is being resized and layout has not yet caught
    ///     up, so the pending-resize overlay should be drawn.
    /// </summary>
    internal required bool IsResizePending { get; init; }

    /// <summary>Syncs hit-bounds after layout changes window dimensions or position.</summary>
    internal required Action SyncHitBounds { get; init; }
}

/// <summary>
///     Executes the per-frame build → layout → paint pipeline for a
///     <see cref="Gui.GuiBase" /> window. Owns the <see cref="PaintingContext" />
///     and the <see cref="SKPicture" /> paint cache so that <c>GuiBase</c> holds
///     no Skia lifecycle fields.
/// </summary>
internal sealed class FramePipeline : IDisposable
{
    private readonly Action<RenderObject> _paintReporter;
    private PaintingContext? _paintingContext;
    private SKPicture? _rootPaintCache;

    /// <summary>
    ///     Creates a new pipeline. <paramref name="onPaint" /> is invoked once per
    ///     render-object paint call so the caller can track repaint counts.
    /// </summary>
    internal FramePipeline(
        Action<RenderObject> onPaint
    )
    {
        _paintReporter = onPaint;
    }

    /// <summary>Disposes cached Skia resources.</summary>
    public void Dispose() => ResetCache();

    /// <summary>
    ///     Disposes the paint cache and painting context, forcing a full repaint on
    ///     the next frame. Call from <c>ForceRebuild</c>.
    /// </summary>
    internal void ResetCache()
    {
        _paintingContext?.Dispose();
        _paintingContext = null;
        _rootPaintCache?.Dispose();
        _rootPaintCache = null;
    }

    /// <summary>
    ///     Executes one full build → layout → paint cycle using the data in
    ///     <paramref name="args" />. Must be called from the render thread.
    /// </summary>
    internal void Run(
        FrameArgs args
    )
    {
        RenderObject.OnAnyPaint = _paintReporter;
        GuiProfiler.Current = GuiProfiler.Enabled
            ? args.Profiler
            : null;
        ConfigureViolationReporter(args);

        SetupPaintContext(args.Canvas, args.LocalElapsedMs);
        var context = _paintingContext!;

        var sw = new Stopwatch();

        sw.Restart();
        args.BuildOwner.BuildDirtyElements();
        sw.Stop();
        args.PerformanceMetrics.RecordBuildTime((float)sw.Elapsed.TotalMilliseconds);

        var rootRo = args.RootElement.RenderObject;
        if (rootRo == null)
        {
            RenderObject.OnAnyPaint = null;
            RenderObject.OnLayoutViolation = null;
            GuiProfiler.Current = null;
            return;
        }

        try
        {
            if (args.ShouldCommitLayout)
            {
                args.OnCommitLayout();
            }

            var windowPos = args.WindowPos;
            var windowSize = args.WindowSize;
            rootRo.ScreenOffset = windowPos;

            if (rootRo.NeedsLayout || rootRo.ChildNeedsLayout)
            {
                (windowPos, windowSize) = PerformLayout(args, rootRo, sw);
            }

            PaintFrame(args, rootRo, context, sw, windowPos, windowSize);
        }
        catch (Exception ex)
        {
            args.PerformanceMetrics.RecordBuildTime(0);
            _ = ex;
        }
        finally
        {
            RenderObject.OnAnyPaint = null;
            RenderObject.OnLayoutViolation = null;
            GuiProfiler.Current = null;
        }
    }

    private void SetupPaintContext(
        SKCanvas canvas,
        double localElapsedMs
    )
    {
        if (_paintingContext == null)
        {
            _paintingContext = new PaintingContext(canvas, localElapsedMs);
        }
        else
        {
            _paintingContext.Reset(canvas, localElapsedMs);
        }
    }

    private static void ConfigureViolationReporter(
        FrameArgs args
    )
    {
        RenderObject.OnLayoutViolation = args.DebugSettings.ShowViolations
            ? args.ViolationReporter
            : null;
    }

    private (Vector2 windowPos, Vector2 windowSize) PerformLayout(
        FrameArgs args,
        RenderObject rootRo,
        Stopwatch sw
    )
    {
        var windowPos = args.WindowPos;
        var windowSize = args.WindowSize;
        args.PerformanceMetrics.IncrementLayout();

        LayoutConstraints constraints;
        if (args.WindowConfig.IsShrinkWrap)
        {
            var maxW = Math.Min(args.ScreenW, args.WindowConfig.MaxSize.X);
            var maxH = Math.Min(args.ScreenH, args.WindowConfig.MaxSize.Y);
            constraints = LayoutConstraints.Loose(
                Math.Max(maxW, args.WindowConfig.MinSize.X),
                Math.Max(maxH, args.WindowConfig.MinSize.Y)
            );
        }
        else
        {
            constraints = LayoutConstraints.Tight(args.LayoutSize.X, args.LayoutSize.Y);
        }

        sw.Restart();
        rootRo.Layout(constraints);
        sw.Stop();
        args.PerformanceMetrics.RecordLayoutTime((float)sw.Elapsed.TotalMilliseconds);

        if (args.WindowConfig.IsShrinkWrap)
        {
            var newSize = ClampSize(rootRo.Size, args.WindowConfig);
            windowSize = newSize;
            windowPos = args.OnShrinkWrapCompleted(newSize);
            rootRo.ScreenOffset = windowPos;
        }

        args.SyncHitBounds();
        return (windowPos, windowSize);
    }

    private void PaintFrame(
        FrameArgs args,
        RenderObject rootRo,
        PaintingContext context,
        Stopwatch sw,
        Vector2 windowPos,
        Vector2 windowSize
    )
    {
        var canvas = args.Canvas;

        using var saveScope = canvas.SaveScope();

        var extraScale = args.UiScale / RuntimeEnv.GUIScale;
        if (Math.Abs(extraScale - 1.0f) > 1e-5f)
        {
            canvas.Scale(extraScale, extraScale);
        }

        canvas.Translate(windowPos.X, windowPos.Y);

        var paintBounds = args.WindowConfig.Clip
            ? new SKRect(0, 0, windowSize.X, windowSize.Y)
            : new SKRect(
                -windowPos.X,
                -windowPos.Y,
                args.ScreenW - windowPos.X,
                args.ScreenH - windowPos.Y
            );

        if (args.WindowConfig.Clip)
        {
            canvas.ClipRect(paintBounds);
        }

        var needsRepaint = rootRo.NeedsPaint || rootRo.ChildNeedsPaint || _rootPaintCache == null;
        GuiProfiler.Current?.RecordFrame(needsRepaint);

        if (needsRepaint)
        {
            _rootPaintCache?.Dispose();
            using var recorder = new SKPictureRecorder();
            var recCanvas = recorder.BeginRecording(paintBounds);
            context.PushCanvas(recCanvas);

            sw.Restart();
            rootRo.Paint(context);
            sw.Stop();
            args.PerformanceMetrics.RecordPaintTime((float)sw.Elapsed.TotalMilliseconds);

            context.PopCanvas();
            _rootPaintCache = recorder.EndRecording();
            canvas.DrawPicture(_rootPaintCache);
        }
        else
        {
            canvas.DrawPicture(_rootPaintCache);
            args.PerformanceMetrics.RecordPaintTime(0);
        }

        if (args.DebugSettings.IsAnyEnabled)
        {
            DebugPainter.PaintDebugInfo(
                rootRo,
                context,
                args.DebugSettings,
                args.LocalElapsedMs,
                args.IsActiveWindow,
                args.PerformanceMetrics,
                args.Profiler
            );
        }

        if (args.IsResizePending)
        {
            args.PaintResizePendingOverlay(canvas);
        }
    }

    private static Vector2 ClampSize(
        Vector2 size,
        WindowConfig config
    ) =>
        new(
            Math.Clamp(size.X, config.MinSize.X, config.MaxSize.X),
            Math.Clamp(size.Y, config.MinSize.Y, config.MaxSize.Y)
        );
}

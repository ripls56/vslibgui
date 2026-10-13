using System;
using Gui.Core.Framework;
using Gui.Rendering;
using SkiaSharp;

namespace Gui.Core.Painting;

/// <summary>
///     Isolates its subtree into its own composited <see cref="PaintLayer" />.
///     <para>
///         The layer is drawn into the parent recording as a live drawable handle, so a
///         dirty subtree only re-records this layer — the parent layer, and the window
///         root layer, keep their recording. <see cref="RenderObject.MarkNeedsPaint" />
///         therefore stops propagating here (see <see cref="TryRegisterDirtyLayer" />)
///         and registers this boundary with the window's <see cref="PaintLayerRegistry" />.
///     </para>
/// </summary>
public class RenderRepaintBoundary : RenderProxyBox
{
    private readonly PaintLayer _layer = new();

    /// <summary>
    ///     <c>true</c> once the parent recording holds a reference to this boundary's
    ///     layer host, which is the precondition for an out-of-band layer re-record to
    ///     become visible without re-recording the parent.
    /// </summary>
    private bool _composited;

    public RenderRepaintBoundary() => IsRepaintBoundary = true;

    /// <inheritdoc />
    internal override bool TryRegisterDirtyLayer()
    {
        if (!_composited || !_layer.HasContent)
        {
            return false;
        }

        var registry = FindLayerRegistry();
        if (registry == null)
        {
            return false;
        }

        registry.MarkDirty(this);
        return true;
    }

    private PaintLayerRegistry? FindLayerRegistry()
    {
        var curr = (RenderObject)this;
        while (curr.LayerRegistry == null && curr.Parent != null)
        {
            curr = curr.Parent;
        }

        return curr.LayerRegistry;
    }

    public override void Paint
    (
        PaintingContext context
    )
    {
        // Do NOT record any event here — the cache check below determines hit vs miss.
        MarkPainted();

        if (context.Canvas == null)
        {
            PaintInternal(context);
            foreach (var child in Children)
            {
                child.Paint(context);
            }

            return;
        }

        var canvas = context.Canvas;

        // Optimization: if neither we nor our children are dirty, replay the layer.
        if (!NeedsPaint && !ChildNeedsPaint && _layer.HasContent)
        {
            // CACHE HIT: record hit frame ID only — distinct from a dirty paint.
            RepaintRecord.CacheHitFrameId = CurrentFrameId;
            RepaintRecord.LastEventTimestampMs = context.CurrentTime;
            GuiModSystem.Instance?.PerformanceMetrics?.IncrementRepaintBoundaryHit();
            DrawLayer(canvas);
            return;
        }

        RecordLayer(context);
        DrawLayer(canvas);
    }

    /// <summary>
    ///     Re-records this boundary's layer without drawing it anywhere. Called by
    ///     <see cref="PaintLayerRegistry" /> for boundaries that went dirty while every
    ///     enclosing layer stayed clean: the parent recording already references the
    ///     layer host, so replacing the content is enough to make the new pixels appear.
    /// </summary>
    internal void RepaintLayer
    (
        PaintingContext context
    )
    {
        // A boundary detached since it was registered no longer reaches the window's
        // registry — re-recording it would paint an orphaned subtree for nobody.
        if (!_composited || !_layer.HasContent || FindLayerRegistry() == null)
        {
            return;
        }

        MarkPainted();
        RecordLayer(context);
    }

    private void RecordLayer
    (
        PaintingContext context
    )
    {
        // CACHE MISS: record dirty paint frame ID here, not at the top of Paint.
        RepaintRecord.DirtyPaintedFrameId = CurrentFrameId;
        RepaintRecord.DirtyPaintCount++;
        RepaintRecord.LastEventTimestampMs = context.CurrentTime;
        UpdateHeatWindow();
        GuiModSystem.Instance?.PerformanceMetrics?.IncrementRepaintBoundaryMiss();
        OnAnyPaint?.Invoke(this);

        var recordingCanvas = _layer.BeginRecording(ComputeRecordingBounds());
        context.PushCanvas(recordingCanvas);

        // Call PaintInternal directly instead of base.Paint to avoid base.Paint resetting
        // NeedsPaint/ChildNeedsPaint and handling the child loop — both of which conflict
        // with the picture recorder. We manually translate and paint children here.
        // If RenderObject.Paint's child loop logic changes, this section must be updated too.
        PaintInternal(context);

        foreach (var child in Children)
        {
            recordingCanvas.Save();
            recordingCanvas.Translate
            (
                child.X,
                child.Y
            );
            child.Paint(context);
            recordingCanvas.Restore();
        }

        context.PopCanvas();
        _layer.EndRecording();

        NeedsPaint = false;
        ChildNeedsPaint = false;
    }

    private void DrawLayer
    (
        SKCanvas canvas
    )
    {
        canvas.DrawDrawable
        (
            _layer.Host,
            0,
            0
        );
        _composited = true;
    }

    /// <summary>
    ///     Recording bounds for this layer: the subtree's real paint bounds
    ///     (<see cref="RenderObject.ComputePaintBounds" />), not just <see cref="Size" />.
    ///     Skia culls recorded draw ops against these bounds, so anything that paints
    ///     outside the box — outer shadows, scale transforms, overflowing children —
    ///     would silently vanish if the rect were tight to the box.
    /// </summary>
    private SKRect ComputeRecordingBounds()
    {
        var bounds = ComputePaintBounds();

        // Guard against a degenerate rect on a zero-size boundary: an empty recording
        // would cull everything, including content a child paints at its own origin.
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            bounds = new SKRect
            (
                MathF.Min(bounds.Left, 0f),
                MathF.Min(bounds.Top, 0f),
                MathF.Max(bounds.Right, MathF.Max(Size.X, 1f)),
                MathF.Max(bounds.Bottom, MathF.Max(Size.Y, 1f))
            );
        }

        return bounds;
    }

    public override void Dispose()
    {
        var registry = FindLayerRegistry();
        if (registry != null)
        {
            registry.Remove(this);
            if (_composited)
            {
                // The live root recording still references our layer host; free it only
                // after the next root re-record drops that reference.
                registry.Retire(_layer);
                _composited = false;
                base.Dispose();
                return;
            }
        }

        _composited = false;
        _layer.Dispose();
        base.Dispose();
    }
}

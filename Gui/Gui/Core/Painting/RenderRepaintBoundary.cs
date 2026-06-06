using System;
using Gui.Core.Framework;
using Gui.Rendering;
using SkiaSharp;

namespace Gui.Core.Painting;

public class RenderRepaintBoundary : RenderProxyBox
{
    private SKPicture? _cache;

    public RenderRepaintBoundary()
    {
        IsRepaintBoundary = true;
    }

    protected override void OnMarkNeedsPaint()
    {
        _cache?.Dispose();
        _cache = null;
    }

    public override void Paint(
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

        // Optimization: If we are not dirty AND children are not dirty AND we have a cache, just draw it.
        if (!NeedsPaint && !ChildNeedsPaint && _cache != null)
        {
            // CACHE HIT: record hit frame ID only — distinct from a dirty paint.
            RepaintRecord.CacheHitFrameId = CurrentFrameId;
            RepaintRecord.LastEventTimestampMs = context.CurrentTime;
            GuiModSystem.Instance?.PerformanceMetrics?.IncrementRepaintBoundaryHit();
            context.Canvas.DrawPicture(_cache);
            return;
        }

        // CACHE MISS: record dirty paint frame ID here, not at the top of the method.
        RepaintRecord.DirtyPaintedFrameId = CurrentFrameId;
        RepaintRecord.DirtyPaintCount++;
        RepaintRecord.LastEventTimestampMs = context.CurrentTime;
        UpdateHeatWindow();
        GuiModSystem.Instance?.PerformanceMetrics?.IncrementRepaintBoundaryMiss();
        OnAnyPaint?.Invoke(this);

        // If we or our children are dirty, re-record.
        _cache?.Dispose();

        using var recorder = new SKPictureRecorder();
        var recordingBounds = ComputeRecordingBounds();
        var recordingCanvas = recorder.BeginRecording(recordingBounds);

        var originalCanvas = context.Canvas;
        // After PopCanvas(), context.Canvas returns to originalCanvas (the real GL surface).
        context.PushCanvas(recordingCanvas);

        // Call PaintInternal directly instead of base.Paint to avoid base.Paint resetting
        // NeedsPaint/ChildNeedsPaint and handling the child loop — both of which conflict
        // with the picture recorder. We manually translate and paint children here.
        // If RenderObject.Paint's child loop logic changes, this section must be updated too.
        PaintInternal(context);

        foreach (var child in Children)
        {
            recordingCanvas.Save();
            recordingCanvas.Translate(
                child.X,
                child.Y
            );
            child.Paint(context);
            recordingCanvas.Restore();
        }

        context.PopCanvas();
        _cache = recorder.EndRecording();

        NeedsPaint = false;
        ChildNeedsPaint = false;
        originalCanvas.DrawPicture(_cache);
    }

    /// <summary>
    ///     Computes recording bounds inflated by the maximum outer shadow extent
    ///     of any RenderBox descendant so outer shadows are not clipped by the picture.
    ///     Walks the subtree because wrapper widgets (Clip, SizedBox, Padding) may
    ///     sit between the boundary and the shadowed RenderBox.
    /// </summary>
    private SKRect ComputeRecordingBounds()
    {
        var extent = 0f;
        CollectMaxShadowExtent(
            this,
            ref extent
        );
        return new SKRect(
            -extent,
            -extent,
            Size.X + extent,
            Size.Y + extent
        );
    }

    private static void CollectMaxShadowExtent(
        RenderObject node,
        ref float extent
    )
    {
        if (node is RenderBox rb && rb.Shadows != null)
        {
            foreach (var shadow in rb.Shadows)
            {
                if (!shadow.Inset)
                {
                    extent = MathF.Max(
                        extent,
                        shadow.Extent
                    );
                }
            }
        }

        foreach (var child in node.Children)
        {
            // Stop at nested repaint boundaries — they manage their own recording
            if (child.IsRepaintBoundary)
            {
                continue;
            }

            CollectMaxShadowExtent(
                child,
                ref extent
            );
        }
    }

    public override void Dispose()
    {
        _cache?.Dispose();
        _cache = null;
        base.Dispose();
    }
}

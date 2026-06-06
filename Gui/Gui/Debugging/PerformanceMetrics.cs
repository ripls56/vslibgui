using System;

namespace Gui.Debugging;

public class PerformanceMetrics
{
    private long _lastFrameMemory;

    public float FrameTime { get; private set; }
    public float BuildTime { get; private set; }
    public float LayoutTime { get; private set; }
    public float PaintTime { get; private set; }
    public float BeginTime { get; private set; }
    public float EndTime { get; private set; }
    public double CurrentFrameTime { get; private set; }
    public int LayoutCalls { get; private set; }
    public int PaintCalls { get; private set; }
    public int RepaintBoundaryHits { get; private set; }
    public int RepaintBoundaryMisses { get; private set; }
    public long TotalAllocatedMemory { get; private set; }
    public long AllocationsThisFrame { get; private set; }

    public int WidgetCount { get; set; }

    public void RecordFrameTime(
        float dtMs
    )
    {
        // Simple moving average (smoothing)
        if (FrameTime == 0)
        {
            FrameTime = dtMs;
        }
        else
        {
            FrameTime = FrameTime * 0.9f + dtMs * 0.1f;
        }

        var currentMemory = GC.GetTotalAllocatedBytes();
        AllocationsThisFrame = currentMemory - _lastFrameMemory;
        _lastFrameMemory = currentMemory;
        TotalAllocatedMemory = GC.GetTotalMemory(false);
    }

    public void RecordBuildTime(
        float ms
    )
    {
        if (BuildTime == 0)
        {
            BuildTime = ms;
        }
        else
        {
            BuildTime = BuildTime * 0.9f + ms * 0.1f;
        }
    }

    public void RecordLayoutTime(
        float ms
    )
    {
        if (LayoutTime == 0)
        {
            LayoutTime = ms;
        }
        else
        {
            LayoutTime = LayoutTime * 0.9f + ms * 0.1f;
        }
    }

    public void RecordPaintTime(
        float ms
    )
    {
        if (PaintTime == 0)
        {
            PaintTime = ms;
        }
        else
        {
            PaintTime = PaintTime * 0.9f + ms * 0.1f;
        }
    }

    public void RecordBeginTime(
        float ms
    )
    {
        if (BeginTime == 0)
        {
            BeginTime = ms;
        }
        else
        {
            BeginTime = BeginTime * 0.9f + ms * 0.1f;
        }
    }

    public void RecordEndTime(
        float ms
    )
    {
        if (EndTime == 0)
        {
            EndTime = ms;
        }
        else
        {
            EndTime = EndTime * 0.9f + ms * 0.1f;
        }
    }

    public void IncrementLayout() => LayoutCalls++;

    public void IncrementPaint() => PaintCalls++;

    public void IncrementRepaintBoundaryHit() => RepaintBoundaryHits++;

    public void IncrementRepaintBoundaryMiss() => RepaintBoundaryMisses++;

    public void OnFrameStart(
        double totalTime
    )
    {
        CurrentFrameTime = totalTime;
        LayoutCalls = 0;
        PaintCalls = 0;
        RepaintBoundaryHits = 0;
        RepaintBoundaryMisses = 0;
    }
}

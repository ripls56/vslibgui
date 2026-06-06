namespace Gui.Core.Painting;

/// <summary>
///     Per-node repaint debug state. Uses frame IDs for unambiguous event detection
///     and a single timestamp for smooth alpha fade in the debug overlay.
///     Frame IDs make hit/miss detection explicit — no fragile timestamp comparisons.
/// </summary>
internal struct RepaintRecord
{
    /// <summary>Frame ID when NeedsPaint was true and the node actually re-executed paint logic.</summary>
    /// <remarks>Initialized to -1 (never painted) to avoid colliding with CurrentFrameId=0 on frame 0.</remarks>
    public int DirtyPaintedFrameId = -1;

    /// <summary>Frame ID when this RepaintBoundary served a cache hit (no re-record needed).</summary>
    /// <remarks>Initialized to -1 (never hit) to avoid false positives on frame 0.</remarks>
    public int CacheHitFrameId = -1;

    /// <summary>Total number of dirty paints since this node was created. Shown in MISS badge.</summary>
    public int DirtyPaintCount;

    /// <summary>
    ///     Rolling repaint frequency counter for heat-map intensity.
    ///     Decays by half every 60 frames to prevent unbounded growth.
    /// </summary>
    public int HotFrameCount;

    /// <summary>Frame ID of the last heat-window decay, used to detect stale windows.</summary>
    public int HotWindowLastFrameId;

    /// <summary>
    ///     Real-clock ms when the most recent significant event occurred (dirty paint or cache hit).
    ///     Used only as a fade-duration source; never compared against another same-frame value.
    /// </summary>
    public double LastEventTimestampMs;

    public RepaintRecord()
    {
    }

    public bool WasDirtyPaintedThisFrame(
        int currentFrameId
    ) =>
        DirtyPaintedFrameId == currentFrameId;

    public bool WasCacheHitThisFrame(
        int currentFrameId
    ) =>
        CacheHitFrameId == currentFrameId;
}

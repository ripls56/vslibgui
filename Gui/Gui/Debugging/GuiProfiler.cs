using System;
using System.Collections.Generic;
using Gui.Core.Framework;

namespace Gui.Debugging;

/// <summary>
///     Per-window GUI frametime profiler. Tracks how often the window re-records its
///     SKPicture (paint churn) so a steadily expensive or every-frame-dirtying HUD can
///     be identified without external profilers. One instance lives on each
///     <c>GuiBase</c>; the instance for the window currently rendering is published as
///     <see cref="Current" />.
/// </summary>
public sealed class GuiProfiler
{
    /// <summary>Number of frames retained in the rolling re-record window.</summary>
    public const int Window = 60;

    private const int StaleAfterFrames = 120;
    private readonly Dictionary<RenderObject, DirtyEntry> _dirty = new();

    private readonly bool[] _reRecorded = new bool[Window];
    private int _cursor;

    /// <summary>
    ///     Master gate. When <see langword="false" />, <see cref="Current" /> is never
    ///     published and every hot-path hook is a no-op.
    /// </summary>
    public static bool Enabled { get; set; }

    /// <summary>
    ///     The profiler collecting events for the window currently rendering, or
    ///     <see langword="null" /> when profiling is disabled or no frame is in flight.
    /// </summary>
    internal static GuiProfiler? Current { get; set; }

    /// <summary>Filled length of the rolling window (capped at <see cref="Window" />).</summary>
    public int FramesSampled { get; private set; }

    /// <summary>Number of re-recorded frames within the current window.</summary>
    public int ReRecordsInWindow
    {
        get
        {
            var count = 0;
            for (var i = 0; i < FramesSampled; i++)
            {
                if (_reRecorded[i])
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Fraction of sampled frames that re-recorded the SKPicture (0..1).</summary>
    public float RepaintFrequency =>
        FramesSampled == 0
            ? 0f
            : (float)ReRecordsInWindow / FramesSampled;

    /// <summary>Records whether the window re-recorded its SKPicture this frame.</summary>
    public void RecordFrame
    (
        bool reRecorded
    )
    {
        _reRecorded[_cursor] = reRecorded;
        _cursor = (_cursor + 1) % Window;
        if (FramesSampled < Window)
        {
            FramesSampled++;
        }

        PruneStale();
    }

    /// <summary>
    ///     Credits <paramref name="node" /> as a dirty initiator. Counted at most once
    ///     per frame (keyed by <see cref="RenderObject.CurrentFrameId" />), so a node
    ///     dirtied repeatedly within one frame still counts as a single initiation.
    /// </summary>
    public void RecordDirtyInitiator
    (
        RenderObject node
    )
    {
        var frame = RenderObject.CurrentFrameId;
        if (_dirty.TryGetValue(node, out var entry))
        {
            if (entry.LastFrameId == frame)
            {
                return;
            }

            entry.Count++;
            entry.LastFrameId = frame;
            _dirty[node] = entry;
        }
        else
        {
            _dirty[node] = new DirtyEntry
            {
                Count = 1, LastFrameId = frame
            };
        }
    }

    /// <summary>The top dirty initiators by frame count, descending.</summary>
    /// <param name="count">The maximum number of hotspots to return.</param>
    public IReadOnlyList<DirtyHotspot> TopDirtyInitiators
    (
        int count
    )
    {
        var list = new List<DirtyHotspot>(_dirty.Count);
        foreach (var (node, entry) in _dirty)
        {
            list.Add(new DirtyHotspot(node, entry.Count));
        }

        list.Sort((a, b) => b.InitiatedFrames.CompareTo(a.InitiatedFrames));
        if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }

        return list;
    }

    private void PruneStale()
    {
        var frame = RenderObject.CurrentFrameId;
        List<RenderObject>? stale = null;
        foreach (var (node, entry) in _dirty)
        {
            if (frame - entry.LastFrameId > StaleAfterFrames)
            {
                (stale ??= new List<RenderObject>()).Add(node);
            }
        }

        if (stale == null)
        {
            return;
        }

        foreach (var node in stale)
        {
            _dirty.Remove(node);
        }
    }

    /// <summary>Clears all collected data.</summary>
    public void Reset()
    {
        Array.Clear(_reRecorded, 0, _reRecorded.Length);
        _cursor = 0;
        FramesSampled = 0;
        _dirty.Clear();
    }

    private struct DirtyEntry
    {
        /// <summary>Number of distinct frames this node initiated dirtying.</summary>
        public int Count;
        /// <summary>The most recent frame id this node was credited on.</summary>
        public int LastFrameId;
    }
}

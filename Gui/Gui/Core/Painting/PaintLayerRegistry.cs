using System.Collections.Generic;
using Gui.Rendering;

namespace Gui.Core.Painting;

/// <summary>
///     Per-window registry of repaint boundaries whose layer content went stale.
///     Owned by <c>FramePipeline</c> and installed on the root render object, so
///     <see cref="Gui.Core.Framework.RenderObject.MarkNeedsPaint" /> can stop
///     propagating <c>ChildNeedsPaint</c> at the first enclosing boundary instead of
///     dirtying the root: the boundary registers here and the pipeline re-records only
///     that layer, then replays the untouched root layer.
/// </summary>
internal sealed class PaintLayerRegistry
{
    private readonly List<RenderRepaintBoundary> _dirty = [];
    private readonly List<PaintLayer> _retired = [];

    /// <summary><c>true</c> when at least one layer awaits a re-record.</summary>
    internal bool HasDirtyLayers => _dirty.Count > 0;

    /// <summary>Registers <paramref name="boundary" /> for an out-of-band re-record.</summary>
    internal void MarkDirty
    (
        RenderRepaintBoundary boundary
    )
    {
        if (!_dirty.Contains(boundary))
        {
            _dirty.Add(boundary);
        }
    }

    internal void Remove
    (
        RenderRepaintBoundary boundary
    ) =>
        _dirty.Remove(boundary);

    /// <summary>
    ///     Queues a disposed boundary's layer for deferred destruction. The live root
    ///     recording still holds a reference to the layer's drawable host, so freeing it
    ///     now would make the next playback read freed native memory. Disposal happens in
    ///     <see cref="DisposeRetiredLayers" /> once the root has been re-recorded without it.
    /// </summary>
    internal void Retire
    (
        PaintLayer layer
    ) =>
        _retired.Add(layer);

    /// <summary>Frees layers retired before the root recording that just completed.</summary>
    internal void DisposeRetiredLayers()
    {
        if (_retired.Count == 0)
        {
            return;
        }

        foreach (var layer in _retired)
        {
            layer.Dispose();
        }

        _retired.Clear();
    }

    /// <summary>
    ///     Drops all pending entries. Called after the root layer was fully re-recorded,
    ///     because that pass already repainted every reachable dirty boundary inline.
    /// </summary>
    internal void Clear() => _dirty.Clear();

    /// <summary>
    ///     Re-records every registered layer that is still dirty, shallowest first so a
    ///     parent layer's re-record consumes its nested dirty boundaries inline.
    /// </summary>
    internal void RepaintDirtyLayers
    (
        PaintingContext context
    )
    {
        if (_dirty.Count == 0)
        {
            return;
        }

        if (_dirty.Count > 1)
        {
            _dirty.Sort(static (a, b) => Depth(a).CompareTo(Depth(b)));
        }

        // Snapshot count: re-recording never adds entries (nested boundaries are
        // painted inline), but a layout-driven mark could, and those belong to the
        // next frame.
        var count = _dirty.Count;
        for (var i = 0; i < count; i++)
        {
            var boundary = _dirty[i];
            if (boundary.NeedsPaint || boundary.ChildNeedsPaint)
            {
                boundary.RepaintLayer(context);
            }
        }

        _dirty.RemoveRange
        (
            0,
            count
        );
    }

    private static int Depth
    (
        RenderRepaintBoundary boundary
    )
    {
        var depth = 0;
        var curr = boundary.Parent;
        while (curr != null)
        {
            depth++;
            curr = curr.Parent;
        }

        return depth;
    }
}

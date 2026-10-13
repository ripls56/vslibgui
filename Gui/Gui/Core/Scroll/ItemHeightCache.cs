using System;

namespace Gui.Core.Scroll;

internal class ItemHeightCache
{
    // Lowest index whose prefix sums are stale. int.MaxValue == clean.
    // SetMeasured only advances this; the actual O(n) rebuild is deferred to Flush().
    private int _dirtyFrom = int.MaxValue;
    private float[] _heights = [];
    private bool[] _isMeasured = [];
    private float[] _prefixSums = [0];

    public ItemHeightCache
    (
        float defaultEstimate = 40f
    )
    {
        DefaultEstimate = defaultEstimate;
    }

    public int Count { get; private set; }
    public float TotalHeight
    {
        get
        {
            Flush();
            return _prefixSums[Count];
        }
    }
    public float DefaultEstimate { get; private set; }

    public void Reset
    (
        int newCount,
        float estimatedHeight
    )
    {
        DefaultEstimate = estimatedHeight;
        Count = newCount;
        _heights = new float[newCount];
        _isMeasured = new bool[newCount];
        _prefixSums = new float[newCount + 1];

        for (var i = 0; i < newCount; i++)
        {
            _heights[i] = estimatedHeight;
        }

        _dirtyFrom = int.MaxValue;
        RebuildPrefixSums(0);
    }

    public void GrowTo
    (
        int newCount,
        float estimatedHeight
    )
    {
        if (newCount <= Count)
        {
            return;
        }

        DefaultEstimate = estimatedHeight;
        var oldCount = Count;
        Count = newCount;

        if (newCount > _heights.Length)
        {
            var capacity = Math.Max
            (
                newCount,
                _heights.Length * 2
            );
            Array.Resize
            (
                ref _heights,
                capacity
            );
            Array.Resize
            (
                ref _isMeasured,
                capacity
            );
            Array.Resize
            (
                ref _prefixSums,
                capacity + 1
            );
        }

        for (var i = oldCount; i < newCount; i++)
        {
            _heights[i] = estimatedHeight;
            _isMeasured[i] = false;
        }

        MarkDirtyFrom(oldCount);
    }

    public float SetMeasured
    (
        int index,
        float height
    )
    {
        if (index < 0 || index >= Count)
        {
            return 0f;
        }

        var oldHeight = _heights[index];
        if (_isMeasured[index] && Math.Abs(oldHeight - height) < 0.5f)
        {
            return 0f;
        }

        var delta = height - oldHeight;
        _heights[index] = height;
        _isMeasured[index] = true;
        MarkDirtyFrom(index);
        return delta;
    }

    public float GetHeight
    (
        int index
    )
    {
        if (index < 0 || index >= Count)
        {
            return DefaultEstimate;
        }

        return _heights[index];
    }

    public bool IsMeasured
    (
        int index
    ) =>
        index >= 0 && index < Count && _isMeasured[index];

    public float GetPosition
    (
        int index
    )
    {
        Flush();
        if (index < 0)
        {
            return 0f;
        }

        if (index >= Count)
        {
            return _prefixSums[Count];
        }

        return _prefixSums[index];
    }

    public int FindFirstVisible
    (
        float scrollOffset
    )
    {
        Flush();
        if (Count == 0)
        {
            return 0;
        }

        scrollOffset = Math.Max
        (
            0,
            scrollOffset
        );

        int lo = 0, hi = Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (_prefixSums[mid + 1] <= scrollOffset)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    public int FindLastVisible
    (
        float scrollOffset,
        float viewportHeight
    )
    {
        Flush();
        var bottomEdge = scrollOffset + viewportHeight;
        if (Count == 0)
        {
            return 0;
        }

        int lo = 0, hi = Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (_prefixSums[mid] < bottomEdge)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    public void SetEstimatedHeight
    (
        float estimate
    )
    {
        DefaultEstimate = estimate;
        var changed = false;
        for (var i = 0; i < Count; i++)
        {
            if (!_isMeasured[i])
            {
                _heights[i] = estimate;
                changed = true;
            }
        }

        if (changed)
        {
            MarkDirtyFrom(0);
        }
    }

    private void MarkDirtyFrom
    (
        int index
    )
    {
        if (index < _dirtyFrom)
        {
            _dirtyFrom = index;
        }
    }

    /// <summary>
    ///     Recomputes stale prefix sums in a single O(Count - dirtyFrom) pass. Called lazily
    ///     from every read path (TotalHeight, GetPosition, Find*), so a burst of SetMeasured
    ///     calls collapses into one rebuild instead of one per measured child.
    /// </summary>
    private void Flush()
    {
        if (_dirtyFrom == int.MaxValue)
        {
            return;
        }

        var from = _dirtyFrom;
        _dirtyFrom = int.MaxValue;
        RebuildPrefixSums(from);
    }

    private void RebuildPrefixSums
    (
        int fromIndex
    )
    {
        for (var i = fromIndex; i < Count; i++)
        {
            _prefixSums[i + 1] = _prefixSums[i] + _heights[i];
        }
    }
}

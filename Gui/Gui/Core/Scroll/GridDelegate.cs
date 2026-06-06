using System;

namespace Gui.Core.Scroll;

/// <summary>
///     Computed grid geometry produced by a <see cref="SliverGridDelegate" />.
///     All cell-positioning arithmetic is centralised here.
/// </summary>
public readonly struct SliverGridGeometry
{
    public int CrossAxisCount { get; }
    public float CellWidth { get; }
    public float CellHeight { get; }
    public float MainAxisSpacing { get; }
    public float CrossAxisSpacing { get; }

    public SliverGridGeometry(
        int crossAxisCount,
        float cellWidth,
        float cellHeight,
        float mainAxisSpacing,
        float crossAxisSpacing
    )
    {
        CrossAxisCount = crossAxisCount;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
    }

    public float TotalContentHeight(
        int itemCount
    )
    {
        if (itemCount <= 0 || CrossAxisCount <= 0)
        {
            return 0;
        }

        var rowCount = (int)Math.Ceiling(itemCount / (double)CrossAxisCount);
        return rowCount * CellHeight + Math.Max(
            0,
            rowCount - 1
        ) * MainAxisSpacing;
    }

    public (float x, float y) CellOrigin(
        int index
    )
    {
        var row = index / CrossAxisCount;
        var col = index % CrossAxisCount;
        var x = col * (CellWidth + CrossAxisSpacing);
        var y = row * (CellHeight + MainAxisSpacing);
        return (x, y);
    }

    /// <summary>
    ///     Returns the flat index range of cells that overlap the viewport window
    ///     <c>[scrollOffset, scrollOffset + viewportHeight]</c>, with a one-row buffer.
    /// </summary>
    public (int firstIndex, int lastIndex) ComputeVisibleRange(
        float scrollOffset,
        float viewportHeight,
        int itemCount
    )
    {
        if (itemCount <= 0 || CrossAxisCount <= 0)
        {
            return (0, -1);
        }

        var rowHeight = CellHeight + MainAxisSpacing;
        if (rowHeight <= 0)
        {
            return (0, itemCount - 1);
        }

        var totalRows = (int)Math.Ceiling(itemCount / (double)CrossAxisCount);

        var firstRow = (int)Math.Floor(scrollOffset / rowHeight);
        var lastRow = (int)Math.Ceiling((scrollOffset + viewportHeight) / rowHeight);

        firstRow = Math.Max(
            0,
            firstRow - 1
        );
        lastRow = Math.Min(
            totalRows - 1,
            lastRow
        );

        var firstIndex = firstRow * CrossAxisCount;
        var lastIndex = Math.Min(
            itemCount - 1,
            (lastRow + 1) * CrossAxisCount - 1
        );

        return (firstIndex, lastIndex);
    }
}

/// <summary>
///     Base class for grid layout delegates that compute cell geometry from
///     a given viewport width.
/// </summary>
public abstract class SliverGridDelegate
{
    public abstract SliverGridGeometry GetGeometry(
        float viewportWidth
    );
}

/// <summary>
///     Grid delegate with a fixed number of columns. Cell width is computed by
///     dividing available space evenly; cell height derives from
///     <see cref="ChildAspectRatio" /> or <see cref="FixedItemHeight" />.
/// </summary>
public class SliverGridDelegateWithFixedCrossAxisCount : SliverGridDelegate
{
    public SliverGridDelegateWithFixedCrossAxisCount(
        int crossAxisCount,
        float mainAxisSpacing = 0,
        float crossAxisSpacing = 0,
        float childAspectRatio = 1.0f,
        float? fixedItemHeight = null
    )
    {
        CrossAxisCount = Math.Max(
            1,
            crossAxisCount
        );
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        FixedItemHeight = fixedItemHeight;
    }

    public int CrossAxisCount { get; }
    public float MainAxisSpacing { get; }
    public float CrossAxisSpacing { get; }
    public float ChildAspectRatio { get; }
    public float? FixedItemHeight { get; }

    public override SliverGridGeometry GetGeometry(
        float viewportWidth
    )
    {
        if (float.IsPositiveInfinity(viewportWidth) || viewportWidth <= 0)
        {
            viewportWidth = 400;
        }

        var totalSpacing = CrossAxisSpacing * (CrossAxisCount - 1);
        var cellWidth = (viewportWidth - totalSpacing) / CrossAxisCount;
        var cellHeight = FixedItemHeight ?? cellWidth / ChildAspectRatio;
        return new SliverGridGeometry(
            CrossAxisCount,
            cellWidth,
            cellHeight,
            MainAxisSpacing,
            CrossAxisSpacing
        );
    }
}

/// <summary>
///     Grid delegate that computes the number of columns from a maximum cell extent.
///     Column count = floor(viewportWidth / maxExtent), then cells fill evenly.
/// </summary>
public class SliverGridDelegateWithMaxCrossAxisExtent : SliverGridDelegate
{
    public SliverGridDelegateWithMaxCrossAxisExtent(
        float maxCrossAxisExtent,
        float mainAxisSpacing = 0,
        float crossAxisSpacing = 0,
        float childAspectRatio = 1.0f,
        float? fixedItemHeight = null
    )
    {
        MaxCrossAxisExtent = Math.Max(
            1,
            maxCrossAxisExtent
        );
        MainAxisSpacing = mainAxisSpacing;
        CrossAxisSpacing = crossAxisSpacing;
        ChildAspectRatio = childAspectRatio;
        FixedItemHeight = fixedItemHeight;
    }

    public float MaxCrossAxisExtent { get; }
    public float MainAxisSpacing { get; }
    public float CrossAxisSpacing { get; }
    public float ChildAspectRatio { get; }
    public float? FixedItemHeight { get; }

    public override SliverGridGeometry GetGeometry(
        float viewportWidth
    )
    {
        if (float.IsPositiveInfinity(viewportWidth) || viewportWidth <= 0)
        {
            viewportWidth = 400;
        }

        var crossAxisCount = Math.Max(
            1,
            (int)Math.Floor(
                (viewportWidth + CrossAxisSpacing) /
                (MaxCrossAxisExtent + CrossAxisSpacing)
            )
        );
        var totalSpacing = CrossAxisSpacing * (crossAxisCount - 1);
        var cellWidth = (viewportWidth - totalSpacing) / crossAxisCount;
        var cellHeight = FixedItemHeight ?? cellWidth / ChildAspectRatio;
        return new SliverGridGeometry(
            crossAxisCount,
            cellWidth,
            cellHeight,
            MainAxisSpacing,
            CrossAxisSpacing
        );
    }
}

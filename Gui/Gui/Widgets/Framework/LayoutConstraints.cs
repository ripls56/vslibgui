using System;
using Gui.Core.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Framework;

/// <summary>
///     Describes the minimum and maximum size that a <see cref="RenderObject" /> is allowed
///     to occupy. Every <c>RenderObject.Layout()</c> call receives constraints from its parent
///     and must produce a <c>Size</c> within those bounds.
/// </summary>
public struct LayoutConstraints(
    float minWidth = 0,
    float maxWidth = float.PositiveInfinity,
    float minHeight = 0,
    float maxHeight = float.PositiveInfinity
)
    : IEquatable<LayoutConstraints>
{
    public float MinWidth = minWidth;
    public float MaxWidth = maxWidth;
    public float MinHeight = minHeight;
    public float MaxHeight = maxHeight;

    /// <summary>
    ///     Creates constraints that force an exact size (<c>min == max</c>). Used by the root
    ///     layout pass and by <c>RenderConstrainedBox</c> with explicit width/height.
    /// </summary>
    public static LayoutConstraints Tight(
        float width,
        float height
    )
    {
        return new LayoutConstraints(
            width,
            width,
            height,
            height
        );
    }

    /// <summary>
    ///     Creates constraints that force an exact size.
    /// </summary>
    public static LayoutConstraints Tight(
        Vector2 size
    )
    {
        return new LayoutConstraints(
            size.X,
            size.X,
            size.Y,
            size.Y
        );
    }

    /// <summary>
    ///     Creates constraints where <c>min == 0</c> on both axes. The child may be any size
    ///     up to <paramref name="maxWidth" /> × <paramref name="maxHeight" />.
    /// </summary>
    public static LayoutConstraints Loose(
        float maxWidth,
        float maxHeight
    )
    {
        return new LayoutConstraints(
            0,
            maxWidth,
            0,
            maxHeight
        );
    }

    public bool IsTight => MinWidth >= MaxWidth && MinHeight >= MaxHeight;
    public bool IsLoose => MinWidth <= 0 && MinHeight <= 0;

    /// <summary>
    ///     Returns these constraints with minimums zeroed. Useful when a parent wants to pass
    ///     its own maximum bounds without forcing the child to fill them.
    /// </summary>
    public LayoutConstraints Loosen()
    {
        return new LayoutConstraints(
            0,
            MaxWidth,
            0,
            MaxHeight
        );
    }

    /// <summary>
    ///     Creates constraints that are tight on the axes where a value is supplied and
    ///     unbounded (<c>0..∞</c>) on axes where <c>null</c> is passed.
    /// </summary>
    public static LayoutConstraints TightFor(
        float? width = null,
        float? height = null
    )
    {
        return new LayoutConstraints(
            width ?? 0,
            width ?? float.PositiveInfinity,
            height ?? 0,
            height ?? float.PositiveInfinity
        );
    }

    /// <summary>
    ///     The largest size permitted by these constraints.
    /// </summary>
    /// <remarks>
    ///     When <see cref="MaxWidth" /> or <see cref="MaxHeight" /> is infinity, returns
    ///     <see cref="MinWidth" /> / <see cref="MinHeight" /> for that axis rather than infinity,
    ///     to avoid propagating infinite values into layout arithmetic or Skia draw calls.
    ///     This differs from Flutter's behavior where <c>constraints.biggest</c> may return
    ///     positive infinity on unconstrained axes.
    /// </remarks>
    public Vector2 Biggest => new(
        float.IsPositiveInfinity(MaxWidth)
            ? MinWidth
            : MaxWidth,
        float.IsPositiveInfinity(MaxHeight)
            ? MinHeight
            : MaxHeight
    );

    /// <summary>The smallest size permitted by these constraints.</summary>
    public Vector2 Smallest => new(
        MinWidth,
        MinHeight
    );

    /// <summary>
    ///     Intersects these constraints with <paramref name="constraints" />, clamping each
    ///     bound into the other's range.
    /// </summary>
    public LayoutConstraints Enforce(
        LayoutConstraints constraints
    )
    {
        return new LayoutConstraints(
            Math.Clamp(
                MinWidth,
                constraints.MinWidth,
                constraints.MaxWidth
            ),
            Math.Clamp(
                MaxWidth,
                constraints.MinWidth,
                constraints.MaxWidth
            ),
            Math.Clamp(
                MinHeight,
                constraints.MinHeight,
                constraints.MaxHeight
            ),
            Math.Clamp(
                MaxHeight,
                constraints.MinHeight,
                constraints.MaxHeight
            )
        );
    }

    /// <summary>
    ///     Clamps <paramref name="size" /> so that both components fall within
    ///     [Min, Max] on each axis.
    /// </summary>
    public Vector2 Constrain(
        Vector2 size
    )
    {
        return new Vector2(
            Math.Clamp(
                size.X,
                MinWidth,
                MaxWidth
            ),
            Math.Clamp(
                size.Y,
                MinHeight,
                MaxHeight
            )
        );
    }

    /// <inheritdoc />
    // Exact bit-equality is intentional: detects any constraint change to avoid missed relayout.
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public bool Equals(LayoutConstraints other)
    {
        return MinWidth == other.MinWidth &&
               MaxWidth == other.MaxWidth &&
               MinHeight == other.MinHeight &&
               MaxHeight == other.MaxHeight;
    }
    // ReSharper restore CompareOfFloatsByEqualityOperator

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LayoutConstraints other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight);

    /// <summary>Returns true when both constraints are identical.</summary>
    public static bool operator ==(LayoutConstraints left, LayoutConstraints right) =>
        left.Equals(right);

    /// <summary>Returns true when constraints differ on any axis.</summary>
    public static bool operator !=(LayoutConstraints left, LayoutConstraints right) =>
        !left.Equals(right);

    public override string ToString()
    {
        return
            $"W:[{MinWidth:F0}..{(float.IsPositiveInfinity(MaxWidth) ? "∞" : MaxWidth.ToString("F0"))}] " +
            $"H:[{MinHeight:F0}..{(float.IsPositiveInfinity(MaxHeight) ? "∞" : MaxHeight.ToString("F0"))}]";
    }
}

using System;

namespace Gui.Rendering;

/// <summary>
///     Defines the border insets for 9-slice (and 3-slice) rendering.
///     All values are in source-texture pixels — the size of each corner/edge
///     region in the source bitmap before any scaling.
/// </summary>
/// <remarks>
///     9-slice: non-zero values on all four sides keep corners pixel-perfect while
///     stretching or tiling the edges and center.
///     3-slice shortcuts:
///     <see cref="Horizontal(float)" /> — fixed left/right caps, middle stretches horizontally.
///     <see cref="Vertical(float)" />   — fixed top/bottom caps, middle stretches vertically.
/// </remarks>
public readonly struct NineSliceBorder : IEquatable<NineSliceBorder>
{
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }

    public static readonly NineSliceBorder Zero = new(
        0,
        0,
        0,
        0
    );

    public NineSliceBorder(
        float left,
        float top,
        float right,
        float bottom
    )
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }


    /// <summary>Uniform border on all four sides (full 9-slice).</summary>
    public static NineSliceBorder All(
        float value
    )
    {
        return new NineSliceBorder(
            value,
            value,
            value,
            value
        );
    }

    /// <summary>Symmetric border: same left/right and same top/bottom.</summary>
    public static NineSliceBorder Symmetric(
        float vertical = 0,
        float horizontal = 0
    )
    {
        return new NineSliceBorder(
            horizontal,
            vertical,
            horizontal,
            vertical
        );
    }

    /// <summary>Explicit left/top/right/bottom values.</summary>
    public static NineSliceBorder Only(
        float left = 0,
        float top = 0,
        float right = 0,
        float bottom = 0
    )
    {
        return new NineSliceBorder(
            left,
            top,
            right,
            bottom
        );
    }

    /// <summary>
    ///     3-slice for horizontal bars (e.g. health bars, buttons).
    ///     Fixed left/right caps; top and bottom borders are zero so the sprite
    ///     stretches vertically without slicing.
    /// </summary>
    public static NineSliceBorder Horizontal(
        float cap
    )
    {
        return new NineSliceBorder(
            cap,
            0,
            cap,
            0
        );
    }

    /// <summary>3-slice horizontal with independent left and right caps.</summary>
    public static NineSliceBorder Horizontal(
        float left,
        float right
    )
    {
        return new NineSliceBorder(
            left,
            0,
            right,
            0
        );
    }

    /// <summary>
    ///     3-slice for vertical bars (e.g. vertical sliders, dividers).
    ///     Fixed top/bottom caps; left and right borders are zero.
    /// </summary>
    public static NineSliceBorder Vertical(
        float cap
    )
    {
        return new NineSliceBorder(
            0,
            cap,
            0,
            cap
        );
    }

    /// <summary>3-slice vertical with independent top and bottom caps.</summary>
    public static NineSliceBorder Vertical(
        float top,
        float bottom
    )
    {
        return new NineSliceBorder(
            0,
            top,
            0,
            bottom
        );
    }


    /// <summary>Single float becomes a uniform All(value) border.</summary>
    public static implicit operator NineSliceBorder(
        float value
    ) =>
        All(value);


    public bool Equals(
        NineSliceBorder other
    )
    {
        return Left == other.Left && Top == other.Top &&
               Right == other.Right && Bottom == other.Bottom;
    }

    public override bool Equals(
        object? obj
    ) =>
        obj is NineSliceBorder o && Equals(o);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Left,
            Top,
            Right,
            Bottom
        );
    }

    public static bool operator ==(
        NineSliceBorder a,
        NineSliceBorder b
    ) =>
        a.Equals(b);

    public static bool operator !=(
        NineSliceBorder a,
        NineSliceBorder b
    ) =>
        !a.Equals(b);

    public override string ToString() => $"NineSliceBorder(L:{Left} T:{Top} R:{Right} B:{Bottom})";
}

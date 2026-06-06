using System;

namespace Gui.Rendering;

public readonly struct EdgeInsets : IEquatable<EdgeInsets>
{
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public static readonly EdgeInsets Zero = new(
        0,
        0,
        0,
        0
    );

    public EdgeInsets(
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

    public static EdgeInsets All(
        float value
    )
    {
        return new EdgeInsets(
            value,
            value,
            value,
            value
        );
    }

    public static EdgeInsets Symmetric(
        float vertical = 0,
        float horizontal = 0
    )
    {
        return new EdgeInsets(
            horizontal,
            vertical,
            horizontal,
            vertical
        );
    }

    public static EdgeInsets Only(
        float left = 0,
        float top = 0,
        float right = 0,
        float bottom = 0
    )
    {
        return new EdgeInsets(
            left,
            top,
            right,
            bottom
        );
    }

    public static EdgeInsets Ltrb(
        float left,
        float top,
        float right,
        float bottom
    )
    {
        return new EdgeInsets(
            left,
            top,
            right,
            bottom
        );
    }

    public bool Equals(
        EdgeInsets other
    )
    {
        return Left.Equals(other.Left) && Top.Equals(other.Top) &&
               Right.Equals(other.Right) && Bottom.Equals(other.Bottom);
    }

    public override bool Equals(
        object? obj
    ) =>
        obj is EdgeInsets other && Equals(other);

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
        EdgeInsets left,
        EdgeInsets right
    ) =>
        left.Equals(right);

    public static bool operator !=(
        EdgeInsets left,
        EdgeInsets right
    ) =>
        !left.Equals(right);
}

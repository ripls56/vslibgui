using System;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic.Theming;

public struct IconThemeData() : IEquatable<IconThemeData>
{
    public Vector4? Color { get; set; } = null;
    public float? Size { get; set; } = null;

    public IconThemeData Merge(IconThemeData other)
    {
        return new IconThemeData
        {
            Color = Color ?? other.Color, Size = Size ?? other.Size
        };
    }

    public bool Equals(IconThemeData other)
    {
        return Nullable.Equals(Color, other.Color) && Nullable.Equals(Size, other.Size);
    }

    public override bool Equals(object? obj) => obj is IconThemeData other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Color, Size);

    public static bool operator ==(IconThemeData left, IconThemeData right) => left.Equals(right);
    public static bool operator !=(IconThemeData left, IconThemeData right) => !left.Equals(right);
}

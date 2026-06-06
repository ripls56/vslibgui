using System;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Rendering;

public static class SkiaExtensions
{
    /// <summary>
    ///     Parses a CSS hex color string (e.g. "#1A1C20" or "#RGB" or "#RRGGBBAA")
    ///     into a Vector4 with components in the [0, 1] range (R, G, B, A).
    /// </summary>
    public static Vector4 FromHex(
        this string hex
    )
    {
        var s = hex.AsSpan().TrimStart('#');
        return s.Length switch
        {
            3 => new Vector4(
                ParseHexByte($"{s[0]}{s[0]}") / 255f,
                ParseHexByte($"{s[1]}{s[1]}") / 255f,
                ParseHexByte($"{s[2]}{s[2]}") / 255f,
                1f
            ),
            6 => new Vector4(
                ParseHexByte(s[..2]) / 255f,
                ParseHexByte(s[2..4]) / 255f,
                ParseHexByte(s[4..6]) / 255f,
                1f
            ),
            8 => new Vector4(
                ParseHexByte(s[..2]) / 255f,
                ParseHexByte(s[2..4]) / 255f,
                ParseHexByte(s[4..6]) / 255f,
                ParseHexByte(s[6..8]) / 255f
            ),
            _ => throw new ArgumentException($"Invalid hex color: {hex}")
        };
    }

    private static float ParseHexByte(
        ReadOnlySpan<char> s
    )
    {
        return Convert.ToByte(
            s.ToString(),
            16
        );
    }

    public static SKColor ToSkColor(
        this Vector4 color
    )
    {
        return new SKColor(
            (byte)Math.Clamp(
                color.X * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                color.Y * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                color.Z * 255f,
                0,
                255
            ),
            (byte)Math.Clamp(
                color.W * 255f,
                0,
                255
            )
        );
    }

    /// <summary>
    ///     Returns a darker shade by multiplying RGB by (1 - <paramref name="amount" />).
    ///     Alpha is preserved. <paramref name="amount" /> is clamped to [0, 1].
    /// </summary>
    public static Vector4 Darken(
        this Vector4 color,
        float amount
    )
    {
        var f = 1f - Math.Clamp(
            amount,
            0f,
            1f
        );
        return new Vector4(
            color.X * f,
            color.Y * f,
            color.Z * f,
            color.W
        );
    }

    /// <summary>
    ///     Returns a lighter shade by lerping RGB toward 1.0 by <paramref name="amount" />.
    ///     Alpha is preserved. <paramref name="amount" /> is clamped to [0, 1].
    /// </summary>
    public static Vector4 Lighten(
        this Vector4 color,
        float amount
    )
    {
        var a = Math.Clamp(
            amount,
            0f,
            1f
        );
        return new Vector4(
            color.X + (1f - color.X) * a,
            color.Y + (1f - color.Y) * a,
            color.Z + (1f - color.Z) * a,
            color.W
        );
    }

    /// <summary>
    ///     Returns the color with the specified alpha value (0..1).
    /// </summary>
    public static Vector4 WithAlpha(
        this Vector4 color,
        float alpha
    )
    {
        return color with
        {
            W = Math.Clamp(
                alpha,
                0f,
                1f
            )
        };
    }

    public static SKPoint ToSkPoint(
        this Vector2 vector
    )
    {
        return new SKPoint(
            vector.X,
            vector.Y
        );
    }

    public static SKSize ToSkSize(
        this Vector2 vector
    )
    {
        return new SKSize(
            vector.X,
            vector.Y
        );
    }

    public static SKRect ToSkRect(
        this Vector2 size,
        Vector2 pos = default
    )
    {
        return new SKRect(
            pos.X,
            pos.Y,
            pos.X + size.X,
            pos.Y + size.Y
        );
    }
}

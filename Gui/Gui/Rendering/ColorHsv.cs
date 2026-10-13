using System;
using OpenTK.Mathematics;

namespace Gui.Rendering;

/// <summary>
///     HSV (hue / saturation / value) conversions for the framework's RGBA
///     <see cref="Vector4" /> color type. Hue is in degrees [0, 360); saturation
///     and value are in [0, 1]. Alpha passes through untouched.
/// </summary>
public static class ColorHsvExtensions
{
    /// <summary>
    ///     Converts an RGB color to HSV. Returns hue in degrees [0, 360),
    ///     saturation and value in [0, 1]. The alpha channel is ignored.
    /// </summary>
    public static (float H, float S, float V) ToHsv
    (
        this Vector4 rgb
    )
    {
        var r = rgb.X;
        var g = rgb.Y;
        var b = rgb.Z;

        var max = MathF.Max
        (
            r,
            MathF.Max
            (
                g,
                b
            )
        );
        var min = MathF.Min
        (
            r,
            MathF.Min
            (
                g,
                b
            )
        );
        var delta = max - min;

        float h;
        if (delta == 0f)
        {
            h = 0f;
        }
        else if (max == r)
        {
            h = 60f * ((g - b) / delta % 6f);
        }
        else if (max == g)
        {
            h = 60f * ((b - r) / delta + 2f);
        }
        else
        {
            h = 60f * ((r - g) / delta + 4f);
        }

        if (h < 0f)
        {
            h += 360f;
        }

        var s = max <= 0f ? 0f : delta / max;

        return (
            h,
            s,
            max
        );
    }

    /// <summary>
    ///     Converts HSV to RGBA. Hue is in degrees (wrapped to [0, 360)),
    ///     saturation and value in [0, 1]. Alpha defaults to opaque.
    /// </summary>
    public static Vector4 FromHsv
    (
        float h,
        float s,
        float v,
        float a = 1f
    )
    {
        h = (h % 360f + 360f) % 360f;

        var c = v * s;
        var x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        var m = v - c;

        float r;
        float g;
        float b;

        if (h < 60f)
        {
            r = c;
            g = x;
            b = 0f;
        }
        else if (h < 120f)
        {
            r = x;
            g = c;
            b = 0f;
        }
        else if (h < 180f)
        {
            r = 0f;
            g = c;
            b = x;
        }
        else if (h < 240f)
        {
            r = 0f;
            g = x;
            b = c;
        }
        else if (h < 300f)
        {
            r = x;
            g = 0f;
            b = c;
        }
        else
        {
            r = c;
            g = 0f;
            b = x;
        }

        return new Vector4
        (
            r + m,
            g + m,
            b + m,
            a
        );
    }
}

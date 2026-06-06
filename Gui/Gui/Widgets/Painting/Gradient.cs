using System;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Painting;

/// <summary>One color stop in a gradient.</summary>
public readonly record struct GradientStop(Vector4 Color, float Position)
{
    public GradientStop Lighten(
        float amount
    )
    {
        var color = new Vector4(
            Math.Min(
                1f,
                Color.X + amount
            ),
            Math.Min(
                1f,
                Color.Y + amount
            ),
            Math.Min(
                1f,
                Color.Z + amount
            ),
            Color.W
        );
        return this with { Color = color };
    }


    public GradientStop Darken(
        float amount
    )
    {
        var color = new Vector4(
            Math.Min(
                1f,
                Color.X - amount
            ),
            Math.Min(
                1f,
                Color.Y - amount
            ),
            Math.Min(
                1f,
                Color.Z - amount
            ),
            Color.W
        );
        return this with { Color = color };
    }
}

/// <summary>
///     Immutable description of a fill gradient.
///     Subclasses provide type-specific parameters and SKShader creation.
/// </summary>
public abstract class Gradient
{
    protected Gradient(
        GradientStop[] stops
    )
    {
        Stops = stops;
    }

    public GradientStop[] Stops { get; }

    /// <summary>
    ///     Creates the SKShader for this gradient given the widget's render size.
    ///     Caller must Dispose the returned shader.
    /// </summary>
    public abstract SKShader CreateShader(
        Vector2 size
    );

    /// <summary>
    ///     Interpolates from this gradient toward <paramref name="to" /> at position t.
    ///     If <paramref name="to" /> is a different gradient type, snaps at t=0.5.
    /// </summary>
    public abstract Gradient LerpTo(
        Gradient to,
        double t
    );

    protected static GradientStop[] LerpStops(
        GradientStop[] begin,
        GradientStop[] end,
        double t
    )
    {
        if (begin.Length == 0 && end.Length == 0)
        {
            return [];
        }

        if (begin.Length == 0)
        {
            return end;
        }

        if (end.Length == 0)
        {
            return begin;
        }

        var ft = (float)t;
        var count = Math.Max(
            begin.Length,
            end.Length
        );
        var result = new GradientStop[count];
        for (var i = 0; i < count; i++)
        {
            var b = i < begin.Length
                ? begin[i]
                : begin[^1];
            var e = i < end.Length
                ? end[i]
                : end[^1];
            result[i] = new GradientStop(
                b.Color + (e.Color - b.Color) * ft,
                b.Position + (e.Position - b.Position) * ft
            );
        }

        return result;
    }

    protected (SKColor[] colors, float[] positions) ExtractStops()
    {
        var colors = new SKColor[Stops.Length];
        var positions = new float[Stops.Length];
        for (var i = 0; i < Stops.Length; i++)
        {
            colors[i] = Stops[i].Color.ToSkColor();
            positions[i] = Stops[i].Position;
        }

        return (colors, positions);
    }
}

/// <summary>
///     A gradient that transitions colors along a line at a given angle.
/// </summary>
public sealed class LinearGradient : Gradient
{
    public LinearGradient(
        float angle,
        params GradientStop[] stops
    )
        : base(stops)
    {
        Angle = angle;
    }

    /// <summary>
    ///     Gradient angle in degrees. 0 = left-to-right, 90 = top-to-bottom.
    /// </summary>
    public float Angle { get; }

    public override SKShader CreateShader(
        Vector2 size
    )
    {
        if (Stops.Length == 0)
        {
            return SKShader.CreateColor(SKColors.Transparent);
        }

        if (Stops.Length == 1)
        {
            return SKShader.CreateColor(Stops[0].Color.ToSkColor());
        }

        var (colors, positions) = ExtractStops();
        var rad = Angle * MathF.PI / 180f;
        var dx = MathF.Cos(rad) * size.X * 0.5f;
        var dy = MathF.Sin(rad) * size.Y * 0.5f;
        var start = new SKPoint(
            size.X * 0.5f - dx,
            size.Y * 0.5f - dy
        );
        var end = new SKPoint(
            size.X * 0.5f + dx,
            size.Y * 0.5f + dy
        );
        return SKShader.CreateLinearGradient(
            start,
            end,
            colors,
            positions,
            SKShaderTileMode.Clamp
        );
    }

    public override Gradient LerpTo(
        Gradient to,
        double t
    )
    {
        if (to is not LinearGradient other)
        {
            return t < 0.5
                ? this
                : to;
        }

        var ft = (float)t;
        var angle = Angle + (other.Angle - Angle) * ft;
        var stops = LerpStops(
            Stops,
            other.Stops,
            t
        );
        return new LinearGradient(
            angle,
            stops
        );
    }
}

/// <summary>
///     A gradient that radiates outward from a center point.
/// </summary>
public sealed class RadialGradient : Gradient
{
    public RadialGradient(
        Vector2 center,
        float radius,
        params GradientStop[] stops
    )
        : base(stops)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>Center in relative coordinates [0, 1].</summary>
    public Vector2 Center { get; }

    /// <summary>Radius as a fraction of max(width, height).</summary>
    public float Radius { get; }

    public override SKShader CreateShader(
        Vector2 size
    )
    {
        if (Stops.Length == 0)
        {
            return SKShader.CreateColor(SKColors.Transparent);
        }

        if (Stops.Length == 1)
        {
            return SKShader.CreateColor(Stops[0].Color.ToSkColor());
        }

        var (colors, positions) = ExtractStops();
        var center = new SKPoint(
            Center.X * size.X,
            Center.Y * size.Y
        );
        var radius = Radius * MathF.Max(
            size.X,
            size.Y
        );
        return SKShader.CreateRadialGradient(
            center,
            radius,
            colors,
            positions,
            SKShaderTileMode.Clamp
        );
    }

    public override Gradient LerpTo(
        Gradient to,
        double t
    )
    {
        if (to is not RadialGradient other)
        {
            return t < 0.5
                ? this
                : to;
        }

        var ft = (float)t;
        var center = Center + (other.Center - Center) * ft;
        var radius = Radius + (other.Radius - Radius) * ft;
        var stops = LerpStops(
            Stops,
            other.Stops,
            t
        );
        return new RadialGradient(
            center,
            radius,
            stops
        );
    }
}

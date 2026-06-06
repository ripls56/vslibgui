using System;

namespace Gui.Sound;

/// <summary>
///     Represents a pitch value that may include randomized variance.
///     Implicitly converts from <c>float</c> for fixed pitch, or use
///     <see cref="Varied" /> for per-play randomization.
///     <code>
/// sound.Play("click", pitch: 1.0f);                       // fixed
/// sound.Play("click", pitch: Pitch.Varied(1f, 0.15f));    // 0.85–1.15
/// </code>
/// </summary>
public readonly struct Pitch
{
    private static readonly Random Rng = new();

    public float Base { get; }
    public float Variance { get; }

    private Pitch(
        float @base,
        float variance
    )
    {
        Base = @base;
        Variance = variance;
    }

    /// <summary>
    ///     Creates a pitch that randomizes within
    ///     [<paramref name="base" /> - <paramref name="variance" />,
    ///     <paramref name="base" /> + <paramref name="variance" />] on each resolve.
    /// </summary>
    public static Pitch Varied(
        float @base,
        float variance
    )
    {
        return new Pitch(
            @base,
            Math.Abs(variance)
        );
    }

    /// <summary>Resolves to a concrete float value, applying variance if any.</summary>
    public float Resolve()
    {
        if (Variance <= 0f)
        {
            return Base;
        }

        var offset = (float)(Rng.NextDouble() * 2.0 - 1.0) * Variance;
        return Math.Max(
            0.01f,
            Base + offset
        );
    }

    public static implicit operator Pitch(
        float value
    ) =>
        new(
            value,
            0f
        );
}

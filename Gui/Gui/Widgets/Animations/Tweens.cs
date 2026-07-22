using System;
using Gui.Rendering;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Animations;

/// <summary>
///     Base class for interpolating between two values of type
///     <typeparamref name="T" /> over an animation's progress (0..1).
/// </summary>
public abstract class Tween<T>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    protected Tween
    (
        T begin,
        T end
    )
    {
        Begin = begin;
        End = end;
    }

    /// <summary>The value at the start of the animation (t = 0).</summary>
    public T Begin { get; set; }

    /// <summary>The value at the end of the animation (t = 1).</summary>
    public T End { get; set; }

    /// <summary>
    ///     Returns the interpolated value at progress <paramref name="t" />
    ///     (0 = begin, 1 = end).
    /// </summary>
    public abstract T Lerp
    (
        double t
    );

    /// <summary>
    ///     Evaluates this tween at the current value of
    ///     <paramref name="animation" />.
    /// </summary>
    public T Evaluate
    (
        IAnimation animation
    ) =>
        Lerp(animation.Value);
}

/// <summary>
///     Linearly interpolates between two <see cref="float" /> values.
/// </summary>
public class FloatTween : Tween<float>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public FloatTween
    (
        float begin,
        float end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override float Lerp
    (
        double t
    ) =>
        Begin + (float)((End - Begin) * t);
}

/// <summary>
///     Linearly interpolates between two <see cref="Vector4" /> values
///     component-wise.
/// </summary>
public class Vector4Tween : Tween<Vector4>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public Vector4Tween
    (
        Vector4 begin,
        Vector4 end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override Vector4 Lerp
    (
        double t
    )
    {
        var ft = (float)t;
        return Begin + (End - Begin) * ft;
    }
}

/// <summary>
///     Linearly interpolates between two RGBA colors represented as
///     <see cref="Vector4" /> values.
/// </summary>
public class ColorTween : Vector4Tween
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public ColorTween
    (
        Vector4 begin,
        Vector4 end
    ) : base
    (
        begin,
        end
    )
    {
    }
}

/// <summary>
///     Linearly interpolates between two <see cref="Vector2" /> offsets
///     component-wise.
/// </summary>
public class OffsetTween : Tween<Vector2>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public OffsetTween
    (
        Vector2 begin,
        Vector2 end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override Vector2 Lerp
    (
        double t
    )
    {
        var ft = (float)t;
        return Begin + (End - Begin) * ft;
    }
}

/// <summary>
///     Interpolates between two <see cref="Gradient" /> values. When both
///     endpoints share the same gradient type, delegates to
///     <see cref="Gradient.LerpTo" />; otherwise snaps at the midpoint.
/// </summary>
public class GradientTween : Tween<Gradient?>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public GradientTween
    (
        Gradient? begin,
        Gradient? end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override Gradient? Lerp
    (
        double t
    )
    {
        if (Begin == null && End == null)
        {
            return null;
        }

        if (Begin == null)
        {
            return End;
        }

        if (End == null)
        {
            return Begin;
        }

        if (Begin.GetType() != End.GetType())
        {
            return t < 0.5
                ? Begin
                : End;
        }

        return Begin.LerpTo
        (
            End,
            t
        );
    }
}

/// <summary>
///     Interpolates between two <see cref="EdgeInsets" /> values by linearly
///     interpolating each side (left, top, right, bottom) independently.
/// </summary>
public class EdgeInsetsTween : Tween<EdgeInsets>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public EdgeInsetsTween
    (
        EdgeInsets begin,
        EdgeInsets end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override EdgeInsets Lerp
    (
        double t
    )
    {
        var ft = (float)t;
        return new EdgeInsets
        (
            Begin.Left + (End.Left - Begin.Left) * ft,
            Begin.Top + (End.Top - Begin.Top) * ft,
            Begin.Right + (End.Right - Begin.Right) * ft,
            Begin.Bottom + (End.Bottom - Begin.Bottom) * ft
        );
    }
}

/// <summary>
///     Interpolates between two <see cref="Vector2" /> values representing sizes
///     by linearly interpolating width (X) and height (Y).
/// </summary>
public class SizeTween : Tween<Vector2>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public SizeTween
    (
        Vector2 begin,
        Vector2 end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override Vector2 Lerp
    (
        double t
    )
    {
        var ft = (float)t;
        return Begin + (End - Begin) * ft;
    }
}

/// <summary>
///     Interpolates between two <see cref="Border" /> values by linearly
///     interpolating each side's width and color independently.
/// </summary>
public class BorderTween : Tween<Border?>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public BorderTween
    (
        Border? begin,
        Border? end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override Border? Lerp
    (
        double t
    )
    {
        if (Begin == null && End == null)
        {
            return null;
        }

        var b = Begin ?? Border.Zero;
        var e = End ?? Border.Zero;
        var ft = (float)t;

        return new Border
        (
            LerpSide
            (
                b.Left,
                e.Left,
                ft
            ),
            LerpSide
            (
                b.Top,
                e.Top,
                ft
            ),
            LerpSide
            (
                b.Right,
                e.Right,
                ft
            ),
            LerpSide
            (
                b.Bottom,
                e.Bottom,
                ft
            )
        );
    }

    private static BorderSide LerpSide
    (
        BorderSide b,
        BorderSide e,
        float t
    ) =>
        new
        (
            b.Width + (e.Width - b.Width) * t,
            b.Color + (e.Color - b.Color) * t
        );
}

/// <summary>
///     Interpolates between two lists of <see cref="BoxShadow" /> values.
/// </summary>
public class BoxShadowListTween : Tween<BoxShadow[]?>
{
    /// <summary>
    ///     Creates a tween that interpolates from <paramref name="begin" /> to
    ///     <paramref name="end" />.
    /// </summary>
    public BoxShadowListTween
    (
        BoxShadow[]? begin,
        BoxShadow[]? end
    ) : base
    (
        begin,
        end
    )
    {
    }

    /// <inheritdoc />
    public override BoxShadow[]? Lerp
    (
        double t
    )
    {
        if (Begin == null && End == null)
        {
            return null;
        }

        var b = Begin ?? Array.Empty<BoxShadow>();
        var e = End ?? Array.Empty<BoxShadow>();
        var ft = (float)t;

        var maxLen = Math.Max
        (
            b.Length,
            e.Length
        );
        var result = new BoxShadow[maxLen];

        for (var i = 0; i < maxLen; i++)
        {
            if (i < b.Length && i < e.Length)
            {
                result[i] = BoxShadow.Lerp
                (
                    b[i],
                    e[i],
                    ft
                );
            }
            else if (i < b.Length)
            {
                var target = b[i] with
                {
                    Color = new Vector4
                    (
                        b[i].Color.X,
                        b[i].Color.Y,
                        b[i].Color.Z,
                        0
                    ),
                    Offset = Vector2.Zero,
                    BlurRadius = 0,
                    SpreadRadius = 0
                };
                result[i] = BoxShadow.Lerp
                (
                    b[i],
                    target,
                    ft
                );
            }
            else
            {
                var start = e[i] with
                {
                    Color = new Vector4
                    (
                        e[i].Color.X,
                        e[i].Color.Y,
                        e[i].Color.Z,
                        0
                    ),
                    Offset = Vector2.Zero,
                    BlurRadius = 0,
                    SpreadRadius = 0
                };
                result[i] = BoxShadow.Lerp
                (
                    start,
                    e[i],
                    ft
                );
            }
        }

        return result;
    }
}

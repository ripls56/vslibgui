using OpenTK.Mathematics;

namespace Gui.Widgets.Layout;

public class Alignment
{
    public static readonly Alignment TopLeft = new(
        -1,
        -1
    );

    public static readonly Alignment TopCenter = new(
        0,
        -1
    );

    public static readonly Alignment TopRight = new(
        1,
        -1
    );

    public static readonly Alignment CenterLeft = new(
        -1,
        0
    );

    public static readonly Alignment Center = new(
        0,
        0
    );

    public static readonly Alignment CenterRight = new(
        1,
        0
    );

    public static readonly Alignment BottomLeft = new(
        -1,
        1
    );

    public static readonly Alignment BottomCenter = new(
        0,
        1
    );

    public static readonly Alignment BottomRight = new(
        1,
        1
    );

    public Alignment(
        float x,
        float y
    )
    {
        X = x;
        Y = y;
    }

    public float X { get; }
    public float Y { get; }

    public Vector2 CalculateOffset(
        Vector2 parentSize,
        Vector2 childSize
    )
    {
        var centerX = (parentSize.X - childSize.X) / 2f;
        var centerY = (parentSize.Y - childSize.Y) / 2f;

        return new Vector2(
            centerX + X * centerX,
            centerY + Y * centerY
        );
    }
}

public enum MainAxisAlignment
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
    Stretch,

    /// <summary>
    ///     Aligns children along their text baseline. In a horizontal <see cref="Gui.Core.Layout.RenderFlex" />,
    ///     children are positioned so their alphabetic baselines are co-linear.
    ///     Falls back to <see cref="Start" /> in a vertical flex.
    /// </summary>
    Baseline
}

/// <summary>
///     Identifies the text baseline to use when aligning children with
///     <see cref="CrossAxisAlignment.Baseline" />.
/// </summary>
public enum TextBaseline
{
    /// <summary>
    ///     The alphabetic baseline — the line on which most Latin characters sit.
    ///     Corresponds to the negated <c>Ascent</c> from <c>SKFontMetrics</c>.
    /// </summary>
    Alphabetic,

    /// <summary>
    ///     The ideographic baseline — used for CJK characters.
    ///     Approximated as <c>CapHeight</c> from <c>SKFontMetrics</c>, or falls
    ///     back to <see cref="Alphabetic" /> when cap-height is unavailable.
    /// </summary>
    Ideographic
}

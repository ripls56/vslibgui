using OpenTK.Mathematics;

namespace Gui.Widgets.Painting;

/// <summary>
///     A single edge of a <see cref="Border" />: stroke width plus RGBA color.
///     A width of zero means the side is absent.
/// </summary>
public readonly record struct BorderSide
(
    float Width,
    Vector4 Color
)
{
    /// <summary>An absent side: zero width, fully transparent.</summary>
    public static readonly BorderSide None = new
    (
        0f,
        Vector4.Zero
    );
}

/// <summary>
///     Per-side box border. Each of the four sides carries its own
///     <see cref="BorderSide" /> (width + color), enabling asymmetric borders
///     such as underlines, separators, or tab indicators. When all four sides
///     are equal the border is painted through the cheaper single-stroke path.
/// </summary>
public readonly record struct Border
(
    BorderSide Left,
    BorderSide Top,
    BorderSide Right,
    BorderSide Bottom
)
{
    /// <summary>A border with no visible sides.</summary>
    public static readonly Border Zero = new
    (
        BorderSide.None,
        BorderSide.None,
        BorderSide.None,
        BorderSide.None
    );

    /// <summary>True when at least one side has a positive width.</summary>
    public bool AnyWidth =>
        Left.Width > 0f || Top.Width > 0f || Right.Width > 0f || Bottom.Width > 0f;

    /// <summary>
    ///     True when all four sides are identical (same width and color), which
    ///     lets the renderer use the single-stroke paint path.
    /// </summary>
    public bool IsUniform =>
        Left == Top && Top == Right && Right == Bottom;

    /// <summary>Uniform border where every side shares the same width and color.</summary>
    public static Border All
    (
        float width,
        Vector4 color
    ) =>
        new
        (
            new BorderSide(width, color),
            new BorderSide(width, color),
            new BorderSide(width, color),
            new BorderSide(width, color)
        );

    /// <summary>
    ///     Border with distinct vertical (top/bottom) and horizontal (left/right)
    ///     widths, all sharing one color.
    /// </summary>
    public static Border Symmetric
    (
        float vertical,
        float horizontal,
        Vector4 color
    ) =>
        new
        (
            new BorderSide(horizontal, color),
            new BorderSide(vertical, color),
            new BorderSide(horizontal, color),
            new BorderSide(vertical, color)
        );

    /// <summary>
    ///     Border built from individual sides. Any side omitted from the call
    ///     defaults to <see cref="BorderSide.None" /> (no border on that edge).
    /// </summary>
    public static Border Only
    (
        BorderSide left = default,
        BorderSide top = default,
        BorderSide right = default,
        BorderSide bottom = default
    ) =>
        new(left, top, right, bottom);
}

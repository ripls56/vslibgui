using SkiaSharp;

namespace Gui.Rendering.Text;

/// <summary>
///     Range of consecutive characters covered by a single <see cref="SKTypeface" />.
///     Produced by <see cref="FontRunSplitter" />.
/// </summary>
public readonly struct FontRun
{
    /// <summary>Index of the first character (UTF-16 code unit) in the source string.</summary>
    public int Start { get; }

    /// <summary>Number of UTF-16 code units in this run.</summary>
    public int Length { get; }

    /// <summary>Typeface covering every codepoint inside this run.</summary>
    public SKTypeface Typeface { get; }

    /// <summary>Creates a new font run.</summary>
    public FontRun(
        int start,
        int length,
        SKTypeface typeface
    )
    {
        Start = start;
        Length = length;
        Typeface = typeface;
    }
}

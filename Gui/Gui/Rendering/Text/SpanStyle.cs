using Gui.Widgets.Spans;
using OpenTK.Mathematics;

namespace Gui.Rendering.Text;

/// <summary>
///     Partial text style with nullable fields for style inheritance
///     in <see cref="TextSpan" /> trees. Non-null fields
///     override the parent style; null fields inherit from the parent.
/// </summary>
public struct SpanStyle
{
    /// <summary>Font family name (e.g. "sans-serif", "monospace").</summary>
    public string? FontFamily { get; set; }

    /// <summary>Font size in pixels.</summary>
    public float? FontSize { get; set; }

    /// <summary>Text color as RGBA.</summary>
    public Vector4? Color { get; set; }

    /// <summary>Font weight (Normal, Bold, Italic).</summary>
    public FontWeight? Weight { get; set; }

    /// <summary>Text alignment within the line.</summary>
    public TextAlignment? Align { get; set; }

    /// <summary>Text overflow behavior.</summary>
    public TextOverflow? Overflow { get; set; }

    /// <summary>MSDF outline width.</summary>
    public float? OutlineWidth { get; set; }

    /// <summary>MSDF outline color.</summary>
    public Vector4? OutlineColor { get; set; }

    /// <summary>MSDF glow width.</summary>
    public float? GlowWidth { get; set; }

    /// <summary>MSDF glow color.</summary>
    public Vector4? GlowColor { get; set; }

    /// <summary>MSDF boldness adjustment (-0.5 to 0.5).</summary>
    public float? Boldness { get; set; }

    /// <summary>Text decoration (none, underline).</summary>
    public TextDecoration? Decoration { get; set; }

    /// <summary>
    ///     Resolves this partial style against a fully specified parent
    ///     <see cref="TextStyle" />, producing a new resolved style.
    /// </summary>
    public TextStyle Resolve(
        TextStyle parent
    )
    {
        return new TextStyle
        {
            FontFamily = FontFamily ?? parent.FontFamily,
            FontSize = FontSize ?? parent.FontSize,
            Color = Color ?? parent.Color,
            Weight = Weight ?? parent.Weight,
            Align = Align ?? parent.Align,
            Overflow = Overflow ?? parent.Overflow,
            OutlineWidth = OutlineWidth ?? parent.OutlineWidth,
            OutlineColor = OutlineColor ?? parent.OutlineColor,
            GlowWidth = GlowWidth ?? parent.GlowWidth,
            GlowColor = GlowColor ?? parent.GlowColor,
            Boldness = Boldness ?? parent.Boldness,
            Decoration = Decoration ?? parent.Decoration,
            SoftWrap = parent.SoftWrap,
            MaxLines = parent.MaxLines
        };
    }
}

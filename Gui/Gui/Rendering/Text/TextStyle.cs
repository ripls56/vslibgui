using System;
using OpenTK.Mathematics;

namespace Gui.Rendering.Text;

/// <summary>Font weight variants for text rendering.</summary>
public enum FontWeight
{
    Normal,
    SemiBold,
    Bold,
    Italic
}

/// <summary>Horizontal alignment of text within its layout box.</summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>Controls how text is clipped when it overflows the available space.</summary>
public enum TextOverflow
{
    /// <summary>Text is clipped at the boundary.</summary>
    Clip,

    /// <summary>Overflowing text is replaced with an ellipsis (…).</summary>
    Ellipsis
}

/// <summary>Visual decorations applied to rendered text.</summary>
public enum TextDecoration
{
    None,
    Underline
}

/// <summary>
///     Immutable description of text appearance passed to <see cref="Gui.Widgets.RichText" />
///     and related text-rendering widgets.
/// </summary>
public struct TextStyle : IEquatable<TextStyle>
{
    /// <summary>Font family name (e.g. "sans-serif", "monospace").</summary>
    public string FontFamily { get; set; }

    /// <summary>Font size in logical pixels.</summary>
    public float FontSize { get; set; }

    /// <summary>Text color as RGBA (each channel 0–1).</summary>
    public Vector4 Color { get; set; }

    /// <summary>Font weight (normal, bold, italic).</summary>
    public FontWeight Weight { get; set; }

    /// <summary>Horizontal alignment within the layout box.</summary>
    public TextAlignment Align { get; set; }

    /// <summary>Overflow handling when text exceeds available space.</summary>
    public TextOverflow Overflow { get; set; }

    /// <summary>
    ///     Outline stroke width as a fraction of <see cref="FontSize" /> (the rendered
    ///     stroke width equals <c>OutlineWidth * FontSize</c>). The stroke is centred on
    ///     the glyph outline and drawn under the fill, so only its outer half is visible.
    ///     0 disables the outline.
    /// </summary>
    public float OutlineWidth { get; set; }

    /// <summary>Outline color as RGBA (each channel 0–1).</summary>
    public Vector4 OutlineColor { get; set; }

    /// <summary>
    ///     Glow radius as a fraction of <see cref="FontSize" /> (the blur radius equals
    ///     <c>GlowWidth * FontSize * 0.5</c>). 0 disables the glow.
    /// </summary>
    public float GlowWidth { get; set; }

    /// <summary>Glow color as RGBA (each channel 0–1).</summary>
    public Vector4 GlowColor { get; set; }

    /// <summary>
    ///     MSDF boldness bias in the range −0.5 to 0.5.
    ///     Positive values thicken glyphs; negative values thin them.
    /// </summary>
    public float Boldness { get; set; }

    /// <summary>
    ///     When <see langword="true" />, text wraps at word boundaries to fit the available width.
    /// </summary>
    public bool SoftWrap { get; set; }

    /// <summary>Text decoration (none, underline).</summary>
    public TextDecoration Decoration { get; set; }

    /// <summary>
    ///     Maximum number of lines to display. 0 means unlimited.
    ///     When exceeded, text is truncated according to <see cref="Overflow" />.
    /// </summary>
    public int MaxLines { get; set; }

    /// <summary>Creates a <see cref="TextStyle" /> with sensible defaults.</summary>
    public TextStyle()
    {
        FontFamily = "sans-serif";
        FontSize = 14;
        Color = Vector4.One;
        Weight = FontWeight.Normal;
        Align = TextAlignment.Left;
        Overflow = TextOverflow.Clip;
        OutlineWidth = 0;
        OutlineColor = Vector4.Zero;
        GlowWidth = 0;
        GlowColor = Vector4.Zero;
        Boldness = 0;
        SoftWrap = true;
        Decoration = TextDecoration.None;
        MaxLines = 0;
    }

    /// <inheritdoc />
    public bool Equals(TextStyle other)
    {
        return FontFamily == other.FontFamily &&
               FontSize.Equals(other.FontSize) &&
               Color.Equals(other.Color) &&
               Weight == other.Weight &&
               Align == other.Align &&
               Overflow == other.Overflow &&
               OutlineWidth.Equals(other.OutlineWidth) &&
               OutlineColor.Equals(other.OutlineColor) &&
               GlowWidth.Equals(other.GlowWidth) &&
               GlowColor.Equals(other.GlowColor) &&
               Boldness.Equals(other.Boldness) &&
               SoftWrap == other.SoftWrap &&
               Decoration == other.Decoration &&
               MaxLines == other.MaxLines;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TextStyle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(FontFamily);
        hashCode.Add(FontSize);
        hashCode.Add(Color);
        hashCode.Add(Weight);
        hashCode.Add(Align);
        hashCode.Add(Overflow);
        hashCode.Add(OutlineWidth);
        hashCode.Add(OutlineColor);
        hashCode.Add(GlowWidth);
        hashCode.Add(GlowColor);
        hashCode.Add(Boldness);
        hashCode.Add(SoftWrap);
        hashCode.Add(Decoration);
        hashCode.Add(MaxLines);
        return hashCode.ToHashCode();
    }

    /// <summary>Returns <see langword="true" /> if both styles are equal.</summary>
    public static bool operator ==(TextStyle left, TextStyle right) => left.Equals(right);

    /// <summary>Returns <see langword="true" /> if the styles differ.</summary>
    public static bool operator !=(TextStyle left, TextStyle right) => !left.Equals(right);
}

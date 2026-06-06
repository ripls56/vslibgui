using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Spans;

/// <summary>
///     Vertical alignment of an inline widget relative to the surrounding text.
/// </summary>
public enum PlaceholderAlignment
{
    /// <summary>Aligns the widget bottom with the text baseline.</summary>
    Baseline,

    /// <summary>Centers the widget vertically within the line.</summary>
    Middle,

    /// <summary>Aligns the widget bottom with the ascent of surrounding text.</summary>
    AboveBaseline
}

/// <summary>
///     An <see cref="InlineSpan" /> that embeds an arbitrary widget inline
///     within a <see cref="RichText" /> span tree. Optionally floats the
///     widget left or right so text wraps around it.
/// </summary>
public sealed class WidgetSpan : InlineSpan
{
    /// <summary>
    ///     Creates a new <see cref="WidgetSpan" />.
    /// </summary>
    /// <param name="child">The widget to embed.</param>
    /// <param name="alignment">Vertical alignment within the line.</param>
    /// <param name="float">Float behavior for this widget.</param>
    public WidgetSpan(
        Widget child,
        PlaceholderAlignment alignment = PlaceholderAlignment.Middle,
        VtmlFloat @float = VtmlFloat.None
    )
    {
        Child = child;
        Alignment = alignment;
        Float = @float == VtmlFloat.None
            ? VtmlFloat.Inline
            : @float;
    }

    /// <summary>The widget to embed inline.</summary>
    public Widget Child { get; }

    /// <summary>Vertical alignment relative to surrounding text.</summary>
    public PlaceholderAlignment Alignment { get; }

    /// <summary>Float behavior: Inline for inline, Left/Right for float.</summary>
    public VtmlFloat Float { get; }

    /// <inheritdoc />
    internal override void CollectRuns(
        TextStyle inherited,
        List<PlacedRun> runs
    )
    {
        runs.Add(
            new PlacedRun
            {
                Text = "",
                Style = inherited,
                Source = this,
                Type = PlacedRunType.Widget,
                WidgetChildIndex = -1,
                Float = Float
            }
        );
    }

    /// <inheritdoc />
    internal override bool HasAnyRecognizer() => false;
}

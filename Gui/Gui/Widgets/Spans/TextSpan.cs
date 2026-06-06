using System.Collections.Generic;
using Gui.Rendering.Text;

namespace Gui.Widgets.Spans;

/// <summary>
///     An <see cref="InlineSpan" /> that represents styled text with optional
///     children and a gesture recognizer. Supports hierarchical style
///     inheritance: child spans inherit unset fields from their parent.
/// </summary>
public sealed class TextSpan : InlineSpan
{
    /// <summary>
    ///     Creates a new <see cref="TextSpan" />.
    /// </summary>
    /// <param name="text">Text content. Null if only children are used.</param>
    /// <param name="style">Partial style overrides.</param>
    /// <param name="children">Child inline spans.</param>
    /// <param name="recognizer">Gesture recognizer for tap handling.</param>
    public TextSpan(
        string? text = null,
        SpanStyle? style = null,
        IReadOnlyList<InlineSpan>? children = null,
        GestureRecognizer? recognizer = null
    )
    {
        Text = text;
        Style = style;
        Children = children;
        Recognizer = recognizer;
    }

    /// <summary>Text content of this span. May be null if only children are used.</summary>
    public string? Text { get; }

    /// <summary>
    ///     Partial style applied to this span. Non-null fields override
    ///     the inherited parent style; null fields are inherited.
    /// </summary>
    public SpanStyle? Style { get; }

    /// <summary>Child spans that inherit this span's resolved style.</summary>
    public IReadOnlyList<InlineSpan>? Children { get; }

    /// <summary>
    ///     Gesture recognizer for this span's hit region.
    ///     When set, taps on this span's text trigger the recognizer.
    /// </summary>
    public GestureRecognizer? Recognizer { get; }

    /// <inheritdoc />
    internal override void CollectRuns(
        TextStyle inherited,
        List<PlacedRun> runs
    )
    {
        var resolved = Style?.Resolve(inherited) ?? inherited;

        if (Text != null)
        {
            runs.Add(
                new PlacedRun { Text = Text, Style = resolved, Source = this }
            );
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.CollectRuns(
                    resolved,
                    runs
                );
            }
        }
    }

    /// <inheritdoc />
    internal override bool HasAnyRecognizer()
    {
        if (Recognizer != null)
        {
            return true;
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                if (child.HasAnyRecognizer())
                {
                    return true;
                }
            }
        }

        return false;
    }
}

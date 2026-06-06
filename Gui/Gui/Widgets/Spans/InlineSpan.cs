using System;
using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Spans;

/// <summary>
///     Base class for inline content within a <see cref="RichText" /> span tree.
/// </summary>
public abstract class InlineSpan
{
    /// <summary>
    ///     Collects flattened text runs from this span and its children,
    ///     resolving styles against the <paramref name="inherited" /> parent style.
    /// </summary>
    internal abstract void CollectRuns(
        TextStyle inherited,
        List<PlacedRun> runs
    );

    /// <summary>
    ///     Returns <c>true</c> if this span or any descendant has a
    ///     non-null <see cref="GestureRecognizer" />.
    /// </summary>
    internal abstract bool HasAnyRecognizer();
}

/// <summary>
///     Distinguishes text runs from widget placeholders and control runs.
/// </summary>
internal enum PlacedRunType
{
    /// <summary>A styled text run.</summary>
    Text,

    /// <summary>A placeholder for a widget child.</summary>
    Widget,

    /// <summary>A clear-float directive.</summary>
    Clear
}

/// <summary>
///     A flattened, fully-resolved text run produced by
///     <see cref="InlineSpan.CollectRuns" />.
/// </summary>
internal struct PlacedRun
{
    /// <summary>Text content of this run.</summary>
    public string Text;

    /// <summary>Fully resolved style for rendering.</summary>
    public TextStyle Style;

    /// <summary>Source span for hit-test → recognizer lookup.</summary>
    public InlineSpan Source;

    /// <summary>Layout bounds, populated during layout pass.</summary>
    public float X, Y, Width, Height;

    /// <summary>Type of this run: text, widget placeholder, or clear directive.</summary>
    public PlacedRunType Type;

    /// <summary>Index into RenderRichText's widget children list. -1 for non-widget runs.</summary>
    public int WidgetChildIndex;

    /// <summary>Float behavior for widget runs.</summary>
    public VtmlFloat Float;
}

/// <summary>
///     Abstract base class for gesture recognizers attached to
///     <see cref="TextSpan" /> instances.
/// </summary>
public abstract class GestureRecognizer : IDisposable
{
    /// <summary>Releases resources held by this recognizer.</summary>
    public abstract void Dispose();
}

/// <summary>
///     Recognizer that fires a callback when the span is tapped.
/// </summary>
public sealed class TapGestureRecognizer : GestureRecognizer
{
    /// <summary>Callback invoked when the span is tapped.</summary>
    public Action? OnTap { get; set; }

    /// <inheritdoc />
    public override void Dispose() => OnTap = null;
}

/// <summary>
///     Walks an <see cref="InlineSpan" /> tree and collects all
///     <see cref="WidgetSpan.Child" /> widgets in tree order.
/// </summary>
internal static class InlineSpanHelper
{
    /// <summary>
    ///     Collects all widget children from WidgetSpan nodes in the span tree.
    /// </summary>
    public static List<Widget> CollectWidgetChildren(
        InlineSpan? root
    )
    {
        var result = new List<Widget>();
        Collect(
            root,
            result
        );
        return result;
    }

    private static void Collect(
        InlineSpan? span,
        List<Widget> result
    )
    {
        switch (span)
        {
            case WidgetSpan ws:
                result.Add(ws.Child);
                return;
            case TextSpan ts when ts.Children != null:
                foreach (var child in ts.Children)
                {
                    Collect(
                        child,
                        result
                    );
                }

                break;
        }
    }
}

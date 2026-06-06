using System;
using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Rendering.Text;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that displays a tree of styled <see cref="InlineSpan" />
///     content with support for gesture recognizers on individual spans.
/// </summary>
public class RichText : StatefulWidget
{
    /// <summary>
    ///     Creates a new <see cref="RichText" /> widget.
    /// </summary>
    /// <param name="span">Root inline span tree.</param>
    /// <param name="defaultStyle">Default text style. If null, uses Library default.</param>
    /// <param name="maxLines">Maximum lines to display. 0 = unlimited.</param>
    /// <param name="overflow">Overflow behavior for truncated text.</param>
    /// <param name="key">Optional widget key for reconciliation.</param>
    public RichText(
        InlineSpan span,
        TextStyle? defaultStyle = null,
        int maxLines = 0,
        TextOverflow overflow = TextOverflow.Clip,
        Framework.Key? key = null
    ) : base(key)
    {
        Span = span;
        DefaultStyle = defaultStyle ?? new TextStyle();
        MaxLines = maxLines;
        Overflow = overflow;
    }

    /// <summary>Root inline span tree to render.</summary>
    public InlineSpan Span { get; }

    /// <summary>
    ///     Default text style applied to spans without explicit style.
    /// </summary>
    public TextStyle DefaultStyle { get; }

    /// <summary>
    ///     Maximum number of visual lines to display. 0 means unlimited.
    /// </summary>
    public int MaxLines { get; }

    /// <summary>
    ///     Overflow behavior when MaxLines is exceeded.
    /// </summary>
    public TextOverflow Overflow { get; }

    /// <inheritdoc />
    public override State CreateState() => new RichTextState();
}

internal class RichTextState
    : State<RichText>, IPointerClickHandler, ISelectiveEventHandler
{
    private bool _hasRecognizers;

    /// <inheritdoc />
    public void OnPointerClick(
        PointerEvent e
    )
    {
        if (!_hasRecognizers)
        {
            return;
        }

        var ro = Element.RenderObject as RenderRichText;
        if (ro == null)
        {
            return;
        }

        var local = ro.GlobalToLocal(
            new Vector2(
                e.X,
                e.Y
            )
        );
        var hit = ro.HitTestRun(
            local.X,
            local.Y
        );
        if (hit is TextSpan { Recognizer: TapGestureRecognizer tap })
        {
            tap.OnTap?.Invoke();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    public bool HandlesEvent(
        Type eventInterface
    )
    {
        return eventInterface == typeof(IPointerClickHandler)
               && _hasRecognizers;
    }

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        _hasRecognizers = Widget.Span.HasAnyRecognizer();
    }

    /// <inheritdoc />
    public override void UpdateWidget(
        RichText old
    )
    {
        base.UpdateWidget(old);
        _hasRecognizers = Widget.Span.HasAnyRecognizer();
    }

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        return new RichTextRenderWidget(
            Widget.Span,
            Widget.DefaultStyle,
            Widget.MaxLines,
            Widget.Overflow
        );
    }
}

/// <summary>
///     Internal <see cref="MultiChildWidget" /> that creates and updates
///     the <see cref="RenderRichText" /> render object. Collects
///     <see cref="WidgetSpan.Child" /> widgets as children.
/// </summary>
internal class RichTextRenderWidget : MultiChildWidget
{
    /// <summary>Creates a new render widget for the given span tree.</summary>
    public RichTextRenderWidget(
        InlineSpan span,
        TextStyle defaultStyle,
        int maxLines = 0,
        TextOverflow overflow = TextOverflow.Clip
    )
        : base(InlineSpanHelper.CollectWidgetChildren(span))
    {
        Span = span;
        DefaultStyle = defaultStyle;
        MaxLines = maxLines;
        Overflow = overflow;
    }

    /// <summary>Root inline span tree.</summary>
    public InlineSpan Span { get; }

    /// <summary>Default text style for unstyled spans.</summary>
    public TextStyle DefaultStyle { get; }

    /// <summary>Maximum lines to display. 0 = unlimited.</summary>
    public int MaxLines { get; }

    /// <summary>Overflow behavior for truncated text.</summary>
    public TextOverflow Overflow { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject() => new RenderRichText();

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        base.UpdateRenderObject(renderObject);
        var ro = (RenderRichText)renderObject;
        ro.DefaultStyle = DefaultStyle;
        ro.Root = Span;
        ro.MaxLines = MaxLines;
        ro.Overflow = Overflow;
    }
}

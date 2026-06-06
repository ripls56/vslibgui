using System;
using System.Collections.Generic;
using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Framework;
using Gui.Widgets.Spans;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that parses and renders VTML (Vintage Story Text Markup Language)
///     content. Supports styled text, links, icons, item stacks (with float),
///     hotkeys, and line breaks.
/// </summary>
/// <remarks>
///     Parses VTML via <see cref="VtmlConverter" />, builds an
///     <see cref="InlineSpan" /> tree via <see cref="VtmlSpanBuilder" />,
///     and delegates rendering to <see cref="RichText" />.
/// </remarks>
public class VtmlText : StatefulWidget
{
    /// <summary>
    ///     Creates a new VTML text widget.
    /// </summary>
    /// <param name="vtml">The VTML markup string to render.</param>
    /// <param name="baseStyle">
    ///     Base text style. If null, uses default 14px sans-serif white.
    /// </param>
    /// <param name="onLinkClick">Optional link click handler.</param>
    /// <param name="maxLines">
    ///     Maximum number of lines to display. 0 means unlimited.
    /// </param>
    /// <param name="overflow">Overflow behavior when maxLines is exceeded.</param>
    /// <param name="key">Optional widget key for reconciliation.</param>
    public VtmlText(
        string vtml,
        TextStyle? baseStyle = null,
        Action<string>? onLinkClick = null,
        int maxLines = 0,
        TextOverflow overflow = TextOverflow.Clip,
        Framework.Key? key = null
    ) : base(key)
    {
        Vtml = vtml;
        BaseStyle = baseStyle ?? new TextStyle();
        OnLinkClick = onLinkClick;
        MaxLines = maxLines;
        Overflow = overflow;
    }

    /// <summary>Raw VTML markup string.</summary>
    public string Vtml { get; }

    /// <summary>Base text style for unstyled content.</summary>
    public TextStyle BaseStyle { get; }

    /// <summary>
    ///     Callback invoked when a link is clicked.
    ///     The argument is the href string from the &lt;a&gt; tag.
    /// </summary>
    public Action<string>? OnLinkClick { get; }

    /// <summary>
    ///     Maximum number of visual lines to display. 0 means unlimited.
    /// </summary>
    public int MaxLines { get; }

    /// <summary>
    ///     Overflow behavior when <see cref="MaxLines" /> is exceeded.
    /// </summary>
    public TextOverflow Overflow { get; }

    /// <inheritdoc />
    public override State CreateState() => new VtmlTextState();
}

/// <summary>
///     State for <see cref="VtmlText" />. Handles VTML parsing, caching,
///     and span tree construction.
/// </summary>
public class VtmlTextState : State<VtmlText>
{
    private List<VtmlInlineElement>? _cachedElements;
    private string? _cachedVtml;

    /// <inheritdoc />
    public override Widget Build(
        BuildContext context
    )
    {
        if (_cachedElements == null || _cachedVtml != Widget.Vtml)
        {
            _cachedElements = VtmlConverter.Convert(
                Widget.Vtml,
                Widget.BaseStyle
            );
            _cachedVtml = Widget.Vtml;
        }

        var span = VtmlSpanBuilder.Build(
            _cachedElements,
            Widget.BaseStyle,
            Widget.OnLinkClick
        );

        return new RichText(
            span,
            Widget.BaseStyle,
            Widget.MaxLines,
            Widget.Overflow
        );
    }
}

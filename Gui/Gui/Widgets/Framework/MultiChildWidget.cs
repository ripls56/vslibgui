using System;
using System.Collections.Generic;
using Gui.Core.Framework;

namespace Gui.Widgets.Framework;

/// <summary>
///     A <see cref="RenderObjectWidget" /> that owns zero or more ordered child widgets.
///     The Library keeps the children's <see cref="RenderObject" /> list in sync with
///     the widget list via <c>MultiChildElement</c>. Children are reconciled by position;
///     use <see cref="Key" /> on children that may be reordered.
/// </summary>
public abstract class MultiChildWidget : RenderObjectWidget, IMultiChildWidget
{
    /// <summary>Ordered list of child widgets.</summary>
    public readonly IReadOnlyList<Widget> Children;

    protected MultiChildWidget(
        IEnumerable<Widget>? children = null,
        Key? key = null
    ) : base(key)
    {
        Children = children != null
            ? new List<Widget>(children)
            : Array.Empty<Widget>();
    }

    IReadOnlyList<Widget> IMultiChildWidget.Children => Children;

    public override void Dispose()
    {
        foreach (var child in Children)
        {
            child.Dispose();
        }

        base.Dispose();
    }

    public override Element CreateElement() => new MultiChildElement(this);
}

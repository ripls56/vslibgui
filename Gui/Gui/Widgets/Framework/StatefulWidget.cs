using System;
using Gui.Core.Framework;

namespace Gui.Widgets.Framework;

/// <summary>
///     A widget that has mutable state. The Library calls <see cref="CreateState" /> once
///     when the widget first appears in the tree; the returned <see cref="State" /> object
///     persists across rebuilds for as long as the element lives.
///     <para>
///         Use <see cref="State{T}.SetState" /> inside the state to trigger rebuilds when data
///         changes. The widget itself is immutable — only the state holds mutable fields.
///     </para>
/// </summary>
public abstract class StatefulWidget : Widget
{
    protected StatefulWidget(
        Key? key = null
    ) : base(key)
    {
    }

    /// <summary>
    ///     Factory method called once by the Library when this widget's element is first
    ///     mounted. The returned <see cref="State" /> object is owned by the element and
    ///     persists until the element is unmounted.
    /// </summary>
    public abstract State CreateState();

    public override Element CreateElement() => new StatefulElement(this);
}

/// <summary>
///     The mutable half of a <see cref="StatefulWidget" /> pair. A <c>State</c> object is
///     created once (via <see cref="StatefulWidget.CreateState" />) and lives until its element
///     is unmounted. The Library calls lifecycle methods in this order:
///     <list type="number">
///         <item><see cref="InitState" /> — element mounted, resources may be allocated</item>
///         <item><see cref="Build" /> — called on mount and after every <see cref="SetState" /></item>
///         <item><see cref="UpdateWidget" /> — parent passed a new widget instance</item>
///         <item><see cref="Dispose" /> — element unmounted, release all resources</item>
///     </list>
/// </summary>
public abstract class State
{
    /// <summary>The current widget configuration. Replaced by the Library on parent rebuild.</summary>
    public StatefulWidget Widget { get; internal set; } = null!;

    /// <summary>The element that owns this state object.</summary>
    public StatefulElement Element { get; internal set; } = null!;

    /// <summary>
    ///     Describes the UI for this state. Called by the Library on mount and after every
    ///     <see cref="SetState" /> call. Must be a pure function of this state's fields and
    ///     <see cref="Widget" />'s properties.
    /// </summary>
    public abstract Widget Build(
        BuildContext context
    );

    /// <summary>
    ///     Executes <paramref name="fn" /> to mutate state fields, then schedules a rebuild of
    ///     this widget's subtree. Only the element that owns this state and its descendants
    ///     are rebuilt — ancestors are unaffected.
    /// </summary>
    protected void SetState(
        Action fn
    )
    {
        fn();
        Element.MarkNeedsBuild();
    }

    /// <summary>
    ///     Called once, immediately after the element is mounted. Use this to initialize
    ///     resources such as <c>AnimationController</c> or event listeners. At this point
    ///     <see cref="Element" /> and its <c>Owner</c> are fully set up.
    /// </summary>
    public virtual void InitState()
    {
    }

    /// <summary>
    ///     Called when the parent rebuilds and passes a new widget instance of the same type.
    ///     <see cref="Widget" /> already holds the new widget when this is called;
    ///     <paramref name="oldWidget" /> is the previous one. Call <see cref="SetState" /> here
    ///     if a property change requires a rebuild.
    /// </summary>
    public virtual void UpdateWidget(
        StatefulWidget oldWidget
    )
    {
    }

    /// <summary>
    ///     Called when a dependency of this state (an <see cref="InheritedWidget" /> that was
    ///     looked up via <c>DependOnInheritedWidgetOfExactType</c>) changes. Override this
    ///     to react to inherited data changes without calling <see cref="SetState" />.
    /// </summary>
    public virtual void DidChangeDependencies()
    {
    }

    /// <summary>
    ///     Called when the element is removed from the tree. Release all resources allocated
    ///     in <see cref="InitState" /> here (e.g. dispose <c>AnimationController</c>s, remove
    ///     listeners from <c>ChangeNotifier</c>s).
    /// </summary>
    public virtual void Dispose()
    {
    }
}

/// <summary>
///     Strongly-typed variant of <see cref="State" /> for a specific <typeparamref name="T" />
///     widget type. Provides a typed <see cref="Widget" /> property.
/// </summary>
public abstract class State<T> : State where T : StatefulWidget
{
    /// <summary>The current widget, strongly typed as <typeparamref name="T" />.</summary>
    public new T Widget => (T)base.Widget;

    public override void UpdateWidget(
        StatefulWidget oldWidget
    ) =>
        UpdateWidget((T)oldWidget);

    /// <summary>
    ///     Typed override of <see cref="State.UpdateWidget" />. Called when the parent passes
    ///     a new <typeparamref name="T" /> widget. The current <see cref="Widget" /> is already
    ///     the new one when this is called.
    /// </summary>
    public virtual void UpdateWidget(
        T oldWidget
    )
    {
    }
}

public class StatefulElement : Element
{
    private Element? _child;

    public StatefulElement(
        StatefulWidget widget
    ) : base(widget)
    {
        State = widget.CreateState();
        State.Widget = widget;
        State.Element = this;
    }

    public State State { get; }
    public override RenderObject? RenderObject => _child?.RenderObject;

    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        State.InitState();
        Rebuild();
    }

    internal override void DidChangeDependencies()
    {
        State.DidChangeDependencies();
        MarkNeedsBuild();
    }

    public override void Rebuild()
    {
        base.Rebuild(); // Clear dirty flag
        var oldRo = _child?.RenderObject;
        var builtWidget = State.Build(
            new BuildContext(
                Widget,
                this
            )
        );
        _child = UpdateChild(
            _child,
            builtWidget
        );

        // When the child's RenderObject changes type, the old RO is
        // removed and the new one appended to the ancestor's children
        // list, breaking the expected order.  Ask the nearest ancestor
        // MultiChildElement to re-sync.
        if (oldRo != _child?.RenderObject)
        {
            (FindAncestorRenderObjectElement() as MultiChildElement)
                ?.ReorderChildRenderObjects();
        }
    }

    public override void Update(
        Widget newWidget
    )
    {
        if (ReferenceEquals(
                Widget,
                newWidget
            ))
        {
            return;
        }

        var oldWidget = (StatefulWidget)Widget;
        base.Update(newWidget);
        State.Widget = (StatefulWidget)newWidget;
        State.UpdateWidget(oldWidget);
        // Schedule via BuildOwner so the dirty pass handles deduplication.
        MarkNeedsBuild();
    }

    public override void Unmount()
    {
        _child?.Unmount();
        _child = null;
        State.Dispose();
        base.Unmount();
    }

    public override void VisitChildren(
        Action<Element> visitor
    )
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }
}

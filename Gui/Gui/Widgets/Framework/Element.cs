using System;
using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Widgets.Framework;

public abstract class Element : IRebuildable
{
    private HashSet<InheritedElement>? _dependencies;

    internal Dictionary<Type, InheritedElement>? InheritedWidgets;

    protected Element(
        Widget widget
    )
    {
        Widget = widget;
    }

    public Widget Widget { get; protected set; }
    public Element? Parent { get; private set; }
    public BuildOwner? Owner { get; private set; }
    public int Depth { get; private set; }
    public bool IsDirty { get; private set; } = true;

    public abstract RenderObject? RenderObject { get; }

    public virtual void Rebuild() => IsDirty = false;

    public virtual void Mount(
        Element? parent
    )
    {
        Parent = parent;
        Depth = parent != null
            ? parent.Depth + 1
            : 0;
        if (parent != null)
        {
            Owner = parent.Owner;
        }

        _UpdateInheritedMap();

        // Auto-link FocusNode to element
        if (Widget is IFocusable focusable && focusable.FocusNode != null)
        {
            focusable.FocusNode.Owner = this;
        }

        if (Widget.Key is GlobalKey gk && Owner != null)
        {
            Owner.RegisterGlobalKey(
                gk,
                this
            );
        }

        Owner?.OnElementMounted?.Invoke();
    }

    public void AssignOwner(
        BuildOwner owner
    ) =>
        Owner = owner;

    public virtual void MarkNeedsBuild()
    {
        if (IsDirty)
        {
            return;
        }

        IsDirty = true;
        Owner?.ScheduleBuildFor(this);
    }

    public virtual void Update(
        Widget newWidget
    )
    {
        Widget = newWidget;
        if (Widget is IFocusable focusable && focusable.FocusNode != null)
        {
            focusable.FocusNode.Owner = this;
        }
    }

    public virtual void Unmount()
    {
        if (_dependencies != null)
        {
            foreach (var inherited in _dependencies)
            {
                inherited.RemoveDependent(this);
            }

            _dependencies = null;
        }

        if (Widget is IFocusable focusable && focusable.FocusNode != null
                                           && focusable.FocusNode.Owner == this)
        {
            focusable.FocusNode.Owner = null;
        }

        if (Widget.Key is GlobalKey gk && Owner != null)
        {
            Owner.UnregisterGlobalKey(gk);
        }

        Owner?.OnElementUnmounted?.Invoke();
        Parent = null;
        Owner = null;
    }

    internal void _UpdateInheritedMap()
    {
        InheritedWidgets = Parent?.InheritedWidgets != null
            ? new Dictionary<Type, InheritedElement>(Parent.InheritedWidgets)
            : null;
    }

    /// <summary>
    ///     Looks up the nearest <see cref="InheritedElement" /> whose widget is
    ///     exactly <typeparamref name="T" /> and registers this element as a
    ///     dependent so it is rebuilt when that inherited data changes.
    /// </summary>
    public T? DependOnInheritedWidgetOfExactType<T>() where T : InheritedWidget
    {
        var inherited = _GetInheritedElement<T>();
        if (inherited == null)
        {
            return null;
        }

        _dependencies ??= new HashSet<InheritedElement>();
        _dependencies.Add(inherited);
        inherited.AddDependent(this);
        return (T)inherited.Widget;
    }

    /// <summary>
    ///     Looks up the nearest inherited widget of type <typeparamref name="T" />
    ///     without registering a dependency (no auto-rebuild).
    /// </summary>
    public T? GetInheritedWidgetOfExactType<T>() where T : InheritedWidget
    {
        var inherited = _GetInheritedElement<T>();
        return (T)inherited?.Widget;
    }

    private InheritedElement? _GetInheritedElement<T>() where T : InheritedWidget
    {
        if (InheritedWidgets == null)
        {
            return null;
        }

        InheritedWidgets.TryGetValue(
            typeof(T),
            out var result
        );
        return result;
    }

    internal virtual void DidChangeDependencies()
    {
    }

    /// <summary>
    ///     Reconciles a child element with a new widget. Applies registered
    ///     <see cref="WidgetTransformerRegistry" /> transformers for keyed widgets before
    ///     inflation, then reuses, updates, or replaces the existing element as needed.
    /// </summary>
    /// <param name="child">Existing child element, or <c>null</c> for first mount.</param>
    /// <param name="newWidget">Widget to reconcile; pass <c>null</c> to unmount the child.</param>
    /// <returns>The reconciled element, or <c>null</c> if <paramref name="newWidget" /> is <c>null</c>.</returns>
    internal Element? UpdateChild(
        Element? child,
        Widget? newWidget
    )
    {
        if (newWidget == null)
        {
            child?.Unmount();
            return null;
        }

        newWidget = WidgetTransformerRegistry.Apply(newWidget);

        if (child != null)
        {
            if (ReferenceEquals(
                    child.Widget,
                    newWidget
                ))
            {
                return child;
            }

            if (Widget.CanUpdate(
                    child.Widget,
                    newWidget
                ))
            {
                child.Update(newWidget);
                return child;
            }

            child.Unmount();
        }

        var newChild = newWidget.CreateElement();
        newChild.AssignOwner(Owner!);
        newChild.Mount(this);
        return newChild;
    }

    /// <summary>
    ///     Walks up the parent chain to find the nearest ancestor that is a
    ///     <see cref="RenderObjectElement" />, i.e. an element that owns a
    ///     <see cref="Core.Framework.RenderObject" />.
    /// </summary>
    internal RenderObjectElement? FindAncestorRenderObjectElement()
    {
        var ancestor = Parent;
        while (ancestor != null && ancestor is not RenderObjectElement)
        {
            ancestor = ancestor.Parent;
        }

        return ancestor as RenderObjectElement;
    }

    public abstract void VisitChildren(
        Action<Element> visitor
    );

    public virtual bool HitTest(
        HitTestResult result,
        Vector2 position
    )
    {
        var ro = RenderObject;
        if (ro == null)
        {
            return false;
        }

        var isInside = position.X >= 0 && position.X <= ro.Size.X &&
                       position.Y >= 0 && position.Y <= ro.Size.Y;

        if (!isInside)
        {
            return false;
        }

        var hitChild = false;
        VisitChildrenInReverse(child =>
            {
                if (hitChild)
                {
                    return;
                }

                var childRo = child.RenderObject;
                if (childRo != null)
                {
                    // Skip transform when child element shares our RO (component elements
                    // delegate RenderObject to a descendant — same coordinate space).
                    var childPos = childRo == ro
                        ? position
                        : ro.GlobalToChild(childRo, position);
                    if (child.HitTest(
                            result,
                            childPos
                        ))
                    {
                        hitChild = true;
                    }
                }
            }
        );

        var isInteractive = IsActivelyInteractive(
            Widget,
            this is StatefulElement se
                ? se.State
                : null
        );

        if (hitChild || isInteractive || ro.HitTest(
                result,
                position,
                this
            ))
        {
            result.Add(this);
            return true;
        }

        return false;
    }

    protected virtual void VisitChildrenInReverse(
        Action<Element> visitor
    ) =>
        VisitChildren(visitor);

    private static bool IsActivelyInteractive(
        Widget widget,
        object? state
    )
    {
        return EventCheckHelper.IsInteractive(widget) ||
               (state != null &&
                EventCheckHelper.IsInteractive(state));
    }
}

public interface IFocusable
{
    FocusNode? FocusNode { get; }
}

public class RenderObjectElement : Element
{
    private RenderObject? _renderObject;

    public RenderObjectElement(
        Widget widget
    ) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _renderObject;

    /// <summary>
    ///     Inserts a child <see cref="Core.Framework.RenderObject" /> into this element's
    ///     render object. Called automatically by <see cref="AttachRenderObject" />
    ///     when a descendant <see cref="RenderObjectElement" /> is mounted.
    /// </summary>
    internal virtual void InsertChildRenderObject(
        RenderObject child
    ) =>
        RenderObject?.AddChild(child);

    /// <summary>
    ///     Removes a child <see cref="Core.Framework.RenderObject" /> from this element's
    ///     render object. Called automatically by <see cref="DetachRenderObject" />
    ///     when a descendant <see cref="RenderObjectElement" /> is unmounted.
    /// </summary>
    internal virtual void RemoveChildRenderObject(
        RenderObject child
    ) =>
        RenderObject?.RemoveChild(child);

    private void AttachRenderObject()
    {
        var ancestor = FindAncestorRenderObjectElement();
        ancestor?.InsertChildRenderObject(RenderObject!);
    }

    private void DetachRenderObject()
    {
        if (RenderObject?.Parent != null)
        {
            var ancestor = FindAncestorRenderObjectElement();
            ancestor?.RemoveChildRenderObject(RenderObject!);
        }
    }

    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        _renderObject = Widget.CreateRenderObject();
        UpdateRenderObject();
        AttachRenderObject();
    }

    public override void Update(
        Widget newWidget
    )
    {
        base.Update(newWidget);
        UpdateRenderObject();
    }

    protected virtual void UpdateRenderObject()
    {
        if (_renderObject != null)
        {
            Widget.UpdateRenderObject(_renderObject);
        }
    }

    public override void Unmount()
    {
        DetachRenderObject();
        _renderObject?.Dispose();
        _renderObject = null;
        base.Unmount();
    }

    public override void VisitChildren(
        Action<Element> visitor
    )
    {
    }
}

public class ComponentElement : Element
{
    private Element? _child;

    public ComponentElement(
        Widget widget
    ) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    protected virtual void OnMountBeforeRebuild()
    {
    }

    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        OnMountBeforeRebuild();
        Rebuild();
    }

    public override void Rebuild()
    {
        base.Rebuild();
        var oldRo = _child?.RenderObject;
        var statelessWidget = (StatelessWidget)Widget;
        var builtWidget = statelessWidget.Build(
            new BuildContext(
                Widget,
                this
            )
        );
        _child = UpdateChild(
            _child,
            builtWidget
        );

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

        base.Update(newWidget);
        // Schedule via BuildOwner instead of rebuilding immediately so that
        // the depth-sorted dirty pass deduplicates redundant rebuilds.
        MarkNeedsBuild();
    }

    public override void Unmount()
    {
        _child?.Unmount();
        _child = null;
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

    protected override void VisitChildrenInReverse(
        Action<Element> visitor
    )
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }
}

public class SingleChildElement : RenderObjectElement
{
    private Element? _child;

    public SingleChildElement(
        Widget widget
    ) : base(widget)
    {
    }

    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        var singleChildWidget = (ISingleChildWidget)Widget;
        _child = UpdateChild(
            null,
            singleChildWidget.Child
        );
        if (RenderObject != null && _child?.RenderObject != null)
        {
            RenderObject.AddChild(_child.RenderObject);
        }
    }

    public override void Update(
        Widget newWidget
    )
    {
        var nextWidget = (ISingleChildWidget)newWidget;
        base.Update(newWidget);

        var oldRo = _child?.RenderObject;
        _child = UpdateChild(
            _child,
            nextWidget.Child
        );

        if (RenderObject != null)
        {
            var newRo = _child?.RenderObject;
            if (oldRo != newRo)
            {
                if (oldRo != null)
                {
                    RenderObject.RemoveChild(oldRo);
                }

                if (newRo != null)
                {
                    RenderObject.AddChild(newRo);
                }
            }
        }
    }

    public override void Unmount()
    {
        _child?.Unmount();
        _child = null;
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

    protected override void VisitChildrenInReverse(
        Action<Element> visitor
    )
    {
        if (_child != null)
        {
            visitor(_child);
        }
    }
}

public class MultiChildElement : RenderObjectElement
{
    private List<Element> _children = [];

    public MultiChildElement(
        Widget widget
    ) : base(widget)
    {
    }

    public IReadOnlyList<Element> ChildrenElements => _children;

    public override void Mount(
        Element? parent
    )
    {
        base.Mount(parent);
        var multiChildWidget = (IMultiChildWidget)Widget;
        var childrenWidgets = multiChildWidget.Children;

        for (var i = 0; i < childrenWidgets.Count; i++)
        {
            var childElement = UpdateChild(
                null,
                childrenWidgets[i]
            );
            if (childElement != null)
            {
                _children.Add(childElement);
                if (RenderObject != null && childElement.RenderObject != null)
                {
                    RenderObject.AddChild(childElement.RenderObject);
                }
            }
        }
    }

    public override void Update(
        Widget newWidget
    )
    {
        var nextWidget = (IMultiChildWidget)newWidget;
        base.Update(newWidget);

        var nextWidgets = nextWidget.Children;
        var nextCount = nextWidgets.Count;
        var currentCount = _children.Count;

        var newElements = new List<Element>(nextCount);

        var minLen = Math.Min(
            currentCount,
            nextCount
        );

        for (var i = 0; i < minLen; i++)
        {
            var oldChild = _children[i];
            var oldRo = oldChild.RenderObject;
            var newChild = UpdateChild(
                oldChild,
                nextWidgets[i]
            )!;
            newElements.Add(newChild);

            var newRo = newChild.RenderObject;
            if (RenderObject != null && oldRo != newRo)
            {
                if (oldRo != null)
                {
                    RenderObject.RemoveChild(oldRo);
                }

                if (newRo != null)
                {
                    RenderObject.AddChild(newRo);
                }
            }
        }

        if (nextCount > currentCount)
        {
            for (var i = currentCount; i < nextCount; i++)
            {
                var newChild = UpdateChild(
                    null,
                    nextWidgets[i]
                )!;
                newElements.Add(newChild);
                if (RenderObject != null && newChild.RenderObject != null)
                {
                    RenderObject.AddChild(newChild.RenderObject);
                }
            }
        }
        else if (currentCount > nextCount)
        {
            for (var i = nextCount; i < currentCount; i++)
            {
                var oldChild = _children[i];
                if (RenderObject != null && oldChild.RenderObject != null)
                {
                    RenderObject.RemoveChild(oldChild.RenderObject!);
                }

                oldChild.Unmount();
            }
        }

        _children = newElements;

        ReorderChildRenderObjects();
    }

    /// <summary>
    ///     Ensures RenderObject children order matches the element order.
    ///     Mount/Unmount during reconciliation may append ROs out of order.
    /// </summary>
    internal void ReorderChildRenderObjects()
    {
        if (RenderObject == null)
        {
            return;
        }

        var orderedRos = new List<RenderObject>(_children.Count);
        foreach (var elem in _children)
        {
            var ro = elem.RenderObject;
            if (ro != null)
            {
                orderedRos.Add(ro);
            }
        }

        RenderObject.ReorderChildren(orderedRos);
    }

    public override void Unmount()
    {
        foreach (var child in _children)
        {
            child.Unmount();
        }

        _children.Clear();
        base.Unmount();
    }

    public override void VisitChildren(
        Action<Element> visitor
    )
    {
        foreach (var child in _children)
        {
            visitor(child);
        }
    }

    protected override void VisitChildrenInReverse(
        Action<Element> visitor
    )
    {
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            visitor(_children[i]);
        }
    }
}

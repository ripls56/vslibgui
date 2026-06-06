using System;
using System.Collections.Generic;

namespace Gui.Widgets.Framework;

/// <summary>
///     Base class for widgets that propagate data down the tree efficiently.
///     Descendants can look up the nearest ancestor <see cref="InheritedWidget" />
///     of a given type via <see cref="BuildContext.DependOnInheritedWidgetOfExactType{T}" />
///     and will be rebuilt automatically when the inherited data changes.
/// </summary>
public abstract class InheritedWidget(
    Widget child,
    Key? key = null) : StatelessWidget(key)
{
    public Widget Child { get; } = child;

    public override Widget Build(
        BuildContext context
    ) =>
        Child;

    /// <summary>
    ///     Called when this widget is replaced by a new instance of the same type.
    ///     Return <c>true</c> if dependents should be rebuilt; <c>false</c> to
    ///     suppress notification.
    /// </summary>
    public abstract bool UpdateShouldNotify(
        InheritedWidget oldWidget
    );

    public override Element CreateElement() => new InheritedElement(this);
}

/// <summary>
///     Element backing an <see cref="InheritedWidget" />. Tracks dependents and
///     notifies them when the inherited data changes.
/// </summary>
public class InheritedElement : ComponentElement
{
    private readonly HashSet<Element> _dependents = new();

    public InheritedElement(
        InheritedWidget widget
    ) : base(widget)
    {
    }

    protected override void OnMountBeforeRebuild() => _RegisterInMap();

    public override void Update(
        Widget newWidget
    )
    {
        var oldWidget = (InheritedWidget)Widget;
        base.Update(newWidget);
        var current = (InheritedWidget)newWidget;
        if (current.UpdateShouldNotify(oldWidget))
        {
            NotifyDependents();
        }
    }

    internal void AddDependent(
        Element dependent
    ) =>
        _dependents.Add(dependent);

    internal void RemoveDependent(
        Element dependent
    ) =>
        _dependents.Remove(dependent);

    private void NotifyDependents()
    {
        foreach (var dependent in _dependents)
        {
            dependent.DidChangeDependencies();
        }
    }

    private void _RegisterInMap()
    {
        InheritedWidgets ??= new Dictionary<Type, InheritedElement>();
        InheritedWidgets[Widget.GetType()] = this;
    }
}

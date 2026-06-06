using System;
using System.Collections.Generic;
using Gui.Core.Framework;

namespace Gui.Widgets.Framework;

/// <summary>
///     Opaque identifier used to preserve element identity across rebuilds.
///     When a parent rebuilds, two widgets are considered the "same slot" only if both their
///     runtime type and their <see cref="Key" /> are equal (see <see cref="Widget.CanUpdate" />).
///     Without a key, identity is purely positional.
/// </summary>
public abstract class Key
{
}

/// <summary>
///     A <see cref="Key" /> that uses a typed value for equality comparison.
///     Use this to give stable identity to list items that may be reordered:
///     <code>new ValueKey&lt;int&gt;(item.Id)</code>
/// </summary>
public class ValueKey<T>(T value) : Key
{
    public T Value { get; } = value;

    public override bool Equals(
        object? obj
    )
    {
        return obj is ValueKey<T> other &&
               EqualityComparer<T>.Default.Equals(
                   Value,
                   other.Value
               );
    }

    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value!);
}

/// <summary>
///     The base class for all UI components. Widgets are <b>immutable configuration objects</b>:
///     they hold no mutable state and create no GPU resources. A new widget instance may be
///     created on every <c>Build()</c> call; the Library reuses or recreates the corresponding
///     <see cref="Element" /> based on <see cref="CanUpdate" />.
/// </summary>
public abstract class Widget(Key? key = null) : IDisposable
{
    /// <summary>
    ///     Optional stable identifier. Two widgets must have equal keys (and equal types) for
    ///     their element to be reused across rebuilds. <c>null</c> means positional identity.
    /// </summary>
    public Key? Key { get; } = key;

    /// <summary>Type name shown in debug output and error messages.</summary>
    public virtual string DebugPropertyName => GetType()?.Name ?? "";

    /// <summary>Recursively disposes this widget and any child widgets it owns.</summary>
    public virtual void Dispose()
    {
    }

    /// <summary>
    ///     Creates the <see cref="Element" /> that manages this widget's lifecycle in the element
    ///     tree. Called by the Library when this widget first appears in the tree or when it
    ///     replaces an incompatible element.
    /// </summary>
    public abstract Element CreateElement();

    /// <summary>
    ///     Returns <c>true</c> if an existing element for <paramref name="oldWidget" /> can be
    ///     <em>updated</em> (rather than unmounted and replaced) when the parent passes
    ///     <paramref name="newWidget" /> in its next build. Requires matching runtime type and
    ///     equal <see cref="Key" />.
    /// </summary>
    public static bool CanUpdate(
        Widget oldWidget,
        Widget newWidget
    )
    {
        return oldWidget.GetType() == newWidget.GetType() &&
               Equals(
                   oldWidget.Key,
                   newWidget.Key
               );
    }

    /// <summary>
    ///     Creates the <see cref="RenderObject" /> for this widget. Called once by
    ///     <see cref="RenderObjectElement" /> on mount. Override in widgets that own a render
    ///     object (i.e. subclasses of <see cref="RenderObjectWidget" />).
    /// </summary>
    public virtual RenderObject CreateRenderObject()
    {
        throw new NotImplementedException(
            "Widgets that produce a RenderObject must override CreateRenderObject."
        );
    }

    /// <summary>
    ///     Applies this widget's current configuration to an already-existing
    ///     <paramref name="renderObject" />. Called every time this widget's element is updated
    ///     with a new widget instance. The render object is <b>reused</b> — not recreated.
    /// </summary>
    public virtual void UpdateRenderObject(
        RenderObject renderObject
    )
    {
    }
}

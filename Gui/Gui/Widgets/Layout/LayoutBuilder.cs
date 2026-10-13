using System;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Layout;

/// <summary>
///     A widget that builds its child tree based on the layout constraints it receives
///     from its parent. The <see cref="Builder" /> callback is invoked with the actual
///     <see cref="LayoutConstraints" /> during the layout pass; any change in constraints
///     schedules a rebuild for the next frame.
/// </summary>
public class LayoutBuilder : RenderObjectWidget
{
    /// <summary>
    ///     Creates a <see cref="LayoutBuilder" /> with the given constraint-aware builder callback.
    /// </summary>
    public LayoutBuilder
    (
        Func<BuildContext, LayoutConstraints, Widget> builder,
        Framework.Key? key = null
    ) : base(key)
    {
        Builder = builder;
    }

    /// <summary>
    ///     Called with the parent's constraints to produce the child widget tree.
    ///     Invoked once at mount and again whenever constraints change.
    /// </summary>
    public Func<BuildContext, LayoutConstraints, Widget> Builder { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject() => new RenderLayoutBuilder();

    /// <inheritdoc />
    public override void UpdateRenderObject
    (
        RenderObject renderObject
    )
    {
    }

    /// <inheritdoc />
    public override Element CreateElement() => new LayoutBuilderElement(this);
}

/// <summary>
///     Element for <see cref="LayoutBuilder" />. Manages a single dynamically-built
///     child element whose widget is produced by the builder callback. Rebuilds the
///     child whenever <see cref="RenderLayoutBuilder" /> detects a constraint change.
/// </summary>
public class LayoutBuilderElement : RenderObjectElement
{
    private Element? _builtElement;
    private LayoutConstraints _lastBuiltConstraints;

    /// <summary>Creates a new <see cref="LayoutBuilderElement" /> for the given widget.</summary>
    public LayoutBuilderElement
    (
        LayoutBuilder widget
    ) : base(widget)
    {
    }

    private LayoutBuilder LayoutBuilderWidget => (LayoutBuilder)Widget;

    private RenderLayoutBuilder RenderLayoutBuilder =>
        (RenderLayoutBuilder)RenderObject!;

    /// <inheritdoc />
    public override void Mount
    (
        Element? parent
    )
    {
        base.Mount(parent);
        RenderLayoutBuilder.LayoutBuilderOwner = this;
        base.Rebuild();
        PerformRebuild();
    }

    /// <inheritdoc />
    public override void Update
    (
        Widget newWidget
    )
    {
        base.Update(newWidget);
        PerformRebuild();
    }

    /// <summary>
    ///     Rebuilds the child widget using the current constraints from the render object.
    ///     Called at mount and scheduled by <see cref="RenderLayoutBuilder" /> when constraints change.
    /// </summary>
    public void PerformRebuild()
    {
        var constraints = RenderObject != null
            ? RenderObject.Constraints
            : default;

        _lastBuiltConstraints = constraints;

        var newWidget = LayoutBuilderWidget.Builder
        (
            new BuildContext
            (
                Widget,
                this
            ),
            constraints
        );

        var oldRo = _builtElement?.RenderObject;
        _builtElement = UpdateChild
        (
            _builtElement,
            newWidget
        );

        var newRo = _builtElement?.RenderObject;
        if (oldRo != newRo)
        {
            if (oldRo != null)
            {
                RenderObject?.RemoveChild(oldRo);
            }

            if (newRo != null)
            {
                RenderObject?.AddChild(newRo);
            }
        }
    }

    /// <summary>
    ///     Schedules this element for rebuild on the next frame because constraints changed.
    ///     Called from <see cref="RenderLayoutBuilder.PerformLayout" />.
    /// </summary>
    public void MarkNeedsRebuild() => MarkNeedsBuild();

    /// <inheritdoc />
    public override void Rebuild()
    {
        base.Rebuild();
        PerformRebuild();
    }

    /// <inheritdoc />
    public override void Unmount()
    {
        _builtElement?.Unmount();
        _builtElement = null;
        base.Unmount();
    }

    /// <inheritdoc />
    public override void VisitChildren
    (
        Action<Element> visitor
    )
    {
        if (_builtElement != null)
        {
            visitor(_builtElement);
        }
    }
}

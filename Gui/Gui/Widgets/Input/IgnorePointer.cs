using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Input;

/// <summary>
///     Makes its entire subtree invisible to hit-testing when
///     <see cref="Ignoring" /> is true. Pointer events pass through to whatever
///     is behind this widget in the render order.
///     <para>
///         Set <paramref name="ignoring" /> to <c>false</c> to let events reach
///         the subtree normally (useful for conditional toggling).
///     </para>
/// </summary>
public class IgnorePointer : SingleChildWidget
{
    public IgnorePointer(
        bool ignoring = true,
        Widget? child = null,
        Framework.Key? key = null
    )
        : base(
            child,
            key
        )
    {
        Ignoring = ignoring;
    }

    /// <summary>Whether hit-testing is blocked for the subtree.</summary>
    public bool Ignoring { get; }

    public override Element CreateElement() => new IgnorePointerElement(this);

    public override RenderObject CreateRenderObject() => new RenderProxyBox();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
    }
}

internal class IgnorePointerElement : SingleChildElement
{
    public IgnorePointerElement(
        Widget widget
    ) : base(widget)
    {
    }

    public override bool HitTest(
        HitTestResult result,
        Vector2 position
    )
    {
        return ((IgnorePointer)Widget).Ignoring
            ? false
            : base.HitTest(
                result,
                position
            );
    }
}

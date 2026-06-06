using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Widgets.Painting;

/// <summary>
///     Clips its child to a rounded rectangle.
/// </summary>
public class Clip : SingleChildWidget
{
    /// <param name="borderRadius">
    ///     Corner radii as a Vector4: X = top-right, Y = bottom-right,
    ///     Z = top-left, W = bottom-left. Use <c>new Vector4(r)</c> for a uniform radius.
    /// </param>
    /// <param name="child">Optional child widget to clip.</param>
    /// <param name="clipBehavior">
    ///     Controls clip-edge anti-aliasing. <see cref="Core.Framework.ClipBehavior.AntiAlias" />
    ///     (default)
    ///     produces smooth edges and is recommended whenever <paramref name="borderRadius" />
    ///     is non-zero. <see cref="Core.Framework.ClipBehavior.HardEdge" /> skips anti-aliasing for
    ///     slightly
    ///     better performance on pixel-aligned rectangular clips.
    ///     <see cref="Core.Framework.ClipBehavior.None" />
    ///     bypasses clipping entirely — the child paints as if this widget were not present.
    /// </param>
    /// <param name="key">Optional key for element identity.</param>
    public Clip(
        Vector4 borderRadius = default,
        Widget? child = null,
        ClipBehavior clipBehavior = ClipBehavior.AntiAlias,
        Framework.Key? key = null
    )
        : base(
            child,
            key
        )
    {
        BorderRadius = borderRadius;
        ClipBehavior = clipBehavior;
    }

    /// <summary>Corner radii used to shape the clip.</summary>
    public Vector4 BorderRadius { get; }

    /// <summary>Anti-aliasing mode applied to the clip edge.</summary>
    public ClipBehavior ClipBehavior { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject() =>
        new RenderClip { BorderRadius = BorderRadius, ClipBehavior = ClipBehavior };

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderClip)renderObject;
        ro.BorderRadius = BorderRadius;
        ro.ClipBehavior = ClipBehavior;
    }
}

using Gui.Core.Framework;
using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Painting;

/// <summary>
///     Determines how a box participates in pointer hit-testing.
/// </summary>
public enum HitTestBehavior
{
    /// <summary>
    ///     Defers hit-testing to child render objects. The box itself is not hit-testable
    ///     unless a child claims the pointer event.
    /// </summary>
    Defer,

    /// <summary>
    ///     The box is always hit-testable within its bounds, even if it has no visible fill.
    ///     Blocks pointer events from reaching widgets behind it.
    /// </summary>
    Opaque,

    /// <summary>
    ///     The box participates in hit-testing but does not block pointer events from
    ///     reaching widgets behind it. Useful for overlays that must receive events
    ///     without consuming them.
    /// </summary>
    Translucent
}

/// <summary>
///     Immutable visual description for a <see cref="Gui.Widgets.Basic.Container" />.
///     Passed to <see cref="Gui.Core.Basic.RenderBox" /> which translates it into Skia
///     draw calls each paint frame.
/// </summary>
public class BoxStyle
{
    /// <summary>Fill color as RGBA. Alpha zero = transparent (default).</summary>
    public Vector4 Color { get; set; } = Vector4.Zero;

    /// <summary>
    ///     Optional gradient fill. When set, painted on top of <see cref="Color" />.
    /// </summary>
    public Gradient? Gradient { get; set; }

    /// <summary>
    ///     Controls pointer hit-test participation. Default is <see cref="HitTestBehavior.Defer" />,
    ///     which forwards hit-testing to children.
    /// </summary>
    public HitTestBehavior HitTestBehavior { get; set; } = HitTestBehavior.Defer;

    /// <summary>
    ///     Per-corner radii as <c>(topLeft, topRight, bottomRight, bottomLeft)</c> in pixels.
    ///     Zero = sharp corners (default).
    /// </summary>
    public Vector4 CornerRadius { get; set; } = Vector4.Zero;

    /// <summary>Border stroke width in pixels. Zero = no border (default).</summary>
    public float BorderThickness { get; set; } = 0;

    /// <summary>
    ///     Border stroke color as RGBA. Default is white at 50% opacity.
    ///     Only painted when <see cref="BorderThickness" /> is greater than zero.
    /// </summary>
    public Vector4 BorderColor { get; set; } = new(
        1,
        1,
        1,
        0.5f
    );

    /// <summary>Inner padding applied before children are laid out.</summary>
    public EdgeInsets Padding { get; set; } = EdgeInsets.Zero;

    /// <summary>
    ///     Clipping mode applied to children. Default is <see cref="Core.Framework.ClipBehavior.None" />.
    ///     Set to <see cref="Core.Framework.ClipBehavior.HardEdge" /> or
    ///     <see cref="Core.Framework.ClipBehavior.AntiAlias" />
    ///     to clip children to the box bounds (respecting <see cref="CornerRadius" />).
    /// </summary>
    public ClipBehavior ClipBehavior { get; set; } = ClipBehavior.None;

    /// <summary>Fixed width in pixels. Null = size determined by layout constraints.</summary>
    public float? Width { get; set; }

    /// <summary>Fixed height in pixels. Null = size determined by layout constraints.</summary>
    public float? Height { get; set; }

    /// <summary>
    ///     Optional texture bitmap tiled or stretched over the fill area.
    ///     Painted after <see cref="Color" /> and <see cref="Gradient" />.
    /// </summary>
    public SKBitmap? Texture { get; set; }

    /// <summary>
    ///     Zero or more box shadows applied to this container, painted in order
    ///     (first = bottommost). Outer shadows are drawn behind the fill;
    ///     inner shadows are drawn above the fill but below children.
    /// </summary>
    public BoxShadow[]? BoxShadows { get; set; }
}

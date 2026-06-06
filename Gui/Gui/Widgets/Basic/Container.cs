using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     A convenience widget that combines common painting, positioning, and sizing widgets.
/// </summary>
public class Container : StatelessWidget
{
    private readonly Widget? _child;
    private readonly BoxStyle _style;

    public Container(
        BoxStyle? style = null,
        Widget? child = null
    )
    {
        _style = style ?? new BoxStyle();
        _child = child;
    }

    public override Widget Build(
        BuildContext context
    )
    {
        var current = _child ?? new SizedBox();

        // Padding goes INSIDE the box (between border and child)
        if (_style.Padding != EdgeInsets.Zero)
        {
            current = new Padding(
                _style.Padding,
                current
            );
        }

        // Calculate preferred size from style (used when there are no children)
        Vector2? preferredSize = null;
        if (_style.Width.HasValue || _style.Height.HasValue)
        {
            preferredSize = new Vector2(
                _style.Width ?? 0,
                _style.Height ?? 0
            );
        }

        // The core painting is done by _BoxWidget which creates a RenderBox directly.
        // ClipBehavior is forwarded to the RenderBox so the clip is applied AFTER
        // outer shadows are painted — shadows therefore extend outside the box.
        current = new BoxWidget(
            _style.Color,
            _style.Gradient,
            _style.CornerRadius,
            _style.BorderThickness,
            _style.BorderColor,
            _style.Texture,
            _style.HitTestBehavior,
            preferredSize,
            _style.BoxShadows,
            _style.ClipBehavior,
            current
        );

        if (_style.Width != null || _style.Height != null)
        {
            current = new SizedBox(
                _style.Width,
                _style.Height,
                current
            );
        }

        return current;
    }
}

/// <summary>
///     Internal RenderObjectWidget that creates a RenderBox for painting.
///     Replaces the previous CustomPaint + BoxPainter combination.
/// </summary>
internal class BoxWidget : SingleChildWidget
{
    private readonly Vector4 _borderColor;
    private readonly float _borderThickness;
    private readonly ClipBehavior _clipBehavior;
    private readonly Vector4 _color;
    private readonly Vector4 _cornerRadii;
    private readonly Gradient? _gradient;
    private readonly HitTestBehavior _hitTestBehavior;
    private readonly Vector2? _preferredSize;
    private readonly BoxShadow[]? _shadows;
    private readonly SKBitmap? _texture;

    public BoxWidget(
        Vector4 color,
        Gradient? gradient,
        Vector4 cornerRadii,
        float borderThickness,
        Vector4 borderColor,
        SKBitmap? texture,
        HitTestBehavior hitTestBehavior,
        Vector2? preferredSize = null,
        BoxShadow[]? shadows = null,
        ClipBehavior clipBehavior = ClipBehavior.None,
        Widget? child = null
    ) : base(child)
    {
        _color = color;
        _gradient = gradient;
        _cornerRadii = cornerRadii;
        _borderThickness = borderThickness;
        _borderColor = borderColor;
        _texture = texture;
        _hitTestBehavior = hitTestBehavior;
        _preferredSize = preferredSize;
        _shadows = shadows;
        _clipBehavior = clipBehavior;
    }

    public override RenderObject CreateRenderObject() => new RenderBox();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderBox)renderObject;
        ro.Color = _color;
        ro.Gradient = _gradient;
        ro.CornerRadii = _cornerRadii;
        ro.BorderThickness = _borderThickness;
        ro.BorderColor = _borderColor;
        ro.Texture = _texture;
        ro.HitTestBehavior = _hitTestBehavior;
        ro.PreferredSize = _preferredSize;
        ro.Shadows = _shadows;
        ro.ClipBehavior = _clipBehavior;
    }
}

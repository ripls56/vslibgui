using Gui.Core.Scroll;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Widgets.Basic;

/// <summary>
///     A thin line used to separate content. A horizontal divider stretches across the cross
///     axis of its parent (e.g. the width of a <see cref="Column" />); a vertical divider
///     stretches along the height of a <see cref="Row" />.
/// </summary>
public class Divider : StatelessWidget
{
    /// <summary>Creates a divider line.</summary>
    /// <param name="thickness">Line thickness in pixels.</param>
    /// <param name="indent">Leading inset (left for horizontal, top for vertical).</param>
    /// <param name="endIndent">Trailing inset (right for horizontal, bottom for vertical).</param>
    /// <param name="color">Line color. Defaults to the theme border color.</param>
    /// <param name="axis">Orientation of the line. Defaults to horizontal.</param>
    public Divider(
        float thickness = 1f,
        float indent = 0f,
        float endIndent = 0f,
        Vector4? color = null,
        Axis axis = Axis.Horizontal,
        Framework.Key? key = null
    ) : base(key)
    {
        Thickness = thickness;
        Indent = indent;
        EndIndent = endIndent;
        Color = color;
        Axis = axis;
    }

    /// <summary>Line thickness in pixels.</summary>
    public float Thickness { get; }

    /// <summary>Leading inset before the line.</summary>
    public float Indent { get; }

    /// <summary>Trailing inset after the line.</summary>
    public float EndIndent { get; }

    /// <summary>Line color. Null uses the theme border color.</summary>
    public Vector4? Color { get; }

    /// <summary>Orientation of the line.</summary>
    public Axis Axis { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var lineColor = Color ?? Theme.Of(context).ColorScheme.Border;

        if (Axis == Axis.Horizontal)
        {
            return new Padding(
                EdgeInsets.Only(Indent, right: EndIndent),
                new Container(new BoxStyle { Height = Thickness, Color = lineColor })
            );
        }

        return new Padding(
            EdgeInsets.Only(top: Indent, bottom: EndIndent),
            new Container(new BoxStyle { Width = Thickness, Color = lineColor })
        );
    }
}

using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Example.Shared;

/// <summary>
///     Renders a small horizontal sample illustrating a single
///     <see cref="CrossAxisAlignment" /> option. Children have different
///     heights so the cross-axis effect is visible.
/// </summary>
internal class CrossAxisAlignmentSample : StatelessWidget
{
    /// <summary>Creates a labeled cross-axis alignment sample.</summary>
    public CrossAxisAlignmentSample(string label, CrossAxisAlignment alignment)
    {
        Label = label;
        Alignment = alignment;
    }

    /// <summary>Label shown above the sample.</summary>
    public string Label { get; }

    /// <summary>Cross-axis alignment applied to the sample row.</summary>
    public CrossAxisAlignment Alignment { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Column(
            mainAxisSize: MainAxisSize.Min,
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 4,
            children:
            [
                new Text(Label, new TextStyle { FontSize = 11, Color = colors.OnSurface }),
                new Container(
                    new BoxStyle
                    {
                        Color = new Vector4(0f, 0f, 0f, 0.18f),
                        CornerRadius = Vector4.One * 3,
                        Padding = EdgeInsets.All(4)
                    },
                    new Row(
                        6,
                        crossAxisAlignment: Alignment,
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 18,
                                CornerRadius = Vector4.One * 3
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 34,
                                CornerRadius = Vector4.One * 3
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 26,
                                CornerRadius = Vector4.One * 3
                            })
                        ]
                    )
                )
            ]
        );
    }
}

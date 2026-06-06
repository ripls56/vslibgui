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
///     <see cref="MainAxisAlignment" /> option inside a labeled box.
/// </summary>
internal class MainAxisAlignmentSample : StatelessWidget
{
    /// <summary>Creates a labeled main-axis alignment sample.</summary>
    public MainAxisAlignmentSample(string label, MainAxisAlignment alignment)
    {
        Label = label;
        Alignment = alignment;
    }

    /// <summary>Label shown above the sample.</summary>
    public string Label { get; }

    /// <summary>Alignment applied to the sample row.</summary>
    public MainAxisAlignment Alignment { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Column(
            mainAxisSize: MainAxisSize.Min,
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
                        mainAxisAlignment: Alignment,
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 20,
                                CornerRadius = Vector4.One * 3
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 20,
                                CornerRadius = Vector4.One * 3
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 32,
                                Height = 20,
                                CornerRadius = Vector4.One * 3
                            })
                        ]
                    )
                )
            ]
        );
    }
}

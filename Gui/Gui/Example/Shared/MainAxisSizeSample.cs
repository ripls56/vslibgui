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
///     Labeled sample illustrating <see cref="MainAxisSize" />. The framed
///     box outlines the actual extent of the inner column, so Min
///     (shrink to content) and Max (fill the main axis) are visible
///     side by side.
/// </summary>
internal class MainAxisSizeSample : StatelessWidget
{
    /// <summary>Creates a labeled sample with the given column sizing mode.</summary>
    public MainAxisSizeSample(string label, MainAxisSize size)
    {
        Label = label;
        Size = size;
    }

    /// <summary>Label shown above the sample.</summary>
    public string Label { get; }

    /// <summary>Sizing mode applied to the inner column.</summary>
    public MainAxisSize Size { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 4,
            children:
            [
                new Text(Label, new TextStyle { FontSize = 11, Color = colors.OnSurface }),
                new Expanded(
                    new Align(
                        Alignment.TopLeft,
                        new Container(
                            new BoxStyle
                            {
                                Color = new Vector4(0f, 0f, 0f, 0.18f),
                                CornerRadius = Vector4.One * 3,
                                BorderThickness = 1f,
                                BorderColor = colors.Primary,
                                Padding = EdgeInsets.All(4)
                            },
                            new Column(
                                mainAxisSize: Size,
                                spacing: 4,
                                children:
                                [
                                    new Container(new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 60,
                                        Height = 16,
                                        CornerRadius = Vector4.One * 3
                                    }),
                                    new Container(new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 60,
                                        Height = 16,
                                        CornerRadius = Vector4.One * 3
                                    })
                                ]
                            )
                        )
                    )
                )
            ]
        );
    }
}

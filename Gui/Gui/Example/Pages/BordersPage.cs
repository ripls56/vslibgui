using Gui.Example.Shared;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class BordersPage : StatelessWidget
{
    public BordersPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widget Build
    (
        BuildContext context
    )
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new DemoCard
                (
                    "Single side",
                    new BorderDemoBox
                    (
                        "Bottom only",
                        Border.Only(bottom: new BorderSide(3f, colors.Primary))
                    ),
                    "Border.Only(bottom: new BorderSide(3f, color))",
                    Capi,
                    "One side only — underline, separator, or selected-tab accent."
                ),
                new DemoCard
                (
                    "Tab indicator",
                    new BorderDemoBox
                    (
                        "Selected",
                        Border.Only(bottom: new BorderSide(4f, colors.Primary)),
                        new Vector4(8)
                    ),
                    "Border.Only(bottom: new BorderSide(4f, color))\n// combined with CornerRadius",
                    Capi,
                    "A thick bottom edge stays crisp alongside rounded corners."
                ),
                new DemoCard
                (
                    "Symmetric widths",
                    new BorderDemoBox
                    (
                        "Symmetric",
                        Border.Symmetric
                        (
                            vertical: 1f,
                            horizontal: 4f,
                            color: colors.Border
                        )
                    ),
                    "Border.Symmetric(vertical: 1f, horizontal: 4f, color: color)",
                    Capi,
                    "Distinct vertical and horizontal widths, one shared color."
                ),
                new DemoCard
                (
                    "Four colors",
                    new BorderDemoBox
                    (
                        "Per-side color",
                        new Border
                        (
                            Left: new BorderSide(3f, "#E05252".FromHex()),
                            Top: new BorderSide(3f, "#52C77E".FromHex()),
                            Right: new BorderSide(3f, "#5B9BD5".FromHex()),
                            Bottom: new BorderSide(3f, "#E0B052".FromHex())
                        )
                    ),
                    "new Border(\n  Left:   new BorderSide(3f, red),\n  Top:    new BorderSide(3f, green),\n  Right:  new BorderSide(3f, blue),\n  Bottom: new BorderSide(3f, yellow))",
                    Capi,
                    "Every edge can carry its own width and color."
                ),
                new DemoCard
                (
                    "Rounded asymmetric",
                    new BorderDemoBox
                    (
                        "Rounded",
                        Border.Symmetric
                        (
                            vertical: 2f,
                            horizontal: 6f,
                            color: colors.Primary
                        ),
                        new Vector4(12)
                    ),
                    "Border.Symmetric(vertical: 2f, horizontal: 6f, color: color)\n// + CornerRadius = 12",
                    Capi,
                    "Asymmetric widths keep rounded corners; each corner's inner\nradius shrinks by the smaller adjacent width."
                )
            ]
        );
    }
}

internal class BorderDemoBox : StatelessWidget
{
    public BorderDemoBox
    (
        string text,
        Border border,
        Vector4 radius = default
    )
    {
        Text = text;
        Border = border;
        Radius = radius;
    }

    public string Text { get; }
    public Border Border { get; }
    public Vector4 Radius { get; }

    public override Widget Build
    (
        BuildContext context
    )
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Container
        (
            new BoxStyle
            {
                Color = colors.SurfaceLow,
                CornerRadius = Radius,
                Border = Border,
                Width = 220,
                Height = 64
            },
            new Center
            (
                new Text
                (
                    Text,
                    new TextStyle
                    {
                        FontSize = 13, Color = colors.OnSurface
                    }
                )
            )
        );
    }
}

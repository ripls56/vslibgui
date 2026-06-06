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

internal class ThemingPage : StatelessWidget
{
    private static readonly ThemeData OceanTheme = new(
        new ColorScheme
        {
            Primary = "#5B9BD5".FromHex(),
            OnPrimary = "#0D1B2A".FromHex(),
            Surface = "#1A2535".FromHex(),
            OnSurface = "#C8D8E8".FromHex(),
            Background = "#0D1520".FromHex(),
            OnBackground = "#C8D8E8".FromHex(),
            Border = "#3A5A8080".FromHex(),
            Error = "#E05252".FromHex(),
            OnError = "#FAEAEA".FromHex()
        }
    );

    public ThemingPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Theming",
                    new TextStyle
                    {
                        FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                    }),

                new DemoCard(
                    "Global theme",
                    new ListenableBuilder(
                        ThemeData.DefaultNotifier,
                        _ => new Theme(ThemeData.DefaultNotifier.Value, new ThemeSwatches())
                    ),
                    "// VintagestoryData/ModConfig/libgui.json\n{\n  \"Theme\": {\n    \"Primary\": \"#D1AB54\",\n    \"Surface\": \"#2A1E12\"\n  }\n}",
                    Capi,
                    "Edit libgui.json and save — changes apply instantly via FileSystemWatcher."
                ),

                new Theme(
                    OceanTheme,
                    new DemoCard(
                        "Per-window override",
                        new ThemeSwatches(),
                        "protected override Widget Build() =>\n    new Theme(data: _myTheme, child: new MyContent());",
                        Capi,
                        "Wrap any subtree in Theme(data, child) to replace the global palette. Nearest ancestor wins."
                    )
                )
            ]
        );
    }

    private class ThemeSwatches : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            var c = Theme.Of(context).ColorScheme;
            return new Row(
                8,
                children:
                [
                    Swatch(c.Primary, c.OnPrimary, "Primary"),
                    Swatch(c.Surface, c.OnSurface, "Surface"),
                    Swatch(c.Background, c.OnBackground, "Background"),
                    Swatch(c.Error, c.OnError, "Error")
                ]
            );
        }

        private static Widget Swatch(Vector4 bg, Vector4 fg, string label)
        {
            return new Container(
                new BoxStyle { Color = bg, CornerRadius = new Vector4(4) },
                new Padding(
                    EdgeInsets.Symmetric(8, 16),
                    new Text(label, new TextStyle { Color = fg, FontSize = 13 })
                )
            );
        }
    }
}

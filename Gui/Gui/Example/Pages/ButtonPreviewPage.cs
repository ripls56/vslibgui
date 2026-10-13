using Gui.Example.Shared;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class ButtonPreviewPage : StatelessWidget
{
    public ButtonPreviewPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widget Build(BuildContext context)
    {
        var theme = Theme.Of(context);
        var colors = theme.ColorScheme;
        var headerStyle = theme.TextTheme.Headline;
        headerStyle.Color = colors.Primary;

        return new Column
        (
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 16,
            children:
            [
                new Text("Buttons", headerStyle),

                new DemoCard
                (
                    "All Variants & States",
                    description: "Matrix of all button variants across common interaction states.",
                    demo: new Column
                    (
                        24,
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        children:
                        [
                            BuildVariantRow("Primary", ButtonVariant.Primary),
                            BuildVariantRow("Secondary", ButtonVariant.Secondary),
                            BuildVariantRow("Danger", ButtonVariant.Danger),
                            BuildVariantRow("Ghost", ButtonVariant.Ghost)
                        ]
                    ),
                    code: "new Button(child, ButtonVariant.Secondary)",
                    capi: Capi
                ),

                new DemoCard
                (
                    "Special States",
                    description: "Buttons handling disabled and async loading states.",
                    demo: new Row
                    (
                        12,
                        children:
                        [
                            new Button(new Text("Disabled"), enabled: false),
                            new Button(new Text("Loading"), status: ButtonStatus.Loading),
                            new Button(new Text("Success"), status: ButtonStatus.Success),
                            new Button(new Text("Error"), status: ButtonStatus.Error)
                        ]
                    ),
                    code: "new Button(child, status: ButtonStatus.Loading)",
                    capi: Capi
                ),

                new DemoCard
                (
                    "Icon Buttons",
                    description: "Button variants with centered icons for compact actions.",
                    demo: new Row
                    (
                        16,
                        children:
                        [
                            new IconButton(new Icon("gui", "textures/icons/sparkles.svg")),
                            new IconButton
                            (
                                new Icon("gui", "textures/icons/edit-3.svg"),
                                ButtonVariant.Secondary
                            ),
                            new IconButton
                            (
                                new Icon("gui", "textures/icons/package.svg"), ButtonVariant.Danger
                            ),
                            new IconButton
                                (new Icon("gui", "textures/icons/palette.svg"), ButtonVariant.Ghost)
                        ]
                    ),
                    code: "new IconButton(new Icon(...), ButtonVariant.Secondary)",
                    capi: Capi
                )
            ]
        );
    }

    private Widget BuildVariantRow(string label, ButtonVariant variant)
    {
        return new Column
        (
            8,
            crossAxisAlignment: CrossAxisAlignment.Start,
            children:
            [
                new Text
                (
                    label, new TextStyle
                    {
                        FontSize = 11, Weight = FontWeight.Bold, Color = new Vector4(1, 1, 1, 0.4f)
                    }
                ),
                new Wrap
                (
                    spacing: 8,
                    runSpacing: 8,
                    children:
                    [
                        new Button(new Text("Normal"), variant),
                        new Button(new Text("Hover"), variant)
                        {
                            ForceHover = true
                        },
                        new Button(new Text("Focus"), variant)
                        {
                            ForceFocus = true
                        },
                        new Button(new Text("Active"), variant)
                        {
                            ForceActive = true
                        }
                    ]
                )
            ]
        );
    }
}

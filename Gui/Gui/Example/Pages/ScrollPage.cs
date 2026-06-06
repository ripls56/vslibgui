using Gui.Core.Scroll;
using Gui.Example.Shared;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using Gui.Widgets.Scroll;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class ScrollPage : StatefulWidget
{
    public ScrollPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override Widgets.Framework.State CreateState() => new State();

    private class State : State<ScrollPage>
    {
        private ScrollController _scrollCtrl = null!;

        public override void InitState()
        {
            base.InitState();
            _scrollCtrl = new ScrollController();
        }

        public override void Dispose()
        {
            _scrollCtrl.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var colors = Theme.Of(context).ColorScheme;

            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 16,
                children:
                [
                    new Text("Scroll",
                        new TextStyle
                        {
                            FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                        }),

                    new DemoCard(
                        "ListView — vertical",
                        description:
                        "Virtualizes large lists. Only visible items are built and laid out.",
                        demo: new SizedBox(
                            height: 200,
                            child: new ListView(
                                (ctx, i) => new Padding(
                                    EdgeInsets.Symmetric(3),
                                    new Container(
                                        new BoxStyle
                                        {
                                            Color = i % 2 == 0 ? colors.Surface : Vector4.Zero,
                                            CornerRadius = Vector4.One * 4,
                                            BorderThickness = 1f,
                                            BorderColor = colors.Border,
                                            Padding = EdgeInsets.Symmetric(horizontal: 12,
                                                vertical: 8)
                                        },
                                        new Text($"Item {i + 1}",
                                            new TextStyle
                                            {
                                                FontSize = 13, Color = colors.OnSurface
                                            })
                                    )
                                ),
                                200,
                                46
                            )
                        ),
                        code: """
                              new ListView(
                                itemBuilder: (context, i) => new Container(
                                  child: new Text($"Item {i + 1}")
                                ),
                                itemCount:  200,
                                itemHeight: 46
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "ListView — horizontal",
                        description: "Pass scrollDirection: Axis.Horizontal for a horizontal list.",
                        demo: new SizedBox(
                            height: 72,
                            child: new ListView(
                                (ctx, i) => new Padding(
                                    EdgeInsets.Symmetric(horizontal: 4),
                                    new Container(
                                        new BoxStyle
                                        {
                                            Color = colors.Surface,
                                            CornerRadius = Vector4.One * 4,
                                            BorderThickness = 1f,
                                            BorderColor = colors.Primary,
                                            Width = 64
                                        },
                                        new Center(
                                            new Text($"{i + 1}",
                                                new TextStyle
                                                {
                                                    FontSize = 14,
                                                    Weight = FontWeight.Bold,
                                                    Color = colors.Primary
                                                })
                                        )
                                    )
                                ),
                                120,
                                72,
                                scrollDirection: Axis.Horizontal
                            )
                        ),
                        code: """
                              new ListView(
                                itemBuilder:     (context, i) => new Container(...),
                                itemCount:       120,
                                itemHeight:      72,
                                scrollDirection: Axis.Horizontal
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "SingleChildScrollView",
                        description:
                        "Wraps any widget in a scrollable area. Non-virtualized — all children are built.",
                        demo: new SizedBox(
                            height: 120,
                            child: new SingleChildScrollView(
                                new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 4,
                                    children:
                                    [
                                        new Container(new BoxStyle
                                        {
                                            Color = colors.Surface,
                                            Height = 32,
                                            CornerRadius = Vector4.One * 3,
                                            BorderThickness = 1f,
                                            BorderColor = colors.Border
                                        }),
                                        new Container(new BoxStyle
                                        {
                                            Color = colors.Surface,
                                            Height = 32,
                                            CornerRadius = Vector4.One * 3,
                                            BorderThickness = 1f,
                                            BorderColor = colors.Border
                                        }),
                                        new Container(new BoxStyle
                                        {
                                            Color = colors.Surface,
                                            Height = 32,
                                            CornerRadius = Vector4.One * 3,
                                            BorderThickness = 1f,
                                            BorderColor = colors.Border
                                        }),
                                        new Container(new BoxStyle
                                        {
                                            Color = colors.Primary,
                                            Height = 32,
                                            CornerRadius = Vector4.One * 3
                                        }),
                                        new Container(new BoxStyle
                                        {
                                            Color = colors.Primary,
                                            Height = 32,
                                            CornerRadius = Vector4.One * 3
                                        })
                                    ]
                                )
                            )
                        ),
                        code: """
                              new SingleChildScrollView(
                                child: new Column(
                                  children: [ /* any content taller than the viewport */ ]
                                )
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "GridView",
                        description:
                        "Virtualized scrollable grid. Builder mode renders only the visible cells.",
                        demo: new SizedBox(
                            height: 220,
                            child: GridView.Builder(
                                (ctx, i) => new Container(
                                    new BoxStyle
                                    {
                                        Color = new Vector4(
                                            colors.Primary.X,
                                            colors.Primary.Y,
                                            colors.Primary.Z,
                                            0.15f + i % 5 * 0.10f),
                                        CornerRadius = Vector4.One * 4
                                    },
                                    new Center(
                                        new Text($"{i + 1}",
                                            new TextStyle
                                            {
                                                FontSize = 12,
                                                Weight = FontWeight.Bold,
                                                Color = colors.Primary
                                            })
                                    )
                                ),
                                240,
                                new SliverGridDelegateWithFixedCrossAxisCount(
                                    4,
                                    crossAxisSpacing: 6,
                                    mainAxisSpacing: 6,
                                    childAspectRatio: 1f
                                )
                            )
                        ),
                        code: """
                              GridView.Builder(
                                itemBuilder: (ctx, i) => new Container(...),
                                itemCount:   240,
                                gridDelegate: new SliverGridDelegateWithFixedCrossAxisCount(
                                  crossAxisCount: 4,
                                  crossAxisSpacing: 6,
                                  mainAxisSpacing:  6
                                )
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Scrollbar",
                        description:
                        "Wraps any scrollable child with a visible track and draggable thumb. Requires a shared ScrollController.",
                        demo: new SizedBox(
                            height: 200,
                            child: new Scrollbar(
                                _scrollCtrl,
                                new ListView(
                                    (ctx, i) => new Padding(
                                        EdgeInsets.Symmetric(3),
                                        new Container(
                                            new BoxStyle
                                            {
                                                Color =
                                                    i % 2 == 0 ? colors.Surface : Vector4.Zero,
                                                CornerRadius = Vector4.One * 4,
                                                BorderThickness = 1f,
                                                BorderColor = colors.Border,
                                                Padding = EdgeInsets.Symmetric(horizontal: 12,
                                                    vertical: 8)
                                            },
                                            new Text($"Row {i + 1}",
                                                new TextStyle
                                                {
                                                    FontSize = 13, Color = colors.OnSurface
                                                })
                                        )
                                    ),
                                    30,
                                    46,
                                    _scrollCtrl
                                )
                            )
                        ),
                        code: """
                              var controller = new ScrollController();

                              new Scrollbar(
                                controller: controller,
                                child: new ListView(
                                  itemCount:  30,
                                  itemHeight: 46,
                                  controller: controller,
                                  itemBuilder: (ctx, i) => new Text($"Row {i + 1}")
                                )
                              )
                              """,
                        capi: Widget.Capi
                    )
                ]
            );
        }
    }
}

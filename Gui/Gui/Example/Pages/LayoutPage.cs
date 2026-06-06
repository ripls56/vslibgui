using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Core.Scroll;
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

internal class LayoutPage : StatelessWidget
{
    public LayoutPage(ICoreClientAPI capi)
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
                new Text("Layout",
                    new TextStyle
                    {
                        FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                    }),

                new DemoCard(
                    "Row",
                    description: "Lays out children horizontally with optional spacing.",
                    demo: new Row(
                        8,
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 48,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Surface,
                                Width = 80,
                                Height = 32,
                                CornerRadius = Vector4.One * 4,
                                BorderThickness = 1f,
                                BorderColor = colors.Border
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 48,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            })
                        ]
                    ),
                    code: """
                          new Row(
                            spacing: 8,
                            children:
                            [
                              new Container(style: new BoxStyle { Color = colors.Primary, Width = 48, Height = 32 }),
                              new Container(style: new BoxStyle { Color = colors.Surface, Width = 80, Height = 32 }),
                              new Container(style: new BoxStyle { Color = colors.Primary, Width = 48, Height = 32 })
                            ]
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Row · MainAxisAlignment",
                    description:
                    "Distributes children along the main (horizontal) axis. Default is Start.",
                    demo: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new MainAxisAlignmentSample("Start", MainAxisAlignment.Start),
                            new MainAxisAlignmentSample("Center", MainAxisAlignment.Center),
                            new MainAxisAlignmentSample("End", MainAxisAlignment.End),
                            new MainAxisAlignmentSample("SpaceBetween",
                                MainAxisAlignment.SpaceBetween),
                            new MainAxisAlignmentSample("SpaceAround",
                                MainAxisAlignment.SpaceAround),
                            new MainAxisAlignmentSample("SpaceEvenly",
                                MainAxisAlignment.SpaceEvenly)
                        ]
                    ),
                    code: """
                          new Row(
                            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                            children: [ /* boxes */ ]
                          )
                          // Also: Start, Center, End, SpaceAround, SpaceEvenly
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Row · CrossAxisAlignment",
                    description:
                    "Aligns children along the cross (vertical) axis. Stretch forces equal height.",
                    demo: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new CrossAxisAlignmentSample("Start", CrossAxisAlignment.Start),
                            new CrossAxisAlignmentSample("Center", CrossAxisAlignment.Center),
                            new CrossAxisAlignmentSample("End", CrossAxisAlignment.End),
                            new CrossAxisAlignmentSample("Stretch", CrossAxisAlignment.Stretch)
                        ]
                    ),
                    code: """
                          new Row(
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children: [ /* boxes of different heights */ ]
                          )
                          // Also: Start, End, Stretch
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Column",
                    description: "Lays out children vertically with optional spacing.",
                    demo: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        spacing: 6,
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 200,
                                Height = 20,
                                CornerRadius = Vector4.One * 3
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Surface,
                                Width = 320,
                                Height = 20,
                                CornerRadius = Vector4.One * 3,
                                BorderThickness = 1f,
                                BorderColor = colors.Border
                            }),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 140,
                                Height = 20,
                                CornerRadius = Vector4.One * 3
                            })
                        ]
                    ),
                    code: """
                          new Column(
                            spacing: 6,
                            children:
                            [
                              new Container(style: new BoxStyle { Color = colors.Primary, Height = 20 }),
                              new Container(style: new BoxStyle { Color = colors.Surface, Height = 20 }),
                              new Container(style: new BoxStyle { Color = colors.Primary, Height = 20 })
                            ]
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Column · MainAxisSize",
                    description:
                    "Max (default) fills the parent on the main axis. Min shrinks to content — useful for chips, badges, toolbar groups.",
                    demo: new SizedBox(
                        height: 220,
                        child: new Row(
                            12,
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children:
                            [
                                new Expanded(new MainAxisSizeSample("MainAxisSize.Max",
                                    MainAxisSize.Max)),
                                new Expanded(new MainAxisSizeSample("MainAxisSize.Min",
                                    MainAxisSize.Min))
                            ]
                        )
                    ),
                    code: """
                          // Default: fills parent on the main axis.
                          new Column(children: [...])

                          // Shrink-to-content: useful for chips, badges, toolbar groups.
                          new Column(
                            mainAxisSize: MainAxisSize.Min,
                            children: [...]
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Expanded",
                    description: "Fills remaining space on the main axis inside a Row or Column.",
                    demo: new Row(
                        8,
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 60,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            }),
                            new Expanded(
                                new Container(new BoxStyle
                                {
                                    Color = colors.Surface,
                                    Height = 32,
                                    CornerRadius = Vector4.One * 4,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Primary
                                })
                            ),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 60,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            })
                        ]
                    ),
                    code: """
                          new Row(
                            spacing: 8,
                            children:
                            [
                              new Container(style: new BoxStyle { Width = 60, Height = 32 }),
                              new Expanded(
                                child: new Container(style: new BoxStyle { Height = 32 })
                              ),
                              new Container(style: new BoxStyle { Width = 60, Height = 32 })
                            ]
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Flexible",
                    description:
                    "Distributes remaining main-axis space by integer flex factor. Loose (default) lets the child be smaller; pair with Expanded for tight fill.",
                    demo: new Row(
                        6,
                        children:
                        [
                            new Flexible(
                                new Container(new BoxStyle
                                {
                                    Color = colors.Primary,
                                    Height = 32,
                                    CornerRadius = Vector4.One * 4
                                }),
                                1,
                                FlexFit.Tight
                            ),
                            new Flexible(
                                new Container(new BoxStyle
                                {
                                    Color = colors.Surface,
                                    Height = 32,
                                    CornerRadius = Vector4.One * 4,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border
                                }),
                                2,
                                FlexFit.Tight
                            ),
                            new Flexible(
                                new Container(new BoxStyle
                                {
                                    Color = colors.Primary,
                                    Height = 32,
                                    CornerRadius = Vector4.One * 4
                                }),
                                1,
                                FlexFit.Tight
                            )
                        ]
                    ),
                    code: """
                          new Row(children: [
                            new Flexible(flex: 1, fit: FlexFit.Tight, child: ...),
                            new Flexible(flex: 2, fit: FlexFit.Tight, child: ...),
                            new Flexible(flex: 1, fit: FlexFit.Tight, child: ...)
                          ])
                          // Loose (default): child may be smaller than allocated.
                          // Tight: child is forced to fill its slot.
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Spacer",
                    description:
                    "Empty flexible gap. Pushes siblings apart inside a Row or Column.",
                    demo: new Row(
                        children:
                        [
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 48,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            }),
                            new Spacer(),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 48,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            }),
                            new Spacer(2),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Width = 48,
                                Height = 32,
                                CornerRadius = Vector4.One * 4
                            })
                        ]
                    ),
                    code: """
                          new Row(children: [
                            box,
                            new Spacer(),          // flex: 1
                            box,
                            new Spacer(flex: 2),   // twice as wide
                            box
                          ])
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Padding",
                    description: "Adds empty space around a child using EdgeInsets.",
                    demo: new Container(
                        new BoxStyle
                        {
                            Color = colors.Surface,
                            CornerRadius = Vector4.One * 4,
                            BorderThickness = 1f,
                            BorderColor = colors.Border
                        },
                        new Padding(
                            EdgeInsets.All(20),
                            new Container(new BoxStyle
                            {
                                Color = colors.Primary,
                                Height = 32,
                                Width = 32,
                                CornerRadius = Vector4.One * 3
                            })
                        )
                    ),
                    code: """
                          new Container(
                            style: new BoxStyle { Color = colors.Surface },
                            child: new Padding(
                              EdgeInsets.All(20),
                              new Container(style: new BoxStyle { Color = colors.Primary, Height = 32 })
                            )
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Wrap",
                    description:
                    "Flows children into runs, wrapping to the next line when the main axis overflows.",
                    demo: new Wrap(
                        spacing: 6,
                        runSpacing: 6,
                        children:
                        [
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Primary,
                                    CornerRadius = Vector4.One * 12,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Layout",
                                    new TextStyle { FontSize = 12, Color = colors.OnPrimary })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Surface,
                                    CornerRadius = Vector4.One * 12,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Animation",
                                    new TextStyle { FontSize = 12, Color = colors.OnSurface })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Primary,
                                    CornerRadius = Vector4.One * 12,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Scroll",
                                    new TextStyle { FontSize = 12, Color = colors.OnPrimary })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Surface,
                                    CornerRadius = Vector4.One * 12,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Input",
                                    new TextStyle { FontSize = 12, Color = colors.OnSurface })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Primary,
                                    CornerRadius = Vector4.One * 12,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("State",
                                    new TextStyle { FontSize = 12, Color = colors.OnPrimary })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Surface,
                                    CornerRadius = Vector4.One * 12,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Events",
                                    new TextStyle { FontSize = 12, Color = colors.OnSurface })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Primary,
                                    CornerRadius = Vector4.One * 12,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Overlay",
                                    new TextStyle { FontSize = 12, Color = colors.OnPrimary })
                            ),
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Surface,
                                    CornerRadius = Vector4.One * 12,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border,
                                    Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 4)
                                },
                                new Text("Rendering",
                                    new TextStyle { FontSize = 12, Color = colors.OnSurface })
                            )
                        ]
                    ),
                    code: """
                          new Wrap(
                            spacing: 6, runSpacing: 6,
                            children:
                            [
                              new Container(
                                style: new BoxStyle { Color = colors.Primary, CornerRadius = Vector4.One * 12 },
                                child: new Text("Tag")
                              ),
                              // ...more children wrap to the next line automatically
                            ]
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Stack + Positioned",
                    description:
                    "Layers children. Positioned anchors a child at absolute coordinates within the stack.",
                    demo: new SizedBox(
                        220,
                        80,
                        new Stack(
                        [
                            new Container(
                                new BoxStyle
                                {
                                    Color = colors.Surface,
                                    CornerRadius = Vector4.One * 8,
                                    BorderThickness = 1f,
                                    BorderColor = colors.Border
                                }
                            ),
                            new Positioned(
                                right: 0,
                                top: 0,
                                child: new Container(
                                    new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        CornerRadius = new Vector4(6, 0, 0, 6),
                                        Padding = EdgeInsets.Symmetric(horizontal: 8,
                                            vertical: 4)
                                    },
                                    new Text("NEW",
                                        new TextStyle
                                        {
                                            FontSize = 10,
                                            Weight = FontWeight.Bold,
                                            Color = colors.OnPrimary
                                        })
                                )
                            ),
                            new Positioned(
                                16,
                                0,
                                bottom: 0,
                                child: new Center(
                                    new Text("Content area",
                                        new TextStyle { FontSize = 13, Color = colors.OnSurface })
                                )
                            )
                        ])
                    ),
                    code: """
                          new Stack(
                          [
                            new Container(/* base card */),
                            new Positioned(
                              right: 0, top: 0,
                              child: new Container(/* badge */)
                            ),
                            new Positioned(
                              left: 16, top: 0, bottom: 0,
                              child: new Center(child: new Text("Content"))
                            )
                          ])
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Align",
                    description:
                    "Positions a child using normalized (-1,-1)→(1,1) space: TopLeft, Center, BottomRight, etc.",
                    demo: new SizedBox(
                        height: 100,
                        child: new Container(
                            new BoxStyle
                            {
                                Color = colors.Surface,
                                CornerRadius = Vector4.One * 6,
                                BorderThickness = 1f,
                                BorderColor = colors.Border
                            },
                            new Stack(
                            [
                                new Align(
                                    Alignment.TopLeft,
                                    new Container(new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 12,
                                        Height = 12,
                                        CornerRadius = Vector4.One * 6
                                    })
                                ),
                                new Align(
                                    Alignment.Center,
                                    new Container(new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 12,
                                        Height = 12,
                                        CornerRadius = Vector4.One * 6
                                    })
                                ),
                                new Align(
                                    Alignment.BottomRight,
                                    new Container(new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 12,
                                        Height = 12,
                                        CornerRadius = Vector4.One * 6
                                    })
                                )
                            ])
                        )
                    ),
                    code: """
                          new Align(alignment: Alignment.TopLeft,    child: myWidget)
                          new Align(alignment: Alignment.Center,     child: myWidget)
                          new Align(alignment: Alignment.BottomRight, child: myWidget)
                          // Custom: new Align(alignment: new Alignment(0.5f, -1f), child: myWidget)
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "BoxShadow",
                    description:
                    "Drop shadows (outer) and inset shadows. Multiple shadows stacked in array order.",
                    demo: new Center(
                        new Container(
                            new BoxStyle
                            {
                                Color = colors.Surface,
                                Width = 220,
                                Height = 64,
                                CornerRadius = Vector4.One * 8,
                                BoxShadows = new[]
                                {
                                    new BoxShadow(
                                        new Vector4(0f, 0f, 0f, 0.5f),
                                        new Vector2(0f, 4f),
                                        12f),
                                    new BoxShadow(
                                        new Vector4(
                                            colors.Primary.X,
                                            colors.Primary.Y,
                                            colors.Primary.Z,
                                            0.15f),
                                        Vector2.Zero,
                                        20f),
                                    new BoxShadow(
                                        new Vector4(0f, 0f, 0f, 0.25f),
                                        new Vector2(0f, 2f),
                                        6f,
                                        Inset: true)
                                }
                            },
                            new Center(
                                new Text("Shadowed card",
                                    new TextStyle { FontSize = 13, Color = colors.OnSurface })
                            )
                        )
                    ),
                    code: """
                          new Container(
                            style: new BoxStyle
                            {
                              BoxShadows = new[]
                              {
                                new BoxShadow(Color: rgba,  Offset: new Vector2(0, 4), BlurRadius: 12f),
                                new BoxShadow(Color: glow,  Offset: Vector2.Zero,      BlurRadius: 20f),
                                new BoxShadow(Color: inner, Offset: new Vector2(0, 2), BlurRadius: 6f, Inset: true)
                              }
                            }
                          )
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Divider",
                    description:
                    "A thin separator line. Horizontal stretches across a Column; vertical across a Row. Indent/endIndent inset the ends.",
                    demo: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        spacing: 8,
                        children:
                        [
                            new Text("Above",
                                new TextStyle { FontSize = 13, Color = colors.OnSurface }),
                            new Divider(),
                            new Text("Below",
                                new TextStyle { FontSize = 13, Color = colors.OnSurface }),
                            new Divider(2, 24, 24, colors.Primary),
                            new SizedBox(
                                height: 32,
                                child: new Row(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    children:
                                    [
                                        new Text("Left",
                                            new TextStyle
                                            {
                                                FontSize = 13, Color = colors.OnSurface
                                            }),
                                        new Padding(
                                            EdgeInsets.Symmetric(horizontal: 10),
                                            new Divider(axis: Axis.Vertical)
                                        ),
                                        new Text("Right",
                                            new TextStyle
                                            {
                                                FontSize = 13, Color = colors.OnSurface
                                            })
                                    ]
                                )
                            )
                        ]
                    ),
                    code: """
                          new Divider()                       // 1px horizontal, theme border color
                          new Divider(
                            thickness: 2, indent: 24, endIndent: 24, color: colors.Primary
                          )
                          new Divider(axis: Axis.Vertical)    // inside a Row
                          """,
                    capi: Capi
                ),

                new DemoCard(
                    "Marquee",
                    description:
                    "A single line of text that scrolls horizontally when it does not fit. Text that fits is drawn statically.",
                    demo: new Container(
                        new BoxStyle
                        {
                            Color = colors.Background,
                            CornerRadius = Vector4.One * 4,
                            BorderThickness = 1f,
                            BorderColor = colors.Border,
                            Padding = EdgeInsets.Symmetric(horizontal: 10, vertical: 8)
                        },
                        new SizedBox(
                            240,
                            child: new Marquee(
                                "This label is far too long to fit, so it scrolls.",
                                new TextStyle { FontSize = 13, Color = colors.OnSurface },
                                36
                            )
                        )
                    ),
                    code: """
                          new SizedBox(
                            width: 240,
                            child: new Marquee(
                              "This label is far too long to fit, so it scrolls.",
                              new TextStyle { FontSize = 13, Color = colors.OnSurface },
                              velocity: 36
                            )
                          )
                          """,
                    capi: Capi
                )
            ]
        );
    }
}

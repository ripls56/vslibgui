using System;
using Gui.Example.Shared;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Pages;

internal class AnimationsPage : StatefulWidget
{
    public AnimationsPage(ICoreClientAPI capi)
    {
        Capi = capi;
    }

    public ICoreClientAPI Capi { get; }

    public override State CreateState() => new AnimationsPageState();

    private class AnimationsPageState : State<AnimationsPage>
    {
        private AnimationController _ctrl = null!;
        private CurvedAnimation _curved = null!;
        private bool _expanded;
        private bool _visible = true;

        public override void InitState()
        {
            base.InitState();
            _ctrl = new AnimationController(TimeSpan.FromMilliseconds(800),
                Element.Owner!.GetTickerProvider());
            _curved = new CurvedAnimation(_ctrl, Curves.EaseInOut);
            _ctrl.OnStatusChanged += OnStatus;
            _ctrl.Forward();
        }

        private void OnStatus(AnimationStatus s)
        {
            if (s == AnimationStatus.Completed)
            {
                _ctrl.Reverse();
            }
            else if (s == AnimationStatus.Dismissed)
            {
                _ctrl.Forward();
            }
        }

        public override void Dispose()
        {
            _ctrl.Dispose();
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
                    new Text("Animations",
                        new TextStyle
                        {
                            FontSize = 22, Weight = FontWeight.Bold, Color = colors.Primary
                        }),

                    new DemoCard(
                        "AnimatedContainer",
                        description:
                        "Smoothly interpolates BoxStyle changes (color, size, radius) on rebuild.",
                        demo: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 10,
                            children:
                            [
                                new Button(
                                    new Text(
                                        _expanded ? "Collapse" : "Expand",
                                        new TextStyle { Color = colors.OnPrimary }
                                    ),
                                    onTap: _ => SetState(() => _expanded = !_expanded)
                                ),
                                new AnimatedContainer(
                                    duration: TimeSpan.FromMilliseconds(300),
                                    style: new BoxStyle
                                    {
                                        Color = _expanded ? colors.Primary : colors.Surface,
                                        Height = _expanded ? 72f : 28f,
                                        CornerRadius = Vector4.One * 6,
                                        BorderThickness = _expanded ? 0f : 1f,
                                        BorderColor = colors.Border
                                    }
                                )
                            ]
                        ),
                        code: """
                              new AnimatedContainer(
                                duration: TimeSpan.FromMilliseconds(300),
                                style: new BoxStyle
                                {
                                  Color  = _expanded ? colors.Primary : colors.Surface,
                                  Height = _expanded ? 72f : 28f
                                }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedOpacity",
                        description:
                        "Fades a child widget in and out by interpolating its opacity.",
                        demo: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 10,
                            children:
                            [
                                new Button(
                                    new Text(
                                        _visible ? "Hide" : "Show",
                                        new TextStyle { Color = colors.OnPrimary }
                                    ),
                                    onTap: _ => SetState(() => _visible = !_visible)
                                ),
                                new AnimatedOpacity(
                                    _visible ? 1f : 0f,
                                    TimeSpan.FromMilliseconds(250),
                                    child: new Container(
                                        new BoxStyle
                                        {
                                            Color = colors.Primary,
                                            Height = 48,
                                            CornerRadius = Vector4.One * 6
                                        }
                                    )
                                )
                            ]
                        ),
                        code: """
                              new AnimatedOpacity(
                                opacity:  _visible ? 1f : 0f,
                                duration: TimeSpan.FromMilliseconds(250),
                                child:    new Container(...)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedPadding",
                        description: "Smoothly transitions padding around a child.",
                        demo: new Column(
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            spacing: 10,
                            children:
                            [
                                new Button(
                                    new Text(
                                        _expanded ? "Less padding" : "More padding",
                                        new TextStyle { Color = colors.OnPrimary }
                                    ),
                                    onTap: _ => SetState(() => _expanded = !_expanded)
                                ),
                                new Container(
                                    new BoxStyle
                                    {
                                        Color = colors.Surface,
                                        CornerRadius = Vector4.One * 6,
                                        BorderThickness = 1f,
                                        BorderColor = colors.Border
                                    },
                                    new AnimatedPadding(
                                        _expanded ? EdgeInsets.All(24) : EdgeInsets.All(6),
                                        TimeSpan.FromMilliseconds(300),
                                        child: new Container(
                                            new BoxStyle
                                            {
                                                Color = colors.Primary,
                                                Height = 32,
                                                CornerRadius = Vector4.One * 4
                                            }
                                        )
                                    )
                                )
                            ]
                        ),
                        code: """
                              new AnimatedPadding(
                                padding:  _expanded ? EdgeInsets.All(24) : EdgeInsets.All(6),
                                duration: TimeSpan.FromMilliseconds(300),
                                child:    new Container(...)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedBuilder",
                        description:
                        "Rebuilds a subtree every animation frame. Uses an AnimationController with ping-pong loop.",
                        demo: new SizedBox(
                            height: 80,
                            child: new AnimatedBuilder(
                                _ctrl,
                                ctx =>
                                {
                                    var size = new FloatTween(14f, 26f).Evaluate(_curved);
                                    var alpha = new FloatTween(0.4f, 1f).Evaluate(_curved);
                                    return new Center(
                                        new Text("Animated text", new TextStyle
                                        {
                                            FontSize = size,
                                            Weight = FontWeight.Bold,
                                            Color = new Vector4(
                                                colors.Primary.X,
                                                colors.Primary.Y,
                                                colors.Primary.Z,
                                                alpha)
                                        })
                                    );
                                }
                            )
                        ),
                        code: """
                              _ctrl = new AnimationController(TimeSpan.FromMilliseconds(800),
                                  Element.Owner!.GetTickerProvider());
                              _ctrl.OnStatusChanged += s =>
                              {
                                  if (s == AnimationStatus.Completed) _ctrl.Reverse();
                                  else if (s == AnimationStatus.Dismissed) _ctrl.Forward();
                              };
                              _ctrl.Forward();

                              new AnimatedBuilder(
                                animation: _ctrl,
                                builder: ctx =>
                                {
                                  float size = new FloatTween(14f, 26f).Evaluate(_curved);
                                  return new Text("Animated", new TextStyle { FontSize = size });
                                }
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedScale",
                        description:
                        "Scales a child around its center. EaseOutBack gives a springy overshoot.",
                        demo: new ScaleDemo(Widget.Capi),
                        code: """
                              bool _scaled = false;

                              new AnimatedScale(
                                scale:     _scaled ? 1.5f : 1f,
                                duration:  TimeSpan.FromMilliseconds(300),
                                curve:     Curves.EaseOutBack,
                                alignment: Alignment.Center,
                                child:     new Container(...)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedRotation",
                        description:
                        "Rotates a child by an angle in radians. Each click accumulates 90°.",
                        demo: new RotateDemo(Widget.Capi),
                        code: """
                              float _angle = 0f;

                              new AnimatedRotation(
                                angle:     _angle,
                                duration:  TimeSpan.FromMilliseconds(350),
                                curve:     Curves.EaseOut,
                                alignment: Alignment.Center,
                                child:     new Container(...)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedSlide",
                        description:
                        "Translates a child by a pixel offset. Click to slide right and back.",
                        demo: new SlideDemo(Widget.Capi),
                        code: """
                              bool _slid = false;

                              new AnimatedSlide(
                                offset:   _slid ? new Vector2(80, 0) : Vector2.Zero,
                                duration: TimeSpan.FromMilliseconds(400),
                                curve:    Curves.EaseInOut,
                                child:    new Container(...)
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "Curves comparison",
                        description:
                        "All 25 easing curves animate the same translation simultaneously. Click Play to compare.",
                        demo: new CurvesShowcaseDemo(Widget.Capi),
                        code: """
                              new AnimatedSlide(
                                offset:   _playing ? new Vector2(164, 0) : Vector2.Zero,
                                duration: TimeSpan.FromMilliseconds(900),
                                curve:    Curves.EaseOutElastic,   // swap any curve here
                                child:    dot
                              )
                              """,
                        capi: Widget.Capi
                    ),

                    new DemoCard(
                        "AnimatedSize",
                        description:
                        "Animates to the child's new size whenever it changes layout. Click to reveal extra content.",
                        demo: new SizeDemo(Widget.Capi),
                        code: """
                              bool _expanded = false;

                              new AnimatedSize(
                                duration: TimeSpan.FromMilliseconds(300),
                                curve:    Curves.EaseInOut,
                                child: _expanded
                                  ? new Column(children: [ header, extraContent ])
                                  : header
                              )
                              """,
                        capi: Widget.Capi
                    )
                ]
            );
        }
    }

    private class ScaleDemo : StatefulWidget
    {
        public ScaleDemo(ICoreClientAPI capi)
        {
            Capi = capi;
        }

        public ICoreClientAPI Capi { get; }

        public override State CreateState() => new ScaleDemoState();

        private class ScaleDemoState : State<ScaleDemo>
        {
            private bool _scaled;

            public override Widget Build(BuildContext context)
            {
                var colors = Theme.Of(context).ColorScheme;
                return new Column(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    spacing: 12,
                    children:
                    [
                        new Button(
                            new Text(_scaled ? "Shrink" : "Scale up",
                                new TextStyle { Color = colors.OnPrimary }),
                            onTap: _ => SetState(() => _scaled = !_scaled)
                        ),
                        new SizedBox(
                            height: 80,
                            child: new Center(
                                new AnimatedScale(
                                    _scaled ? 1.5f : 1f,
                                    TimeSpan.FromMilliseconds(300),
                                    Curves.EaseOutBack,
                                    alignment: Alignment.Center,
                                    child: new Container(
                                        new BoxStyle
                                        {
                                            Color = colors.Primary,
                                            Width = 48,
                                            Height = 48,
                                            CornerRadius = Vector4.One * 8
                                        }
                                    )
                                )
                            )
                        )
                    ]
                );
            }
        }
    }

    private class RotateDemo : StatefulWidget
    {
        public RotateDemo(ICoreClientAPI capi)
        {
            Capi = capi;
        }

        public ICoreClientAPI Capi { get; }

        public override State CreateState() => new RotateDemoState();

        private class RotateDemoState : State<RotateDemo>
        {
            private float _angle;

            public override Widget Build(BuildContext context)
            {
                var colors = Theme.Of(context).ColorScheme;
                return new Column(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    spacing: 12,
                    children:
                    [
                        new Button(
                            new Text("Rotate 90°", new TextStyle { Color = colors.OnPrimary }),
                            onTap: _ => SetState(() => _angle += MathF.PI / 2f)
                        ),
                        new SizedBox(
                            height: 80,
                            child: new Center(
                                new AnimatedRotation(
                                    _angle,
                                    TimeSpan.FromMilliseconds(350),
                                    Curves.EaseOut,
                                    alignment: Alignment.Center,
                                    child: new Container(
                                        new BoxStyle
                                        {
                                            Color = colors.Primary,
                                            Width = 48,
                                            Height = 48,
                                            CornerRadius = Vector4.One * 4
                                        }
                                    )
                                )
                            )
                        )
                    ]
                );
            }
        }
    }

    private class SlideDemo : StatefulWidget
    {
        public SlideDemo(ICoreClientAPI capi)
        {
            Capi = capi;
        }

        public ICoreClientAPI Capi { get; }

        public override State CreateState() => new SlideDemoState();

        private class SlideDemoState : State<SlideDemo>
        {
            private bool _slid;

            public override Widget Build(BuildContext context)
            {
                var colors = Theme.Of(context).ColorScheme;
                return new Column(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    spacing: 12,
                    children:
                    [
                        new Button(
                            new Text(_slid ? "Slide back" : "Slide right",
                                new TextStyle { Color = colors.OnPrimary }),
                            onTap: _ => SetState(() => _slid = !_slid)
                        ),
                        new SizedBox(
                            height: 60,
                            child: new AnimatedSlide(
                                _slid ? new Vector2(80, 0) : Vector2.Zero,
                                TimeSpan.FromMilliseconds(400),
                                Curves.EaseInOut,
                                child: new Container(
                                    new BoxStyle
                                    {
                                        Color = colors.Primary,
                                        Width = 60,
                                        Height = 40,
                                        CornerRadius = Vector4.One * 6
                                    }
                                )
                            )
                        )
                    ]
                );
            }
        }
    }

    private class CurvesShowcaseDemo : StatefulWidget
    {
        public CurvesShowcaseDemo(ICoreClientAPI capi)
        {
            Capi = capi;
        }

        public ICoreClientAPI Capi { get; }

        public override State CreateState() => new CurvesShowcaseDemoState();

        private class CurvesShowcaseDemoState : State<CurvesShowcaseDemo>
        {
            private bool _playing;

            private static Widget MakeRow(string label, Curve curve, Vector2 offset,
                Vector4 labelColor,
                Vector4 dotColor)
            {
                return new Row(
                    crossAxisAlignment: CrossAxisAlignment.Center,
                    children:
                    [
                        new SizedBox(
                            128,
                            child: new Text(label,
                                new TextStyle { FontSize = 11, Color = labelColor })
                        ),
                        new SizedBox(
                            180,
                            18,
                            new AnimatedSlide(
                                offset,
                                TimeSpan.FromMilliseconds(900),
                                curve,
                                child: new Container(
                                    new BoxStyle
                                    {
                                        Color = dotColor,
                                        Width = 14,
                                        Height = 14,
                                        CornerRadius = Vector4.One * 7
                                    }
                                )
                            )
                        )
                    ]
                );
            }

            public override Widget Build(BuildContext context)
            {
                var colors = Theme.Of(context).ColorScheme;
                var off = _playing ? new Vector2(164, 0) : Vector2.Zero;
                var lc = colors.OnSurface;
                var dc = colors.Primary;

                return new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    spacing: 3,
                    children:
                    [
                        new Button(
                            new Text(_playing ? "Reset" : "Play",
                                new TextStyle { Color = colors.OnPrimary }),
                            onTap: _ => SetState(() => _playing = !_playing)
                        ),
                        MakeRow("Linear", Curves.Linear, off, lc, dc),
                        MakeRow("EaseIn", Curves.EaseIn, off, lc, dc),
                        MakeRow("EaseOut", Curves.EaseOut, off, lc, dc),
                        MakeRow("EaseInOut", Curves.EaseInOut, off, lc, dc),
                        MakeRow("EaseInCubic", Curves.EaseInCubic, off, lc, dc),
                        MakeRow("EaseOutCubic", Curves.EaseOutCubic, off, lc, dc),
                        MakeRow("EaseInOutCubic", Curves.EaseInOutCubic, off, lc, dc),
                        MakeRow("EaseInQuart", Curves.EaseInQuart, off, lc, dc),
                        MakeRow("EaseOutQuart", Curves.EaseOutQuart, off, lc, dc),
                        MakeRow("EaseInOutQuart", Curves.EaseInOutQuart, off, lc, dc),
                        MakeRow("EaseInQuint", Curves.EaseInQuint, off, lc, dc),
                        MakeRow("EaseOutQuint", Curves.EaseOutQuint, off, lc, dc),
                        MakeRow("EaseInOutQuint", Curves.EaseInOutQuint, off, lc, dc),
                        MakeRow("EaseInExpo", Curves.EaseInExpo, off, lc, dc),
                        MakeRow("EaseOutExpo", Curves.EaseOutExpo, off, lc, dc),
                        MakeRow("EaseInOutExpo", Curves.EaseInOutExpo, off, lc, dc),
                        MakeRow("EaseInBack", Curves.EaseInBack, off, lc, dc),
                        MakeRow("EaseOutBack", Curves.EaseOutBack, off, lc, dc),
                        MakeRow("EaseInOutBack", Curves.EaseInOutBack, off, lc, dc),
                        MakeRow("EaseInElastic", Curves.EaseInElastic, off, lc, dc),
                        MakeRow("EaseOutElastic", Curves.EaseOutElastic, off, lc, dc),
                        MakeRow("EaseInOutElastic", Curves.EaseInOutElastic, off, lc, dc),
                        MakeRow("BounceIn", Curves.BounceIn, off, lc, dc),
                        MakeRow("BounceOut", Curves.BounceOut, off, lc, dc),
                        MakeRow("BounceInOut", Curves.BounceInOut, off, lc, dc)
                    ]
                );
            }
        }
    }

    private class SizeDemo : StatefulWidget
    {
        public SizeDemo(ICoreClientAPI capi)
        {
            Capi = capi;
        }

        public ICoreClientAPI Capi { get; }

        public override State CreateState() => new SizeDemoState();

        private class SizeDemoState : State<SizeDemo>
        {
            private bool _expanded;

            public override Widget Build(BuildContext context)
            {
                var colors = Theme.Of(context).ColorScheme;

                Widget header = new Container(
                    new BoxStyle
                    {
                        Color = colors.Primary, Height = 36, CornerRadius = Vector4.One * 6
                    },
                    new Center(
                        new Text("Header", new TextStyle { Color = colors.OnPrimary })
                    )
                );

                Widget extra = new Container(
                    new BoxStyle
                    {
                        Color = colors.Surface,
                        Height = 56,
                        CornerRadius = Vector4.One * 6,
                        BorderThickness = 1f,
                        BorderColor = colors.Border
                    },
                    new Center(
                        new Text("Extra content", new TextStyle { Color = colors.OnSurface })
                    )
                );

                return new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10,
                    children:
                    [
                        new Button(
                            new Text(_expanded ? "Collapse" : "Expand",
                                new TextStyle { Color = colors.OnPrimary }),
                            onTap: _ => SetState(() => _expanded = !_expanded)
                        ),
                        new AnimatedSize(
                            TimeSpan.FromMilliseconds(300),
                            Curves.EaseInOut,
                            _expanded
                                ? new Column(
                                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                                    spacing: 6,
                                    children: [header, extra]
                                )
                                : header
                        )
                    ]
                );
            }
        }
    }
}

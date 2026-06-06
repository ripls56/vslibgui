using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Shared;

internal class CodeSnippet : StatelessWidget
{
    public CodeSnippet(string code, ICoreClientAPI capi)
    {
        Code = code;
        Capi = capi;
    }

    public string Code { get; }
    public ICoreClientAPI Capi { get; }

    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;
        var codeBg = new Vector4(
            colors.Background.X * 0.70f,
            colors.Background.Y * 0.70f,
            colors.Background.Z * 0.70f,
            1f
        );

        return new Container(
            new BoxStyle
            {
                Color = codeBg,
                CornerRadius = Vector4.One * 4,
                BorderThickness = 1f,
                BorderColor = colors.Border,
                ClipBehavior = ClipBehavior.HardEdge
            },
            new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children:
                [
                    new Padding(
                        EdgeInsets.All(12),
                        new Text(Code,
                            new TextStyle
                            {
                                FontFamily = "JetBrains Mono",
                                FontSize = 12,
                                Color = colors.OnSurface,
                                SoftWrap = false
                            })
                    ),
                    new Container(new BoxStyle { Color = colors.Border, Height = 1 }),
                    new CopyBar(Code, Capi, colors.Primary, colors.OnSurface)
                ]
            )
        );
    }

    private class CopyBar : StatefulWidget
    {
        public CopyBar(
            string code,
            ICoreClientAPI capi,
            Vector4 primary,
            Vector4 onSurface
        )
        {
            Code = code;
            Capi = capi;
            Primary = primary;
            OnSurface = onSurface;
        }

        public string Code { get; }
        public ICoreClientAPI Capi { get; }
        public Vector4 Primary { get; }
        public Vector4 OnSurface { get; }

        public override Widgets.Framework.State CreateState() => new State();

        private class State : State<CopyBar>
        {
            private bool _copied;
            private bool _hovered;

            public override Widget Build(BuildContext context)
            {
                var label = _copied ? "Copied!" : "Copy";
                var color = _hovered
                    ? Widget.Primary
                    : new Vector4(Widget.OnSurface.X, Widget.OnSurface.Y, Widget.OnSurface.Z, 0.4f);
                var bg = _hovered
                    ? new Vector4(Widget.Primary.X * 0.12f, Widget.Primary.Y * 0.12f,
                        Widget.Primary.Z * 0.12f, 1f)
                    : Vector4.Zero;

                return new GestureDetector(
                    onTap: _ =>
                    {
                        Widget.Capi.Input.ClipboardText = Widget.Code;
                        SetState(() => _copied = true);
                    },
                    onEnter: _ => SetState(() => _hovered = true),
                    onExit: _ => SetState(() =>
                    {
                        _hovered = false;
                        _copied = false;
                    }),
                    child: new Container(
                        new BoxStyle { Color = bg },
                        new Padding(
                            EdgeInsets.Symmetric(6, 12),
                            new Row(
                                children:
                                [
                                    new Expanded(new SizedBox()),
                                    new Text(label,
                                        new TextStyle
                                        {
                                            FontFamily = "JetBrains Mono",
                                            FontSize = 11,
                                            Color = color
                                        })
                                ]
                            )
                        )
                    )
                );
            }
        }
    }
}

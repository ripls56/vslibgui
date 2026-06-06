using System.Collections.Generic;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Gui.Example.Shared;

internal class DemoCard : StatelessWidget
{
    public DemoCard(
        string title,
        Widget demo,
        string code,
        ICoreClientAPI capi,
        string? description = null
    )
    {
        Title = title;
        Demo = demo;
        Code = code;
        Capi = capi;
        Description = description;
    }

    public string Title { get; }
    public Widget Demo { get; }
    public string Code { get; }
    public ICoreClientAPI Capi { get; }
    public string? Description { get; }

    public override Widget Build(BuildContext context)
    {
        var colors = Theme.Of(context).ColorScheme;
        var children = new List<Widget>
        {
            new Text(Title,
                new TextStyle { FontSize = 15, Weight = FontWeight.Bold, Color = colors.OnSurface })
        };

        if (Description != null)
        {
            children.Add(new Text(Description,
                new TextStyle
                {
                    FontSize = 12,
                    Color = new Vector4(colors.OnSurface.X, colors.OnSurface.Y,
                        colors.OnSurface.Z, 0.6f)
                }));
        }

        children.Add(new Padding(EdgeInsets.Symmetric(8), Demo));
        children.Add(new CodeSnippet(Code, Capi));

        return new Container(
            new BoxStyle
            {
                Color = colors.Surface,
                CornerRadius = Vector4.One * 6,
                BorderThickness = 1f,
                BorderColor = colors.Border,
                Padding = EdgeInsets.All(16)
            },
            new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 10,
                children: children
            )
        );
    }
}

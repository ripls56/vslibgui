using Gui.Rendering;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Framework;

[TestFixture, NonParallelizable]
public class GoldenTests
{
    [Test]
    public void FlexRowBasic()
    {
        var widget = new Row(children:
        [
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(1, 0, 0, 1) }),
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(0, 1, 0, 1) }),
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(0, 0, 1, 1) })
        ]);

        GoldenAssert.Matches(widget, "flex_row_basic", new SKSizeI(300, 100));
    }

    [Test]
    public void FlexColumnBasic()
    {
        var widget = new Column(children:
        [
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(1, 0, 0, 1) }),
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(0, 1, 0, 1) }),
            new Container(new BoxStyle { Width = 100, Height = 50, Color = new Vector4(0, 0, 1, 1) })
        ]);

        GoldenAssert.Matches(widget, "flex_column_basic", new SKSizeI(100, 150));
    }

    [Test]
    public void FlexExpanded()
    {
        var widget = new Row(children:
        [
            new Expanded(new Container(new BoxStyle { Color = new Vector4(0.8f, 0.2f, 0.2f, 1) })),
            new Expanded(new Container(new BoxStyle { Color = new Vector4(0.2f, 0.2f, 0.8f, 1) }))
        ]);

        GoldenAssert.Matches(widget, "flex_expanded", new SKSizeI(200, 80));
    }

    [Test]
    public void StackBasic()
    {
        var widget = new Stack(
        [
            new Container(new BoxStyle { Width = 150, Height = 150, Color = new Vector4(0.3f, 0.6f, 0.3f, 1) }),
            new Container(new BoxStyle { Width = 80, Height = 80, Color = new Vector4(0.9f, 0.5f, 0.1f, 0.85f) })
        ]);

        GoldenAssert.Matches(widget, "stack_basic", new SKSizeI(150, 150));
    }

    [Test]
    public void ContainerBorder()
    {
        var widget = new Container(new BoxStyle
        {
            Width = 120,
            Height = 80,
            Color = new Vector4(0.15f, 0.15f, 0.35f, 1),
            BorderThickness = 3,
            BorderColor = new Vector4(0.8f, 0.8f, 1f, 1f),
            CornerRadius = new Vector4(8, 8, 8, 8)
        });

        GoldenAssert.Matches(widget, "container_border", new SKSizeI(120, 80));
    }

    [Test]
    public void PaddingNested()
    {
        var widget = new Padding(
            EdgeInsets.All(20),
            new Container(new BoxStyle
            {
                Color = new Vector4(0.5f, 0.2f, 0.7f, 1)
            })
        );

        GoldenAssert.Matches(widget, "padding_nested", new SKSizeI(150, 100));
    }
}

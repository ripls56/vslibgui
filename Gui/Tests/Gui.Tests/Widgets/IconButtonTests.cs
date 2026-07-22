using System;
using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class IconButtonTests
{
    private sealed class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static Element MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return element;
    }

    private static RenderObject LayoutWidget(Widget widget, LayoutConstraints constraints)
    {
        var element = MountTree(widget);
        var renderObject = element.RenderObject!;
        renderObject.Layout(constraints);
        return renderObject;
    }

    private static RenderBox? FindRenderBoxWithColor(Element root, Vector4 color)
    {
        RenderBox? result = null;

        void Visit(Element element)
        {
            if (result != null)
            {
                return;
            }

            if (element is RenderObjectElement { RenderObject: RenderBox renderBox } &&
                renderBox.Color == color)
            {
                result = renderBox;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }

    private static (RenderIcon Icon, float X, float Y)? FindRenderIcon
    (
        RenderObject root,
        float x = 0f,
        float y = 0f
    )
    {
        if (root is RenderIcon icon)
        {
            return (icon, x, y);
        }

        foreach (var child in root.Children)
        {
            var match = FindRenderIcon(child, x + child.X, y + child.Y);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    [Test]
    public void UsesDedicatedThemeStyle()
    {
        var background = new Vector4(0.8f, 0.1f, 0.2f, 1f);
        var style = IconButtonStyle.Default(ColorScheme.Default()) with
        {
            Primary = IconButtonStyle.Default(ColorScheme.Default()).Primary with
            {
                BackgroundColor = background
            }
        };
        var theme = new ThemeData(iconButtonStyle: style);

        var root = MountTree
        (
            new Theme(theme, new IconButton(new Icon("gui", "textures/icons/undo-2.svg")))
        );

        Assert.That(FindRenderBoxWithColor(root, background), Is.Not.Null);
    }

    [Test]
    public void CentersIcon()
    {
        var root = LayoutWidget
        (
            new SizedBox
            (
                40,
                40,
                new IconButton(new Icon("gui", "textures/icons/undo-2.svg", size: 12))
            ),
            LayoutConstraints.Tight(40, 40)
        );
        var icon = FindRenderIcon(root);

        Assert.That(icon, Is.Not.Null);
        Assert.That(icon!.Value.Icon.Size, Is.EqualTo(new Vector2(12f, 12f)));
        Assert.That(icon.Value.X, Is.EqualTo(14f));
        Assert.That(icon.Value.Y, Is.EqualTo(14f));
    }
}

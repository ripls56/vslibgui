using Gui.Core.Framework;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class ButtonStyleTests
{
    private class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static ColorScheme Scheme(
        Vector4? primary = null,
        Vector4? error = null,
        Vector4? border = null,
        Vector4? onSurface = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = new Vector4(0.3f, 0.3f, 0.3f, 1),
        OnSurface = onSurface ?? new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = border ?? new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = error ?? new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One
    };

    private static (BuildOwner, Element) MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        return (owner, el);
    }

    private static RenderBox? FindRenderBoxWithColor(Element root, Vector4 color)
    {
        RenderBox? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el is RenderObjectElement roe && roe.RenderObject is RenderBox rb &&
                rb.Color == color)
            {
                result = rb;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }


    [Test]
    public void Default_Primary_MapsPrimaryToBackground()
    {
        var primary = new Vector4(0.1f, 0.4f, 0.8f, 1);
        var style = ButtonStyle.Default(Scheme(primary));
        Assert.That(style.Primary.BackgroundColor, Is.EqualTo(primary));
    }

    [Test]
    public void Default_Danger_MapsErrorToBackground()
    {
        var error = new Vector4(0.8f, 0.1f, 0.1f, 1);
        var style = ButtonStyle.Default(Scheme(error: error));
        Assert.That(style.Danger.BackgroundColor, Is.EqualTo(error));
    }

    [Test]
    public void Default_Secondary_HasTransparentBackground()
    {
        var style = ButtonStyle.Default(Scheme());
        Assert.That(style.Secondary.BackgroundColor.W, Is.EqualTo(0f));
    }

    [Test]
    public void Default_Ghost_HasTransparentBackground()
    {
        var style = ButtonStyle.Default(Scheme());
        Assert.That(style.Ghost.BackgroundColor.W, Is.EqualTo(0f));
    }

    [Test]
    public void Default_Secondary_MapsBorderToBorderColor()
    {
        var border = new Vector4(0.5f, 0.5f, 0.1f, 1);
        var style = ButtonStyle.Default(Scheme(border: border));
        Assert.That(style.Secondary.BorderColor, Is.EqualTo(border));
    }

    [Test]
    public void Indexer_ReturnsCorrectVariantStyle()
    {
        var primary = new Vector4(0.1f, 0.4f, 0.8f, 1);
        var style = ButtonStyle.Default(Scheme(primary));
        Assert.That(style[ButtonVariant.Primary].BackgroundColor, Is.EqualTo(primary));
    }


    [Test]
    public void WithNoStyle_UsesThemePrimaryBackground()
    {
        var bgColor = new Vector4(0.1f, 0.6f, 0.2f, 1);
        var themeStyle = ButtonStyle.Default(ColorScheme.Default()) with
        {
            Primary = ButtonStyle.Default(ColorScheme.Default()).Primary with
            {
                BackgroundColor = bgColor
            }
        };
        var theme = new ThemeData(buttonStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new Button(new SizedBox())));

        Assert.That(FindRenderBoxWithColor(root, bgColor), Is.Not.Null);
    }

    [Test]
    public void WithStyleOverride_UsesOverridePrimaryBackground()
    {
        var bgColor = new Vector4(0.9f, 0.1f, 0.5f, 1);
        var overrideStyle = ButtonStyle.Default(ColorScheme.Default()) with
        {
            Primary = ButtonStyle.Default(ColorScheme.Default()).Primary with
            {
                BackgroundColor = bgColor
            }
        };
        var (_, root) = MountTree(new Button(new SizedBox(), style: overrideStyle));

        Assert.That(FindRenderBoxWithColor(root, bgColor), Is.Not.Null);
    }

    [Test]
    public void WithStyleOverride_IgnoresThemePrimaryBackground()
    {
        var themeColor = new Vector4(0, 1, 0, 1);
        var overrideColor = new Vector4(1, 0, 0, 1);
        var themeStyle = ButtonStyle.Default(ColorScheme.Default()) with
        {
            Primary = ButtonStyle.Default(ColorScheme.Default()).Primary with
            {
                BackgroundColor = themeColor
            }
        };
        var overrideStyle = ButtonStyle.Default(ColorScheme.Default()) with
        {
            Primary = ButtonStyle.Default(ColorScheme.Default()).Primary with
            {
                BackgroundColor = overrideColor
            }
        };
        var theme = new ThemeData(buttonStyle: themeStyle);
        var (_, root) =
            MountTree(new Theme(theme, new Button(new SizedBox(), style: overrideStyle)));

        Assert.That(FindRenderBoxWithColor(root, overrideColor), Is.Not.Null,
            "Override color should be present");
        Assert.That(FindRenderBoxWithColor(root, themeColor), Is.Null,
            "Theme color should not appear");
    }
}

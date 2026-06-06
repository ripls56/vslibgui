using Gui.Core.Framework;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class CheckboxStyleTests
{
    private class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static ColorScheme Scheme(
        Vector4? primary = null,
        Vector4? surface = null,
        Vector4? onSurface = null,
        Vector4? border = null,
        Vector4? surfaceHigh = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = surface ?? new Vector4(0.3f, 0.3f, 0.3f, 1),
        OnSurface = onSurface ?? new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = border ?? new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One,
        SurfaceHigh = surfaceHigh ?? new Vector4(0.35f, 0.35f, 0.35f, 1)
    };

    private static (BuildOwner, Element) MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
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
    public void Default_MapsPrimaryToCheckColor()
    {
        var primary = new Vector4(0.8f, 0.1f, 0.3f, 1);
        var style = CheckboxStyle.Default(Scheme(primary), TextTheme.Default());
        Assert.That(style.CheckColor, Is.EqualTo(primary));
    }

    [Test]
    public void Default_MapsSurfaceHighToBackgroundColor()
    {
        var surfaceHigh = new Vector4(0.6f, 0.2f, 0.4f, 1);
        var style = CheckboxStyle.Default(Scheme(surfaceHigh: surfaceHigh), TextTheme.Default());
        Assert.That(style.BackgroundColor, Is.EqualTo(surfaceHigh));
    }

    [Test]
    public void Default_MapsBorderToBorderColor()
    {
        var border = new Vector4(0.5f, 0.5f, 0.1f, 1);
        var style = CheckboxStyle.Default(Scheme(border: border), TextTheme.Default());
        Assert.That(style.BorderColor, Is.EqualTo(border));
    }

    [Test]
    public void Default_MapsOnSurfaceToLabelStyleColor()
    {
        var onSurface = new Vector4(0.7f, 0.8f, 0.6f, 1);
        var style = CheckboxStyle.Default(Scheme(onSurface: onSurface), TextTheme.Default());
        Assert.That(style.LabelStyle.Color, Is.EqualTo(onSurface));
    }

    [Test]
    public void Default_MapsBodyFontSizeToLabelStyleFontSize()
    {
        var text = new TextTheme
        {
            Headline = new TextStyle { FontSize = 24 },
            Body = new TextStyle { FontSize = 18 },
            Label = new TextStyle { FontSize = 12 }
        };
        var style = CheckboxStyle.Default(Scheme(), text);
        Assert.That(style.LabelStyle.FontSize, Is.EqualTo(text.Body.FontSize));
    }


    [Test]
    public void WithNoStyle_UsesThemeCheckColor()
    {
        var checkColor = new Vector4(0.1f, 0.9f, 0.3f, 1);
        var themeStyle = CheckboxStyle.Default(ColorScheme.Default(), TextTheme.Default()) with
        {
            CheckColor = checkColor
        };
        var theme = new ThemeData(checkboxStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new Checkbox(true)));

        Assert.That(FindRenderBoxWithColor(root, checkColor), Is.Not.Null);
    }

    [Test]
    public void WithStyleOverride_UsesOverrideCheckColor()
    {
        var overrideColor = new Vector4(0.9f, 0.1f, 0.5f, 1);
        var style = CheckboxStyle.Default(ColorScheme.Default(), TextTheme.Default()) with
        {
            CheckColor = overrideColor
        };
        var (_, root) = MountTree(new Checkbox(true, style: style));

        Assert.That(FindRenderBoxWithColor(root, overrideColor), Is.Not.Null);
    }

    [Test]
    public void WithStyleOverride_IgnoresThemeCheckColor()
    {
        var themeColor = new Vector4(0, 1, 0, 1);
        var overrideColor = new Vector4(1, 0, 0, 1);
        var themeStyle = CheckboxStyle.Default(ColorScheme.Default(), TextTheme.Default()) with
        {
            CheckColor = themeColor
        };
        var theme = new ThemeData(checkboxStyle: themeStyle);
        var overrideStyle = CheckboxStyle.Default(ColorScheme.Default(), TextTheme.Default()) with
        {
            CheckColor = overrideColor
        };
        var (_, root) = MountTree(new Theme(theme, new Checkbox(true, style: overrideStyle)));

        Assert.That(FindRenderBoxWithColor(root, overrideColor), Is.Not.Null,
            "Override color should be present");
        Assert.That(FindRenderBoxWithColor(root, themeColor), Is.Null,
            "Theme color should not appear");
    }
}

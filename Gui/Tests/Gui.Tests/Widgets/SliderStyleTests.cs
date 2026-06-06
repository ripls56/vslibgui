using Gui.Core.Framework;
using Gui.Core.Input;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class SliderStyleTests
{
    private static ColorScheme Scheme(
        Vector4? primary = null,
        Vector4? onSurface = null,
        Vector4? outlineVariant = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = new Vector4(0.3f, 0.3f, 0.3f, 1),
        OnSurface = onSurface ?? new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One,
        OutlineVariant = outlineVariant ?? new Vector4(0.4f, 0.4f, 0.4f, 0.25f)
    };

    private static (BuildOwner, Element) MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        return (owner, el);
    }

    private static T? FindRo<T>(Element root) where T : RenderObject
    {
        T? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el is RenderObjectElement roe && roe.RenderObject is T typed)
            {
                result = typed;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }


    [Test]
    public void Default_MapsPrimaryToActiveColor()
    {
        var primary = new Vector4(0.7f, 0.2f, 0.4f, 1);
        var style = SliderStyle.Default(Scheme(primary));
        Assert.That(style.ActiveColor, Is.EqualTo(primary));
    }

    [Test]
    public void Default_MapsPrimaryToThumbColor()
    {
        var primary = new Vector4(0.7f, 0.2f, 0.4f, 1);
        var style = SliderStyle.Default(Scheme(primary));
        Assert.That(style.ThumbColor, Is.EqualTo(primary));
    }

    [Test]
    public void Default_MapsOutlineVariantToInactiveColor()
    {
        var outlineVariant = new Vector4(0.6f, 0.7f, 0.8f, 0.25f);
        var style = SliderStyle.Default(Scheme(outlineVariant: outlineVariant));
        Assert.That(style.InactiveColor, Is.EqualTo(outlineVariant));
    }


    [Test]
    public void WithNoStyle_UsesThemeActiveColor()
    {
        var activeColor = new Vector4(0.1f, 0.8f, 0.4f, 1);
        var themeStyle = SliderStyle.Default(ColorScheme.Default()) with
        {
            ActiveColor = activeColor
        };
        var theme = new ThemeData(sliderStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new Slider(0.5f, showValueLabel: false)));

        var ro = FindRo<RenderSliderTrack>(root);
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.ActiveColor, Is.EqualTo(activeColor));
    }

    [Test]
    public void WithStyleOverride_UsesOverrideActiveColor()
    {
        var overrideColor = new Vector4(0.9f, 0.2f, 0.1f, 1);
        var style = SliderStyle.Default(ColorScheme.Default()) with { ActiveColor = overrideColor };
        var (_, root) = MountTree(new Slider(0.5f, style: style, showValueLabel: false));

        var ro = FindRo<RenderSliderTrack>(root);
        Assert.That(ro!.ActiveColor, Is.EqualTo(overrideColor));
    }

    [Test]
    public void WithStyleOverride_IgnoresThemeActiveColor()
    {
        var themeColor = new Vector4(0, 1, 0, 1);
        var overrideColor = new Vector4(1, 0, 0, 1);
        var themeStyle = SliderStyle.Default(ColorScheme.Default()) with
        {
            ActiveColor = themeColor
        };
        var theme = new ThemeData(sliderStyle: themeStyle);
        var overrideStyle = SliderStyle.Default(ColorScheme.Default()) with
        {
            ActiveColor = overrideColor
        };
        var (_, root) = MountTree(new Theme(theme,
            new Slider(0.5f, style: overrideStyle, showValueLabel: false)));

        var ro = FindRo<RenderSliderTrack>(root);
        Assert.That(ro!.ActiveColor, Is.EqualTo(overrideColor));
    }

    [Test]
    public void ActiveColorParam_OverridesStyleActiveColor()
    {
        var styleColor = new Vector4(0, 0, 1, 1);
        var paramColor = new Vector4(1, 0, 0, 1);
        var style = SliderStyle.Default(ColorScheme.Default()) with { ActiveColor = styleColor };
        var (_, root) = MountTree(new Slider(0.5f, activeColor: paramColor, style: style,
            showValueLabel: false));

        var ro = FindRo<RenderSliderTrack>(root);
        Assert.That(ro!.ActiveColor, Is.EqualTo(paramColor));
    }
}

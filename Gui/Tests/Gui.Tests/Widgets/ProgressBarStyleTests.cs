using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class ProgressBarStyleTests
{
    private static ColorScheme Scheme(
        Vector4? primary = null,
        Vector4? surface = null,
        Vector4? border = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = surface ?? new Vector4(0.3f, 0.3f, 0.3f, 1),
        OnSurface = new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = border ?? new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One
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
    public void Default_MapsPrimaryToFillColor()
    {
        var primary = new Vector4(0.9f, 0.1f, 0.1f, 1);
        var style = ProgressBarStyle.Default(Scheme(primary));
        Assert.That(style.FillColor, Is.EqualTo(primary));
    }

    [Test]
    public void Default_MapsSurfaceToTrackColor()
    {
        var surface = new Vector4(0.1f, 0.9f, 0.1f, 1);
        var style = ProgressBarStyle.Default(Scheme(surface: surface));
        Assert.That(style.TrackColor, Is.EqualTo(surface));
    }

    [Test]
    public void Default_MapsBorderToBorderColor()
    {
        var border = new Vector4(0.1f, 0.1f, 0.9f, 1);
        var style = ProgressBarStyle.Default(Scheme(border: border));
        Assert.That(style.BorderColor, Is.EqualTo(border));
    }


    [Test]
    public void WithNoStyle_UsesThemeFillColor()
    {
        var fillColor = new Vector4(0.1f, 0.8f, 0.2f, 1);
        var themeStyle = new ProgressBarStyle
        {
            FillColor = fillColor,
            TrackColor = Vector4.Zero,
            BorderColor = Vector4.Zero,
            Height = 10,
            CornerRadius = 2,
            BorderThickness = 1
        };
        var theme = new ThemeData(progressBarStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new ProgressBar(0.5f)));

        var ro = FindRo<RenderProgressBar>(root);
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.FillColor, Is.EqualTo(fillColor));
    }

    [Test]
    public void WithNoStyle_UsesThemeTrackColor()
    {
        var trackColor = new Vector4(0.5f, 0.0f, 0.8f, 1);
        var themeStyle = new ProgressBarStyle
        {
            FillColor = Vector4.Zero,
            TrackColor = trackColor,
            BorderColor = Vector4.Zero,
            Height = 10,
            CornerRadius = 2,
            BorderThickness = 1
        };
        var theme = new ThemeData(progressBarStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new ProgressBar(0.5f)));

        var ro = FindRo<RenderProgressBar>(root);
        Assert.That(ro!.TrackColor, Is.EqualTo(trackColor));
    }

    [Test]
    public void WithStyleOverride_UsesFillColor()
    {
        var overrideFill = new Vector4(1, 0.4f, 0, 1);
        var styleOverride = ProgressBarStyle.Default(ColorScheme.Default()) with
        {
            FillColor = overrideFill
        };
        var (_, root) = MountTree(new ProgressBar(0.5f, styleOverride));

        var ro = FindRo<RenderProgressBar>(root);
        Assert.That(ro!.FillColor, Is.EqualTo(overrideFill));
    }

    [Test]
    public void WithStyleOverride_IgnoresThemeFillColor()
    {
        var themeFill = new Vector4(0, 1, 0, 1);
        var overrideFill = new Vector4(1, 0, 0, 1);
        var themeStyle = ProgressBarStyle.Default(ColorScheme.Default()) with
        {
            FillColor = themeFill
        };
        var theme = new ThemeData(progressBarStyle: themeStyle);
        var styleOverride = ProgressBarStyle.Default(ColorScheme.Default()) with
        {
            FillColor = overrideFill
        };

        var (_, root) = MountTree(new Theme(theme, new ProgressBar(0.5f, styleOverride)));

        var ro = FindRo<RenderProgressBar>(root);
        Assert.That(ro!.FillColor, Is.EqualTo(overrideFill));
    }
}

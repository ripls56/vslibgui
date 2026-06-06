using Gui.Core.Framework;
using Gui.Rendering.Text;
using Gui.Tests.Helpers;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class DropdownStyleTests
{
    private static ColorScheme Scheme(
        Vector4? primary = null,
        Vector4? surface = null,
        Vector4? surfaceHigh = null,
        Vector4? background = null,
        Vector4? onSurface = null,
        Vector4? border = null,
        Vector4? stateHover = null,
        Vector4? stateSelected = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = surface ?? new Vector4(0.3f, 0.3f, 0.3f, 1),
        SurfaceHigh = surfaceHigh ?? new Vector4(0.35f, 0.35f, 0.35f, 1),
        OnSurface = onSurface ?? new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = background ?? new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = border ?? new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One,
        StateHover = stateHover ?? new Vector4(0.9f, 0.9f, 0.9f, 0.08f),
        StateSelected = stateSelected ?? new Vector4(0.2f, 0.5f, 0.9f, 0.18f)
    };

    private static List<DropdownItem<string>> OneItem() =>
        [new() { Value = "A", Label = "Alpha" }];

    private static (BuildOwner, Element) MountTree(Widget widget)
    {
        var owner = TestHelpers.NewBuildOwner();
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
    public void Default_MapsSurfaceToButtonColor()
    {
        var surface = new Vector4(0.6f, 0.1f, 0.3f, 1);
        var style = DropdownStyle.Default(Scheme(surface: surface), TextTheme.Default());
        Assert.That(style.ButtonColor, Is.EqualTo(surface));
    }

    [Test]
    public void Default_MapsBorderToBorderColor()
    {
        var border = new Vector4(0.7f, 0.2f, 0.5f, 1);
        var style = DropdownStyle.Default(Scheme(border: border), TextTheme.Default());
        Assert.That(style.BorderColor, Is.EqualTo(border));
    }

    [Test]
    public void Default_MapsOnSurfaceToTextStyleColor()
    {
        var onSurface = new Vector4(0.8f, 0.7f, 0.6f, 1);
        var style = DropdownStyle.Default(Scheme(onSurface: onSurface), TextTheme.Default());
        Assert.That(style.TextStyle.Color, Is.EqualTo(onSurface));
    }

    [Test]
    public void Default_MapsBodyFontSizeToTextStyleFontSize()
    {
        var text = new TextTheme
        {
            Headline = new TextStyle { FontSize = 24 },
            Body = new TextStyle { FontSize = 18 },
            Label = new TextStyle { FontSize = 12 }
        };
        var style = DropdownStyle.Default(Scheme(), text);
        Assert.That(style.TextStyle.FontSize, Is.EqualTo(text.Body.FontSize));
    }

    [Test]
    public void Default_MapsStateSelectedToSelectionColor()
    {
        var stateSelected = new Vector4(0.1f, 0.6f, 0.4f, 0.2f);
        var style =
            DropdownStyle.Default(Scheme(stateSelected: stateSelected), TextTheme.Default());
        Assert.That(style.SelectionColor, Is.EqualTo(stateSelected));
    }

    [Test]
    public void Default_MapsSurfaceHighToMenuColorXyz()
    {
        var surfaceHigh = new Vector4(0.05f, 0.07f, 0.09f, 1);
        var style = DropdownStyle.Default(Scheme(surfaceHigh: surfaceHigh), TextTheme.Default());
        Assert.That(style.MenuColor.X, Is.EqualTo(surfaceHigh.X).Within(0.001f));
        Assert.That(style.MenuColor.Y, Is.EqualTo(surfaceHigh.Y).Within(0.001f));
        Assert.That(style.MenuColor.Z, Is.EqualTo(surfaceHigh.Z).Within(0.001f));
    }

    [Test]
    public void Default_MapsStateHoverToHoverColor()
    {
        var stateHover = new Vector4(0.85f, 0.75f, 0.65f, 0.08f);
        var style = DropdownStyle.Default(Scheme(stateHover: stateHover), TextTheme.Default());
        Assert.That(style.HoverColor, Is.EqualTo(stateHover));
    }


    [Test]
    public void WithNoStyle_UsesThemeButtonColor()
    {
        var surface = new Vector4(0.4f, 0.6f, 0.8f, 1);
        var theme = new ThemeData(Scheme(surface: surface));
        var (_, root) = MountTree(new Theme(theme, new Dropdown<string>("A", OneItem())));

        var ro = FindRenderBoxWithColor(root, surface);
        Assert.That(ro, Is.Not.Null, "Expected RenderBox with ButtonColor from theme");
    }

    [Test]
    public void WithStyleOverride_UsesOverrideButtonColor()
    {
        var overrideColor = new Vector4(0.9f, 0.3f, 0.1f, 1);
        var customStyle = DropdownStyle.Default(ColorScheme.Default(), TextTheme.Default())
            with
            {
                ButtonColor = overrideColor
            };
        var (_, root) = MountTree(new Dropdown<string>("A", OneItem(), style: customStyle));

        var ro = FindRenderBoxWithColor(root, overrideColor);
        Assert.That(ro, Is.Not.Null, "Expected RenderBox with override ButtonColor");
    }

    [Test]
    public void WithStyleOverride_IgnoresThemeButtonColor()
    {
        var themeSurface = new Vector4(0, 1, 0, 1);
        var overrideColor = new Vector4(1, 0, 0, 1);
        var theme = new ThemeData(Scheme(surface: themeSurface));
        var customStyle = DropdownStyle.Default(ColorScheme.Default(), TextTheme.Default())
            with
            {
                ButtonColor = overrideColor
            };
        var (_, root) =
            MountTree(new Theme(theme, new Dropdown<string>("A", OneItem(), style: customStyle)));

        Assert.That(FindRenderBoxWithColor(root, overrideColor), Is.Not.Null,
            "Override color should be present");
        Assert.That(FindRenderBoxWithColor(root, themeSurface), Is.Null,
            "Theme color should not appear");
    }
}

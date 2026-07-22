using Gui.Core.Input;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Input;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class TextFieldStyleTests
{
    private class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private static ColorScheme Scheme
    (
        Vector4? background = null,
        Vector4? onSurface = null,
        Vector4? border = null,
        Vector4? primary = null
    ) => new()
    {
        Primary = primary ?? new Vector4(0.2f, 0.5f, 0.9f, 1),
        OnPrimary = Vector4.One,
        Surface = new Vector4(0.3f, 0.3f, 0.3f, 1),
        OnSurface = onSurface ?? new Vector4(0.9f, 0.9f, 0.9f, 1),
        Background = background ?? new Vector4(0.1f, 0.1f, 0.1f, 1),
        OnBackground = Vector4.One,
        Border = border ?? new Vector4(0.4f, 0.4f, 0.4f, 1),
        Error = new Vector4(0.9f, 0.2f, 0.2f, 1),
        OnError = Vector4.One
    };

    private static (BuildOwner owner, Element root) MountTree(Widget widget)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        return (owner, el);
    }

    private static RenderTextField? FindRenderTextField(Element root)
    {
        RenderTextField? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el is RenderObjectElement roe && roe.RenderObject is RenderTextField tf)
            {
                result = tf;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }


    [Test]
    public void PartialStyle_KeepsUsableDefaults()
    {
        var style = new TextFieldStyle
        {
            Height = 32
        };

        Assert.Multiple
        (
            () =>
            {
                Assert.That(style.TextStyle.FontFamily, Is.EqualTo("sans-serif"));
                Assert.That(style.TextStyle.FontSize, Is.GreaterThan(0));
                Assert.That(style.FillColor.W, Is.GreaterThan(0));
                Assert.That(style.BorderThickness, Is.GreaterThan(0));
            }
        );
    }

    [Test]
    public void WithNoStyle_UsesBorderColorFromTheme()
    {
        var border = new Vector4(0.8f, 0.2f, 0.5f, 1);
        var theme = new ThemeData(Scheme(border: border));
        var (_, root) = MountTree(new Theme(theme, new TextField()));

        var ro = FindRenderTextField(root);
        Assert.That(ro, Is.Not.Null);
        Assert.That(ro!.BorderColor, Is.EqualTo(border));
    }

    [Test]
    public void WithNoStyle_UsesBackgroundXyzForBoxColor()
    {
        var bg = new Vector4(0.05f, 0.15f, 0.25f, 1);
        var theme = new ThemeData(Scheme(bg));
        var (_, root) = MountTree(new Theme(theme, new TextField()));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.Color.X, Is.EqualTo(bg.X).Within(0.001f));
        Assert.That(ro!.Color.Y, Is.EqualTo(bg.Y).Within(0.001f));
        Assert.That(ro!.Color.Z, Is.EqualTo(bg.Z).Within(0.001f));
        Assert.That(ro!.Color.W, Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void WithNoTextStyle_UsesOnSurfaceForTextColor()
    {
        var onSurface = new Vector4(0.7f, 0.8f, 0.6f, 1);
        var theme = new ThemeData(Scheme(onSurface: onSurface));
        var (_, root) = MountTree(new Theme(theme, new TextField()));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.TextStyle.Color, Is.EqualTo(onSurface));
    }

    [Test]
    public void WithNoTextStyle_UsesBodyFontSize()
    {
        var text = new TextTheme
        {
            Headline = new TextStyle
            {
                FontSize = 24
            },
            Body = new TextStyle
            {
                FontSize = 20
            },
            Label = new TextStyle
            {
                FontSize = 12
            }
        };
        var theme = new ThemeData(textTheme: text);
        var (_, root) = MountTree(new Theme(theme, new TextField()));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.TextStyle.FontSize, Is.EqualTo(text.Body.FontSize));
    }


    [Test]
    public void WithStyleOverride_UsesBorderColor()
    {
        var overrideBorder = new Vector4(1, 0, 0.5f, 1);
        var customStyle =
            new TextFieldStyle
            {
                BorderColor = overrideBorder, BorderThickness = 1, Height = 40
            };
        var (_, root) = MountTree(new TextField(style: customStyle));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.BorderColor, Is.EqualTo(overrideBorder));
    }

    [Test]
    public void WithStyleOverride_IgnoresThemeBorderColor()
    {
        var themeBorder = new Vector4(0, 1, 0, 1);
        var overrideBorder = new Vector4(1, 0, 0, 1);
        var theme = new ThemeData(Scheme(border: themeBorder));
        var customStyle =
            new TextFieldStyle
            {
                BorderColor = overrideBorder, BorderThickness = 1, Height = 40
            };
        var (_, root) = MountTree(new Theme(theme, new TextField(style: customStyle)));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.BorderColor, Is.EqualTo(overrideBorder));
    }

    [Test]
    public void WithTextStyleOverride_UsesProvidedColor()
    {
        var overrideColor = new Vector4(0.3f, 0.7f, 1, 1);
        var customText = new TextStyle
        {
            Color = overrideColor, FontSize = 14
        };
        var (_, root) =
            MountTree
            (
                new TextField
                (
                    style: new TextFieldStyle
                    {
                        TextStyle = customText
                    }
                )
            );

        var ro = FindRenderTextField(root);
        Assert.That(ro!.TextStyle.Color, Is.EqualTo(overrideColor));
    }

    [Test]
    public void WithTextStyleOverride_IgnoresThemeOnSurface()
    {
        var themeOnSurface = new Vector4(0, 1, 0, 1);
        var overrideColor = new Vector4(1, 0, 0, 1);
        var theme = new ThemeData(Scheme(onSurface: themeOnSurface));
        var customText = new TextStyle
        {
            Color = overrideColor, FontSize = 14
        };
        var (_, root) = MountTree
        (
            new Theme
            (
                theme, new TextField
                (
                    style: new TextFieldStyle
                    {
                        TextStyle = customText
                    }
                )
            )
        );

        var ro = FindRenderTextField(root);
        Assert.That(ro!.TextStyle.Color, Is.EqualTo(overrideColor));
    }


    [Test]
    public void Default_MapsBackgroundToFillColorWithAlpha()
    {
        var background = new Vector4(0.2f, 0.6f, 0.3f, 1);
        var style = TextFieldStyle.Default(Scheme(background: background), TextTheme.Default());
        Assert.That(style.FillColor, Is.EqualTo(new Vector4(0.2f, 0.6f, 0.3f, 0.8f)));
    }

    [Test]
    public void Default_MapsPrimaryToSelectionColorWithAlpha()
    {
        var primary = new Vector4(0.8f, 0.1f, 0.3f, 1);
        var style = TextFieldStyle.Default(Scheme(primary: primary), TextTheme.Default());
        Assert.That(style.SelectionColor, Is.EqualTo(new Vector4(0.8f, 0.1f, 0.3f, 0.4f)));
    }

    [Test]
    public void WithTextFieldStyleOverride_UsesThemeFillColor()
    {
        var fillColor = new Vector4(0.1f, 0.9f, 0.3f, 1);
        var themeStyle = TextFieldStyle.Default(Scheme(), TextTheme.Default()) with
        {
            FillColor = fillColor
        };
        var theme = new ThemeData(textFieldStyle: themeStyle);
        var (_, root) = MountTree(new Theme(theme, new TextField()));

        var ro = FindRenderTextField(root);
        Assert.That(ro!.Color, Is.EqualTo(fillColor));
    }

    [Test]
    public void ResolveBorder_Unfocused_UsesStyleBorder()
    {
        var border = new Vector4(0.5f, 0.5f, 0.5f, 1);
        var style = new TextFieldStyle
        {
            BorderColor = border, BorderThickness = 1, Height = 40
        };

        TextFieldState.ResolveBorder
        (
            style,
            new ThemeData(Scheme()),
            focused: false,
            keyboard: false,
            out var color,
            out var thickness
        );

        Assert.That(color, Is.EqualTo(border));
        Assert.That(thickness, Is.EqualTo(1f));
    }

    [Test]
    public void ResolveBorder_KeyboardFocus_UsesFocusOutline()
    {
        var focusColor = new Vector4(0.95f, 0.78f, 0.38f, 1);
        var style = new TextFieldStyle
        {
            BorderColor = Vector4.Zero, BorderThickness = 1, Height = 40
        };
        var theme = new ThemeData(Scheme())
        {
            FocusOutlineColor = focusColor, FocusOutlineWidth = 2.5f
        };

        TextFieldState.ResolveBorder
        (
            style,
            theme,
            focused: true,
            keyboard: true,
            out var color,
            out var thickness
        );

        Assert.That(color, Is.EqualTo(focusColor));
        Assert.That(thickness, Is.EqualTo(2.5f));
    }

    [Test]
    public void ResolveBorder_PointerFocus_BlendsBaseWithFocus()
    {
        var baseBorder = new Vector4(0.1f, 0.1f, 0.1f, 1);
        var focusColor = new Vector4(1f, 1f, 1f, 1);
        var style = new TextFieldStyle
        {
            BorderColor = baseBorder, BorderThickness = 1, Height = 40
        };
        var theme = new ThemeData(Scheme())
        {
            FocusOutlineColor = focusColor, FocusOutlineWidth = 3f
        };

        TextFieldState.ResolveBorder
        (
            style,
            theme,
            focused: true,
            keyboard: false,
            out var color,
            out var thickness
        );

        Assert.That(color, Is.EqualTo(Vector4.Lerp(baseBorder, focusColor, 0.35f)));
        Assert.That(thickness, Is.EqualTo(MathHelper.Lerp(1f, 3f, 0.4f)).Within(0.0001f));
        Assert.That(color, Is.Not.EqualTo(focusColor));
    }
}

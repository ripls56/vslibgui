using Gui.Core.Framework;
using Gui.Core.Windowing;
using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Basic;
using Gui.Widgets.Events;
using Gui.Widgets.Framework;
using Gui.Widgets.Gestures;
using OpenTK.Mathematics;
using TextWidget = Gui.Widgets.Basic.Text;

namespace Gui.Tests.Widgets;

[TestFixture]
public class WindowTitleBarStyleTests
{
    private const string TitleText = "My Window";
    private const string MinimizeChar = "\u2013";
    private const string CloseChar = "\u00d7";

    private class TestTickerProvider : ITickerProvider
    {
        public Ticker CreateTicker(Action<TimeSpan> onTick) => new(onTick);
    }

    private sealed class FakeWindowHost : IWindowHost
    {
        public List<(object Owner, bool Collapsed, float Height)> Calls { get; } = [];

        public bool IsHostCollapsed { get; private set; }

        public void SetHostCollapsed(object owner, bool collapsed, float collapsedHeight)
        {
            Calls.Add((owner, collapsed, collapsedHeight));
            IsHostCollapsed = collapsed;
        }
    }

    private static (BuildOwner, Element) MountTree(Widget widget, IWindowHost? host = null)
    {
        var owner = new BuildOwner();
        owner.SetTickerProvider(new TestTickerProvider());
        owner.SetWindowHost(host);
        var el = widget.CreateElement();
        el.AssignOwner(owner);
        el.Mount(null);
        owner.BuildDirtyElements();
        el.RenderObject!.Layout(LayoutConstraints.Loose(400, 200));
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

    private static Element? FindTextElement(Element root, string content)
    {
        Element? result = null;

        void Visit(Element el)
        {
            if (result != null)
            {
                return;
            }

            if (el.Widget is TextWidget t && t.Content == content)
            {
                result = el;
                return;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return result;
    }

    private static float TextX(Element root, string content)
    {
        var el = FindTextElement(root, content);
        Assert.That(el, Is.Not.Null, $"Text '{content}' not found");
        return el!.RenderObject!.LocalToGlobal(Vector2.Zero).X;
    }

    private static void Tap(BuildOwner owner, Element root, string content)
    {
        var ro = FindTextElement(root, content)!.RenderObject!;
        var center = ro.LocalToGlobal(ro.Size / 2);
        var dispatcher = new EventDispatcher();
        var e = new PointerEvent(center.X, center.Y);
        dispatcher.DispatchPointerDown(root, e);
        dispatcher.DispatchPointerUp(root, e);
        owner.BuildDirtyElements();
        root.RenderObject!.Layout(LayoutConstraints.Loose(400, 200));
    }

    private static WindowTitleBarStyle DefaultStyle() =>
        WindowTitleBarStyle.Default(ColorScheme.Default(), TextTheme.Default());

    [Test]
    public void Default_ReproducesLegacyLook()
    {
        var colors = ColorScheme.Default();
        var style = WindowTitleBarStyle.Default(colors, TextTheme.Default(colors));

        Assert.That(style.Height, Is.EqualTo(28f));
        Assert.That(style.BackgroundColor, Is.EqualTo(colors.Surface));
        Assert.That(style.CloseHoverColor, Is.EqualTo(colors.Error));
        Assert.That(style.TitleTextStyle.FontSize, Is.EqualTo(14f));
        Assert.That(style.TitleTextStyle.Color, Is.EqualTo(colors.OnSurface));
        Assert.That(style.MinimizeGlyph, Is.EqualTo(MinimizeChar));
        Assert.That(style.RestoreGlyph, Is.EqualTo("+"));
        Assert.That(style.CloseGlyph, Is.EqualTo(CloseChar));
        Assert.That(style.ButtonPlacement, Is.EqualTo(TitleBarButtonPlacement.End));
        Assert.That(style.TitleAlignment, Is.EqualTo(TitleBarAlignment.Left));
    }

    [Test]
    public void ThemeData_RegistersDefaultStyle()
    {
        Assert.That(new ThemeData().WindowTitleBarStyle.Height, Is.EqualTo(28f));
    }

    [Test]
    public void DefaultBar_HeightIs28()
    {
        var (_, root) = MountTree(new WindowTitleBar(TitleText));

        Assert.That(root.RenderObject!.Size.Y, Is.EqualTo(28f));
    }

    [Test]
    public void LegacyBackgroundColor_BeatsStyle()
    {
        var legacy = new Vector4(0, 1, 0, 1);
        var styled = new Vector4(1, 0, 0, 1);
        var style = DefaultStyle() with
        {
            BackgroundColor = styled
        };

        var (_, root) = MountTree
        (
            new WindowTitleBar
            (
                TitleText,
                backgroundColor: legacy,
                style: style
            )
        );

        Assert.That(FindRenderBoxWithColor(root, legacy), Is.Not.Null);
        Assert.That(FindRenderBoxWithColor(root, styled), Is.Null);
    }

    [Test]
    public void Style_BeatsThemeDefault()
    {
        var themed = new Vector4(0, 0, 1, 1);
        var styled = new Vector4(1, 0, 0, 1);
        var theme = new ThemeData
        {
            WindowTitleBarStyle = DefaultStyle() with
            {
                BackgroundColor = themed
            }
        };
        var style = DefaultStyle() with
        {
            BackgroundColor = styled
        };

        var (_, root) = MountTree(new Theme(theme, new WindowTitleBar(TitleText, style: style)));

        Assert.That(FindRenderBoxWithColor(root, styled), Is.Not.Null);
        Assert.That(FindRenderBoxWithColor(root, themed), Is.Null);
    }

    [Test]
    public void ThemeStyle_AppliesWithoutExplicitStyle()
    {
        var themed = new Vector4(0, 0, 1, 1);
        var theme = new ThemeData
        {
            WindowTitleBarStyle = DefaultStyle() with
            {
                BackgroundColor = themed, Height = 40
            }
        };

        var (_, root) = MountTree(new Theme(theme, new WindowTitleBar(TitleText)));

        Assert.That(FindRenderBoxWithColor(root, themed), Is.Not.Null);
        Assert.That(root.RenderObject!.Size.Y, Is.EqualTo(40f));
    }

    [Test]
    public void LegacyHeight_BeatsStyleHeight()
    {
        var style = DefaultStyle() with
        {
            Height = 40
        };

        var (_, root) = MountTree(new WindowTitleBar(TitleText, 32, style: style));

        Assert.That(root.RenderObject!.Size.Y, Is.EqualTo(32f));
    }

    [Test]
    public void DefaultPlacement_ButtonsAfterTitle()
    {
        var (_, root) = MountTree(new WindowTitleBar(TitleText));

        Assert.That(TextX(root, MinimizeChar), Is.GreaterThan(TextX(root, TitleText)));
        Assert.That(TextX(root, CloseChar), Is.GreaterThan(TextX(root, MinimizeChar)));
    }

    [Test]
    public void ButtonPlacementStart_PutsButtonsBeforeTitle()
    {
        var style = DefaultStyle() with
        {
            ButtonPlacement = TitleBarButtonPlacement.Start
        };

        var (_, root) = MountTree(new WindowTitleBar(TitleText, style: style));

        Assert.That(TextX(root, MinimizeChar), Is.LessThan(TextX(root, TitleText)));
        Assert.That(TextX(root, CloseChar), Is.LessThan(TextX(root, TitleText)));
    }

    [Test]
    public void ReverseButtonOrder_SwapsCloseAndMinimize()
    {
        var style = DefaultStyle() with
        {
            ReverseButtonOrder = true
        };

        var (_, root) = MountTree(new WindowTitleBar(TitleText, style: style));

        Assert.That(TextX(root, CloseChar), Is.LessThan(TextX(root, MinimizeChar)));
    }

    [Test]
    public void CustomGlyphs_ReachRenderedText()
    {
        var style = DefaultStyle() with
        {
            MinimizeGlyph = "_", RestoreGlyph = "^", CloseGlyph = "X"
        };

        var (owner, root) = MountTree(new WindowTitleBar(TitleText, style: style));

        Assert.That(FindTextElement(root, "_"), Is.Not.Null);
        Assert.That(FindTextElement(root, "X"), Is.Not.Null);
        Assert.That(FindTextElement(root, MinimizeChar), Is.Null);
        Assert.That(FindTextElement(root, CloseChar), Is.Null);

        Tap(owner, root, "_");

        Assert.That(FindTextElement(root, "^"), Is.Not.Null);
    }

    [Test]
    public void CustomTitleFontFamily_ReachesTitleText()
    {
        var style = DefaultStyle() with
        {
            TitleTextStyle = DefaultStyle().TitleTextStyle with
            {
                FontFamily = "monospace"
            }
        };

        var (_, root) = MountTree(new WindowTitleBar(TitleText, style: style));

        var title = (TextWidget)FindTextElement(root, TitleText)!.Widget;
        Assert.That(title.Style.FontFamily, Is.EqualTo("monospace"));
    }

    [Test]
    public void LegacyTextColor_BeatsStyleTitleColor()
    {
        var legacy = new Vector4(0, 1, 0, 1);

        var (_, root) = MountTree(new WindowTitleBar(TitleText, textColor: legacy));

        var title = (TextWidget)FindTextElement(root, TitleText)!.Widget;
        Assert.That(title.Style.Color, Is.EqualTo(legacy));
    }

    [Test]
    public void Leading_AndTrailing_AreRendered()
    {
        var (_, root) = MountTree
        (
            new WindowTitleBar
            (
                TitleText,
                leading: new TextWidget("L"),
                trailing: new TextWidget("R")
            )
        );

        Assert.That(TextX(root, "L"), Is.LessThan(TextX(root, MinimizeChar)));
        Assert.That(TextX(root, "L"), Is.GreaterThan(TextX(root, TitleText)));
        Assert.That(TextX(root, "R"), Is.GreaterThan(TextX(root, CloseChar)));
    }

    [Test]
    public void Minimize_NotifiesWindowHost_AndUnmountExpands()
    {
        var host = new FakeWindowHost();
        var (owner, root) = MountTree(new WindowTitleBar(TitleText), host);

        Tap(owner, root, MinimizeChar);

        Assert.That(host.Calls, Has.Count.EqualTo(1));
        Assert.That(host.Calls[0].Collapsed, Is.True);
        Assert.That(host.Calls[0].Height, Is.EqualTo(28f));

        root.Unmount();

        Assert.That(host.Calls[^1].Collapsed, Is.False);
        Assert.That(host.Calls[^1].Owner, Is.SameAs(host.Calls[0].Owner));
    }

    [Test]
    public void Minimize_UsesResolvedBarHeight()
    {
        var host = new FakeWindowHost();
        var style = DefaultStyle() with
        {
            Height = 36
        };
        var (owner, root) = MountTree(new WindowTitleBar(TitleText, style: style), host);

        Tap(owner, root, MinimizeChar);

        Assert.That(host.Calls[0].Height, Is.EqualTo(36f));
    }

    [Test]
    public void CollapseWindowFalse_DoesNotNotifyHost()
    {
        var host = new FakeWindowHost();
        var (owner, root) = MountTree(new WindowTitleBar(TitleText, collapseWindow: false), host);

        Tap(owner, root, MinimizeChar);

        Assert.That(host.Calls.Where(c => c.Collapsed), Is.Empty);
    }

    [Test]
    public void Minimize_WithoutHost_DoesNotThrow()
    {
        var minimized = false;
        var (owner, root) = MountTree
        (
            new WindowTitleBar(TitleText, onMinimizeChanged: m => minimized = m)
        );

        Assert.DoesNotThrow(() => Tap(owner, root, MinimizeChar));
        Assert.That(minimized, Is.True);
        Assert.DoesNotThrow(root.Unmount);
    }

    [Test]
    public void WindowFrame_ForwardsOnMinimizeChanged_AndHidesChild()
    {
        bool? minimized = null;
        var host = new FakeWindowHost();
        var (owner, root) = MountTree
        (
            new WindowFrame
            (
                TitleText,
                new TextWidget("Body"),
                onMinimizeChanged: m => minimized = m
            ),
            host
        );

        Tap(owner, root, MinimizeChar);

        Assert.That(minimized, Is.True);
        Assert.That(FindTextElement(root, "Body"), Is.Null);
        Assert.That(host.Calls[0].Collapsed, Is.True);
    }

    [Test]
    public void WindowFrame_StyleReachesTitleBar()
    {
        var style = DefaultStyle() with
        {
            CloseGlyph = "X"
        };

        var (_, root) = MountTree(new WindowFrame(TitleText, style: style));

        Assert.That(FindTextElement(root, "X"), Is.Not.Null);
    }
}

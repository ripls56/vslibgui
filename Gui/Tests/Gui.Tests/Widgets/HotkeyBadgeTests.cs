using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Core.Layout;
using Gui.Rendering;
using Gui.Rendering.Text;
using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class HotkeyBadgeTests
{
    private static (BuildOwner owner, Element root)
        MountTree(Widget widget)
    {
        var owner = TestHelpers.NewBuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        return (owner, element);
    }

    private static T? FindRenderObject<T>(Element root) where T : RenderObject
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

    private static RenderText? FindRenderText(Element root)
        => FindRenderObject<RenderText>(root);

    private static int CountRenderObjects<T>(Element root) where T : RenderObject
    {
        var count = 0;

        void Visit(Element el)
        {
            if (el is RenderObjectElement roe && roe.RenderObject is T)
            {
                count++;
            }

            el.VisitChildren(Visit);
        }

        Visit(root);
        return count;
    }

    [Test]
    public void TextLabel_RendersLabelText()
    {
        var badge = new HotkeyBadge("Ctrl");
        var (_, root) = MountTree(badge);

        var textRo = FindRenderText(root);
        Assert.That(textRo, Is.Not.Null);
        Assert.That(textRo!.Text, Is.EqualTo("Ctrl"));
    }

    [Test]
    public void IconWidget_RendersIconInsteadOfText()
    {
        var icon = new Icon("gui", "icons/mouse.svg", 14f);
        var badge = new HotkeyBadge(icon);
        var (_, root) = MountTree(badge);

        var iconRo = FindRenderObject<RenderIcon>(root);
        Assert.That(iconRo, Is.Not.Null);
    }

    [Test]
    public void UsesThemeStyle_WhenNoOverride()
    {
        var customStyle = new HotkeyBadgeStyle
        {
            BackgroundColor = new Vector4(1, 0, 0, 1),
            BorderColor = new Vector4(0, 1, 0, 1),
            BorderThickness = 2f,
            CornerRadius = 8f,
            TextStyle = new TextStyle { FontSize = 20 },
            Padding = EdgeInsets.All(10),
            MinWidth = 30f
        };
        var theme = new ThemeData(hotkeyBadgeStyle: customStyle);

        var badge = new HotkeyBadge("E");
        var tree = new Theme(theme, badge);
        var (_, root) = MountTree(tree);

        // The badge should pick up the theme's font size
        var textRo = FindRenderText(root);
        Assert.That(textRo, Is.Not.Null);
        Assert.That(textRo!.Style.FontSize, Is.EqualTo(20));
    }

    [Test]
    public void StyleOverride_TakesPrecedence()
    {
        var overrideStyle = new HotkeyBadgeStyle
        {
            BackgroundColor = new Vector4(0, 0, 1, 1),
            BorderColor = Vector4.Zero,
            BorderThickness = 0f,
            CornerRadius = 2f,
            TextStyle = new TextStyle { FontSize = 10 },
            Padding = EdgeInsets.All(2),
            MinWidth = 16f
        };

        var badge = new HotkeyBadge("X", overrideStyle);
        var (_, root) = MountTree(badge);

        var textRo = FindRenderText(root);
        Assert.That(textRo, Is.Not.Null);
        Assert.That(textRo!.Style.FontSize, Is.EqualTo(10));
    }

    [Test]
    public void DefaultThemeStyle_HasReasonableDefaults()
    {
        var colors = ColorScheme.Default();
        var style = HotkeyBadgeStyle.Default(colors);

        Assert.That(style.CornerRadius, Is.GreaterThan(0));
        Assert.That(style.TextStyle.FontSize, Is.GreaterThan(0));
        Assert.That(style.Padding.Horizontal, Is.GreaterThan(0));
        Assert.That(style.BackgroundColor.W, Is.GreaterThan(0));
    }

    [Test]
    public void ThemeData_IncludesHotkeyBadgeStyle()
    {
        var td = new ThemeData();
        Assert.That(td.HotkeyBadgeStyle.CornerRadius, Is.GreaterThan(0));
        Assert.That(td.HotkeyBadgeStyle.TextStyle.FontSize, Is.GreaterThan(0));
    }

    [Test]
    public void BadgeTree_ContainsBothBoxAndText()
    {
        var badge = new HotkeyBadge("Q");
        var (_, root) = MountTree(badge);

        // Tree should contain a Container (RenderBox) and a Text (RenderText)
        var boxCount = CountRenderObjects<RenderBox>(root);
        Assert.That(boxCount, Is.GreaterThanOrEqualTo(2),
            "Badge should have at least a Container box and a Text box");
    }

    [Test]
    public void Column_FirstChildAtTop_SecondBelow()
    {
        var col = new Column(
            mainAxisSize: MainAxisSize.Min,
            children:
            [
                new SizedBox(50, 20),
                new SizedBox(50, 30)
            ]
        );
        var (_, root) = MountTree(col);
        var ro = root.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(500, 500));

        // Column should shrink-wrap: 20 + 30 = 50
        Assert.That(ro.Size.Y, Is.EqualTo(50));

        // child[0] at Y=0, child[1] at Y=20
        Assert.That(ro.Children[0].Y, Is.EqualTo(0), "First child should be at top (Y=0)");
        Assert.That(ro.Children[1].Y, Is.EqualTo(20), "Second child should be below first (Y=20)");
    }

    [Test]
    public void Padding_AffectsBadgeSize()
    {
        var style = new HotkeyBadgeStyle
        {
            BackgroundColor = Vector4.One,
            BorderColor = Vector4.Zero,
            BorderThickness = 0f,
            CornerRadius = 0f,
            TextStyle = new TextStyle { FontSize = 12 },
            Padding = new EdgeInsets(10, 5, 10, 5),
            MinWidth = 0f
        };

        var badge = new HotkeyBadge("A", style);
        var (_, root) = MountTree(badge);

        // Layout with generous constraints
        var ro = root.RenderObject!;
        ro.Layout(new LayoutConstraints(0, 500, 0, 500));

        // Badge size must be larger than text alone due to padding
        var textRo = FindRenderText(root);
        Assert.That(textRo, Is.Not.Null);
        Assert.That(ro.Size.X, Is.GreaterThan(textRo!.Size.X),
            "Badge width should include horizontal padding");
        Assert.That(ro.Size.Y, Is.GreaterThan(textRo.Size.Y),
            "Badge height should include vertical padding");
    }
}

using Gui.Rendering.Text;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Text;

[TestFixture]
public class TextLayoutTests
{
    [Test]
    public void RenderText_EmptyString_ShouldHaveZeroSize()
    {
        var buildOwner = new BuildOwner();
        var widget = new Gui.Widgets.Basic.Text("");
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);

        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Loose(1000, 1000));

        Assert.That(ro.Size, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void RenderText_SizeShouldIncreaseWithTextLength()
    {
        var buildOwner = new BuildOwner();

        var widgetShort = new Gui.Widgets.Basic.Text("Short");
        var elementShort = widgetShort.CreateElement();
        elementShort.AssignOwner(buildOwner);
        elementShort.Mount(null);
        var roShort = elementShort.RenderObject!;
        roShort.Layout(LayoutConstraints.Loose(1000, 1000));

        var widgetLong = new Gui.Widgets.Basic.Text("This is a much longer string");
        var elementLong = widgetLong.CreateElement();
        elementLong.AssignOwner(buildOwner);
        elementLong.Mount(null);
        var roLong = elementLong.RenderObject!;
        roLong.Layout(LayoutConstraints.Loose(1000, 1000));

        Assert.That(roLong.Size.X, Is.GreaterThan(roShort.Size.X));
    }

    [Test]
    public void RenderText_SizeShouldIncreaseWithFontSize()
    {
        var buildOwner = new BuildOwner();

        var styleSmall = new TextStyle { FontSize = 10 };
        var widgetSmall = new Gui.Widgets.Basic.Text("Scaling test", styleSmall);
        var elementSmall = widgetSmall.CreateElement();
        elementSmall.AssignOwner(buildOwner);
        elementSmall.Mount(null);
        var roSmall = elementSmall.RenderObject!;
        roSmall.Layout(LayoutConstraints.Loose(1000, 1000));

        var styleLarge = new TextStyle { FontSize = 20 };
        var widgetLarge = new Gui.Widgets.Basic.Text("Scaling test", styleLarge);
        var elementLarge = widgetLarge.CreateElement();
        elementLarge.AssignOwner(buildOwner);
        elementLarge.Mount(null);
        var roLarge = elementLarge.RenderObject!;
        roLarge.Layout(LayoutConstraints.Loose(1000, 1000));

        Assert.That(roLarge.Size.X, Is.GreaterThan(roSmall.Size.X));
        Assert.That(roLarge.Size.Y, Is.GreaterThan(roSmall.Size.Y));
    }

    [Test]
    public void RenderText_Multiline_ShouldHaveCorrectHeight()
    {
        var buildOwner = new BuildOwner();

        var widgetSingle = new Gui.Widgets.Basic.Text("Line 1");
        var elementSingle = widgetSingle.CreateElement();
        elementSingle.AssignOwner(buildOwner);
        elementSingle.Mount(null);
        var roSingle = elementSingle.RenderObject!;
        roSingle.Layout(LayoutConstraints.Loose(1000, 1000));

        var widgetMulti = new Gui.Widgets.Basic.Text("Line 1\nLine 2\nLine 3");
        var elementMulti = widgetMulti.CreateElement();
        elementMulti.AssignOwner(buildOwner);
        elementMulti.Mount(null);
        var roMulti = elementMulti.RenderObject!;
        roMulti.Layout(LayoutConstraints.Loose(1000, 1000));

        // Height should be approximately 3x (plus/minus some leading)
        Assert.That(roMulti.Size.Y, Is.GreaterThan(roSingle.Size.Y * 2.5f));
        Assert.That(roMulti.Size.Y, Is.LessThan(roSingle.Size.Y * 3.5f));
    }

    [Test]
    public void GetFont_ByTypeface_ReturnsCachedFontForSameSize()
    {
        var tf = SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);

        var a = TextLayoutHelper.GetFont(tf, 16f);
        var b = TextLayoutHelper.GetFont(tf, 16f);

        Assert.That(a, Is.SameAs(b));
        Assert.That(a.Size, Is.EqualTo(16f));
    }

    [Test]
    public void BreakIntoLines_CjkText_BreaksAtCharBoundary()
    {
        var tf = SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);
        var font = TextLayoutHelper.GetFont(tf, 16f);

        var cjkCharWidth = TextShaper.Shape("你", font).Sum(r => r.Advance);
        var maxWidth = cjkCharWidth * 2.5f;

        var lines = TextLayoutHelper.BreakIntoLines("你好世界", font, maxWidth);

        Assert.That(lines, Has.Count.EqualTo(2));
        Assert.That(lines[0], Is.EqualTo("你好"));
        Assert.That(lines[1], Is.EqualTo("世界"));
    }

    [Test]
    public void MeasureText_MixedLatinCjk_WidthMatchesShapedRuns()
    {
        const string text = "Hi你好";
        const float fontSize = 16f;

        var measured = TextLayoutHelper.MeasureText(text, "Arial", fontSize, FontWeight.Normal);

        var tf = SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);
        var font = TextLayoutHelper.GetFont(tf, fontSize);
        var runs = TextShaper.Shape(text, font);
        var shapedWidth = runs.Sum(r => r.Advance);

        Assert.That(measured.X, Is.EqualTo(shapedWidth).Within(0.5f));
    }
}

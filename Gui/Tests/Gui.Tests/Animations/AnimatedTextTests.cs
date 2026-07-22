using Gui.Rendering.Text;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using TextWidget = Gui.Widgets.Basic.Text;

namespace Gui.Tests.Animations;

[TestFixture]
public class AnimatedTextTests
{
    private (StatefulElement element, BuildOwner owner, MockTickerProvider vsync)
        Mount
        (
            string content,
            TextStyle style,
            TimeSpan duration,
            Action? onEnd = null
        )
    {
        var vsync = new MockTickerProvider();
        var owner = new BuildOwner();
        owner.SetTickerProvider(vsync);
        var widget = new AnimatedText
        (
            content,
            style,
            duration,
            onEnd: onEnd
        );
        var element = (StatefulElement)widget.CreateElement();
        element.AssignOwner(owner);
        element.Mount(null);
        owner.BuildDirtyElements();
        return (element, owner, vsync);
    }

    private static TextWidget? FindText(Element element)
    {
        if (element.Widget is TextWidget text)
        {
            return text;
        }

        TextWidget? found = null;
        element.VisitChildren(child => { found ??= FindText(child); });
        return found;
    }

    [Test]
    public void StyleChange_AnimatesColorAndFontSize()
    {
        var redSmall = new TextStyle
        {
            Color = new Vector4(1, 0, 0, 1), FontSize = 14
        };
        var blueLarge = new TextStyle
        {
            Color = new Vector4(0, 0, 1, 1), FontSize = 28
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount("count", redSmall, duration);

        element.Update(new AnimatedText("count", blueLarge, duration));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var mid = FindText(element)!.Style;
        Assert.That(mid.FontSize, Is.InRange(20.5f, 21.5f));
        Assert.That(mid.Color.X, Is.InRange(0.45f, 0.55f));
        Assert.That(mid.Color.Z, Is.InRange(0.45f, 0.55f));
    }

    [Test]
    public void PassThroughFields_PreservedThroughAnimation()
    {
        var before = new TextStyle
        {
            Color = new Vector4(1, 0, 0, 1),
            FontFamily = "monospace",
            Weight = FontWeight.Bold,
            Align = TextAlignment.Center
        };
        var after = new TextStyle
        {
            Color = new Vector4(0, 1, 0, 1),
            FontFamily = "monospace",
            Weight = FontWeight.Bold,
            Align = TextAlignment.Center
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount("label", before, duration);

        element.Update(new AnimatedText("label", after, duration));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var style = FindText(element)!.Style;
        Assert.That(style.FontFamily, Is.EqualTo("monospace"));
        Assert.That(style.Weight, Is.EqualTo(FontWeight.Bold));
        Assert.That(style.Align, Is.EqualTo(TextAlignment.Center));
    }

    [Test]
    public void SameStyle_DoesNotAnimate()
    {
        var style = new TextStyle
        {
            Color = new Vector4(1, 0, 0, 1), FontSize = 14
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount("name", style, duration);

        element.Update(new AnimatedText("name", style, duration));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        var rendered = FindText(element)!.Style;
        Assert.That(rendered.FontSize, Is.EqualTo(14f));
        Assert.That(rendered.Color, Is.EqualTo(new Vector4(1, 0, 0, 1)));
    }

    [Test]
    public void Content_PreservedThroughAnimation()
    {
        var a = new TextStyle
        {
            Color = new Vector4(1, 0, 0, 1)
        };
        var b = new TextStyle
        {
            Color = new Vector4(0, 0, 1, 1)
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount("hello", a, duration);

        element.Update(new AnimatedText("hello", b, duration));
        owner.BuildDirtyElements();

        vsync.Advance(TimeSpan.FromMilliseconds(100));
        owner.BuildDirtyElements();

        Assert.That(FindText(element)!.Content, Is.EqualTo("hello"));
    }

    [Test]
    public void OnEnd_FiresOnCompletion()
    {
        var called = false;
        var a = new TextStyle
        {
            Color = new Vector4(1, 0, 0, 1)
        };
        var b = new TextStyle
        {
            Color = new Vector4(0, 0, 1, 1)
        };
        var duration = TimeSpan.FromMilliseconds(200);

        var (element, owner, vsync) = Mount("x", a, duration, () => called = true);

        element.Update
        (
            new AnimatedText("x", b, duration, onEnd: () => called = true)
        );
        owner.BuildDirtyElements();

        Assert.That(called, Is.False);

        vsync.Advance(TimeSpan.FromMilliseconds(300));
        owner.BuildDirtyElements();

        Assert.That(called, Is.True);
    }
}

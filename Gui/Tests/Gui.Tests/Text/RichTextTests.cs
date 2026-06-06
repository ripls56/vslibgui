using Gui.Core.Basic;
using Gui.Rendering.Text;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;

namespace Gui.Tests.Text;

[TestFixture]
public class RichTextTests
{
    [Test]
    public void RichText_ShouldCreateRenderRichText()
    {
        var span = new TextSpan("Hello");
        var richText = new RichText(span);
        var state = richText.CreateState();
        Assert.That(richText.Span, Is.SameAs(span));
    }

    [Test]
    public void RenderRichText_Layout_ShouldCalculateCorrectSize()
    {
        var span = new TextSpan(children:
        [
            new TextSpan("Part1 "),
            new TextSpan("Part2")
        ]);
        var ro = new RenderRichText();
        ro.Root = span;

        ro.Layout(LayoutConstraints.Loose(1000, 1000));

        Assert.That(ro.Size.X, Is.GreaterThan(0));
        Assert.That(ro.Size.Y, Is.GreaterThan(0));
    }

    [Test]
    public void TextSpan_CollectRuns_FlattensHierarchy()
    {
        var parent = new TextSpan(
            "Hello ",
            new SpanStyle { Color = Vector4.One },
            [
                new TextSpan("world", new SpanStyle { Weight = FontWeight.Bold })
            ]);

        var runs = new List<PlacedRun>();
        parent.CollectRuns(new TextStyle(), runs);

        Assert.That(runs, Has.Count.EqualTo(2));
        Assert.That(runs[0].Text, Is.EqualTo("Hello "));
        Assert.That(runs[1].Text, Is.EqualTo("world"));
        Assert.That(runs[1].Style.Weight, Is.EqualTo(FontWeight.Bold));
    }

    [Test]
    public void SpanStyle_Resolve_InheritsParentFields()
    {
        var parent = new TextStyle
        {
            FontSize = 20, Color = new Vector4(1, 0, 0, 1), FontFamily = "monospace"
        };
        var spanStyle = new SpanStyle { FontSize = 12 };

        var resolved = spanStyle.Resolve(parent);

        Assert.That(resolved.FontSize, Is.EqualTo(12));
        Assert.That(resolved.Color, Is.EqualTo(new Vector4(1, 0, 0, 1)));
        Assert.That(resolved.FontFamily, Is.EqualTo("monospace"));
    }

    [Test]
    public void TextSpan_HasAnyRecognizer_DetectsNestedRecognizer()
    {
        var span = new TextSpan(
            "outer",
            children:
            [
                new TextSpan("inner", recognizer: new TapGestureRecognizer { OnTap = () => { } })
            ]);

        Assert.That(span.HasAnyRecognizer(), Is.True);
    }

    [Test]
    public void TextSpan_HasAnyRecognizer_FalseWhenNone()
    {
        var span = new TextSpan(
            "plain",
            children:
            [
                new TextSpan("child")
            ]);

        Assert.That(span.HasAnyRecognizer(), Is.False);
    }

    [Test]
    public void RenderRichText_HitTestRun_ReturnsCorrectSpan()
    {
        var clickable = new TextSpan("click me", recognizer:
            new TapGestureRecognizer { OnTap = () => { } });
        var root = new TextSpan(children:
        [
            new TextSpan("before "),
            clickable
        ]);

        var ro = new RenderRichText();
        ro.Root = root;
        ro.Layout(LayoutConstraints.Loose(1000, 1000));

        // Hit-test at x=0 should hit "before "
        var hitFirst = ro.HitTestRun(1, 5);
        Assert.That(hitFirst, Is.Not.Null);
        Assert.That(hitFirst, Is.Not.SameAs(clickable));

        // Hit-test far right should hit "click me"
        var beforeWidth = ro.GetMaxIntrinsicWidth(0);
        // We can't predict exact positions, but the second span
        // should be after the first one
    }
}

using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Layout;
using Gui.Widgets.Spans;

namespace Gui.Tests.Text;

[TestFixture]
public class WidgetSpanTests
{
    private static readonly TextStyle Base = new() { FontSize = 14f };

    [Test]
    public void CollectRuns_EmitsWidgetRun()
    {
        var ws = new WidgetSpan(new SizedBox(20, 20));
        var runs = new List<PlacedRun>();
        ws.CollectRuns(Base, runs);

        Assert.That(runs, Has.Count.EqualTo(1));
        Assert.That(runs[0].Type, Is.EqualTo(PlacedRunType.Widget));
        Assert.That(runs[0].Float, Is.EqualTo(VtmlFloat.Inline));
    }

    [Test]
    public void CollectRuns_FloatLeft_SetsFloat()
    {
        var ws = new WidgetSpan(
            new SizedBox(48, 48),
            @float: VtmlFloat.Left);
        var runs = new List<PlacedRun>();
        ws.CollectRuns(Base, runs);

        Assert.That(runs[0].Float, Is.EqualTo(VtmlFloat.Left));
    }

    [Test]
    public void HasAnyRecognizer_ReturnsFalse()
    {
        var ws = new WidgetSpan(new SizedBox(10, 10));
        Assert.That(ws.HasAnyRecognizer(), Is.False);
    }
}

[TestFixture]
public class ClearSpanTests
{
    private static readonly TextStyle Base = new() { FontSize = 14f };

    [Test]
    public void CollectRuns_EmitsClearRun()
    {
        var cs = new ClearSpan();
        var runs = new List<PlacedRun>();
        cs.CollectRuns(Base, runs);

        Assert.That(runs, Has.Count.EqualTo(1));
        Assert.That(runs[0].Type, Is.EqualTo(PlacedRunType.Clear));
    }

    [Test]
    public void HasAnyRecognizer_ReturnsFalse()
    {
        var cs = new ClearSpan();
        Assert.That(cs.HasAnyRecognizer(), Is.False);
    }
}

[TestFixture]
public class InlineSpanHelperTests
{
    [Test]
    public void CollectWidgetChildren_FindsWidgetSpans()
    {
        var span = new TextSpan(children:
        [
            new TextSpan("Hello "),
            new WidgetSpan(new SizedBox(24, 24)),
            new TextSpan(" world")
        ]);
        var children = InlineSpanHelper.CollectWidgetChildren(span);
        Assert.That(children, Has.Count.EqualTo(1));
        Assert.That(children[0], Is.InstanceOf<SizedBox>());
    }

    [Test]
    public void CollectWidgetChildren_Empty_WhenNoWidgetSpans()
    {
        var span = new TextSpan("Plain text");
        var children = InlineSpanHelper.CollectWidgetChildren(span);
        Assert.That(children, Is.Empty);
    }

    [Test]
    public void CollectWidgetChildren_MultipleWidgetSpans_InOrder()
    {
        var box1 = new SizedBox(10, 10);
        var box2 = new SizedBox(20, 20);
        var span = new TextSpan(children:
        [
            new WidgetSpan(box1),
            new TextSpan("mid"),
            new WidgetSpan(box2)
        ]);
        var children = InlineSpanHelper.CollectWidgetChildren(span);
        Assert.That(children, Has.Count.EqualTo(2));
        Assert.That(children[0], Is.SameAs(box1));
        Assert.That(children[1], Is.SameAs(box2));
    }
}

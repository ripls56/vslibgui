using Gui.Rendering.Text;
using Gui.Vtml;
using Gui.Widgets.Basic;
using Gui.Widgets.Spans;
using OpenTK.Mathematics;

namespace Gui.Tests.Vtml;

[TestFixture]
public class VtmlSpanBuilderTests
{
    private static readonly TextStyle Base = new() { FontSize = 14f, Color = Vector4.One };

    [Test]
    public void PlainTextRun_ProducesTextSpan()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlTextRun { Text = "Hello", Style = Base }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);

        var root = span as TextSpan;
        Assert.That(root, Is.Not.Null);
        Assert.That(root!.Children, Has.Count.EqualTo(1));
        var child = root.Children![0] as TextSpan;
        Assert.That(child!.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void TextRunWithHref_HasRecognizerAndUnderline()
    {
        string? clicked = null;
        var elements = new List<VtmlInlineElement>
        {
            new VtmlTextRun { Text = "Click", Style = Base, Href = "handbook://test" }
        };
        var span = VtmlSpanBuilder.Build(elements, Base, href => clicked = href);

        var root = span as TextSpan;
        var child = root!.Children![0] as TextSpan;
        Assert.That(child!.Recognizer, Is.InstanceOf<TapGestureRecognizer>());
        Assert.That(child.Style?.Decoration, Is.EqualTo(TextDecoration.Underline));

        ((TapGestureRecognizer)child.Recognizer!).OnTap?.Invoke();
        Assert.That(clicked, Is.EqualTo("handbook://test"));
    }

    [Test]
    public void LineBreak_ProducesNewlineTextSpan()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlTextRun { Text = "Line1", Style = Base },
            new VtmlLineBreak(),
            new VtmlTextRun { Text = "Line2", Style = Base }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        Assert.That(root!.Children, Has.Count.EqualTo(3));
        var nl = root.Children![1] as TextSpan;
        Assert.That(nl!.Text, Is.EqualTo("\n"));
    }

    [Test]
    public void IconRun_ProducesWidgetSpan()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlIconRun { Name = "dice", Size = 24f }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        var child = root!.Children![0];
        Assert.That(child, Is.InstanceOf<WidgetSpan>());
        Assert.That(((WidgetSpan)child).Child, Is.InstanceOf<Icon>());
    }

    [Test]
    public void HotkeyRun_ProducesWidgetSpanWithHotkeyChip()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlHotkeyRun { HotkeyCode = "sprint", Style = Base }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        var child = root!.Children![0];
        Assert.That(child, Is.InstanceOf<WidgetSpan>());
        Assert.That(((WidgetSpan)child).Child, Is.InstanceOf<HotkeyChip>());
    }

    [Test]
    public void ItemStackInline_ProducesInlineWidgetSpan()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlItemStackRun
            {
                Code = "plank-oak",
                FloatType = VtmlFloat.Inline,
                RSize = 1f,
                FontHeight = 14f
            }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        var child = root!.Children![0] as WidgetSpan;
        Assert.That(child, Is.Not.Null);
        Assert.That(child!.Float, Is.EqualTo(VtmlFloat.Inline));
    }

    [Test]
    public void ItemStackFloat_ProducesFloatWidgetSpan()
    {
        var elements = new List<VtmlInlineElement>
        {
            new VtmlItemStackRun
            {
                Code = "plank-oak", FloatType = VtmlFloat.Left, RSize = 1f, FontHeight = 48f
            }
        };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        var child = root!.Children![0] as WidgetSpan;
        Assert.That(child!.Float, Is.EqualTo(VtmlFloat.Left));
    }

    [Test]
    public void ClearFloat_ProducesClearSpan()
    {
        var elements = new List<VtmlInlineElement> { new VtmlClearFloat() };
        var span = VtmlSpanBuilder.Build(elements, Base);
        var root = span as TextSpan;
        Assert.That(root!.Children![0], Is.InstanceOf<ClearSpan>());
    }

    [Test]
    public void EmptyList_ProducesEmptyTextSpan()
    {
        var span = VtmlSpanBuilder.Build(
            new List<VtmlInlineElement>(), Base);
        var root = span as TextSpan;
        Assert.That(root, Is.Not.Null);
    }
}

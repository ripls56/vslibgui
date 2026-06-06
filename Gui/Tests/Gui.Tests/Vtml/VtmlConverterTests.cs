using Gui.Rendering.Text;
using Gui.Vtml;
using OpenTK.Mathematics;

namespace Gui.Tests.Vtml;

[TestFixture]
public class VtmlConverterTests
{
    private static readonly TextStyle BaseStyle = new() { FontSize = 14f, Color = Vector4.One };

    [Test]
    public void PlainText_ProducesSingleTextRun()
    {
        var elems = VtmlConverter.Convert("Hello world", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));
        var run = elems[0] as VtmlTextRun;
        Assert.That(run, Is.Not.Null);
        Assert.That(run!.Text, Is.EqualTo("Hello world"));
        Assert.That(run.Href, Is.Null);
    }

    [Test]
    public void EmptyString_ReturnsEmptyList()
    {
        var elems = VtmlConverter.Convert("", BaseStyle);
        Assert.That(elems, Is.Empty);
    }

    [Test]
    public void NullString_ReturnsEmptyList()
    {
        var elems = VtmlConverter.Convert(null!, BaseStyle);
        Assert.That(elems, Is.Empty);
    }

    [Test]
    public void Bold_ProducesBoldTextRun()
    {
        var elems = VtmlConverter.Convert(
            "Normal <strong>Bold</strong> Normal", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(3));

        var bold = elems[1] as VtmlTextRun;
        Assert.That(bold, Is.Not.Null);
        Assert.That(bold!.Text, Is.EqualTo("Bold"));
        Assert.That(bold.Style.Weight, Is.EqualTo(FontWeight.Bold));
        Assert.That(bold.Style.Boldness, Is.GreaterThan(0));
    }

    [Test]
    public void Italic_ProducesItalicTextRun()
    {
        var elems = VtmlConverter.Convert("<i>Italic</i>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var italic = elems[0] as VtmlTextRun;
        Assert.That(italic, Is.Not.Null);
        Assert.That(italic!.Style.Weight, Is.EqualTo(FontWeight.Italic));
    }

    [Test]
    public void FontColor_AppliesColor()
    {
        var elems = VtmlConverter.Convert(
            "<font color=\"#ff0000\">Red</font>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var run = elems[0] as VtmlTextRun;
        Assert.That(run, Is.Not.Null);
        Assert.That(run!.Style.Color.X, Is.EqualTo(1f).Within(0.01f));
        Assert.That(run.Style.Color.Y, Is.EqualTo(0f).Within(0.01f));
        Assert.That(run.Style.Color.Z, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void FontSize_AppliesSize()
    {
        var elems = VtmlConverter.Convert(
            "<font size=\"20\">Big</font>", BaseStyle);
        var run = elems[0] as VtmlTextRun;
        Assert.That(run!.Style.FontSize, Is.EqualTo(20f));
    }

    [Test]
    public void FontOpacity_AppliesOpacity()
    {
        var elems = VtmlConverter.Convert(
            "<font opacity=\"0.5\">Faded</font>", BaseStyle);
        var run = elems[0] as VtmlTextRun;
        Assert.That(run!.Style.Color.W, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void Br_ProducesLineBreak()
    {
        var elems = VtmlConverter.Convert("Line1<br>Line2", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(3));
        Assert.That(elems[1], Is.InstanceOf<VtmlLineBreak>());
    }

    [Test]
    public void Anchor_ProducesTextRunWithHref()
    {
        var elems = VtmlConverter.Convert(
            "<a href=\"handbook://page\">Click me</a>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var run = elems[0] as VtmlTextRun;
        Assert.That(run, Is.Not.Null);
        Assert.That(run!.Text, Is.EqualTo("Click me"));
        Assert.That(run.Href, Is.EqualTo("handbook://page"));
    }

    [Test]
    public void Icon_WithName_ProducesIconRun()
    {
        var elems = VtmlConverter.Convert(
            "<icon name=\"dice\"></icon>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var icon = elems[0] as VtmlIconRun;
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon!.Name, Is.EqualTo("dice"));
    }

    [Test]
    public void Icon_WithPath_ProducesIconRun()
    {
        var elems = VtmlConverter.Convert(
            "<icon path=\"icons/checkmark.svg\"></icon>", BaseStyle);
        var icon = elems[0] as VtmlIconRun;
        Assert.That(icon!.Path, Is.EqualTo("icons/checkmark.svg"));
    }

    [Test]
    public void Hotkey_ProducesHotkeyRun()
    {
        var elems = VtmlConverter.Convert(
            "<hk>sprint</hk>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var hk = elems[0] as VtmlHotkeyRun;
        Assert.That(hk, Is.Not.Null);
        Assert.That(hk!.HotkeyCode, Is.EqualTo("sprint"));
    }

    [Test]
    public void Hotkey_RemapsLegacyCodes()
    {
        var elems = VtmlConverter.Convert("<hk>leftmouse</hk>", BaseStyle);
        var hk = elems[0] as VtmlHotkeyRun;
        Assert.That(hk!.HotkeyCode, Is.EqualTo("primarymouse"));
    }

    [Test]
    public void ItemStack_ProducesItemStackRun()
    {
        var elems = VtmlConverter.Convert(
            "<itemstack type=\"block\" code=\"plank-oak\" rsize=\"2\" " +
            "floattype=\"left\" offx=\"5\" offy=\"10\"></itemstack>",
            BaseStyle);

        var item = elems[0] as VtmlItemStackRun;
        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Code, Is.EqualTo("plank-oak"));
        Assert.That(item.ItemType, Is.EqualTo("block"));
        Assert.That(item.FloatType, Is.EqualTo(VtmlFloat.Left));
        Assert.That(item.RSize, Is.EqualTo(2f));
        Assert.That(item.OffX, Is.EqualTo(5f));
        Assert.That(item.OffY, Is.EqualTo(10f));
    }

    [Test]
    public void Clear_ProducesClearFloat()
    {
        var elems = VtmlConverter.Convert("<clear>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));
        Assert.That(elems[0], Is.InstanceOf<VtmlClearFloat>());
    }

    [Test]
    public void NestedTags_ProduceCorrectStyles()
    {
        var elems = VtmlConverter.Convert(
            "<strong><font color=\"#00ff00\">BoldGreen</font></strong>",
            BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));

        var run = elems[0] as VtmlTextRun;
        Assert.That(run!.Style.Weight, Is.EqualTo(FontWeight.Bold));
        Assert.That(run.Style.Color.Y, Is.EqualTo(1f).Within(0.01f));
        Assert.That(run.Style.Color.X, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void StyleRestoresAfterClosingTag()
    {
        var elems = VtmlConverter.Convert(
            "Before <strong>Bold</strong> After", BaseStyle);

        var before = elems[0] as VtmlTextRun;
        var after = elems[2] as VtmlTextRun;
        Assert.That(before!.Style.Weight, Is.EqualTo(FontWeight.Normal));
        Assert.That(after!.Style.Weight, Is.EqualTo(FontWeight.Normal));
    }

    [Test]
    public void HexColor_WithHash_Parses()
    {
        Assert.That(
            VtmlConverter.TryParseHexColor("#aabbcc", out var color),
            Is.True);
        Assert.That(color.X, Is.EqualTo(0xaa / 255f).Within(0.001f));
        Assert.That(color.Y, Is.EqualTo(0xbb / 255f).Within(0.001f));
        Assert.That(color.Z, Is.EqualTo(0xcc / 255f).Within(0.001f));
    }

    [Test]
    public void HexColor_WithoutHash_Parses()
    {
        Assert.That(
            VtmlConverter.TryParseHexColor("ff8800", out var color),
            Is.True);
        Assert.That(color.X, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void HexColor_8Digit_ParsesWithAlpha()
    {
        Assert.That(
            VtmlConverter.TryParseHexColor("#80ff0000", out var color),
            Is.True);
        Assert.That(color.W, Is.EqualTo(0x80 / 255f).Within(0.001f));
        Assert.That(color.X, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Code_ProducesMonospaceFont()
    {
        var elems = VtmlConverter.Convert(
            "<code>mono text</code>", BaseStyle);
        var run = elems[0] as VtmlTextRun;
        Assert.That(run!.Style.FontFamily, Is.EqualTo("monospace"));
    }

    [Test]
    public void UnknownTag_ContentPreserved()
    {
        var elems = VtmlConverter.Convert(
            "<unknown>inner text</unknown>", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(1));
        var run = elems[0] as VtmlTextRun;
        Assert.That(run!.Text, Is.EqualTo("inner text"));
    }

    [Test]
    public void LiteralNewline_ProducesLineBreak()
    {
        var elems = VtmlConverter.Convert("Line1\nLine2", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(3));
        Assert.That(elems[0], Is.InstanceOf<VtmlTextRun>());
        Assert.That(elems[1], Is.InstanceOf<VtmlLineBreak>());
        Assert.That(elems[2], Is.InstanceOf<VtmlTextRun>());
        Assert.That(((VtmlTextRun)elems[0]).Text, Is.EqualTo("Line1"));
        Assert.That(((VtmlTextRun)elems[2]).Text, Is.EqualTo("Line2"));
    }

    [Test]
    public void CrLf_ProducesLineBreak()
    {
        var elems = VtmlConverter.Convert("A\r\nB", BaseStyle);
        Assert.That(elems, Has.Count.EqualTo(3));
        Assert.That(elems[1], Is.InstanceOf<VtmlLineBreak>());
    }
}

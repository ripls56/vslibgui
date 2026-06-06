using Gui.Core.Basic;
using Gui.Rendering.Text;
using Gui.Tests.Helpers;
using Gui.Vtml;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Vtml;

/// <summary>
///     Integration tests for the VtmlSpanBuilder → RichText → RenderRichText
///     pipeline, replacing the former RenderVtml-based tests.
/// </summary>
[TestFixture]
public class VtmlRichTextIntegrationTests
{
    private static readonly TextStyle BaseStyle = new() { FontSize = 14f, Color = Vector4.One };

    private static RenderRichText LayoutSpan(
        List<VtmlInlineElement> elements, float w = 500, float h = 500)
    {
        var span = VtmlSpanBuilder.Build(elements, BaseStyle);
        var widget = new RichText(span, BaseStyle);
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        element.RenderObject!.Layout(LayoutConstraints.Loose(w, h));
        return TestHelpers.FindRenderRichText(element)!;
    }

    [Test]
    public void EmptyElements_SizesZero()
    {
        var ro = LayoutSpan([]);
        Assert.That(ro.Size, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void SingleTextRun_HasPositiveSize()
    {
        var ro = LayoutSpan([new VtmlTextRun { Text = "Hello world", Style = BaseStyle }]);

        Assert.That(ro.Size.X, Is.GreaterThan(0));
        Assert.That(ro.Size.Y, Is.GreaterThan(0));
    }

    [Test]
    public void LineBreak_IncreasesHeight()
    {
        var singleLine = LayoutSpan([new VtmlTextRun { Text = "Line1", Style = BaseStyle }]);

        var twoLines = LayoutSpan([
            new VtmlTextRun { Text = "Line1", Style = BaseStyle },
            new VtmlLineBreak(),
            new VtmlTextRun { Text = "Line2", Style = BaseStyle }
        ]);

        Assert.That(twoLines.Size.Y, Is.GreaterThan(singleLine.Size.Y));
    }

    [Test]
    public void LongText_WrapsWithinConstraints()
    {
        var ro = LayoutSpan([
            new VtmlTextRun
            {
                Text = "This is a very long text that should wrap to multiple " +
                       "lines when the available width is constrained to 100 pixels",
                Style = BaseStyle
            }
        ], 100, 1000);

        Assert.That(ro.Size.X, Is.LessThanOrEqualTo(101));
        Assert.That(ro.Size.Y, Is.GreaterThan(20));
    }

    [Test]
    public void InlineIcon_ContributesToSize()
    {
        var ro = LayoutSpan([
            new VtmlTextRun { Text = "Text ", Style = BaseStyle },
            new VtmlIconRun { Name = "dice", Size = 24f },
            new VtmlTextRun { Text = " more", Style = BaseStyle }
        ]);

        Assert.That(ro.Size.X, Is.GreaterThan(24));
        Assert.That(ro.Size.Y, Is.GreaterThanOrEqualTo(24));
    }

    [Test]
    public void InlineItemStack_ContributesToSize()
    {
        var ro = LayoutSpan([
            new VtmlItemStackRun
            {
                Code = "plank-oak", FloatType = VtmlFloat.Inline, RSize = 1f, FontHeight = 14f
            }
        ]);

        var expectedSize = 14f * 1.3f;
        Assert.That(ro.Size.X, Is.EqualTo(expectedSize).Within(1f));
        Assert.That(ro.Size.Y, Is.EqualTo(expectedSize).Within(1f));
    }

    [Test]
    public void FloatedItemStack_ReservesSpace()
    {
        var ro = LayoutSpan([
            new VtmlItemStackRun
            {
                Code = "plank-oak", FloatType = VtmlFloat.Left, RSize = 1f, FontHeight = 48f
            },

            new VtmlTextRun { Text = "Text next to float", Style = BaseStyle }
        ]);

        var itemSize = 48f * 1.3f;
        Assert.That(ro.Size.Y, Is.GreaterThanOrEqualTo(itemSize - 1));
    }

    [Test]
    public void ClearFloat_AdvancesPastFloats()
    {
        var ro = LayoutSpan([
            new VtmlItemStackRun
            {
                Code = "plank-oak", FloatType = VtmlFloat.Left, RSize = 1f, FontHeight = 48f
            },

            new VtmlClearFloat(),
            new VtmlTextRun { Text = "Below", Style = BaseStyle }
        ]);

        var itemSize = 48f * 1.3f;
        Assert.That(ro.Size.Y, Is.GreaterThanOrEqualTo(itemSize));
    }

    [Test]
    public void FullVtmlPipeline_ParsesAndLayouts()
    {
        var elements = VtmlConverter.Convert(
            "Hello <strong>bold</strong> and <i>italic</i><br>" +
            "<font color=\"#ff0000\" size=\"20\">Red big text</font>",
            BaseStyle);

        var ro = LayoutSpan(elements, 300, 1000);

        Assert.That(ro.Size.X, Is.GreaterThan(0));
        Assert.That(ro.Size.Y, Is.GreaterThan(0));
    }
}

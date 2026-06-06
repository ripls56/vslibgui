using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class BoxShadowTests
{
    private static readonly Vector4 Black = new(0, 0, 0, 1);
    private static readonly Vector4 Red = new(1, 0, 0, 1);


    [Test]
    public void DefaultValues_AreCorrect()
    {
        var s = new BoxShadow(Black, Vector2.Zero);
        Assert.That(s.BlurRadius, Is.EqualTo(0f));
        Assert.That(s.SpreadRadius, Is.EqualTo(0f));
        Assert.That(s.Inset, Is.False);
    }

    [Test]
    public void Extent_IsZero_WhenNoBlurOrSpreadOrOffset()
    {
        var s = new BoxShadow(Black, Vector2.Zero);
        Assert.That(s.Extent, Is.EqualTo(0f));
    }

    [Test]
    public void Extent_AccountsForBlurRadius()
    {
        var s = new BoxShadow(Black, Vector2.Zero, 10f);
        // 10 * 3 = 30
        Assert.That(s.Extent, Is.EqualTo(30f).Within(0.01f));
    }

    [Test]
    public void Extent_AccountsForSpread()
    {
        var s = new BoxShadow(Black, Vector2.Zero, SpreadRadius: 5f);
        Assert.That(s.Extent, Is.EqualTo(5f).Within(0.01f));
    }

    [Test]
    public void Extent_AccountsForOffset()
    {
        // Max of abs components: max(3, 4) = 4
        var s = new BoxShadow(Black, new Vector2(3f, 4f));
        Assert.That(s.Extent, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void Extent_CombinesAllFactors()
    {
        // blur=10*3=30, spread=5, offset=max(2,3)=3 → total=38
        var s = new BoxShadow(Black, new Vector2(2f, 3f),
            10f, 5f);
        Assert.That(s.Extent, Is.EqualTo(38f).Within(0.01f));
    }

    [Test]
    public void Extent_IsZero_ForInsetShadow()
    {
        var s = new BoxShadow(Black, new Vector2(10f, 10f),
            20f, 15f, true);
        Assert.That(s.Extent, Is.EqualTo(0f));
    }

    [Test]
    public void Extent_ClampsNegativeBlur()
    {
        var s = new BoxShadow(Black, Vector2.Zero, -5f);
        Assert.That(s.Extent, Is.EqualTo(0f));
    }

    [Test]
    public void ValueEquality_Works()
    {
        var a = new BoxShadow(Black, new Vector2(2f, 3f), 4f, 1f);
        var b = new BoxShadow(Black, new Vector2(2f, 3f), 4f, 1f);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ValueEquality_DetectsDifferences()
    {
        var a = new BoxShadow(Black, Vector2.Zero, 4f);
        var b = new BoxShadow(Red, Vector2.Zero, 4f);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void ToSKColor_ConvertsCorrectly()
    {
        var s = new BoxShadow(new Vector4(1f, 0f, 0.5f, 0.8f), Vector2.Zero);
        var c = s.ToSkColor();
        Assert.That(c.Red, Is.EqualTo(255));
        Assert.That(c.Green, Is.EqualTo(0));
        Assert.That(c.Blue, Is.EqualTo(128).Within(1));
        Assert.That(c.Alpha, Is.EqualTo(204).Within(1));
    }


    [Test]
    public void BoxStyle_BoxShadows_DefaultsToNull()
    {
        var style = new BoxStyle();
        Assert.That(style.BoxShadows, Is.Null);
    }

    [Test]
    public void BoxStyle_BoxShadows_CanBeAssigned()
    {
        var shadows = new[] { new BoxShadow(Black, new Vector2(0, 4f), 8f) };
        var style = new BoxStyle { BoxShadows = shadows };
        Assert.That(style.BoxShadows, Is.SameAs(shadows));
    }


    [Test]
    public void RenderBox_Shadows_MarkNeedsPaint_WhenChanged()
    {
        var ro = new RenderBox();
        ro.Layout(LayoutConstraints.Tight(100, 100));
        // Paint once to clear flags
        ro.NeedsPaint = false;

        ro.Shadows = [new BoxShadow(Black, Vector2.Zero, 4f)];
        Assert.That(ro.NeedsPaint, Is.True);
    }

    [Test]
    public void RenderBox_Shadows_NoRepaint_WhenSameReference()
    {
        var shadows = new[] { new BoxShadow(Black, Vector2.Zero, 4f) };
        var ro = new RenderBox();
        ro.Shadows = shadows;
        ro.Layout(LayoutConstraints.Tight(100, 100));
        ro.NeedsPaint = false;

        ro.Shadows = shadows; // same reference
        Assert.That(ro.NeedsPaint, Is.False);
    }

    [Test]
    public void RenderBox_Shadows_DoesNotMarkNeedsLayout()
    {
        var ro = new RenderBox();
        ro.Layout(LayoutConstraints.Tight(100, 100));
        ro.NeedsLayout = false;

        ro.Shadows = [new BoxShadow(Black, Vector2.Zero, 4f)];
        Assert.That(ro.NeedsLayout, Is.False);
    }
}

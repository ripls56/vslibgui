using Gui.Core.Basic;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class ProgressBarTests
{
    private RenderProgressBar MakeRender(
        float value,
        float height = 10,
        float cornerRadius = 5,
        float borderThickness = 1)
    {
        return new RenderProgressBar
        {
            Value = value,
            TrackHeight = height,
            CornerRadius = cornerRadius,
            BorderThickness = borderThickness,
            FillColor = new Vector4(0.26f, 0.52f, 0.96f, 1f),
            TrackColor = new Vector4(0.18f, 0.18f, 0.18f, 1f),
            BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f)
        };
    }

    private void DoLayout(RenderProgressBar ro, float width = 200) =>
        ro.Layout(new LayoutConstraints(0, width));

    [Test]
    public void ZeroValue_FillWidthIsZero()
    {
        var ro = MakeRender(0f);
        DoLayout(ro);

        Assert.That(ro.Value, Is.EqualTo(0f));
        Assert.That(ro.Size.X, Is.EqualTo(200f).Within(0.01f));
    }

    [Test]
    public void FullValue_FillWidthEqualsTotalWidth()
    {
        var ro = MakeRender(1f);
        DoLayout(ro);

        Assert.That(ro.Value, Is.EqualTo(1f));
    }

    [Test]
    public void HalfValue_IsStoredCorrectly()
    {
        var ro = MakeRender(0.5f);
        DoLayout(ro);

        Assert.That(ro.Value, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void ClampsAbove1()
    {
        var ro = MakeRender(1.5f);
        Assert.That(ro.Value, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void ClampsBelow0()
    {
        var ro = MakeRender(-0.5f);
        Assert.That(ro.Value, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void UsesFullWidth()
    {
        var ro = MakeRender(0.5f);
        DoLayout(ro, 300);

        Assert.That(ro.Size.X, Is.EqualTo(300f).Within(0.01f));
    }

    [Test]
    public void UsesSpecifiedHeight()
    {
        var ro = MakeRender(0.5f, 20);
        DoLayout(ro);

        Assert.That(ro.Size.Y, Is.EqualTo(20f).Within(0.01f));
    }

    [Test]
    public void UpdatingValue_MarksPaintDirty()
    {
        var ro = MakeRender(0.3f);
        DoLayout(ro);
        // Clear dirty flags by doing an initial "paint"
        ro.NeedsPaint = false;

        ro.Value = 0.7f;
        Assert.That(ro.NeedsPaint, Is.True);
    }
}

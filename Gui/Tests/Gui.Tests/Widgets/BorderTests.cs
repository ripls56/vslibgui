using Gui.Widgets.Painting;
using OpenTK.Mathematics;

namespace Gui.Tests.Widgets;

[TestFixture]
public class BorderTests
{
    private static readonly Vector4 Red = new(1, 0, 0, 1);
    private static readonly Vector4 Blue = new(0, 0, 1, 1);

    [Test]
    public void All_FourSidesEqual_IsUniform()
    {
        var border = Border.All(3f, Red);

        Assert.Multiple
        (() =>
            {
                Assert.That(border.IsUniform, Is.True);
                Assert.That(border.AnyWidth, Is.True);
                Assert.That(border.Left.Width, Is.EqualTo(3f));
                Assert.That(border.Bottom.Color, Is.EqualTo(Red));
            }
        );
    }

    [Test]
    public void Symmetric_SetsVerticalAndHorizontal()
    {
        var border = Border.Symmetric
        (
            vertical: 2f,
            horizontal: 4f,
            color: Red
        );

        Assert.Multiple
        (() =>
            {
                Assert.That(border.Top.Width, Is.EqualTo(2f));
                Assert.That(border.Left.Width, Is.EqualTo(4f));
                Assert.That(border.Right.Width, Is.EqualTo(4f));
                Assert.That(border.Bottom.Width, Is.EqualTo(2f));
                Assert.That(border.IsUniform, Is.False);
            }
        );
    }

    [Test]
    public void Only_SingleSide_NotUniformButHasWidth()
    {
        var border = Border.Only
        (
            top: new BorderSide(5f, Red)
        );

        Assert.Multiple
        (() =>
            {
                Assert.That(border.IsUniform, Is.False);
                Assert.That(border.AnyWidth, Is.True);
                Assert.That(border.Top.Width, Is.EqualTo(5f));
                Assert.That(border.Left.Width, Is.EqualTo(0f));
            }
        );
    }

    [Test]
    public void Zero_AllSidesNone_IsUniformWithoutWidth()
    {
        Assert.Multiple
        (() =>
            {
                Assert.That(Border.Zero.AnyWidth, Is.False);
                Assert.That(Border.Zero.IsUniform, Is.True);
            }
        );
    }

    [Test]
    public void EqualWidths_DifferentColors_NotUniform()
    {
        var border = new Border
        (
            new BorderSide(2f, Red),
            new BorderSide(2f, Red),
            new BorderSide(2f, Blue),
            new BorderSide(2f, Red)
        );

        Assert.That(border.IsUniform, Is.False);
    }

    [Test]
    public void Symmetric_UniformValues_IsUniform()
    {
        var border = Border.Symmetric
        (
            vertical: 4f,
            horizontal: 4f,
            color: Red
        );

        Assert.That(border.IsUniform, Is.True);
    }
}

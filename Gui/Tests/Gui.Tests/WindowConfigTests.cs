using OpenTK.Mathematics;

namespace Gui.Tests;

public class WindowConfigTests
{
    [Test]
    public void Defaults_Position_IsNull()
    {
        var config = new WindowConfig();
        Assert.That(config.Position, Is.Null);
    }

    [Test]
    public void Defaults_Size_IsNull()
    {
        var config = new WindowConfig();
        Assert.That(config.Size, Is.Null);
    }

    [Test]
    public void Defaults_Draggable_IsTrue()
    {
        var config = new WindowConfig();
        Assert.That(config.Draggable, Is.True);
    }

    [Test]
    public void Defaults_Resizable_IsTrue()
    {
        var config = new WindowConfig();
        Assert.That(config.Resizable, Is.True);
    }

    [Test]
    public void Defaults_MinSize()
    {
        var config = new WindowConfig();
        Assert.That(config.MinSize, Is.EqualTo(new Vector2(100, 50)));
    }

    [Test]
    public void IsShrinkWrap_FalseWhenSizeSet()
    {
        var config = new WindowConfig { Size = new Vector2(400, 300) };
        Assert.That(config.IsShrinkWrap, Is.False);
    }

    [Test]
    public void IsShrinkWrap_TrueWhenSizeNull()
    {
        var config = new WindowConfig { Size = null };
        Assert.That(config.IsShrinkWrap, Is.True);
    }

    [Test]
    public void ResizeEdge_FlagsComposition()
    {
        var edge = ResizeEdge.Left | ResizeEdge.Top;
        Assert.Multiple(() =>
        {
            Assert.That(edge.HasFlag(ResizeEdge.Left), Is.True);
            Assert.That(edge.HasFlag(ResizeEdge.Top), Is.True);
            Assert.That(edge.HasFlag(ResizeEdge.Right), Is.False);
            Assert.That(edge.HasFlag(ResizeEdge.Bottom), Is.False);
        });
    }

    [Test]
    public void ResizeEdge_AllCorners()
    {
        var topLeft = ResizeEdge.Left | ResizeEdge.Top;
        Assert.That((int)topLeft, Is.EqualTo(5));

        var bottomRight = ResizeEdge.Right | ResizeEdge.Bottom;
        Assert.That((int)bottomRight, Is.EqualTo(10));
    }
}

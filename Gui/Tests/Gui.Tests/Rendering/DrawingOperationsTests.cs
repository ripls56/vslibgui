using Gui.Rendering;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Rendering;

[TestFixture]
public class DrawingOperationsTests
{
    [Test]
    public void InsetRadii_WithPositiveInset_ReducesAllCorners()
    {
        var radii = new Vector4(10, 8, 12, 6);
        var result = DrawingOperations.InsetRadii(radii, 3f);
        Assert.That(result, Is.EqualTo(new Vector4(7, 5, 9, 3)));
    }

    [Test]
    public void InsetRadii_ClampsToZero()
    {
        var radii = new Vector4(2, 1, 3, 0);
        var result = DrawingOperations.InsetRadii(radii, 5f);
        Assert.That(result, Is.EqualTo(new Vector4(0, 0, 0, 0)));
    }

    [Test]
    public void InsetRadii_ZeroInset_ReturnsOriginal()
    {
        var radii = new Vector4(4, 5, 6, 7);
        var result = DrawingOperations.InsetRadii(radii, 0f);
        Assert.That(result, Is.EqualTo(radii));
    }

    [Test]
    public void ComputeBgInset_OpaqueThickBorder_ReturnsHalf()
    {
        var result = DrawingOperations.ComputeBgInset(4f, 1.0f);
        Assert.That(result, Is.EqualTo(2f));
    }

    [Test]
    public void ComputeBgInset_TransparentBorder_ReturnsZero()
    {
        var result = DrawingOperations.ComputeBgInset(4f, 0.5f);
        Assert.That(result, Is.EqualTo(0f));
    }

    [Test]
    public void ComputeBgInset_ZeroBorder_ReturnsZero()
    {
        var result = DrawingOperations.ComputeBgInset(0f, 1.0f);
        Assert.That(result, Is.EqualTo(0f));
    }

    [Test]
    public void DrawBorder_ZeroBorder_IsNoop()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var paint = new SKPaint();
        using var rr = new SKRoundRect();
        var buf = new SKPoint[4];
        paint.Style = SKPaintStyle.Fill;

        DrawingOperations.DrawBorder(
            surface.Canvas, new SKRect(0, 0, 50, 50),
            new Vector4(4), 0f, new Vector4(1, 0, 0, 1),
            paint, rr, buf);

        Assert.That(paint.Style, Is.EqualTo(SKPaintStyle.Fill));
    }

    [Test]
    public void DrawBorder_PositiveBorder_SetsPaintToStroke()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var paint = new SKPaint();
        using var rr = new SKRoundRect();
        var buf = new SKPoint[4];

        DrawingOperations.DrawBorder(
            surface.Canvas, new SKRect(0, 0, 50, 50),
            new Vector4(4), 2f, new Vector4(1, 0, 0, 1),
            paint, rr, buf);

        Assert.That(paint.Style, Is.EqualTo(SKPaintStyle.Stroke));
        Assert.That(paint.StrokeWidth, Is.EqualTo(2f));
    }
}

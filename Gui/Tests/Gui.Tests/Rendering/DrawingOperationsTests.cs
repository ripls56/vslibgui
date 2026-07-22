using Gui.Rendering;
using Gui.Widgets.Painting;
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

        DrawingOperations.DrawBorder
        (
            surface.Canvas, new SKRect(0, 0, 50, 50),
            new Vector4(4), 0f, new Vector4(1, 0, 0, 1),
            paint, rr, buf
        );

        Assert.That(paint.Style, Is.EqualTo(SKPaintStyle.Fill));
    }

    [Test]
    public void DrawBorder_PositiveBorder_SetsPaintToStroke()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var paint = new SKPaint();
        using var rr = new SKRoundRect();
        var buf = new SKPoint[4];

        DrawingOperations.DrawBorder
        (
            surface.Canvas, new SKRect(0, 0, 50, 50),
            new Vector4(4), 2f, new Vector4(1, 0, 0, 1),
            paint, rr, buf
        );

        Assert.That(paint.Style, Is.EqualTo(SKPaintStyle.Stroke));
        Assert.That(paint.StrokeWidth, Is.EqualTo(2f));
    }

    [Test]
    public void DrawBorder_PerSide_PaintsEachSideColor()
    {
        using var bmp = new SKBitmap
        (
            60,
            60,
            SKColorType.Rgba8888,
            SKAlphaType.Premul
        );
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint();
        using var outerRR = new SKRoundRect();
        using var innerRR = new SKRoundRect();
        var buf = new SKPoint[4];
        canvas.Clear(SKColors.Transparent);

        var border = new Border
        (
            Left: new BorderSide(4f, new Vector4(0, 0, 1, 1)),
            Top: new BorderSide(4f, new Vector4(0, 1, 0, 1)),
            Right: new BorderSide(4f, new Vector4(1, 0, 1, 1)),
            Bottom: new BorderSide(4f, new Vector4(1, 1, 0, 1))
        );

        DrawingOperations.DrawBorder
        (
            canvas,
            new SKRect(10, 10, 50, 50),
            Vector4.Zero,
            border,
            paint,
            outerRR,
            innerRR,
            buf
        );

        Assert.Multiple
        (() =>
            {
                Assert.That(bmp.GetPixel(30, 11), Is.EqualTo(new SKColor(0, 255, 0, 255)));
                Assert.That(bmp.GetPixel(48, 30), Is.EqualTo(new SKColor(255, 0, 255, 255)));
                Assert.That(bmp.GetPixel(30, 48), Is.EqualTo(new SKColor(255, 255, 0, 255)));
                Assert.That(bmp.GetPixel(11, 30), Is.EqualTo(new SKColor(0, 0, 255, 255)));
            }
        );
    }

    [Test]
    public void DrawBorder_PerSide_ZeroWidthSideLeavesGap()
    {
        using var bmp = new SKBitmap
        (
            60,
            60,
            SKColorType.Rgba8888,
            SKAlphaType.Premul
        );
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint();
        using var outerRR = new SKRoundRect();
        using var innerRR = new SKRoundRect();
        var buf = new SKPoint[4];
        canvas.Clear(SKColors.Transparent);

        var border = Border.Only
        (
            top: new BorderSide(4f, new Vector4(0, 1, 0, 1))
        );

        DrawingOperations.DrawBorder
        (
            canvas,
            new SKRect(10, 10, 50, 50),
            Vector4.Zero,
            border,
            paint,
            outerRR,
            innerRR,
            buf
        );

        Assert.Multiple
        (() =>
            {
                Assert.That(bmp.GetPixel(30, 11).Green, Is.EqualTo(255));
                Assert.That(bmp.GetPixel(30, 48).Alpha, Is.EqualTo(0));
                Assert.That(bmp.GetPixel(11, 30).Alpha, Is.EqualTo(0));
            }
        );
    }
}

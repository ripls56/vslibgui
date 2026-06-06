using Gui.Core.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Rendering;

public class ImageTests
{
    [Test]
    public void Layout_ExplicitSize_IgnoresBitmapDimensions()
    {
        var bitmap = new SKBitmap(400, 300);
        var ro = new RenderImage { Bitmap = bitmap, Width = 100, Height = 80 };

        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size, Is.EqualTo(new Vector2(100, 80)));
        bitmap.Dispose();
    }

    [Test]
    public void Layout_IntrinsicSize_UsesBitmapDimensions()
    {
        var bitmap = new SKBitmap(200, 150);
        var ro = new RenderImage { Bitmap = bitmap };

        ro.Layout(LayoutConstraints.Loose(500, 500));

        Assert.That(ro.Size, Is.EqualTo(new Vector2(200, 150)));
        bitmap.Dispose();
    }


    [Test]
    public void ComputeRects_Fill_FullSrcAndDst()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.Fill);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, dst) = ro.ComputeRects();

        Assert.That(src, Is.EqualTo(new SKRect(0, 0, 200, 100)));
        Assert.That(dst, Is.EqualTo(new SKRect(0, 0, 100, 100)));
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_Contain_LandscapeBitmapCenteredVertically()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.Contain);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, dst) = ro.ComputeRects();

        Assert.That(src, Is.EqualTo(new SKRect(0, 0, 200, 100)));
        AssertRect(dst, 0, 25, 100, 50);
        bmp.Dispose();
    }

    [Test]
    public void ComputeRects_Contain_PortraitBitmapCenteredHorizontally()
    {
        var (ro, bmp) = MakeRenderImage(100, 200, BoxFit.Contain);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (_, dst) = ro.ComputeRects();

        AssertRect(dst, 25, 0, 50, 100);
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_Cover_LandscapeBitmapCropsCenterHorizontally()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.Cover);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, dst) = ro.ComputeRects();

        AssertRect(src, 50, 0, 100, 100);
        Assert.That(dst, Is.EqualTo(new SKRect(0, 0, 100, 100)));
        bmp.Dispose();
    }

    [Test]
    public void ComputeRects_Cover_AlignmentTopLeft_CropsFromTopLeft()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.Cover, Alignment.TopLeft);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, _) = ro.ComputeRects();

        AssertRect(src, 0, 0, 100, 100);
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_FitWidth_ScalesToWidth()
    {
        var (ro, bmp) = MakeRenderImage(100, 200, BoxFit.FitWidth);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, dst) = ro.ComputeRects();

        Assert.That(src, Is.EqualTo(new SKRect(0, 0, 100, 200)));
        AssertRect(dst, 0, -50, 100, 200);
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_FitHeight_ScalesToHeight()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.FitHeight);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (_, dst) = ro.ComputeRects();

        AssertRect(dst, -50, 0, 200, 100);
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_None_UnscaledCentered()
    {
        var (ro, bmp) = MakeRenderImage(50, 30, BoxFit.None);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (src, dst) = ro.ComputeRects();

        Assert.That(src, Is.EqualTo(new SKRect(0, 0, 50, 30)));
        AssertRect(dst, 25, 35, 50, 30);
        bmp.Dispose();
    }


    [Test]
    public void ComputeRects_ScaleDown_SmallBitmapNotUpscaled()
    {
        var (ro, bmp) = MakeRenderImage(50, 30, BoxFit.ScaleDown);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (_, dst) = ro.ComputeRects();

        AssertRect(dst, 25, 35, 50, 30);
        bmp.Dispose();
    }

    [Test]
    public void ComputeRects_ScaleDown_LargeBitmapScaledDown()
    {
        var (ro, bmp) = MakeRenderImage(200, 100, BoxFit.ScaleDown);
        ro.Layout(LayoutConstraints.Tight(100, 100));
        var (_, dst) = ro.ComputeRects();

        AssertRect(dst, 0, 25, 100, 50);
        bmp.Dispose();
    }


    private static (RenderImage ro, SKBitmap bmp) MakeRenderImage(
        int bitmapW, int bitmapH,
        BoxFit fit,
        Alignment? alignment = null)
    {
        var bitmap = new SKBitmap(bitmapW, bitmapH);
        var ro = new RenderImage
        {
            Bitmap = bitmap, Fit = fit, Alignment = alignment ?? Alignment.Center
        };
        return (ro, bitmap);
    }

    private static void AssertRect(SKRect rect, float left, float top, float width, float height)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rect.Left, Is.EqualTo(left).Within(0.01f), "Left");
            Assert.That(rect.Top, Is.EqualTo(top).Within(0.01f), "Top");
            Assert.That(rect.Width, Is.EqualTo(width).Within(0.01f), "Width");
            Assert.That(rect.Height, Is.EqualTo(height).Within(0.01f), "Height");
        });
    }
}

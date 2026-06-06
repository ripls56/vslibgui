using Gui.Core.Basic;
using Gui.Rendering;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Tests.Rendering;

public class NineSliceTests
{
    [Test]
    public void EdgeInsets_All_SetsAllSides()
    {
        var b = EdgeInsets.All(16);
        Assert.That(b.Left, Is.EqualTo(16f));
        Assert.That(b.Top, Is.EqualTo(16f));
        Assert.That(b.Right, Is.EqualTo(16f));
        Assert.That(b.Bottom, Is.EqualTo(16f));
    }

    [Test]
    public void EdgeInsets_Symmetric_HorizontalOnly()
    {
        var b = EdgeInsets.Symmetric(horizontal: 12);
        Assert.That(b.Left, Is.EqualTo(12f));
        Assert.That(b.Right, Is.EqualTo(12f));
        Assert.That(b.Top, Is.EqualTo(0f));
        Assert.That(b.Bottom, Is.EqualTo(0f));
    }

    [Test]
    public void EdgeInsets_Symmetric_VerticalOnly()
    {
        var b = EdgeInsets.Symmetric(10);
        Assert.That(b.Top, Is.EqualTo(10f));
        Assert.That(b.Bottom, Is.EqualTo(10f));
        Assert.That(b.Left, Is.EqualTo(0f));
        Assert.That(b.Right, Is.EqualTo(0f));
    }

    [Test]
    public void EdgeInsets_Symmetric_BothAxes()
    {
        var b = EdgeInsets.Symmetric(4, 8);
        Assert.That(b.Left, Is.EqualTo(8f));
        Assert.That(b.Right, Is.EqualTo(8f));
        Assert.That(b.Top, Is.EqualTo(4f));
        Assert.That(b.Bottom, Is.EqualTo(4f));
    }

    [Test]
    public void EdgeInsets_Only_SetsIndividualSides()
    {
        var b = EdgeInsets.Only(5, 10, 15, 20);
        Assert.That(b.Left, Is.EqualTo(5f));
        Assert.That(b.Top, Is.EqualTo(10f));
        Assert.That(b.Right, Is.EqualTo(15f));
        Assert.That(b.Bottom, Is.EqualTo(20f));
    }

    [Test]
    public void EdgeInsets_Equality()
    {
        var a = EdgeInsets.All(16);
        var b = EdgeInsets.All(16);
        var c = EdgeInsets.All(8);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
    }


    [Test]
    public void RenderNineSlice_ExplicitSize_LayoutsCorrectly()
    {
        var ro = new RenderNineSlice
        {
            MinWidth = 200, MaxWidth = 200, MinHeight = 80, MaxHeight = 80
        };
        ro.Layout(LayoutConstraints.Loose(500, 500));
        Assert.That(ro.Size, Is.EqualTo(new Vector2(200, 80)));
    }

    [Test]
    public void RenderNineSlice_NoExplicitSize_WrapsToZeroWithNoBitmap()
    {
        var ro = new RenderNineSlice();
        ro.Layout(LayoutConstraints.Loose(500, 500));
        Assert.That(ro.Size, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void RenderNineSlice_IntrinsicSize_UsesBitmapDimensionsWhenNoConstraints()
    {
        var bmp = new SKBitmap(128, 64);
        var ro = new RenderNineSlice { Bitmap = bmp };
        ro.Layout(LayoutConstraints.Loose(500, 500));
        Assert.That(ro.Size, Is.EqualTo(new Vector2(128, 64)));
        bmp.Dispose();
    }

    [Test]
    public void RenderNineSlice_ExplicitConstraintsOverrideIntrinsicSize()
    {
        var bmp = new SKBitmap(128, 64);
        var ro = new RenderNineSlice
        {
            Bitmap = bmp,
            MinWidth = 200,
            MaxWidth = 200,
            MinHeight = 50,
            MaxHeight = 50
        };
        ro.Layout(LayoutConstraints.Loose(500, 500));
        Assert.That(ro.Size, Is.EqualTo(new Vector2(200, 50)));
        bmp.Dispose();
    }

    [Test]
    public void RenderNineSlice_MarksPaintOnBitmapChange()
    {
        var ro = new RenderNineSlice();
        ro.Layout(LayoutConstraints.Loose(100, 100));
        ro.ResetDirtyFlags();

        var bmp = new SKBitmap(64, 64);
        ro.Bitmap = bmp;
        Assert.That(ro.NeedsPaint, Is.True);
        bmp.Dispose();
    }

    [Test]
    public void RenderNineSlice_MarksPaintOnSliceChange()
    {
        var ro = new RenderNineSlice();
        ro.Layout(LayoutConstraints.Loose(100, 100));
        ro.ResetDirtyFlags();

        ro.Slice = EdgeInsets.All(16);
        Assert.That(ro.NeedsPaint, Is.True);
    }

    [Test]
    public void RenderNineSlice_MarksPaintOnDrawModeChange()
    {
        var ro = new RenderNineSlice();
        ro.Layout(LayoutConstraints.Loose(100, 100));
        ro.ResetDirtyFlags();

        ro.DrawMode = ImageDrawMode.Tiled;
        Assert.That(ro.NeedsPaint, Is.True);
    }

    [Test]
    public void RenderNineSlice_NoBitmapNoRedundantPaintFlag()
    {
        var ro = new RenderNineSlice();
        ro.Layout(LayoutConstraints.Loose(100, 100));
        ro.ResetDirtyFlags();

        // Setting the same bitmap value (null) should not trigger repaint.
        ro.Bitmap = null;
        Assert.That(ro.NeedsPaint, Is.False);
    }


    [Test]
    public void NineSliceBox_UpdateRenderObject_SyncsAllProperties()
    {
        var bmp = new SKBitmap(64, 64);
        var slice = EdgeInsets.All(16);
        var tint = new Vector4(1, 0.5f, 0.5f, 1);

        var widget = new NineSliceBox(bmp, slice, 1f, ImageDrawMode.Tiled, tint, 200, 80);
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);

        // NineSliceBox now directly creates RenderNineSlice
        var ro = (RenderNineSlice)element.RenderObject!;

        Assert.That(ro.Bitmap, Is.EqualTo(bmp));
        Assert.That(ro.Slice, Is.EqualTo(slice));
        Assert.That(ro.DrawMode, Is.EqualTo(ImageDrawMode.Tiled));
        Assert.That(ro.Tint, Is.EqualTo(tint));

        // Size is constrained by the MinWidth/MaxWidth set in UpdateRenderObject
        ro.Layout(LayoutConstraints.Loose(1000, 1000));
        Assert.That(ro.Size.X, Is.EqualTo(200f));
        Assert.That(ro.Size.Y, Is.EqualTo(80f));

        bmp.Dispose();
    }

    [Test]
    public void NineSliceBox_DefaultTint_IsWhite()
    {
        var bmp = new SKBitmap(32, 32);
        var widget = new NineSliceBox(bmp, EdgeInsets.All(8));
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);

        // NineSliceBox now directly creates RenderNineSlice
        var ro = (RenderNineSlice)element.RenderObject!;

        Assert.That(ro.Tint, Is.EqualTo(Vector4.One));
        bmp.Dispose();
    }
}

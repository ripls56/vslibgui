using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Rendering;
using SkiaSharp;

namespace Gui.Tests.Rendering;

[TestFixture]
public class RepaintTests
{
    [Test]
    public void MarkNeedsPaint_BubblesToRoot()
    {
        // Arrange
        var root = new RenderBox();
        var child = new RenderBox();
        root.AddChild(child);

        // Act
        child.MarkNeedsPaint();

        // Assert
        Assert.That(child.NeedsPaint, Is.True);
        Assert.That(root.NeedsPaint, Is.True);
    }

    [Test]
    public void MarkNeedsPaint_StopsIfAlreadyDirty()
    {
        var root = new RenderBox();
        var child = new RenderBox();
        root.AddChild(child);

        // Act & Assert (No crash, no infinite loop)
        child.MarkNeedsPaint();
        Assert.That(child.NeedsPaint, Is.True);
        Assert.That(root.NeedsPaint, Is.True);
    }

    [Test]
    public void RepaintBoundary_IsolatesRepaint()
    {
        // Arrange
        var root = new RenderBox();
        var boundary = new RenderRepaintBoundary();
        var leaf = new RenderBox();

        root.AddChild(boundary);
        boundary.AddChild(leaf);

        // Act
        leaf.MarkNeedsPaint();

        // Assert
        Assert.That(leaf.NeedsPaint, Is.True);
        Assert.That(boundary.NeedsPaint, Is.True);
        Assert.That(root.NeedsPaint, Is.True);
    }

    [Test]
    public void Paint_UpdatesRepaintRecord_WhenDirty()
    {
        // Arrange — a fresh RenderBox starts with NeedsPaint=true
        RenderObject.AdvanceFrame();
        var ro = new RenderBox();
        using var bmp = new SKBitmap(1, 1);
        using var canvas = new SKCanvas(bmp);
        var context = new PaintingContext(canvas, 1234.5);

        // Act
        ro.Paint(context);

        // Assert
        Assert.That(ro.RepaintRecord.WasDirtyPaintedThisFrame(RenderObject.CurrentFrameId),
            Is.True);
        Assert.That(ro.RepaintRecord.LastEventTimestampMs, Is.EqualTo(1234.5));
        Assert.That(ro.WasPaintedThisFrame, Is.True);
    }

    [Test]
    public void Paint_DoesNotUpdateRepaintRecord_WhenClean()
    {
        // Arrange
        RenderObject.AdvanceFrame();
        var ro = new RenderBox();
        ro.NeedsPaint = false;
        ro.ChildNeedsPaint = false;
        var frameId = RenderObject.CurrentFrameId;

        using var bmp = new SKBitmap(1, 1);
        using var canvas = new SKCanvas(bmp);
        var context = new PaintingContext(canvas, 999.0);

        // Act
        ro.Paint(context);

        // Assert: node was visited but not dirty-painted
        Assert.That(ro.RepaintRecord.WasDirtyPaintedThisFrame(frameId), Is.False);
        Assert.That(ro.WasPaintedThisFrame, Is.True);
    }

    [Test]
    public void Paint_IncrementsDirtyPaintCount_OnEachDirtyPaint()
    {
        var ro = new RenderBox();
        var countBefore = ro.RepaintRecord.DirtyPaintCount;

        // Paint once while dirty
        RenderObject.AdvanceFrame();
        ro.MarkNeedsPaint();
        using var bmp = new SKBitmap(1, 1);
        using var canvas = new SKCanvas(bmp);
        ro.Paint(new PaintingContext(canvas, 0));

        Assert.That(ro.RepaintRecord.DirtyPaintCount, Is.EqualTo(countBefore + 1));
    }

    [Test]
    public void Paint_HotFrameCount_IncrementsOnDirtyPaint()
    {
        var ro = new RenderBox();
        var hotBefore = ro.RepaintRecord.HotFrameCount;

        RenderObject.AdvanceFrame();
        ro.MarkNeedsPaint();
        using var bmp = new SKBitmap(1, 1);
        using var canvas = new SKCanvas(bmp);
        ro.Paint(new PaintingContext(canvas, 0));

        Assert.That(ro.RepaintRecord.HotFrameCount, Is.GreaterThan(hotBefore));
    }

    [Test]
    public void RepaintRecord_StartsWithSentinelFrameIds()
    {
        // Frame IDs must start at -1 (not 0) to avoid false positives on frame 0
        // where CurrentFrameId also starts at 0.
        var ro = new RenderBox();
        Assert.That(ro.RepaintRecord.DirtyPaintedFrameId, Is.EqualTo(-1));
        Assert.That(ro.RepaintRecord.CacheHitFrameId, Is.EqualTo(-1));
        Assert.That(ro.RepaintRecord.DirtyPaintCount, Is.EqualTo(0));
    }
}

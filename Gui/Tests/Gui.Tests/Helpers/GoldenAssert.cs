using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using SkiaSharp;

namespace Gui.Tests.Helpers;

/// <summary>Provides golden image assertion utilities for snapshot testing.</summary>
internal static class GoldenAssert
{
    /// <summary>Renders <paramref name="widget"/> at <paramref name="size"/> and compares to stored golden PNG.</summary>
    internal static void Matches(Widget widget, string name, SKSizeI size)
    {
        var buildOwner = new BuildOwner();
        var element = widget.CreateElement();
        element.AssignOwner(buildOwner);
        element.Mount(null);
        buildOwner.BuildDirtyElements();
        var ro = element.RenderObject!;
        ro.Layout(LayoutConstraints.Tight(size.Width, size.Height));

        RenderObject.AdvanceFrame();
        using var bmp = new SKBitmap(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        ro.Paint(new PaintingContext(canvas, 0.0));

        var actualBytes = bmp.Encode(SKEncodedImageFormat.Png, 100).ToArray();

        if (Environment.GetEnvironmentVariable("GUI_UPDATE_GOLDENS") == "1")
        {
            var sourceDir = Environment.GetEnvironmentVariable("GUI_GOLDEN_SOURCE_DIR")
                ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../Goldens"));
            Directory.CreateDirectory(sourceDir);
            File.WriteAllBytes(Path.Combine(sourceDir, $"{name}.png"), actualBytes);
            Assert.Pass($"Golden updated: {name}");
            return;
        }

        var baselinePath = Path.Combine(AppContext.BaseDirectory, "Goldens", $"{name}.png");
        if (!File.Exists(baselinePath))
            Assert.Fail($"Golden not found: {baselinePath}. Run with GUI_UPDATE_GOLDENS=1 to create.");

        using var expectedBmp = SKBitmap.Decode(File.ReadAllBytes(baselinePath));
        CompareOrFail(bmp, expectedBmp, name);
    }

    private static void CompareOrFail(SKBitmap actual, SKBitmap expected, string name)
    {
        if (actual.Width != expected.Width || actual.Height != expected.Height)
            Assert.Fail($"Golden '{name}' size mismatch: {expected.Width}x{expected.Height} vs {actual.Width}x{actual.Height}");

        int total = actual.Width * actual.Height;
        int failed = 0;
        using var diff = new SKBitmap(actual.Width, actual.Height);

        for (int y = 0; y < actual.Height; y++)
        {
            for (int x = 0; x < actual.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);
                int maxDelta = Math.Max(
                    Math.Max(Math.Abs(a.Red - e.Red), Math.Abs(a.Green - e.Green)),
                    Math.Max(Math.Abs(a.Blue - e.Blue), Math.Abs(a.Alpha - e.Alpha)));
                if (maxDelta > 4)
                {
                    failed++;
                    diff.SetPixel(x, y, SKColors.Red);
                }
                else
                {
                    diff.SetPixel(x, y, SKColors.White);
                }
            }
        }

        if ((double)failed / total > 0.001)
        {
            var dir = AppContext.BaseDirectory;
            File.WriteAllBytes(Path.Combine(dir, $"{name}.actual.png"),
                actual.Encode(SKEncodedImageFormat.Png, 100).ToArray());
            File.WriteAllBytes(Path.Combine(dir, $"{name}.diff.png"),
                diff.Encode(SKEncodedImageFormat.Png, 100).ToArray());
            Assert.Fail($"Golden '{name}': {failed}/{total} pixels differ. See {name}.actual.png");
        }
    }
}

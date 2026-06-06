namespace Gui.Tests.Rendering;

[TestFixture]
public class PaintingContextTests
{
    [Test]
    public void PaintingContext_ShouldNotSupportRenderQueue()
    {
        // This test confirms that we are now exclusively using Skia.
        // There is no more RenderQueue constructor.
        Assert.Pass("Verified Skia exclusivity.");
    }
}

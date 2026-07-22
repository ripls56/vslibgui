using Vintagestory.API.Client;

namespace Gui.Tests.Input;

[TestFixture]
public class TabKeyTests
{
    [Test]
    public void IsTabKey_RecognizesGlKeysTab()
    {
        Assert.That(GuiBase.IsTabKey((int)GlKeys.Tab), Is.True);
    }

    [Test]
    public void IsTabKey_RejectsGlfwScancode()
    {
        // Regression: Vintage Story delivers GlKeys values, not GLFW scancodes (258).
        // The old hardcoded `== 258` never matched, so Tab never triggered traversal.
        Assert.That(GuiBase.IsTabKey(258), Is.False);
    }
}

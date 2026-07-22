using Gui.Debugging;

namespace Gui.Tests.Debugging;

[TestFixture]
public class DebugSettingsProfilerTests
{
    [Test]
    public void ShowProfiler_TogglesIsAnyEnabled()
    {
        var settings = new DebugSettings();
        Assert.That(settings.IsAnyEnabled, Is.False);

        settings.ShowProfiler = true;

        Assert.That(settings.ShowProfiler, Is.True);
        Assert.That(settings.IsAnyEnabled, Is.True);
    }

    [Test]
    public void DisableAll_ClearsShowProfiler()
    {
        var settings = new DebugSettings { ShowProfiler = true };

        settings.DisableAll();

        Assert.That(settings.ShowProfiler, Is.False);
    }
}

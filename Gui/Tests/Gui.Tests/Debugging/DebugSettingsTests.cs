using Gui.Debugging;

namespace Gui.Tests.Debugging;

public class DebugSettingsTests
{
    [Test]
    public void Should_Toggle_Debug_States()
    {
        var settings = new DebugSettings();

        Assert.That(settings.ShowTree, Is.False);
        settings.ShowTree = true;
        Assert.That(settings.ShowTree, Is.True);
    }

    [Test]
    public void Should_Reset_All_States()
    {
        var settings = new DebugSettings
        {
            ShowTree = true, ShowBounds = true, ShowPaint = true, ShowHud = true
        };

        Assert.That(settings.IsAnyEnabled, Is.True);

        settings.DisableAll();

        Assert.That(settings.ShowTree, Is.False);
        Assert.That(settings.ShowBounds, Is.False);
        Assert.That(settings.ShowPaint, Is.False);
        Assert.That(settings.ShowHud, Is.False);
        Assert.That(settings.IsAnyEnabled, Is.False);
    }
}

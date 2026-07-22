using System.Text.Json;
using Gui;
using Gui.Widgets.Framework;
using NUnit.Framework;

namespace Gui.Tests;

public class GuiConfigTests
{
    [Test]
    public void Onboarded_Defaults_ToFalse()
    {
        var config = JsonSerializer.Deserialize<GuiConfig>("{}")!;
        Assert.That(config.Onboarded, Is.False);
    }

    [Test]
    public void Onboarded_RoundTrips_ThroughJson()
    {
        var original = new GuiConfig
        {
            Theme = ThemeSection.FromColorScheme(ColorScheme.Default()),
            Onboarded = true
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GuiConfig>(json)!;

        Assert.That(restored.Onboarded, Is.True);
    }
}

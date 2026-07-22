using Gui;
using Gui.Theming;
using Gui.Widgets.Framework;

namespace Gui.Tests.Theming;

[TestFixture]
public class ThemePresetRegistryTests
{
    [Test]
    public void PresetIds_IncludesDefault_AndHas22Entries()
    {
        Assert.That(ThemePresetRegistry.PresetIds, Has.Count.EqualTo(22));
        Assert.That(ThemePresetRegistry.PresetIds, Does.Contain("default"));
    }

    [Test]
    public void LoadAll_ConvertsEachSection_AndSkipsMissing()
    {
        var fakeLoader = new Dictionary<string, ThemeSection>
        {
            ["default"] = ThemeSection.FromColorScheme(ColorScheme.Default()),
            ["void"] = ThemeSection.FromColorScheme(ColorScheme.Default())
        };

        var presets = ThemePresetRegistry.LoadAll(id =>
            fakeLoader.TryGetValue(id, out var section) ? section : null);

        Assert.That(presets, Has.Count.EqualTo(2));
        Assert.That(presets[0].Id, Is.EqualTo("default"));
        Assert.That(presets[0].DisplayName, Is.EqualTo("Default"));
        Assert.That(presets[1].Id, Is.EqualTo("void"));
    }
}

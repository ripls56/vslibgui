using Gui;
using Gui.Theming;
using Gui.Widgets.Framework;
using NUnit.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Theming;

[TestFixture]
public class ThemeApplierTests
{
    [Test]
    public void Apply_UpdatesDefaultNotifier()
    {
        var original = ThemeData.DefaultNotifier.Value;
        var preset = new ThemePreset("void", "Void", ColorScheme.Default());

        try
        {
            ThemeApplier.Apply(preset);
            Assert.That(ThemeData.DefaultNotifier.Value.ColorScheme, Is.EqualTo(preset.Scheme));
        }
        finally
        {
            ThemeData.DefaultNotifier.Value = original;
        }
    }

    [Test]
    public void BuildPersistedConfig_CarriesThemeAndOnboarded()
    {
        var preset = new ThemePreset("ember", "Ember", ColorScheme.Default());

        var config = ThemeApplier.BuildPersistedConfig(preset, onboarded: true);

        Assert.That(config.Onboarded, Is.True);
        Assert.That(config.Theme, Is.Not.Null);
    }

    [Test]
    public void Apply_WithOverrides_LayersOverridesOntoPreset()
    {
        var original = ThemeData.DefaultNotifier.Value;
        var preset = new ThemePreset("void", "Void", ColorScheme.Default());
        var magenta = new Vector4(1f, 0f, 1f, 1f);
        var overrides = new ThemeOverrides
        {
            Colors = new ColorOverrides { Primary = magenta }
        };

        try
        {
            ThemeApplier.Apply(preset, overrides);

            Assert.That(ThemeData.DefaultNotifier.Value.ColorScheme.Primary, Is.EqualTo(magenta));
            Assert.That(
                ThemeData.DefaultNotifier.Value.ColorScheme.Secondary,
                Is.EqualTo(preset.Scheme.Secondary));
        }
        finally
        {
            ThemeData.DefaultNotifier.Value = original;
        }
    }

    [Test]
    public void BuildPersistedConfig_IncludesOverridesSection()
    {
        var preset = new ThemePreset("void", "Void", ColorScheme.Default());
        var overrides = new ThemeOverrides
        {
            Focus = new FocusOverrides { OutlineWidth = 7f }
        };

        var config = ThemeApplier.BuildPersistedConfig(preset, overrides, true);

        Assert.That(config.Custom, Is.Not.Null);
        Assert.That(config.Custom!.Focus!.OutlineWidth, Is.EqualTo(7.0));
    }
}

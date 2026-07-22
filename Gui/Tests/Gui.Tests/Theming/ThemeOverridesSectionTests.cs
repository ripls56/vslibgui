using Gui.Theming;
using Gui.Widgets.Framework;
using NUnit.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Theming;

[TestFixture]
public class ThemeOverridesSectionTests
{
    [Test]
    public void RoundTrip_PreservesColorAndGeometryOverrides()
    {
        var overrides = new ThemeOverrides
        {
            Colors = new ColorOverrides { Primary = new Vector4(1f, 0f, 0f, 1f) },
            Button = new ButtonOverrides
            {
                Primary = new ButtonVariantOverrides { CornerRadius = 9f }
            },
            Focus = new FocusOverrides { OutlineWidth = 5f }
        };

        var section = ThemeOverridesSection.FromOverrides(overrides);
        var restored = section.ToOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(restored.Colors!.Primary, Is.EqualTo(overrides.Colors!.Primary));
            Assert.That(restored.Button!.Primary!.CornerRadius, Is.EqualTo(9f));
            Assert.That(restored.Focus!.OutlineWidth, Is.EqualTo(5f));
        });
    }

    [Test]
    public void EmptySection_ProducesEmptyOverrides()
    {
        var restored = new ThemeOverridesSection().ToOverrides();

        Assert.That(restored.IsEmpty, Is.True);
    }

    [Test]
    public void NullColorString_IsSkipped()
    {
        var section = new ThemeOverridesSection
        {
            Colors = new ColorOverrideSection { Primary = "#FF0000" }
        };

        var restored = section.ToOverrides();

        Assert.That(restored.Colors!.Primary, Is.EqualTo(new Vector4(1f, 0f, 0f, 1f)));
        Assert.That(restored.Colors!.Secondary, Is.Null);
    }
}

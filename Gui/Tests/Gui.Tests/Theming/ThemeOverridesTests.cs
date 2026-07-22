using Gui.Theming;
using Gui.Widgets.Framework;
using NUnit.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Theming;

[TestFixture]
public class ThemeOverridesTests
{
    [Test]
    public void ApplyOnto_WithEmptyOverrides_ReturnsEquivalentTheme()
    {
        var baseTheme = new ThemeData(ColorScheme.Default());

        var result = new ThemeOverrides().ApplyOnto(baseTheme);

        Assert.Multiple(() =>
        {
            Assert.That(result.ColorScheme, Is.EqualTo(baseTheme.ColorScheme));
            Assert.That(result.ButtonStyle, Is.EqualTo(baseTheme.ButtonStyle));
            Assert.That(result.FocusOutlineWidth, Is.EqualTo(baseTheme.FocusOutlineWidth));
        });
    }

    [Test]
    public void ApplyOnto_ReplacesOnlyOverriddenColorFields()
    {
        var baseTheme = new ThemeData(ColorScheme.Default());
        var magenta = new Vector4(1f, 0f, 1f, 1f);
        var overrides = new ThemeOverrides
        {
            Colors = new ColorOverrides { Primary = magenta }
        };

        var result = overrides.ApplyOnto(baseTheme);

        Assert.Multiple(() =>
        {
            Assert.That(result.ColorScheme.Primary, Is.EqualTo(magenta));
            Assert.That(result.ColorScheme.Secondary, Is.EqualTo(baseTheme.ColorScheme.Secondary));
        });
    }

    [Test]
    public void ApplyOnto_AppliesTypographySizeOverride()
    {
        var baseTheme = new ThemeData(ColorScheme.Default());
        var overrides = new ThemeOverrides
        {
            Typography = new TypographyOverrides { BodyFontSize = 42f }
        };

        var result = overrides.ApplyOnto(baseTheme);

        Assert.That(result.TextTheme.Body.FontSize, Is.EqualTo(42f));
        Assert.That(result.TextTheme.Body.Color, Is.EqualTo(baseTheme.TextTheme.Body.Color));
    }

    [Test]
    public void ApplyOnto_AppliesButtonCornerRadius()
    {
        var baseTheme = new ThemeData(ColorScheme.Default());
        var overrides = new ThemeOverrides
        {
            Button = new ButtonOverrides
            {
                Primary = new ButtonVariantOverrides { CornerRadius = 12f }
            }
        };

        var result = overrides.ApplyOnto(baseTheme);

        Assert.That(result.ButtonStyle.Primary.CornerRadius, Is.EqualTo(12f));
    }

    [Test]
    public void DeepCopy_IsIndependent()
    {
        var original = new ThemeOverrides
        {
            Colors = new ColorOverrides { Primary = new Vector4(1f, 0f, 0f, 1f) }
        };

        var copy = original.DeepCopy();
        copy.Colors!.Primary = new Vector4(0f, 1f, 0f, 1f);

        Assert.That(original.Colors!.Primary, Is.EqualTo(new Vector4(1f, 0f, 0f, 1f)));
    }

    [Test]
    public void IsEmpty_TrueWhenNoCategorySet()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new ThemeOverrides().IsEmpty, Is.True);
            Assert.That(new ThemeOverrides { Focus = new FocusOverrides() }.IsEmpty, Is.False);
        });
    }
}

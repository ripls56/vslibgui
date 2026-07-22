using Gui.Theming;

namespace Gui.Tests.Theming;

[TestFixture]
public class ThemePresetTests
{
    [TestCase("default", ExpectedResult = "Default")]
    [TestCase("neon_city", ExpectedResult = "Neon City")]
    [TestCase("blood_moon", ExpectedResult = "Blood Moon")]
    public string DeriveDisplayName_TitleCases_TheId(string id)
    {
        return ThemePreset.DeriveDisplayName(id);
    }
}

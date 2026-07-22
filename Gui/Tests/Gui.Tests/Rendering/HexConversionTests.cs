using Gui.Rendering;
using NUnit.Framework;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class HexConversionTests
{
    [TestCase("#FF0000", 1f, 0f, 0f, 1f)]
    [TestCase("#00FF0080", 0f, 1f, 0f, 0x80 / 255f)]
    [TestCase("#FFFFFF", 1f, 1f, 1f, 1f)]
    public void RoundTrip_PreservesColor(string hex, float r, float g, float b, float a)
    {
        var color = hex.FromHex();
        var roundTripped = color.ToHex().FromHex();

        Assert.That(roundTripped, Is.EqualTo(new Vector4(r, g, b, a)));
    }

    [Test]
    public void ToHex_OmitsAlphaWhenOpaque()
    {
        var opaque = new Vector4(1f, 0f, 0f, 1f);

        Assert.That(opaque.ToHex(), Is.EqualTo("#FF0000"));
    }
}

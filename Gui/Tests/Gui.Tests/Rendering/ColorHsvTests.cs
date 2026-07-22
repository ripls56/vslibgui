using Gui.Rendering;
using OpenTK.Mathematics;

namespace Gui.Tests.Rendering;

[TestFixture]
public class ColorHsvTests
{
    [Test]
    public void Red_IsPureHueAtZero()
    {
        var hsv = new Vector4(
            1f,
            0f,
            0f,
            1f
        ).ToHsv();

        Assert.Multiple(
            () =>
            {
                Assert.That(
                    hsv.H,
                    Is.EqualTo(0f).Within(0.5f)
                );
                Assert.That(
                    hsv.S,
                    Is.EqualTo(1f).Within(1e-4f)
                );
                Assert.That(
                    hsv.V,
                    Is.EqualTo(1f).Within(1e-4f)
                );
            }
        );
    }

    [Test]
    public void PureHues_MapToCanonicalAngles()
    {
        Assert.That(
            new Vector4(
                0f,
                1f,
                0f,
                1f
            ).ToHsv().H,
            Is.EqualTo(120f).Within(0.5f)
        );
        Assert.That(
            new Vector4(
                1f,
                1f,
                0f,
                1f
            ).ToHsv().H,
            Is.EqualTo(60f).Within(0.5f)
        );
        Assert.That(
            new Vector4(
                0f,
                0f,
                1f,
                1f
            ).ToHsv().H,
            Is.EqualTo(240f).Within(0.5f)
        );
        Assert.That(
            new Vector4(
                1f,
                0f,
                1f,
                1f
            ).ToHsv().H,
            Is.EqualTo(300f).Within(0.5f)
        );
    }

    [Test]
    public void White_IsZeroSaturationFullValue()
    {
        var hsv = new Vector4(
            1f,
            1f,
            1f,
            1f
        ).ToHsv();

        Assert.Multiple(
            () =>
            {
                Assert.That(
                    hsv.S,
                    Is.EqualTo(0f).Within(1e-4f)
                );
                Assert.That(
                    hsv.V,
                    Is.EqualTo(1f).Within(1e-4f)
                );
            }
        );
    }

    [Test]
    public void Black_IsZeroValue()
    {
        var hsv = new Vector4(
            0f,
            0f,
            0f,
            1f
        ).ToHsv();

        Assert.That(
            hsv.V,
            Is.EqualTo(0f).Within(1e-4f)
        );
    }

    [Test]
    public void RoundTrip_RecoversOriginalColor()
    {
        var original = new Vector4(
            0.2f,
            0.55f,
            0.82f,
            1f
        );
        var hsv = original.ToHsv();
        var recovered = ColorHsvExtensions.FromHsv(
            hsv.H,
            hsv.S,
            hsv.V
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(
                    recovered.X,
                    Is.EqualTo(original.X).Within(1e-3f)
                );
                Assert.That(
                    recovered.Y,
                    Is.EqualTo(original.Y).Within(1e-3f)
                );
                Assert.That(
                    recovered.Z,
                    Is.EqualTo(original.Z).Within(1e-3f)
                );
            }
        );
    }

    [Test]
    public void FromHsv_PreservesAlpha()
    {
        var withAlpha = ColorHsvExtensions.FromHsv(
            210f,
            0.5f,
            0.9f,
            0.25f
        );

        Assert.That(
            withAlpha.W,
            Is.EqualTo(0.25f).Within(1e-4f)
        );
    }

    [Test]
    public void FromHsv_WrapsHueOutOfRange()
    {
        var wrapped = ColorHsvExtensions.FromHsv(
            420f,
            1f,
            1f,
            1f
        );
        var canonical = ColorHsvExtensions.FromHsv(
            60f,
            1f,
            1f,
            1f
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(
                    wrapped.X,
                    Is.EqualTo(canonical.X).Within(1e-5f)
                );
                Assert.That(
                    wrapped.Y,
                    Is.EqualTo(canonical.Y).Within(1e-5f)
                );
                Assert.That(
                    wrapped.Z,
                    Is.EqualTo(canonical.Z).Within(1e-5f)
                );
                Assert.That(
                    wrapped.W,
                    Is.EqualTo(canonical.W).Within(1e-5f)
                );
            }
        );
    }
}

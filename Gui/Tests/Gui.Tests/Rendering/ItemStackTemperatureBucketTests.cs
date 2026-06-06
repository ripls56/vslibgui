using Gui.Rendering;

namespace Gui.Tests.Rendering;

[TestFixture]
public class ItemStackTemperatureBucketTests
{
    [Test]
    public void ColdTemperatures_ShareBucketZero()
    {
        Assert.That(ItemStackRenderer.TemperatureToBucket(0f), Is.EqualTo(0));
        Assert.That(ItemStackRenderer.TemperatureToBucket(20f), Is.EqualTo(0));
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(ItemStackRenderer.GlowStartTemperature - 1f),
            Is.EqualTo(0)
        );
    }

    [Test]
    public void NaNTemperature_ResolvesToColdBucket() =>
        Assert.That(ItemStackRenderer.TemperatureToBucket(float.NaN), Is.EqualTo(0));

    [Test]
    public void GlowStart_IsFirstGlowingBucket()
    {
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(ItemStackRenderer.GlowStartTemperature),
            Is.EqualTo(1)
        );
    }

    [Test]
    public void GlowFullAndAbove_ClampToMaxBucket()
    {
        var max = ItemStackRenderer.TemperatureBuckets;
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(ItemStackRenderer.GlowFullTemperature),
            Is.EqualTo(max)
        );
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(ItemStackRenderer.GlowFullTemperature + 5000f),
            Is.EqualTo(max)
        );
    }

    [Test]
    public void NearbyTemperatures_ShareOneBucket()
    {
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(800f),
            Is.EqualTo(ItemStackRenderer.TemperatureToBucket(805f))
        );
    }

    [Test]
    public void DistantTemperatures_FallInDifferentBuckets()
    {
        Assert.That(
            ItemStackRenderer.TemperatureToBucket(600f),
            Is.Not.EqualTo(ItemStackRenderer.TemperatureToBucket(1400f))
        );
    }

    [Test]
    public void Bucketing_IsMonotonicNonDecreasing()
    {
        var previous = ItemStackRenderer.TemperatureToBucket(-50f);
        for (var t = 0f; t <= 2000f; t += 7f)
        {
            var current = ItemStackRenderer.TemperatureToBucket(t);
            Assert.That(current, Is.GreaterThanOrEqualTo(previous), $"regressed at {t} C");
            previous = current;
        }
    }

    [Test]
    public void AllGlowingBuckets_StayWithinConfiguredRange()
    {
        for (var t = ItemStackRenderer.GlowStartTemperature;
             t <= ItemStackRenderer.GlowFullTemperature;
             t += 1f)
        {
            var bucket = ItemStackRenderer.TemperatureToBucket(t);
            Assert.That(bucket, Is.InRange(1, ItemStackRenderer.TemperatureBuckets));
        }
    }
}

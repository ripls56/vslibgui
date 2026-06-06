using System.Collections;
using System.Reflection;
using Gui.Rendering.Text;

namespace Gui.Tests.Perf;

[TestFixture]
public class PerformanceTestsFixed
{
    [Test]
    public void TextLayoutHelper_CacheGrowth_Test()
    {
        // Clear cache first
        TextLayoutHelper.ClearCache();

        // Simulate animation
        for (var i = 0; i < 1000; i++)
        {
            var size = 32f + i * 0.01f;
            TextLayoutHelper.GetFont("sans-serif", size, FontWeight.Normal);
        }

        // Use reflection to check cache size
        var cacheField = typeof(TextLayoutHelper).GetField("FontCache",
            BindingFlags.NonPublic | BindingFlags.Static);
        var cache = cacheField?.GetValue(null) as IDictionary;

        Assert.That(cache, Is.Not.Null);
        // With rounding to 0.5, 1000 steps of 0.01f (total 10.0f) should produce about 20 unique sizes (10 / 0.5).
        // Plus potentially one clear if it hit 500 (unlikely with this loop).
        // So we expect cache.Count to be relatively small (e.g. < 50).
        Assert.That(cache.Count, Is.LessThan(100));
    }
}

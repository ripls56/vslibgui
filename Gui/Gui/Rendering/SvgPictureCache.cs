using System.Collections.Generic;
using Svg.Skia;

namespace Gui.Rendering;

internal static class SvgPictureCache
{
    private static readonly object Lock = new();
    private static readonly Dictionary<string, SKSvg> Cache = new();

    internal static SKSvg? TryGet(
        string domain,
        string path
    )
    {
        var key = $"{domain}:{path}";
        lock (Lock)
        {
            return Cache.GetValueOrDefault(key);
        }
    }

    internal static void Store(
        string domain,
        string path,
        SKSvg data
    )
    {
        var key = $"{domain}:{path}";
        lock (Lock)
        {
            Cache.TryAdd(
                key,
                data
            );
        }
    }

    internal static void Clear()
    {
        lock (Lock)
        {
            foreach (var data in Cache.Values)
            {
                data.Picture?.Dispose();
            }

            Cache.Clear();
        }
    }
}

using System.Collections.Generic;
using Svg.Skia;

namespace Gui.Rendering;

internal static class SvgPictureCache
{
    private static readonly object Lock = new();
    private static readonly Dictionary<(string Domain, string Path), SKSvg> Cache = new();

    internal static SKSvg? TryGet
    (
        string domain,
        string path
    )
    {
        lock (Lock)
        {
            return Cache.GetValueOrDefault((domain, path));
        }
    }

    internal static void Store
    (
        string domain,
        string path,
        SKSvg data
    )
    {
        lock (Lock)
        {
            Cache.TryAdd
            (
                (domain, path),
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

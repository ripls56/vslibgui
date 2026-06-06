using System.IO;
using System.Text;
using SkiaSharp;
using Svg.Skia;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Rendering;

public class SkiaAssetLoader
{
    private readonly ICoreClientAPI _capi;

    public SkiaAssetLoader(
        ICoreClientAPI capi
    )
    {
        _capi = capi;
    }

    public string? LoadSksl(
        string domain,
        string path
    )
    {
        var asset = _capi.Assets.TryGet(
            new AssetLocation(
                domain,
                path
            )
        );
        if (asset == null)
        {
            _capi.Logger.Error($"Failed to load SKSL asset: {domain}:{path}");
            return null;
        }

        return Encoding.UTF8.GetString(asset.Data);
    }

    public SKBitmap? LoadBitmap(
        string domain,
        string path
    )
    {
        var asset = _capi.Assets.TryGet(
            new AssetLocation(
                domain,
                path
            )
        );
        if (asset == null)
        {
            return null;
        }

        using var stream = new MemoryStream(asset.Data);
        return SKBitmap.Decode(stream);
    }

    public SKTypeface? LoadFont(
        string domain,
        string path
    )
    {
        var asset = _capi.Assets.TryGet(
            new AssetLocation(
                domain,
                path.ToLower()
            )
        );
        if (asset == null)
        {
            _capi.Logger.Error($"Failed to load font asset: {domain}:{path}");
            return null;
        }

        using var stream = new MemoryStream(asset.Data);
        return SKTypeface.FromStream(stream);
    }

    public SKSvg? LoadSvg(
        string domain,
        string path
    )
    {
        var cached = SvgPictureCache.TryGet(
            domain,
            path
        );
        if (cached != null)
        {
            return cached;
        }

        var asset = _capi.Assets.TryGet(
            new AssetLocation(
                domain,
                path
            )
        );
        if (asset == null)
        {
            _capi.Logger.Error($"Failed to load SVG asset: {domain}:{path}");
            return null;
        }


        using var stream = new MemoryStream(asset.Data);
        var svg = new SKSvg();
        svg.Load(stream);
        if (svg.Picture == null)
        {
            _capi.Logger.Error($"Failed to parse SVG asset: {domain}:{path}");
            return null;
        }

        SvgPictureCache.Store(
            domain,
            path,
            svg
        );
        return svg;
    }
}

using System.Collections.Generic;
using Cairo;
using SkiaSharp;
using Vintagestory.API.Client;

namespace Gui.Rendering;

/// <summary>
///     Renders VS built-in icons via Cairo <see cref="IconUtil.DrawIconInt" />, uploads
///     each to an OpenGL texture, then wraps it in an <see cref="SKImage" /> for zero-copy
///     use in the Skia pipeline. Results are cached by icon name + pixel size.
/// </summary>
public class VsIconTextureCache
{
    private readonly Dictionary<(string, int), (LoadedTexture Texture, SKImage Image)> _cache
        = new();

    private readonly ICoreClientAPI _capi;

    /// <summary>Creates a new cache bound to <paramref name="capi" />.</summary>
    public VsIconTextureCache(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    /// <summary>
    ///     Returns a white-filled <see cref="SKImage" /> (GPU texture) for
    ///     <paramref name="iconName" /> at <paramref name="size" /> pixels.
    ///     Renders and caches on first call. Returns <c>null</c> if the icon is unknown.
    ///     Must be called from the GL thread.
    /// </summary>
    public SKImage? Get(string iconName, int size, GRContext grContext)
    {
        var key = (iconName, size);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached.Image;
        }

        var surface = new ImageSurface(Format.ARGB32, size, size);
        var ctx = new Context(surface);
        _capi.Gui.Icons.DrawIconInt(ctx, iconName, 0, 0, size, size, new double[] { 1, 1, 1, 1 });
        surface.Flush();
        ctx.Dispose();

        var textureId = _capi.Gui.LoadCairoTexture(surface, true);
        surface.Dispose();

        var loadedTexture = new LoadedTexture(_capi)
        {
            TextureId = textureId, Width = size, Height = size
        };

        var glInfo = new GRGlTextureInfo(3553u, (uint)textureId, 32856u);
        var backendTexture = new GRBackendTexture(size, size, false, glInfo);
        var image = SKImage.FromTexture(
            grContext,
            backendTexture,
            GRSurfaceOrigin.TopLeft,
            SKColorType.Rgba8888,
            SKAlphaType.Premul
        );

        if (image == null)
        {
            loadedTexture.Dispose();
            return null;
        }

        _cache[key] = (loadedTexture, image);
        return image;
    }

    /// <inheritdoc cref="System.IDisposable.Dispose" />
    public void Dispose()
    {
        foreach (var (texture, image) in _cache.Values)
        {
            image.Dispose();
            texture.Dispose();
        }

        _cache.Clear();
    }
}

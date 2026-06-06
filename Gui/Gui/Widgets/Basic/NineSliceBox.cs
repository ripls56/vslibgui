using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Rendering;
using Gui.Widgets.Framework;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that paints a bitmap using 9-slice (or 3-slice) scaling so
///     corners remain pixel-perfect while edges and the center stretch or tile.
/// </summary>
public class NineSliceBox : SingleChildWidget
{
    private readonly float? _height;
    private readonly ImageDrawMode _mode;
    private readonly float _scale;
    private readonly EdgeInsets _slice;
    private readonly SKBitmap _texture;
    private readonly Vector4 _tint;
    private readonly float? _width;

    public NineSliceBox(
        SKBitmap texture,
        EdgeInsets slice,
        float scale = 1f,
        ImageDrawMode mode = ImageDrawMode.Sliced,
        Vector4? tint = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        _texture = texture;
        _slice = slice;
        _scale = scale;
        _mode = mode;
        _tint = tint ?? Vector4.One;
        _width = width;
        _height = height;
    }

    public static NineSliceBox Sliced(
        SKBitmap texture,
        EdgeInsets slice,
        float scale = 1f,
        Vector4? tint = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    )
    {
        return new NineSliceBox(
            texture,
            slice,
            scale,
            ImageDrawMode.Sliced,
            tint,
            width,
            height,
            child,
            key
        );
    }

    public static NineSliceBox Sliced(
        SKBitmap texture,
        float slice,
        float scale = 1f,
        Vector4? tint = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    )
    {
        return Sliced(
            texture,
            EdgeInsets.All(slice),
            scale,
            tint,
            width,
            height,
            child,
            key
        );
    }

    public static NineSliceBox Tiled(
        SKBitmap texture,
        EdgeInsets slice,
        float scale = 1f,
        Vector4? tint = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    )
    {
        return new NineSliceBox(
            texture,
            slice,
            scale,
            ImageDrawMode.Tiled,
            tint,
            width,
            height,
            child,
            key
        );
    }

    public static NineSliceBox Tiled(
        SKBitmap texture,
        float slice,
        float scale = 1f,
        Vector4? tint = null,
        float? width = null,
        float? height = null,
        Widget? child = null,
        Framework.Key? key = null
    )
    {
        return Tiled(
            texture,
            EdgeInsets.All(slice),
            scale,
            tint,
            width,
            height,
            child,
            key
        );
    }

    public override RenderObject CreateRenderObject() => new RenderNineSlice();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderNineSlice)renderObject;
        ro.Bitmap = _texture;
        ro.Slice = _slice;
        ro.Scale = _scale;
        ro.DrawMode = _mode;
        ro.Tint = _tint;
        ro.MinWidth = _width;
        ro.MaxWidth = _width;
        ro.MinHeight = _height;
        ro.MaxHeight = _height;
        ro.HitTestBehavior = HitTestBehavior.Opaque;
    }

    public override void Dispose()
    {
        Child?.Dispose();
        base.Dispose();
    }
}

using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;
using SkiaSharp;

namespace Gui.Widgets.Painting;

/// <summary>
///     Paints its child normally, then overlays an <see cref="SKShader" />
///     across the child's bounds using the specified <see cref="SKBlendMode" />.
///     The shader is NOT owned by this widget — the caller manages its lifecycle.
/// </summary>
public class ShaderMask : SingleChildWidget
{
    /// <summary>Creates a new ShaderMask widget.</summary>
    public ShaderMask(
        SKShader? shader,
        SKBlendMode blendMode,
        Widget? child = null,
        float offsetX = 0f,
        float offsetY = 0f,
        Framework.Key? key = null
    ) : base(
        child,
        key
    )
    {
        Shader = shader;
        BlendMode = blendMode;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>The shader to overlay on the child.</summary>
    public SKShader? Shader { get; }

    /// <summary>Blend mode used when painting the shader overlay.</summary>
    public SKBlendMode BlendMode { get; }

    /// <summary>Pixel offset applied to the shader via local matrix.</summary>
    public float OffsetX { get; }

    /// <summary>Vertical pixel offset applied to the shader.</summary>
    public float OffsetY { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject() => new RenderShaderMask();

    /// <inheritdoc />
    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderShaderMask)renderObject;
        ro.Shader = Shader;
        ro.BlendMode = BlendMode;
        ro.OffsetX = OffsetX;
        ro.OffsetY = OffsetY;
    }
}

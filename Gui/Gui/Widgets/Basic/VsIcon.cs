using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     Renders a VS built-in icon by name using <see cref="RenderVsIcon" />.
///     The icon is rendered once via Cairo, uploaded to a GL texture, and reused
///     every frame without CPU copies.
/// </summary>
public class VsIcon : RenderObjectWidget
{
    /// <summary>Creates a VS icon widget.</summary>
    public VsIcon(
        string iconName,
        float size = 24f,
        Vector4? color = null,
        SKBlendMode blendMode = SKBlendMode.SrcIn,
        Framework.Key? key = null
    ) : base(key)
    {
        IconName = iconName;
        Size = size;
        Color = color ?? new Vector4(0f, 0f, 0f, 1f);
        BlendMode = blendMode;
    }

    /// <summary>VS icon name passed to <see cref="Vintagestory.API.Client.IconUtil.DrawIconInt" />.</summary>
    public string IconName { get; }

    /// <summary>Side length of the icon in pixels.</summary>
    public float Size { get; }

    /// <summary>Tint color applied via <see cref="SKBlendMode.SrcIn" />.</summary>
    public Vector4 Color { get; }

    /// <summary>Blend mode used for tinting.</summary>
    public SKBlendMode BlendMode { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject() => new RenderVsIcon();

    /// <inheritdoc />
    public override void UpdateRenderObject(RenderObject renderObject)
    {
        var ro = (RenderVsIcon)renderObject;
        ro.IconName = IconName;
        ro.IconSize = Size;
        ro.TintColor = Color;
        ro.BlendMode = BlendMode;
    }
}

using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

public class Icon : RenderObjectWidget
{
    public Icon(
        string domain,
        string path,
        float size = 24f,
        Vector4? color = null,
        SKBlendMode? blendMode = SKBlendMode.SrcIn,
        Framework.Key? key = null
    ) : base(key)
    {
        Domain = domain;
        Path = path;
        Size = size;
        BlendMode = blendMode ?? SKBlendMode.SrcIn;
        Color = color ?? new Vector4(
            0f,
            0f,
            0f,
            1f
        );
    }

    public string Domain { get; }
    public string Path { get; }
    public float Size { get; }
    public Vector4 Color { get; }
    public SKBlendMode BlendMode { get; }

    public override RenderObject CreateRenderObject() => new RenderIcon();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderIcon)renderObject;

        if (ro.SourceDomain != Domain || ro.SourcePath != Path || ro.SvgData == null)
        {
            ro.SvgData = GuiModSystem.Instance?.SkiaAssetLoader?.LoadSvg(
                Domain,
                Path
            );
            ro.SourceDomain = Domain;
            ro.SourcePath = Path;
        }

        ro.IconSize = Size;
        ro.TintColor = Color;
        ro.BlendMode = BlendMode;
    }
}

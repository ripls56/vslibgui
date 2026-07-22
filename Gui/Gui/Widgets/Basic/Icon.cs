using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Basic.Theming;
using Gui.Widgets.Framework;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

public class Icon : StatelessWidget
{
    public Icon
    (
        string domain,
        string path,
        float? size = null,
        Vector4? color = null,
        SKBlendMode? blendMode = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Domain = domain;
        Path = path;
        Size = size;
        Color = color;
        BlendMode = blendMode ?? SKBlendMode.SrcIn;
    }

    public string Domain { get; }
    public string Path { get; }
    public float? Size { get; }
    public Vector4? Color { get; }
    public SKBlendMode BlendMode { get; }

    public override Widget Build(BuildContext context)
    {
        var inherited = IconTheme.Of(context);
        var effective = new IconThemeData
        {
            Color = Color, Size = Size
        }.Merge(inherited);

        return new _RawIcon
        (
            Domain,
            Path,
            effective.Size ?? 24f,
            effective.Color ?? new Vector4(0, 0, 0, 1),
            BlendMode
        );
    }
}

internal class _RawIcon : RenderObjectWidget
{
    public _RawIcon
    (
        string domain,
        string path,
        float size,
        Vector4 color,
        SKBlendMode blendMode
    )
    {
        Domain = domain;
        Path = path;
        Size = size;
        Color = color;
        BlendMode = blendMode;
    }

    public string Domain { get; }
    public string Path { get; }
    public float Size { get; }
    public Vector4 Color { get; }
    public SKBlendMode BlendMode { get; }

    public override RenderObject CreateRenderObject() => new RenderIcon();

    public override void UpdateRenderObject(RenderObject renderObject)
    {
        var ro = (RenderIcon)renderObject;

        if (ro.SourceDomain != Domain || ro.SourcePath != Path || ro.SvgData == null)
        {
            ro.SvgData = GuiModSystem.Instance?.SkiaAssetLoader?.LoadSvg(Domain, Path);
            ro.SourceDomain = Domain;
            ro.SourcePath = Path;
        }

        ro.IconSize = Size;
        ro.TintColor = Color;
        ro.BlendMode = BlendMode;
    }
}

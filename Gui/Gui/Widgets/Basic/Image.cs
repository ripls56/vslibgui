using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that displays an image loaded from game assets.
/// </summary>
public class Image : RenderObjectWidget
{
    public Image(
        string domain,
        string path,
        BoxFit fit = BoxFit.Contain,
        Alignment? alignment = null,
        float? width = null,
        float? height = null,
        Framework.Key? key = null
    ) : base(key)
    {
        Domain = domain;
        Path = path;
        Fit = fit;
        Alignment = alignment ?? Alignment.Center;
        Width = width;
        Height = height;
    }

    public string Domain { get; }
    public string Path { get; }
    public BoxFit Fit { get; }
    public Alignment Alignment { get; }
    public float? Width { get; }
    public float? Height { get; }

    public override RenderObject CreateRenderObject() => new RenderImage();

    public override void UpdateRenderObject(
        RenderObject renderObject
    )
    {
        var ro = (RenderImage)renderObject;

        // Only reload bitmap if the source changed
        if (ro.SourceDomain != Domain || ro.SourcePath != Path)
        {
            ro.Bitmap = GuiModSystem.Instance?.SkiaAssetLoader?.LoadBitmap(
                Domain,
                Path
            );
            ro.SourceDomain = Domain;
            ro.SourcePath = Path;
        }

        ro.Fit = Fit;
        ro.Alignment = Alignment;
        ro.Width = Width;
        ro.Height = Height;
    }
}

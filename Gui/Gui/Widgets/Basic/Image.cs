using Gui.Core.Basic;
using Gui.Core.Framework;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Gui.Widgets.Painting;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Gui.Widgets.Basic;

/// <summary>
///     A widget that displays an image loaded from game assets.
/// </summary>
public class Image : RenderObjectWidget
{
    public Image
    (
        string domain,
        string path,
        BoxFit fit = BoxFit.Contain,
        Alignment? alignment = null,
        float? width = null,
        float? height = null,
        Vector4? tint = null,
        SKBlendMode blendMode = SKBlendMode.Modulate,
        Framework.Key? key = null
    ) : base(key)
    {
        Domain = domain;
        Path = path;
        Fit = fit;
        Alignment = alignment ?? Alignment.Center;
        Width = width;
        Height = height;
        Tint = tint;
        BlendMode = blendMode;
    }

    /// <summary>
    ///     Pre-tint signature, kept for source and binary compatibility with callers
    ///     that pass <paramref name="key" /> positionally as the 7th argument. Has no
    ///     default values so it only matches fully-positional 7-argument calls.
    /// </summary>
    public Image
    (
        string domain,
        string path,
        BoxFit fit,
        Alignment? alignment,
        float? width,
        float? height,
        Framework.Key? key
    ) : this
    (
        domain,
        path,
        fit,
        alignment,
        width,
        height,
        null,
        SKBlendMode.Modulate,
        key
    )
    {
    }

    public string Domain { get; }
    public string Path { get; }
    public BoxFit Fit { get; }
    public Alignment Alignment { get; }
    public float? Width { get; }
    public float? Height { get; }

    /// <summary>
    ///     Recolors the bitmap through <see cref="BlendMode" />. Null draws the
    ///     bitmap untouched. With the default <see cref="SKBlendMode.Modulate" />
    ///     every channel — alpha included — is multiplied, so a grayscale source
    ///     keeps its shading while taking this hue, and the tint's own alpha acts
    ///     as opacity.
    /// </summary>
    public Vector4? Tint { get; }

    public SKBlendMode BlendMode { get; }

    public override RenderObject CreateRenderObject() => new RenderImage();

    public override void UpdateRenderObject
    (
        RenderObject renderObject
    )
    {
        var ro = (RenderImage)renderObject;

        // Only reload bitmap if the source changed
        if (ro.SourceDomain != Domain || ro.SourcePath != Path)
        {
            ro.Bitmap = GuiModSystem.Instance?.SkiaAssetLoader?.LoadBitmap
            (
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
        ro.Tint = Tint;
        ro.BlendMode = BlendMode;
    }
}

using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Gui.Rendering;

/// <summary>
///     Runs once per frame before any <see cref="GuiBase" /> dialog:
///     processes pre-Skia GL renderers, then calls
///     <see cref="SkiaRenderer.Begin" /> to acquire the shared Skia canvas.
///     <see cref="PostSkiaPipeline" /> closes the frame with
///     <see cref="SkiaRenderer.End" />.
/// </summary>
public class PreSkiaPipeline : IRenderer
{
    private readonly ICoreClientAPI _capi;
    private readonly List<IPreSkiaRenderer> _renderers = new();
    private readonly SkiaRenderer _skiaRenderer;

    /// <inheritdoc cref="PreSkiaPipeline" />
    public PreSkiaPipeline(SkiaRenderer skiaRenderer, ICoreClientAPI capi)
    {
        _skiaRenderer = skiaRenderer;
        _capi = capi;
    }

    /// <summary>
    ///     The shader that was active before <see cref="SkiaRenderer.Begin" />
    ///     was called. <see cref="PostSkiaPipeline" /> restores it after
    ///     <see cref="SkiaRenderer.End" />.
    /// </summary>
    internal IShaderProgram? SavedShader { get; private set; }

    /// <summary>True if <see cref="SkiaRenderer.Begin" /> was called this frame.</summary>
    internal bool WasBegun { get; private set; }

    /// <inheritdoc />
    public double RenderOrder => -1000.0;

    /// <inheritdoc />
    public int RenderRange => 1;

    /// <inheritdoc />
    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        foreach (var r in _renderers)
        {
            r.ProcessQueue();
        }

        SavedShader = _capi.Render.CurrentActiveShader;
        WasBegun = true;
        _skiaRenderer.Begin(
            _capi.Render.FrameWidth,
            _capi.Render.FrameHeight,
            RuntimeEnv.GUIScale
        );
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>Adds a renderer to the pipeline.</summary>
    public void Register(IPreSkiaRenderer renderer) => _renderers.Add(renderer);

    /// <summary>Removes a renderer from the pipeline.</summary>
    public void Unregister(IPreSkiaRenderer renderer) => _renderers.Remove(renderer);
}

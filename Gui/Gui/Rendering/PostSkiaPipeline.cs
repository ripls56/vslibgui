using System;
using Vintagestory.API.Client;

namespace Gui.Rendering;

/// <summary>
///     Flushes the shared Skia surface and restores GL state exactly once per
///     frame, after all <see cref="GuiBase" /> dialogs have recorded their paint
///     commands into the shared canvas.
///     <para>
///         The flush writes every dialog's pixels straight into the game
///         framebuffer, so its <see cref="RenderOrder" /> decides where all
///         Skia content sits relative to vanilla GUIs. Vanilla
///         <c>GuiManager.OnRenderFrameGUI</c> renders dialogs in the
///         <see cref="EnumRenderStage.Ortho" /> stage at render order
///         <c>1.0</c>; the flush must run strictly after it, otherwise Skia
///         content composites behind every vanilla dialog regardless of an
///         individual dialog's <c>DrawOrder</c>. It stays below the aim
///         reticle (<c>1.02</c>) so the crosshair remains on top.
///     </para>
/// </summary>
public sealed class PostSkiaPipeline : IRenderer, IDisposable
{
    private readonly PreSkiaPipeline _pre;
    private readonly SkiaRenderer _skiaRenderer;

    /// <inheritdoc cref="PostSkiaPipeline" />
    public PostSkiaPipeline(SkiaRenderer skiaRenderer, PreSkiaPipeline pre)
    {
        _skiaRenderer = skiaRenderer;
        _pre = pre;
    }

    /// <inheritdoc />
    public double RenderOrder => 1;

    /// <inheritdoc />
    public int RenderRange => 1;

    /// <inheritdoc />
    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (!_pre.WasBegun)
        {
            return;
        }

        _skiaRenderer.End();
        _pre.SavedShader?.Use();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

using System;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace Gui.Rendering;

public class SkiaRenderer : IDisposable
{
    private const int TextureUnitsToReset = 8;
    private int _height;
    private GRBackendRenderTarget? _renderTarget;

    private GlStateSnapshot _savedState;
    private SKSurface? _surface;

    private int _width;

    public SKCanvas? Canvas { get; private set; }
    public GRContext? GrContext { get; private set; }

    public void Dispose()
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();
        GrContext?.Dispose();
    }

    public void Begin(
        int width,
        int height,
        float scale = 1.0f
    )
    {
        if (GrContext == null)
        {
            var glInterface = GRGlInterface.Create();
            GrContext = GRContext.CreateGl(glInterface);
        }

        _savedState = GlStateSnapshot.Capture();

        if (_width != width || _height != height || _surface == null)
        {
            _width = width;
            _height = height;

            _surface?.Dispose();
            _renderTarget?.Dispose();

            GL.GetInteger(
                GetPName.FramebufferBinding,
                out var framebuffer
            );
            GL.GetInteger(
                GetPName.Samples,
                out var samples
            );

            var stencil = 8;

            var glInfo = new GRGlFramebufferInfo(
                (uint)framebuffer,
                SKColorType.Rgba8888.ToGlSizedFormat()
            );
            _renderTarget = new GRBackendRenderTarget(
                width,
                height,
                samples,
                stencil,
                glInfo
            );
            _surface = SKSurface.Create(
                GrContext,
                _renderTarget,
                GRSurfaceOrigin.BottomLeft,
                SKColorType.Rgba8888
            );
            Canvas = _surface.Canvas;
        }

        GrContext.ResetContext();
        Canvas?.ResetMatrix();
        Canvas?.Scale(
            scale,
            scale
        );
    }

    public void End()
    {
        Canvas?.Flush();
        GrContext?.Flush();
        RestoreGlState();
    }

    private void RestoreGlState()
    {
        // Skia binds its own shaders, VAOs, textures, samplers, blend state, and
        // toggles sRGB/color-mask on flush. Unbind the objects it leaves dangling,
        // then restore the exact state the game had before Begin() so the rest of
        // the frame (3D item slots, their shadows) blends identically.
        GL.UseProgram(0);
        GL.BindVertexArray(0);

        for (var unit = 0; unit < TextureUnitsToReset; unit++)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(
                TextureTarget.Texture2D,
                0
            );
            GL.BindSampler(
                (uint)unit,
                0
            );
        }

        GL.ActiveTexture(TextureUnit.Texture0);

        _savedState.Restore();
    }
}

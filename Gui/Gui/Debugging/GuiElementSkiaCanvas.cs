using System;
using Gui.Rendering;
using Vintagestory.API.Client;

namespace Gui.Debugging;

public class GuiElementSkiaCanvas : GuiElement
{
    private readonly Action<PaintingContext> _onPaint;
    private PaintingContext? _paintingContext;

    public GuiElementSkiaCanvas(
        ICoreClientAPI capi,
        ElementBounds bounds,
        Action<PaintingContext> onPaint
    ) : base(
        capi,
        bounds
    )
    {
        _onPaint = onPaint;
    }

    public override void RenderInteractiveElements(
        float deltaTime
    )
    {
        base.RenderInteractiveElements(deltaTime);

        var skiaRenderer = GuiModSystem.Instance?.SkiaRenderer;
        if (skiaRenderer == null)
        {
            return;
        }

        // Skia usually renders to the whole screen or a specific framebuffer.
        // For legacy UI integration, we need to ensure it draws within our bounds.
        // SkiaRenderer.Begin() sets up the surface.

        // We might need a way to pass the current scissor or clip to the PaintingContext.
        // For now, let's assume we can draw globally and rely on Skia's clipping.

        // Note: Legacy UI rendering happens in a specific order. 
        // We use RenderInteractiveElements to ensure we draw on top or at least when expected.

        // However, SkiaRenderer.Begin requires width/height.
        // We'll use the window dimensions.
        skiaRenderer.Begin(
            api.Render.FrameWidth,
            api.Render.FrameHeight
        );

        if (_paintingContext == null || _paintingContext.Canvas != skiaRenderer.Canvas)
        {
            if (skiaRenderer.Canvas != null)
            {
                _paintingContext = new PaintingContext(
                    skiaRenderer.Canvas,
                    api.InWorldEllapsedMilliseconds
                );
            }
        }

        if (_paintingContext != null)
        {
            _paintingContext.SaveLayer();
            // Translate to our element's position
            _paintingContext.Canvas?.Translate(
                (float)Bounds.renderX,
                (float)Bounds.renderY
            );

            // Invoke the paint callback
            _onPaint(_paintingContext);

            _paintingContext.Restore();
        }

        skiaRenderer.End();
    }
}

public static class GuiComposerExtensions
{
    public static GuiComposer AddSkiaCanvas(
        this GuiComposer composer,
        ElementBounds bounds,
        Action<PaintingContext> onPaint,
        string key = null
    )
    {
        composer.AddInteractiveElement(
            new GuiElementSkiaCanvas(
                composer.Api,
                bounds,
                onPaint
            ),
            key
        );
        return composer;
    }
}

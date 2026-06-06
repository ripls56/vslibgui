using System;
using SkiaSharp;

namespace Gui.Rendering;

public static class CanvasExtensions
{
    public static CanvasScope SaveScope(this SKCanvas canvas) => new(canvas);

    public struct CanvasScope : IDisposable
    {
        private readonly SKCanvas _canvas;

        public CanvasScope(
            SKCanvas canvas
        )
        {
            _canvas = canvas;
            _canvas.Save();
        }

        public void Dispose() => _canvas.Restore();
    }
}

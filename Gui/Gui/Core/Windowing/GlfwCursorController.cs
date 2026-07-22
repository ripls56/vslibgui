using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gui.Core.Windowing;

/// <summary>
///     Manages the GLFW cursor lifetime for a single dialog window.
///     Owns the current <see cref="CursorShape" /> state and the
///     underlying native <c>Cursor*</c> resource so that GuiBase
///     holds no unsafe fields.
/// </summary>
internal sealed class GlfwCursorController : IDisposable
{
    private CursorShape _currentShape = CursorShape.Arrow;
    private unsafe Cursor* _customCursor;

    /// <summary>
    ///     Sets the OS cursor to the given <paramref name="shape" />.
    ///     No-ops when the shape is already active to avoid redundant GLFW calls.
    /// </summary>
    public unsafe void SetShape(
        CursorShape shape
    )
    {
        if (shape == _currentShape)
        {
            return;
        }

        _currentShape = shape;

        var window = GLFW.GetCurrentContext();
        if (window == null)
        {
            return;
        }

        if (_customCursor != null)
        {
            GLFW.DestroyCursor(_customCursor);
            _customCursor = null;
        }

        if (shape == CursorShape.Arrow)
        {
            GLFW.SetCursor(window, null);
        }
        else
        {
            _customCursor = GLFW.CreateStandardCursor(shape);
            GLFW.SetCursor(window, _customCursor);
        }
    }

    /// <summary>
    ///     Destroys any active custom cursor and resets the OS cursor to the
    ///     default arrow. Call from <c>OnGuiClosed</c>.
    /// </summary>
    public unsafe void Reset()
    {
        var window = GLFW.GetCurrentContext();
        if (_customCursor != null)
        {
            GLFW.DestroyCursor(_customCursor);
            _customCursor = null;
        }

        if (window != null)
        {
            GLFW.SetCursor(window, null);
        }

        _currentShape = CursorShape.Arrow;
    }

    /// <summary>Releases the native cursor resource.</summary>
    public void Dispose() => Reset();
}

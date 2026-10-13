using System;
using OpenTK.Mathematics;
using Vintagestory.API.Config;

namespace Gui.Core.Input;

/// <summary>
///     Reads the cursor position in logical (UI-scaled) screen units without waiting for a
///     pointer event.
///     <para>
///         The Vintage Story engine hands a mouse move to one dialog only: it walks
///         <c>LoadedGuis</c> in order and stops at the first dialog that marks the event
///         handled. A window in front therefore starves every window behind it, and their
///         latched hover state (item-slot highlight, tooltip) never receives the exit event
///         that would clear it. Widgets that must not stay hovered read this instead of
///         relying on <c>onExit</c> alone.
///     </para>
///     <para>
///         The coordinate space matches <c>RenderObject.LocalToGlobal(p) + GetScreenOffset()</c>,
///         i.e. the same space <see cref="GuiBase" /> uses for <c>WindowPos</c>.
///     </para>
/// </summary>
public static class GlobalPointer
{
    /// <summary>
    ///     Test seam. When set, <see cref="LogicalPosition" /> returns this value instead of
    ///     reading the game input. Assign null to restore the game source.
    /// </summary>
    internal static Func<Vector2?>? TestOverride;

    /// <summary>
    ///     Current cursor position in logical screen units, or null when the game input is
    ///     unavailable (unit tests, headless mounts). Callers treat null as "unknown" and
    ///     keep their event-driven behaviour.
    /// </summary>
    public static Vector2? LogicalPosition
    {
        get
        {
            if (TestOverride != null)
            {
                return TestOverride();
            }

            var input = GuiModSystem.Instance?.Capi?.Input;
            if (input == null)
            {
                return null;
            }

            var scale = RuntimeEnv.GUIScale;
            if (scale <= 0f)
            {
                return null;
            }

            return new Vector2(input.MouseX / scale, input.MouseY / scale);
        }
    }

    /// <summary>
    ///     True when <paramref name="point" /> lies inside the rect that starts at
    ///     <paramref name="topLeft" /> and spans <paramref name="size" />. An empty rect
    ///     contains nothing.
    /// </summary>
    public static bool InsideRect(Vector2 point, Vector2 topLeft, Vector2 size) =>
        size.X > 0f && size.Y > 0f &&
        point.X >= topLeft.X && point.X <= topLeft.X + size.X &&
        point.Y >= topLeft.Y && point.Y <= topLeft.Y + size.Y;
}

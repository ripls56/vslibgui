using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Gui.Rendering;

/// <summary>
///     Projects a world-space position to window-space coordinates using the
///     game's perspective matrices. Used by floaty dialogs and HUD overlays
///     that anchor to in-world targets.
/// </summary>
public sealed class WorldAnchor
{
    /// <summary>World-space anchor point. Mutable; owners may update each frame.</summary>
    public Vec3d WorldPos { get; set; } = new();

    /// <summary>
    ///     Vertical alignment of the dialog relative to the projected point.
    ///     0 = top-aligned, 0.5 = centered, 1 = bottom-aligned. Matches VS semantics.
    /// </summary>
    public double Align { get; set; } = 0.5;

    /// <summary>
    ///     Projects <see cref="WorldPos" /> into window-space. Returns false when the
    ///     target is behind the camera (caller should keep the previous position).
    /// </summary>
    /// <param name="projection">Perspective projection matrix (16-element row-major).</param>
    /// <param name="view">Camera view matrix (16-element row-major).</param>
    /// <param name="frameWidth">Render frame width in raw pixels.</param>
    /// <param name="frameHeight">Render frame height in raw pixels.</param>
    /// <param name="guiScale">UI scale (e.g. <c>RuntimeEnv.GUIScale</c>).</param>
    /// <param name="windowSize">The dialog's window size in logical pixels.</param>
    /// <param name="windowPos">Computed top-left window position in logical pixels.</param>
    /// <returns>True if the projection succeeded; false if behind camera.</returns>
    public bool TryProject(
        double[] projection,
        double[] view,
        int frameWidth,
        int frameHeight,
        float guiScale,
        Vector2 windowSize,
        out Vector2 windowPos)
    {
        var screen = MatrixToolsd.Project(
            WorldPos,
            projection,
            view,
            frameWidth,
            frameHeight);

        if (screen.Z < 0)
        {
            windowPos = default;
            return false;
        }

        windowPos = new Vector2(
            (float)(screen.X / guiScale) - windowSize.X / 2f,
            (float)((frameHeight - screen.Y) / guiScale) - windowSize.Y * (float)Align);
        return true;
    }
}

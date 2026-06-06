using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Gui.Shared;

/// <summary>
///     Window-positioning helper used by player-inventory dialogs to honor the
///     <c>immersiveMouseMode</c> setting: anchored to the right edge of the
///     viewport in immersive mode, centered (null) otherwise.
/// </summary>
public static class ImmersiveWindowPosition
{
    private const float ImmersiveRightMargin = 24f;

    /// <summary>
    ///     Returns the right-anchored window position for a window of the given
    ///     logical size when immersive mouse mode is enabled, or <c>null</c> for
    ///     default centered placement.
    /// </summary>
    public static Vector2? RightAnchoredOrCentered(ICoreClientAPI capi, float width, float height)
    {
        if (!capi.Settings.Bool["immersiveMouseMode"])
        {
            return null;
        }

        var scale = RuntimeEnv.GUIScale;
        return new Vector2(
            capi.Render.FrameWidth / scale - width - ImmersiveRightMargin,
            (capi.Render.FrameHeight / scale - height) / 2f);
    }
}

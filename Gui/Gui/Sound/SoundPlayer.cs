using Gui.Widgets.Framework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Gui.Sound;

/// <summary>
///     Default <see cref="ISoundPlayer" /> implementation backed by the Vintage Story
///     client audio system. Created once per <see cref="GuiBase" /> and registered in
///     <see cref="BuildOwner" />.
/// </summary>
public class SoundPlayer : ISoundPlayer
{
    private readonly ICoreClientAPI _capi;

    public SoundPlayer(
        ICoreClientAPI capi
    )
    {
        _capi = capi;
    }

    public void Play(
        string name,
        Pitch pitch = default,
        float volume = 0.5f
    )
    {
        var loc = new AssetLocation(
            "gui",
            $"sounds/{name}"
        );
        var param = new SoundParams(loc)
        {
            SoundType = EnumSoundType.Sound,
            Pitch = ResolvePitch(pitch),
            Volume = volume,
            DisposeOnFinish = true
        };
        _capi.World.LoadSound(param)?.Start();
    }

    public SoundHandle Load(
        string name,
        bool loop = false,
        Pitch pitch = default,
        float volume = 0.5f
    )
    {
        var loc = new AssetLocation(
            "gui",
            $"sounds/{name}"
        );
        var param = new SoundParams(loc)
        {
            SoundType = EnumSoundType.Sound,
            Pitch = ResolvePitch(pitch),
            Volume = volume,
            DisposeOnFinish = false,
            ShouldLoop = loop
        };
        var sound = _capi.World.LoadSound(param);
        return new SoundHandle(sound);
    }

    private static float ResolvePitch(
        Pitch pitch
    )
    {
        // default(Pitch) has Base=0; treat as 1.0 (normal pitch).
        if (pitch.Base == 0f && pitch.Variance == 0f)
        {
            return 1f;
        }

        return pitch.Resolve();
    }
}

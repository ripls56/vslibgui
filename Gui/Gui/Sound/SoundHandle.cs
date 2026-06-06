using System;
using Vintagestory.API.Client;

namespace Gui.Sound;

/// <summary>
///     Wraps <see cref="ILoadedSound" /> to provide a controlled sound playback handle.
///     Create via <see cref="ISoundPlayer.Load" />. Dispose when no longer needed.
/// </summary>
public class SoundHandle : IDisposable
{
    private ILoadedSound? _sound;

    internal SoundHandle(
        ILoadedSound sound
    )
    {
        _sound = sound;
    }

    /// <summary>Whether the sound is currently playing.</summary>
    public bool IsPlaying => _sound?.IsPlaying ?? false;

    public void Dispose()
    {
        _sound?.Dispose();
        _sound = null;
    }

    /// <summary>Starts or restarts playback from the beginning.</summary>
    public void Start() => _sound?.Start();

    /// <summary>Stops playback. Can be resumed with <see cref="Start" />.</summary>
    public void Stop() => _sound?.Stop();

    /// <summary>Sets the playback pitch multiplier (1.0 = normal).</summary>
    public void SetPitch(
        float pitch
    ) =>
        _sound?.SetPitch(pitch);

    /// <summary>Sets the playback volume (0.0–1.0).</summary>
    public void SetVolume(
        float volume
    ) =>
        _sound?.SetVolume(volume);
}

namespace Gui.Sound;

/// <summary>
///     Provides sound playback for UI widgets. Accessible via
///     <c>BuildContext.GetSoundPlayer()</c> inside widget build methods and state callbacks.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    ///     Plays a one-shot sound that auto-disposes when finished.
    /// </summary>
    /// <param name="name">Sound name relative to the mod's sounds folder (e.g. "click").</param>
    /// <param name="pitch">Playback pitch — fixed float or <see cref="Pitch.Varied" />.</param>
    /// <param name="volume">Playback volume (0.0–1.0).</param>
    void Play(
        string name,
        Pitch pitch = default,
        float volume = 0.5f
    );

    /// <summary>
    ///     Loads a sound without playing it, returning a handle for full control
    ///     (start, stop, resume, pitch, volume, dispose).
    /// </summary>
    /// <param name="name">Sound name relative to the mod's sounds folder.</param>
    /// <param name="loop">Whether the sound should loop.</param>
    /// <param name="pitch">Initial pitch — fixed float or <see cref="Pitch.Varied" />.</param>
    /// <param name="volume">Initial volume (0.0–1.0).</param>
    SoundHandle Load(
        string name,
        bool loop = false,
        Pitch pitch = default,
        float volume = 0.5f
    );
}

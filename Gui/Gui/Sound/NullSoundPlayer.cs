namespace Gui.Sound;

/// <summary>
///     Silent <see cref="ISoundPlayer" /> used when no real player is registered
///     (headless tests, or a widget tree not owned by a <c>GuiBase</c>).
/// </summary>
internal sealed class NullSoundPlayer : ISoundPlayer
{
    internal static readonly NullSoundPlayer Instance = new();

    private NullSoundPlayer()
    {
    }

    public void Play
    (
        string name,
        Pitch pitch = default,
        float volume = 0.5f
    )
    {
    }

    public SoundHandle Load
    (
        string name,
        bool loop = false,
        Pitch pitch = default,
        float volume = 0.5f
    ) =>
        new(null);
}

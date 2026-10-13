using Gui.Widgets.Framework;

namespace Gui.Sound;

/// <summary>
///     Shared UI click-sound presets for widgets. Call from any <see cref="State" />
///     (e.g. inside a <c>GestureDetector.onPress</c> callback):
///     <code>
/// onPress: _ => SetState(() => { _isPressed = true; this.Click(); })
/// </code>
///     All presets share the single <c>click</c> asset and differ only in pitch and
///     volume, so an action's character is audible without extra assets.
/// </summary>
public static class UiSounds
{
    private const string Click_ = "click";

    /// <summary>Generic press — buttons and menu items.</summary>
    public static void Click
    (
        this State state
    ) =>
        Play
        (
            state,
            Pitch.Varied(.95f, .05f),
            .2f
        );

    /// <summary>A toggle turning on / a selection being made — rising tone.</summary>
    public static void ToggleOn
    (
        this State state
    ) =>
        Play(state, 1.05f, .2f);

    /// <summary>A toggle turning off — falling tone.</summary>
    public static void ToggleOff
    (
        this State state
    ) =>
        Play(state, .9f, .2f);

    /// <summary>A panel, menu or dialog opening — low tone.</summary>
    public static void Open
    (
        this State state
    ) =>
        Play(state, .9f, .2f);

    /// <summary>A panel, menu or dialog closing — high tone.</summary>
    public static void Close
    (
        this State state
    ) =>
        Play(state, 1.1f, .2f);

    /// <summary>A discrete increment / decrement, e.g. a spinner step.</summary>
    public static void Step
    (
        this State state
    ) =>
        Play
        (
            state,
            Pitch.Varied(1.15f, .05f),
            .15f
        );

    /// <summary>A light touch on a continuous control — slider or colour surface.</summary>
    public static void Tick
    (
        this State state
    ) =>
        Play
        (
            state,
            Pitch.Varied(1.2f, .07f),
            .12f
        );

    private static void Play
    (
        State state,
        Pitch pitch,
        float volume
    ) =>
        state.Element.Owner?.GetSoundPlayer()
            .Play
            (
                Click_,
                pitch,
                volume
            );
}

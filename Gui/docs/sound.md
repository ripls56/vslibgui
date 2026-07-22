# Sound

The sound system provides UI widgets with access to Vintage Story's audio engine
through `BuildContext`. It supports two usage levels: fire-and-forget one-shot sounds
and fully controlled sound handles for looping, pitch/volume changes, and manual lifecycle.

## Access

`ISoundPlayer` is registered in `BuildOwner` by `GuiBase` and available via:

```csharp
var sound = ctx.GetSoundPlayer();
```


## One-shot playback

For UI feedback sounds (clicks, hovers, notifications) that play once and auto-dispose:

```csharp
sound.Play("click");
sound.Play("click", pitch: 1.2f, volume: 0.3f);
```

## Pitch randomization

Use `Pitch.Varied` to add per-play randomness — prevents repetitive-sounding UI:

```csharp
// Randomizes pitch in [0.85, 1.15] on each call
sound.Play("click", pitch: Pitch.Varied(1f, 0.15f));

// Subtle variation around a lower pitch
sound.Play("click", pitch: Pitch.Varied(0.9f, 0.05f));
```

`Pitch` implicitly converts from `float`, so fixed values work unchanged:

```csharp
sound.Play("click", pitch: 1.0f);  // equivalent to Pitch with no variance
```

## Full control (SoundHandle)

For looping sounds, background music, or sounds that need runtime adjustment:

```csharp
class MyWidgetState : State<MyWidget>
{
    private SoundHandle? _ambient;

    public override void InitState()
    {
        _ambient = ctx.GetSoundPlayer().Load("ambient", loop: true, volume: 0.3f);
        _ambient.Start();
    }

    // Adjust at runtime
    void OnVolumeChanged(float v) => _ambient?.SetVolume(v);
    void OnPitchChanged(float p) => _ambient?.SetPitch(p);

    // Pause / resume
    void ToggleSound()
    {
        if (_ambient?.IsPlaying == true)
            _ambient.Stop();
        else
            _ambient?.Start();
    }

    public override void Dispose()
    {
        _ambient?.Dispose();
        base.Dispose();
    }
}
```

### SoundHandle API

| Method / Property | Description |
|-------------------|-------------|
| `Start()` | Starts or restarts playback |
| `Stop()` | Pauses playback |
| `SetPitch(float)` | Changes pitch multiplier (1.0 = normal) |
| `SetVolume(float)` | Changes volume (0.0–1.0) |
| `IsPlaying` | Whether the sound is currently playing |
| `Dispose()` | Releases the sound resource |

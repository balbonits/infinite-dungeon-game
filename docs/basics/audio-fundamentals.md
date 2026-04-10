# Audio Fundamentals

## Why This Matters
Our game has zero audio. No hit sounds, no music, no ambient dungeon noise. Audio is 50% of game feel — a silent game feels broken even if everything else works perfectly.

## Core Concepts

### Three Audio Categories

| Category | Purpose | Volume Level | Format |
|----------|---------|-------------|--------|
| **Music** | Mood, atmosphere, emotional pacing | 40-60% of master | OGG Vorbis (streaming) |
| **SFX** | Feedback for actions (hit, click, pickup) | 70-100% of master | WAV (short, low-latency) |
| **Ambient** | Environmental presence (wind, dripping, fire) | 20-40% of master | OGG Vorbis (looping) |

Music is QUIETER than SFX. This is counterintuitive but correct — the player needs to hear gameplay sounds over music.

### Godot Audio Bus System
Godot routes audio through buses. Create separate buses for volume control:

```
Master (controls everything)
├── Music (background music)
├── SFX (sound effects)
└── Ambient (environmental sounds)
```

```csharp
// Set up in _Ready or autoload
AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), -10);  // quieter
AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), 0);      // full
AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Ambient"), -15); // subtle
```

### When to Play Sounds
Every player action and game event should have audio:

| Event | Sound Type | Priority |
|-------|-----------|----------|
| Attack swing | SFX, short whoosh | Critical |
| Hit confirmed | SFX, impact thud | Critical |
| Enemy death | SFX, crunch/splat | Critical |
| Player damage | SFX, pain grunt | Critical |
| Level up | SFX, triumphant chime | High |
| Item pickup | SFX, coin/click | High |
| UI button click | SFX, soft click | Medium |
| Menu open/close | SFX, whoosh/thud | Medium |
| Floor transition | SFX, staircase echo | Medium |
| Ambient dungeon | Ambient, drip/wind loop | Low |
| Boss encounter | Music change, dramatic | Low |

### File Formats
- **WAV**: Uncompressed, instant playback, larger files. Use for SFX (< 2 seconds).
- **OGG Vorbis**: Compressed, streaming, smaller files. Use for music and ambient (> 2 seconds).
- **MP3**: Supported in Godot 4 but OGG is preferred (better looping, no licensing issues).

### Spatial Audio (2D)
In Godot 4, `AudioStreamPlayer2D` makes sounds louder when the listener (camera) is closer:

```csharp
var sfx = new AudioStreamPlayer2D();
sfx.Stream = ResourceLoader.Load<AudioStream>("res://assets/audio/hit.wav");
sfx.Bus = "SFX";
sfx.MaxDistance = 500;  // Sound fades over 500px
sfx.Position = hitPosition;
AddChild(sfx);
sfx.Play();
sfx.Finished += () => sfx.QueueFree();  // Clean up after playing
```

## Godot 4 + C# Implementation

```csharp
// Simple SFX player utility
public static class AudioUtil
{
    public static void PlaySFX(Node parent, string path, Vector2? position = null)
    {
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) return;
        
        if (position.HasValue)
        {
            var player = new AudioStreamPlayer2D();
            player.Stream = stream;
            player.Bus = "SFX";
            player.Position = position.Value;
            parent.AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }
        else
        {
            var player = new AudioStreamPlayer();
            player.Stream = stream;
            player.Bus = "SFX";
            parent.AddChild(player);
            player.Play();
            player.Finished += () => player.QueueFree();
        }
    }
}

// Usage
AudioUtil.PlaySFX(this, "res://assets/audio/sfx/hit_01.wav", enemy.Position);
```

## Common Mistakes
1. **No audio at all** — silent game feels broken
2. **Music louder than SFX** — player can't hear gameplay feedback
3. **No audio bus separation** — can't control music/SFX volume independently
4. **Playing sounds without QueueFree** — AudioStreamPlayer nodes accumulate (memory leak)
5. **WAV for music** — huge file sizes, use OGG instead
6. **OGG for short SFX** — slight decode delay, use WAV for instant playback
7. **No mute option in settings** — players will mute their system instead
8. **Identical sound every time** — use 2-3 variants per action, randomly selected

## Checklist
- [ ] Audio bus layout: Master → Music, SFX, Ambient
- [ ] Every combat action has a sound (attack, hit, death at minimum)
- [ ] Music volume < SFX volume
- [ ] SFX uses WAV, music uses OGG
- [ ] AudioStreamPlayers are QueueFree'd after playing
- [ ] Settings panel has volume sliders per bus
- [ ] Mute option exists

## Sources
- [Godot Audio docs](https://docs.godotengine.org/en/stable/tutorials/audio/index.html)
- [Godot AudioBus](https://docs.godotengine.org/en/stable/tutorials/audio/audio_buses.html)
- [GDC: Practical Game Audio Tips](https://www.youtube.com/watch?v=MrME8w6gFXI)
- [Free game audio: Freesound.org](https://freesound.org)
- [Free game music: OpenGameArt.org](https://opengameart.org/art-search-advanced?keys=&field_art_type_tid%5B%5D=12)

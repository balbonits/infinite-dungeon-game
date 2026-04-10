# Godot Animation Systems

## Why This Matters
Our characters use basic `Sprite2D.Frame` stepping. Godot has dedicated animation systems (AnimationPlayer, AnimatedSprite2D, SpriteFrames) that handle timing, transitions, and callbacks properly. Using the right tool prevents animation bugs.

## Core Concepts

### Three Animation Approaches

| Approach | Best For | Complexity |
|----------|---------|------------|
| **Manual frame stepping** | Simple sprite sheets, our current approach | Low |
| **AnimatedSprite2D + SpriteFrames** | Sprite sheet animations with named clips | Medium |
| **AnimationPlayer** | Complex multi-property animations (position, scale, modulate, sound) | High |

### Manual Frame Stepping (Current)
```csharp
sprite.Frame = direction * Hframes + animFrame;
// Step animFrame in _Process with a timer
```
Works but you handle all timing, looping, and state management yourself.

### AnimatedSprite2D
Uses a `SpriteFrames` resource to define named animations:

```csharp
var animSprite = new AnimatedSprite2D();
var frames = new SpriteFrames();

// Add animation
frames.AddAnimation("walk_south");
frames.SetAnimationSpeed("walk_south", 8.0f);  // 8 FPS
frames.SetAnimationLoop("walk_south", true);

// Add frames to animation
for (int i = 1; i <= 3; i++)  // Walk frames 1-3
{
    var tex = new AtlasTexture();
    tex.Atlas = spriteSheet;
    tex.Region = new Rect2(i * frameW, 0 * frameH, frameW, frameH);
    frames.AddFrame("walk_south", tex);
}

animSprite.SpriteFrames = frames;
animSprite.Play("walk_south");
```

**Signals:**
- `AnimationFinished` — fires when a non-looping animation ends (use for attack recovery)
- `FrameChanged` — fires every frame change (use for attack hit frame)

### AnimationPlayer
The most powerful system — animates ANY property on ANY node:

```csharp
var player = new AnimationPlayer();
var anim = new Animation();

// Animate sprite modulate (flash white on hit)
int track = anim.AddTrack(Animation.TrackType.Value);
anim.TrackSetPath(track, "Sprite:modulate");
anim.TrackInsertKey(track, 0.0f, Colors.White);
anim.TrackInsertKey(track, 0.1f, Colors.White);
anim.TrackInsertKey(track, 0.2f, new Color(1, 1, 1, 1));
anim.Length = 0.2f;

var library = new AnimationLibrary();
library.AddAnimation("hit_flash", anim);
player.AddAnimationLibrary("", library);
player.Play("hit_flash");
```

### Tween vs AnimationPlayer
- **Tween**: Quick one-shot effects (damage number float, screen shake, fade). Created and forgotten.
- **AnimationPlayer**: Reusable, complex, multi-track animations. Defined once, played many times.

**Rule:** If you'll play it once and throw it away, use a Tween. If you'll play it repeatedly, use AnimationPlayer.

### Animation State Machine Integration
Gameplay state determines which animation plays:

```csharp
switch (_state)
{
    case PlayerState.Idle:
        animSprite.Play("idle");
        break;
    case PlayerState.Walk:
        animSprite.Play($"walk_{directionName}");
        break;
    case PlayerState.Attack:
        animSprite.Play($"attack_{directionName}");
        break;
}
```

The animation should START when the state ENTERS, not every frame. Track the previous state:

```csharp
if (_state != _previousState)
{
    PlayAnimationForState(_state);
    _previousState = _state;
}
```

## Common Mistakes
1. **Playing animation every frame** — call Play() on state ENTER, not in _Process
2. **Forgetting AnimationFinished signal** — attack animation loops instead of returning to idle
3. **Wrong FPS** — too slow (choppy) or too fast (blurred). 6-10 FPS for pixel art.
4. **AtlasTexture regions wrong** — animation shows garbled frames (check region Rect2)
5. **Tween for reusable animations** — creates a new tween every time; use AnimationPlayer
6. **Not syncing direction** — animation plays "walk_south" but character faces north

## Checklist
- [ ] Each gameplay state has a matching animation
- [ ] Animation starts on state enter, not every frame
- [ ] Non-looping animations (attack) trigger callback on finish
- [ ] Animation FPS is 6-10 for pixel art
- [ ] Tween for one-shot effects, AnimationPlayer for reusable clips

## Sources
- [Godot AnimatedSprite2D](https://docs.godotengine.org/en/stable/classes/class_animatedsprite2d.html)
- [Godot AnimationPlayer](https://docs.godotengine.org/en/stable/classes/class_animationplayer.html)
- [Godot SpriteFrames](https://docs.godotengine.org/en/stable/classes/class_spriteframes.html)
- [Godot Tween](https://docs.godotengine.org/en/stable/classes/class_tween.html)

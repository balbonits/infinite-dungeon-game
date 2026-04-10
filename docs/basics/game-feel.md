# Game Feel

## Why This Matters
Our combat has no visual or audio feedback — you press attack, damage is dealt, but nothing on screen communicates impact. The game feels dead because every player action lacks the "juice" that makes games satisfying. Game feel is not polish — it's a core design discipline that determines whether a game feels good or feels like a spreadsheet simulator.

## Core Concepts

### The 3 Cs: Character, Camera, Controls
Every game feel problem traces to one of these three systems:

1. **Character** — Does the avatar respond instantly to input? Does it have weight and momentum? Does it telegraph its actions?
2. **Camera** — Does it follow smoothly? Does it react to action (shake, zoom)? Is the frame right?
3. **Controls** — Is input latency < 1 frame? Are buttons responsive? Is the mapping intuitive?

If ANY of the 3 Cs feels off, the entire game feels wrong — even if the other two are perfect.

### Juice Techniques

Every player action should produce visible, audible, and physical feedback:

| Technique | When | Implementation |
|-----------|------|---------------|
| **Screen shake** | Player hit, boss slam, explosion | Camera.Offset += random (3-5px, 50-150ms decay) |
| **Hit pause/freeze** | Landing an attack | `Engine.TimeScale = 0.05f` for 50-80ms, then restore |
| **Flash white** | Taking or dealing damage | `sprite.Modulate = Colors.White` for 100ms |
| **Damage numbers** | Every hit | Floating label, tween upward + fade (0.8s) |
| **Particles** | Impact, death, level up | Small burst at contact point |
| **Sound** | Every action | Unique sound per action type |
| **Knockback** | Melee hit | Push target 10-20px opposite to attack direction |

### Animation Timing: Anticipation → Action → Recovery
Every action has three phases:

- **Anticipation** (wind-up): Character pulls back before attacking. This is the MOST IMPORTANT phase — it tells the player "something is about to happen" and gives enemies a telegraph.
- **Action** (contact): The actual hit. This should be the shortest phase — fast = impactful.
- **Recovery** (follow-through): Character returns to idle. This is when you're vulnerable. Longer recovery = more commitment = more weight.

**Timing rule:** Anticipation > Recovery > Action. A 0.4s attack might be: 0.15s wind-up, 0.05s hit, 0.2s recovery.

### Tweening and Easing
Godot's `CreateTween()` is the primary tool for game feel:

```csharp
// Damage number float up and fade
var label = new Label { Text = $"-{damage}" };
label.Position = hitPosition;
var tween = CreateTween();
tween.TweenProperty(label, "position:y", hitPosition.Y - 40, 0.6f)
     .SetEase(Tween.EaseType.Out);
tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.6f);
tween.TweenCallback(Callable.From(label.QueueFree));
```

**Easing types that matter:**
| Ease | Feel | Use For |
|------|------|---------|
| `EaseType.Out` | Fast start, slow stop | Damage numbers, knockback |
| `EaseType.In` | Slow start, fast end | Charging up, pulling back |
| `EaseType.InOut` | Smooth both ends | Camera movements, UI transitions |
| Linear | Constant speed | Only for timers, never for visuals |

**Rule: Never use Linear easing for anything the player sees.** Linear motion looks robotic.

### Input Responsiveness
Players notice input delay above ~50ms. Rules:
- Movement should respond on the **same frame** as input (in `_PhysicsProcess`)
- Attacks should start **immediately** on button press, not next frame
- Use **input buffering**: if the player presses attack during cooldown, queue it and execute when cooldown ends (100-150ms buffer window)

```csharp
// Input buffer example
private float _attackBuffer;
private const float BufferWindow = 0.15f;

public override void _Process(double delta)
{
    if (Input.IsActionJustPressed("action_cross"))
        _attackBuffer = BufferWindow;
    
    if (_attackBuffer > 0)
    {
        _attackBuffer -= (float)delta;
        if (CanAttack())
        {
            PerformAttack();
            _attackBuffer = 0;
        }
    }
}
```

### Weight and Commitment
Heavy attacks should **commit** the player — during the attack animation, movement is reduced or stopped. This creates weight:

- Light attack: 0% movement reduction, 0.3s total
- Heavy attack: 100% movement reduction, 0.8s total, screen shake on hit
- The choice between light and heavy IS the combat depth

## Godot 4 + C# Implementation

```csharp
// Screen shake
public void Shake(Camera2D camera, float intensity = 4f, float duration = 0.15f)
{
    var tween = camera.CreateTween();
    int steps = (int)(duration / 0.02f);
    for (int i = 0; i < steps; i++)
    {
        float decay = 1f - (float)i / steps;
        float x = (float)GD.RandRange(-intensity, intensity) * decay;
        float y = (float)GD.RandRange(-intensity, intensity) * decay;
        tween.TweenProperty(camera, "offset", new Vector2(x, y), 0.02f);
    }
    tween.TweenProperty(camera, "offset", Vector2.Zero, 0.02f);
}

// Hit flash (white flash on damage)
public void FlashWhite(Sprite2D sprite, float duration = 0.1f)
{
    sprite.Modulate = Colors.White;
    var tween = sprite.CreateTween();
    tween.TweenProperty(sprite, "modulate", Colors.White, duration * 0.3f);
    tween.TweenProperty(sprite, "modulate", sprite.SelfModulate, duration * 0.7f);
}

// Hit pause (freeze frame for impact)
public async void HitPause(float duration = 0.06f)
{
    Engine.TimeScale = 0.05;
    await ToSignal(GetTree().CreateTimer(duration, true, false, true), "timeout");
    Engine.TimeScale = 1.0;
}
```

## Common Mistakes
1. **No feedback on player actions** — game feels unresponsive and dead
2. **Screen shake too strong** — makes people nauseous (keep it 2-5px, 50-150ms)
3. **Hit pause too long** — game feels laggy instead of impactful (keep it 50-80ms)
4. **Linear easing on everything** — all motion looks robotic and lifeless
5. **Forgetting input buffering** — players press attack during cooldown and nothing happens (feels unresponsive)
6. **Movement not in _PhysicsProcess** — introduces 1-frame input delay
7. **Adding feel last** — game feel should be designed from the start, not added as "polish" at the end
8. **No anticipation frames** — attacks appear from nowhere with no telegraph

## Checklist
- [ ] Every player action has visual feedback (flash, shake, particles, OR sound)
- [ ] Attacks have anticipation → action → recovery timing
- [ ] Easing is never Linear for visible motion
- [ ] Input responds same-frame (in _PhysicsProcess)
- [ ] Attack input is buffered (100-150ms window)
- [ ] Camera has screen shake for impacts
- [ ] Damage numbers float up and fade

## Sources
- ["Juice it or Lose it" — GDC 2012 (Martin Jonasson & Petri Purho)](https://www.youtube.com/watch?v=Fy0aCDmgnxg)
- ["The Art of Screenshake" — GDC (Jan Willem Nijman)](https://www.youtube.com/watch?v=AJdEqssNZ-U)
- [Game Feel by Steve Swink (book)](https://www.amazon.com/Game-Feel-Designers-Sensation-Kaufmann/dp/0123743281)
- [Game Programming Patterns: Game Loop (Robert Nystrom)](https://gameprogrammingpatterns.com/game-loop.html)

# Visual Feedback

## Why This Matters
Our game deals damage, awards XP, and triggers effects — but the player can't SEE any of it. Without visible feedback, every system feels invisible. Visual feedback is how you communicate "something happened" without requiring the player to read a log.

## Core Concepts

### The Feedback Hierarchy
Every game action should produce feedback in this priority order:

1. **Immediate visual** (same frame): flash, animation change, sprite reaction
2. **Floating text** (0-0.5s): damage numbers, "+XP", "Level Up!"
3. **Persistent indicator** (ongoing): health bars, status icons, buff/debuff markers
4. **Audio** (same frame): hit sound, death sound, pickup chime
5. **Environmental** (0.5-2s): screen shake, particle burst, blood splatter

The player should NEVER have to look at a log to know what happened.

### Damage Numbers
The most important feedback in an ARPG:

```
HIT: white number, floats up, fades over 0.6s
CRIT: larger gold number with "!" suffix, slight bounce
HEAL: green number with "+" prefix
MISS/BLOCK: grey "MISS" or "BLOCK" text
ELEMENTAL: colored by element (red=fire, blue=ice, purple=dark)
```

```csharp
public void ShowDamage(Vector2 position, int amount, bool crit, DamageType type)
{
    var label = new Label();
    label.Text = crit ? $"{amount}!" : $"{amount}";
    label.AddThemeFontSizeOverride("font_size", crit ? 18 : 13);
    
    Color color = type switch
    {
        DamageType.Fire => new Color(1, 0.4f, 0.2f),
        DamageType.Water => new Color(0.3f, 0.6f, 1.0f),
        DamageType.Dark => new Color(0.6f, 0.2f, 0.8f),
        _ => crit ? new Color("#f5c86b") : Colors.White,
    };
    label.AddThemeColorOverride("font_color", color);
    
    // Random horizontal scatter so numbers don't stack
    label.Position = position + new Vector2(GD.RandRange(-15, 15), -10);
    label.ZIndex = 100;
    AddChild(label);
    
    var tween = CreateTween();
    tween.TweenProperty(label, "position:y", label.Position.Y - 35, 0.6f)
         .SetEase(Tween.EaseType.Out);
    tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.6f);
    tween.TweenCallback(Callable.From(label.QueueFree));
}
```

### Health Bars
Two types:
- **Player HP/MP**: Persistent orbs/bars always visible on HUD
- **Enemy HP**: Small bar above enemy, only visible when damaged

Enemy HP bar design:
```
Width: 40-60px (proportional to enemy size)
Height: 4-6px
Background: dark grey (20% opacity)
Fill: red (100% to 0%)
Position: 20-30px above enemy center
Show: when HP < MaxHP
Hide: when enemy dies
```

### Status Effect Indicators
When a buff or debuff is active, show an ICON near the entity:

| Effect | Visual | Position |
|--------|--------|----------|
| Poison | Green tint on sprite + green icon | Above HP bar |
| Fire DoT | Orange pulsing glow | Above HP bar |
| Speed buff | Blue trail behind movement | Below entity |
| Defense buff | Shield icon | Above HP bar |
| Stunned | Stars circling head | Above entity |

### Attack Telegraphing
Enemies should telegraph attacks so players can react:

| Telegraph | Duration | What It Shows |
|-----------|----------|---------------|
| **Wind-up animation** | 0.3-0.8s | Enemy pulls back before striking |
| **Ground indicator** | 0.5-1.5s | Red circle/line showing attack area |
| **Flash/glow** | 0.2-0.5s | Enemy glows before firing projectile |
| **Audio cue** | 0.2s before hit | Whoosh, charge-up sound |

Longer telegraph = more powerful attack. Players learn: "long wind-up = dodge NOW."

### Level Up / Milestone Feedback
Big moments deserve big feedback:

```
LEVEL UP:
- Large gold "LEVEL UP!" text, centered screen
- Flash effect on character (gold glow, 0.5s)
- Sound effect (triumphant chime)
- Brief slow-motion (Engine.TimeScale = 0.5 for 0.3s)
- Stat increase numbers cascade ("+8 HP, +3 MP")

BOSS KILL:
- Screen shake (strong, 0.3s)
- Slow-motion on final hit (0.5s)
- Loot explosion (items scatter outward from corpse)
- XP number (large, gold)
- "Floor Complete" announcement
```

## Godot 4 + C# Implementation

```csharp
// Flash entity white on damage
public void FlashDamage(Node2D entity)
{
    if (entity.GetChildOrNull<Sprite2D>(0) is Sprite2D sprite)
    {
        var originalModulate = sprite.Modulate;
        sprite.Modulate = Colors.White;
        var tween = entity.CreateTween();
        tween.TweenInterval(0.08f);
        tween.TweenProperty(sprite, "modulate", originalModulate, 0.12f);
    }
}

// Enemy HP bar
public class EnemyHPBar
{
    private ColorRect _bg;
    private ColorRect _fill;
    private const float BarWidth = 40f;
    private const float BarHeight = 5f;
    
    public void Create(Node2D parent)
    {
        _bg = new ColorRect { Size = new Vector2(BarWidth, BarHeight) };
        _bg.Position = new Vector2(-BarWidth / 2, -28);
        _bg.Color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        parent.AddChild(_bg);
        
        _fill = new ColorRect { Size = new Vector2(BarWidth, BarHeight) };
        _fill.Position = _bg.Position;
        _fill.Color = new Color(0.85f, 0.2f, 0.2f);
        parent.AddChild(_fill);
    }
    
    public void Update(float hpPercent)
    {
        _fill.Size = new Vector2(BarWidth * hpPercent, BarHeight);
    }
}
```

## Common Mistakes
1. **No damage numbers** — player can't tell if they're doing damage or how much
2. **All damage numbers look the same** — crits should be visually distinct from normal hits
3. **Numbers stack on top of each other** — add random horizontal scatter
4. **No enemy HP bar** — player can't tell how close the enemy is to dying
5. **No attack telegraph** — enemies hit instantly with no warning (feels unfair)
6. **Level up has no fanfare** — the biggest milestone in the session passes unnoticed
7. **Status effects invisible** — player is poisoned but has no idea why HP is dropping

## Checklist
- [ ] Every damage instance shows a floating number
- [ ] Crits are visually distinct (bigger, colored, "!" suffix)
- [ ] Enemies have HP bars that show when damaged
- [ ] At least one form of feedback per player action (visual OR audio)
- [ ] Attacks are telegraphed (wind-up animation or ground indicator)
- [ ] Level up has at least 3 simultaneous feedback types

## Sources
- [GDC: Juice it or Lose it (Jonasson & Purho)](https://www.youtube.com/watch?v=Fy0aCDmgnxg)
- [Game Feel: A Game Designer's Guide to Virtual Sensation (Steve Swink)](https://www.amazon.com/Game-Feel-Designers-Sensation-Kaufmann/dp/0123743281)
- [Diablo 3 Visual Effects Postmortem (GDC)](https://www.gdcvault.com/play/1020351/Diablo-III-from-Cool-to)

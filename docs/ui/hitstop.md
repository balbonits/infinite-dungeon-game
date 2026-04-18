# Hitstop

## Summary

Exact frame count the game pauses on a registered hit to make impact *feel* heavier. SPEC-HITSTOP-01 (Phase H). Purely cosmetic — no gameplay effect on the hit outcome, just a tiny pause that makes the hit land visibly.

## Current State

**Spec status: LOCKED** via SPEC-HITSTOP-01 (Phase H). No hitstop currently exists. This spec introduces it.

## Design

### What hitstop is

On a hit, the game freezes for a few frames. The attacker's swing pauses mid-animation, the target is locked in its hit-reaction pose, and no movement / AI / physics advances. After the pause window, everything resumes. It's a feel mechanic that predates high-framerate games — see Street Fighter, Hollow Knight, Celeste. The pause is too short to register as "the game lagged" but long enough that the hit reads as something that happened vs something that was computed.

### Frame counts (at 60 FPS)

| Event | Frames | Duration (ms @ 60 FPS) |
|-------|--------|------------------------|
| Player lands a regular hit | **2** | ~33 ms |
| Player lands a crit | **4** | ~67 ms |
| Player takes a hit | **3** | ~50 ms |
| Boss phase-shift trigger | **6** | ~100 ms |
| Boss defeat (kill blow) | **10** | ~167 ms |

**Why these numbers:**

- **2 frames (regular hit):** the threshold where hitstop is felt as weight without being noticed as pause. Below 2 frames it disappears; above 3 starts to feel laggy.
- **4 frames (crit):** visible without being jarring — doubles regular hitstop so crits feel noticeably punchy.
- **3 frames (damage received):** slightly longer than landing a regular hit because damage-to-player is a bigger moment emotionally; pairs with the camera shake.
- **6 frames (phase-shift):** long enough to let the player register the aura-color change before combat resumes. Pairs with `FlashFx.Flash`.
- **10 frames (boss defeat):** the celebration beat. Long enough to feel like the game acknowledges the moment; short enough not to stall the reward flow.

### Framerate independence

Games running at higher framerates (120 FPS, 144 FPS) should NOT divide the frame counts — that would cut hitstop duration and kill the feel. Instead, compute hitstop as a real-time duration and round to the render framerate:

```
duration = frame_count / 60.0   // locked to 60 FPS reference
```

So "2 frames" always means ~33 ms regardless of actual render rate. On a 120 FPS display, a 2-frame hitstop becomes 4 render-frames; on 144 FPS, it's ~5.

### Scope — what pauses and what doesn't

**Pauses during hitstop:**
- Game-world physics (`Engine.TimeScale = 0`).
- Enemy AI ticks.
- Projectile travel.
- Player movement input (the held input keeps its state; it just doesn't advance position).

**Does NOT pause:**
- Music and ambient audio. Audio cutting out mid-note reads as a lag spike; hitstop keeps audio flowing.
- The hit SFX itself. The attack's sound plays normally; only visual time freezes.
- Particle effects tied to the impact. These continue so the player can see the hit land.
- UI animations (HUD orb refills, floating-text rises). These are feel-feedback and should not freeze.

### Not-hitstop-triggering events

- Pickups, XP text, loot.
- Enemy-on-enemy damage (if that ever becomes possible — no hitstop).
- Heals, buffs, de-buffs. Status changes don't "land" visually the way a hit does.

### Stacking

If two hitstop-triggering events fire in the same frame (e.g., player crits a boss at the exact moment the boss phase-shifts), take the **max** of the two durations — not the sum. Stacking sum reads as the game lagging; max preserves the feel of the longer beat.

### Accessibility

- **"Disable hitstop" option.** Similar to [camera-shake](camera-shake.md)'s reduce-motion toggle, an Options item to turn hitstop off entirely. Some players find frame-pauses disorienting, especially on fast combat.
- **When disabled:** all hitstop durations become 0 frames; hits still register, just without the pause.
- **Default:** enabled (the spec values above).

---

## Acceptance Criteria

- [ ] Hitstop fires on the five locked events (regular hit, crit, damage taken, phase shift, boss defeat) with the specified frame counts.
- [ ] Durations are computed as `frame_count / 60` seconds, not frame-count literal.
- [ ] At 120 FPS and 144 FPS display rates, the effective duration matches the 60-FPS reference (visible as more render-frames, same wall-clock).
- [ ] Audio and particle effects continue during hitstop.
- [ ] UI overlays (HUD orbs, floating text) continue animating during hitstop.
- [ ] Overlapping hitstops take the max duration, not the sum.
- [ ] Accessibility toggle "Disable hitstop" zeroes all durations.

## Implementation Notes

- **Hitstop script:** new `Hitstop` autoload exposing `public static void Trigger(float durationSeconds)`. Internally it sets `Engine.TimeScale = 0` and uses an un-scaled timer (`GetTree().CreateTimer(seconds, processInPhysics: false, processAlways: true)`) to restore `TimeScale = 1` after the duration elapses. `processAlways` is critical — without it the restoration timer itself is paused.
- **Event subscriptions:** callers fire `Hitstop.Trigger(seconds)` at the right moment:
  - Player attack-hit handler (2 frames = 0.033s regular, 4 frames = 0.067s crit).
  - Player damage handler (3 frames = 0.050s).
  - Boss `PhaseShifted` signal (6 frames = 0.100s).
  - Enemy `Defeated` signal with `IsBoss` flag (10 frames = 0.167s).
- **Max-stacking:** track the current hitstop's remaining duration; incoming Trigger calls only extend if the new duration is greater. Ignore shorter incoming durations.
- **Audio exemption:** Godot's `AudioStreamPlayer.Stream` doesn't pause with `Engine.TimeScale = 0` by default — good; this is what we want. Verify on implementation.
- **Settings key:** add `player_prefs.DisableHitstop: bool` to settings.

## Open Questions

None — spec is locked.

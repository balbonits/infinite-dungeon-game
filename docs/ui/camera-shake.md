# Camera Shake

## Summary

Damage-scaled screen shake intensity + on-screen flash for critical damage events. SPEC-CAMERA-SHAKE-01 (Phase H). Purely a feel layer — no gameplay effect.

## Current State

**Spec status: LOCKED** via SPEC-CAMERA-SHAKE-01 (Phase H). No camera-shake currently exists in the game; this spec introduces it.

## Design

### When to shake

- **Player takes damage:** shake proportional to damage-to-max-HP ratio.
- **Player lands a crit:** short shake (~100 ms) regardless of damage size; the crit itself is the feel payoff.
- **Boss defeat:** longer shake (~500 ms) on the kill-blow.
- **Phase-shift trigger** (per [SPEC-BOSS-*](../world/bosses/) specs — e.g., Bone Overlord Phase 2): medium shake (~200 ms) paired with the existing `FlashFx.Flash` white pulse.

### When NOT to shake

- Player lands a regular (non-crit) hit. Too much shake = motion-sickness territory and dilutes the crit-shake signal.
- Enemy takes damage. Their hit-flash is enough; player-camera shake is reserved for player-felt moments.
- Pickups, XP gain, loot drops. Neutral events; no shake.
- Level up. A positive moment that doesn't benefit from shake (shake reads as "violence" to the player's nervous system).

### Intensity formula

Shake amount is a vector offset applied to the camera each frame during the shake duration. Two axes of magnitude:

```
intensity = base_intensity * damage_ratio
duration  = base_duration  * damage_ratio
```

Where:

- `damage_ratio = damage_taken / player_max_hp` (clamped 0.0 to 1.0).
- `base_intensity = 4.0 px` (at 100% damage-ratio → 4 px max offset in each direction).
- `base_duration = 0.3 s` (at 100% damage-ratio → 300 ms shake).

**Examples (player with 120 max HP):**

| Damage taken | Ratio | Intensity | Duration |
|--------------|-------|-----------|----------|
| 12 (10%) | 0.10 | 0.4 px | 30 ms |
| 24 (20%) | 0.20 | 0.8 px | 60 ms |
| 60 (50%) | 0.50 | 2.0 px | 150 ms |
| 120 (lethal) | 1.00 | 4.0 px | 300 ms |
| 30% damage crit by player | (flat) | 1.0 px | 100 ms |
| Boss kill (any) | (flat) | 3.0 px | 500 ms |
| Phase-shift trigger | (flat) | 2.0 px | 200 ms |

### Shake mechanics (per-frame)

```
shake(t):
  elapsed_fraction = elapsed / duration
  decay = 1.0 - elapsed_fraction   // linear fall-off from peak to zero
  offset_x = random(-1, +1) * intensity * decay
  offset_y = random(-1, +1) * intensity * decay
  camera.position = base_position + (offset_x, offset_y)
```

- **Decay is linear** — not exponential (exponential feels too aggressive at peak). Linear reads as "hit, settle."
- **Random jitter is per-frame**, not a smoothed curve. Keeps the shake visually chaotic — a smoothed oscillation reads as "earthquake" instead of "hit impact."
- **Maximum displacement at peak = `intensity`.** Players on low-DPI/small screens see a 4-px shake; at 3× upscale (per [SPEC-UI-HIGH-DPI-01](high-dpi.md)) the same 4-px shake reads as 12 physical pixels, which is still tasteful — not nausea-inducing.

### Screen flash (pairs with heavy shake)

For lethal-or-near-lethal hits (ratio ≥ 0.75), pair the shake with a full-screen red flash via `FlashFx.Flash(Color.Red with 30% opacity, 120 ms)`. This communicates "that was a big one" beyond the shake alone.

The existing `FlashFx.Flash` is also used by boss phase-shift triggers per [SPEC-BOSS-*](../world/bosses/) specs — so the same system handles both camera-shake-paired flashes and phase-shift flashes.

### Accessibility — "reduce motion" toggle

An Options-menu toggle **Reduce screen shake** (default Off). When On:

- Damage-scaled shake is scaled to 25% of the formula (not zero — eliminating it entirely loses the hit-feedback signal).
- Crit shake drops to 50%.
- Boss-defeat shake drops to 50%.
- Phase-shift shake drops to 50%.
- Flash remains unchanged (it's not motion; it's light).

### Shake stacking

If a new shake starts while one is still decaying, take the **max** of the two (intensity AND duration), not the sum. Additive stacking compounds sickness risk; max-stacking still surfaces the bigger hit.

---

## Acceptance Criteria

- [ ] Shake triggers only on the four locked events (player damage, player crit, boss defeat, phase shift).
- [ ] Damage-proportional shake uses the `damage / max_hp` ratio and caps at 4 px / 300 ms.
- [ ] Crit / boss-defeat / phase-shift shakes use their flat-value overrides.
- [ ] Lethal-range damage (≥75% max HP in one hit) fires the red-flash pairing.
- [ ] Shake decay is linear, offset is per-frame random jitter.
- [ ] Accessibility toggle "Reduce screen shake" applies the 25%/50% scale-downs.
- [ ] Overlapping shakes take the max of their (intensity, duration), not the sum.
- [ ] Shake applies to the gameplay camera only — NOT to UI overlays (Pause menu, modals). UI is screen-locked.

## Implementation Notes

- **Camera-shake script:** new `CameraShake` node attached to the `Camera2D`; exposes a public `Shake(intensity, duration)` method. Subscribes to:
  - `Player.SignalName.DamageTaken` — computes ratio + calls Shake.
  - `Player.SignalName.CritLanded` — flat values.
  - `Enemy.SignalName.Defeated` — only if `enemy.IsBoss` → flat values.
  - `Enemy.SignalName.PhaseShifted` (new signal for SPEC-BOSS-* phase triggers) — flat values.
- **Flash pairing:** on Shake calls with ratio ≥ 0.75, also call `FlashFx.Flash(Color.Red with alpha 0.3, 120)`.
- **Settings key:** add `player_prefs.ReduceScreenShake: bool` to the settings file (via SettingsPanel).
- **Test handling:** tests often run at accelerated time; shake should respect `Engine.TimeScale` so shake duration tracks the actual game-time, not wall-time.

## Open Questions

None — spec is locked.

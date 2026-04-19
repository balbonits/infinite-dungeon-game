# Class Color Coding

## Summary

Per-class accent color: **Warrior = brick red, Ranger = forest green, Mage = royal violet-blue.** Shifted RGB triad — the hues are recognizably R/G/B, but darker and less saturated than the existing HP/heal/MP semantic slots so they don't collide. Applies to class-specific UI surfaces (class-select cards, class-name labels, class sprite accent zones); does NOT apply to gameplay feedback colors (HP stays red, MP stays blue, heals stay green). SPEC-CLASS-COLOR-CODING-01 (post-Phase H addendum).

## Current State

**Spec status: LOCKED** via SPEC-CLASS-COLOR-CODING-01. Replaces the single-color "player-blue accent for all three classes" rule from [SPEC-PC-ART-01 §Color](../world/player-classes-art.md) — each class now has its own accent.

## Design

### Why a triad

PO direction 2026-04-18: class identity should read immediately at a glance — "oh that's the Warrior" via color alone, before the silhouette is parsed. The classic R/G/B triad is the most-readable 3-class signal in gaming (Diablo 2's Barbarian/Druid/Sorceress, Path of Exile's STR/DEX/INT gem colors, generic MMO tank/DPS/caster coding). A triad gives each class a memorable mental slot.

### The collision with existing semantic colors

The raw R/G/B triad collides with UiTheme's semantic slots. Values below are from the canonical spec [`docs/assets/ui-theme.md`](../assets/ui-theme.md) — specs are the source of truth per [development-paradigm.md](../development-paradigm.md); code is reconciled to match specs, not the other way around.

| Semantic slot | Hex | Role |
|---------------|-----|------|
| `Danger` | `#ff6f6f` | Damage numbers, warning states, high-threat enemies |
| `Safe` | `#76f79f` | Heals, success toasts, low-threat enemies |
| `Action` | `#518ef4` | Buttons, interactive UI, action emphasis |
| `Player` | `#8ed6ff` | Current player accent (before this spec) |

(The HP orb and MP orb use their own fills — `#CC2222` / `#2244CC` respectively per the locked [HUD layout](hud-layout.md) — not the `Danger` / `Action` semantic slots. The HP orb's low-HP pulse is `#FF4444`, not `Danger`.)

**Known code drift (reconcile in a separate follow-up):** [`scripts/ui/UiTheme.cs`](../../scripts/ui/UiTheme.cs) currently defines `Colors.Safe = #6bff89`, which is stale against the spec's `#76f79f`. `Colors.Action` exists in code (`#518ef4`) but is not yet listed in `ui-theme.md`'s semantic-slot table. Both drifts are outside this PR's scope.

A literal Warrior-`Danger`-red would make a red "Warrior Guildmaster" label look like a warning. A literal Ranger-`Safe`-green would blur into heal toasts. Pure RGB is out.

### Locked colors (shifted RGB)

Each class color is a darker, less-saturated cousin of the pure hue — the triad intent survives, the conflict goes away.

| Class | Hex | Name | HSL | Intent |
|-------|-----|------|-----|--------|
| Warrior | `#b53238` | **Brick Red** | H=357, S=57%, L=45% | Martial, weighty, blood-and-iron — distinct from `#ff6f6f` Danger (damage numbers, warning states) and from the separate `#CC2222` HP orb fill. |
| Ranger | `#3a7a4d` | **Forest Green** | H=140, S=35%, L=35% | Woodland, wilderness, earthy — distinct from `#76f79f` Safe (brighter, mint-forward). |
| Mage | `#5b47a0` | **Royal Violet** | H=255, S=39%, L=45% | Arcane, deep-magic, purple-blue — distinct from `#518ef4` Action button blue (brighter, pure blue). |

Players should perceive these as "red / green / blue class" in under half a second. The darker/saturated shift is invisible at a glance but load-bearing for avoiding semantic overlap.

### Scope — where class colors apply

**Applied:**

1. **Class-select screen cards.** Each class card's border + header banner + highlight uses its class color.
2. **Class name labels in menus.** Anywhere a class name appears as text (`Warrior Guildmaster`, `Mage Class`, Stats panel header), render the name in the class color.
3. **Player-class sprites — accent zones.** Per [SPEC-PC-ART-01 §Color](../world/player-classes-art.md), PCs are not modulated by the level-relative tint system — so no exempt-pixel carve-out mechanism is needed for PC accents. The class accent is instead **baked directly into specific pixel clusters** (cape trim, gem/glow, insignia) on the sprite sheet. Replace the single player-blue (`#8ed6ff`) accent rule with **per-class accent**: Warrior sprites carry brick-red accent pixels, Ranger sprites carry forest-green, Mage sprites carry royal-violet.
4. **HUD class indicator** (if added later) — small class-name chip uses the class color.
5. **Skill-bar slot borders for class-specific abilities** (future polish) — may carry a subtle class-color accent to distinguish Warrior abilities from Innate / generic ones.

**NOT applied — these stay on their semantic slots regardless of class:**

1. **HP orb** — always `#CC2222` red per the locked [HUD layout](hud-layout.md) (pulsing to `#FF4444` below 25% HP). Not per class.
2. **MP orb** — always `#2244CC` blue per the locked [HUD layout](hud-layout.md). Not per class.
3. **Damage numbers** — Danger red regardless of attacker class.
4. **Heal text / buff toasts** — Safe green regardless of caster class.
5. **Crit indicator** — Accent gold (`#f5c86b`), not class.
6. **Damage / warning-state coloring** — `Danger` red (`#ff6f6f`), regardless of class. (The HP orb's low-HP pulse `#FF4444` is separate from this slot and covered in bullet 1.)

The split reads as "gameplay feedback colors are shared; identity colors are per-class." Never let one bleed into the other.

### Player-class sprite update (SPEC-PC-ART-01 supersession note)

[SPEC-PC-ART-01 §Color](../world/player-classes-art.md) previously stated all three classes share a single `#8ed6ff` player-blue accent. This spec supersedes that:

- Warrior sprite accent zones: `#b53238` brick red (cape trim, shield rim, shoulder pauldron inlay).
- Ranger sprite accent zones: `#3a7a4d` forest green (cloak fringe, quiver strap, bow grip wrap).
- Mage sprite accent zones: `#5b47a0` royal violet (robe trim, staff gem, hood inner lining).

Art-spec side ([ART-SPEC-PC-01](../assets/player-class-pipeline.md)) needs a corresponding update so per-class PixelLab prompts include the correct accent hex. The exempt-pixel carve-out technique does not change — only the hex values do.

### Accessibility

- **Red-green color blindness** (deuteranopia / protanopia — affects ~8% of males): brick red and forest green have similar luminance (L=45 / L=35) which may be hard to distinguish in grayscale. Mitigation: **color is never the only cue.** Every class-coded surface also has the class NAME in text, the class icon (future: Warrior = shield, Ranger = bow, Mage = staff), or the class sprite silhouette alongside the color. The color reinforces identity; it doesn't carry it alone.
- **Blue-yellow color blindness** (tritanopia): rare, but the Mage violet-blue could blur into Warrior brick-red in grayscale. Same mitigation — text + silhouette redundancy.
- **Future option**: an Options-menu "Class color accessibility mode" could swap one of the classes to a distinctly-different hue (e.g., Ranger = teal instead of green) for affected players. Not in scope for this spec; open a future ticket if playtesting surfaces the need.

### Naming convention

Colors are referenced as constants in code:

- `UiTheme.Colors.ClassWarrior` = `new Color("#b53238")`
- `UiTheme.Colors.ClassRanger` = `new Color("#3a7a4d")`
- `UiTheme.Colors.ClassMage` = `new Color("#5b47a0")`

Plus a helper method:

- `UiTheme.Colors.ForClass(PlayerClass cls)` returns the appropriate Color.

## Acceptance Criteria

- [ ] `UiTheme.Colors.ClassWarrior / ClassRanger / ClassMage` defined with exact hex values.
- [ ] `UiTheme.Colors.ForClass(PlayerClass)` helper method.
- [ ] Class-select cards render each card's border + header in its class color.
- [ ] Class-name text labels (Stats panel header, NpcPanel greetings that use `{Class} Guildmaster`, pause-menu titles) render the class name in its class color.
- [ ] Player-class sprites (Warrior / Ranger / Mage) have accent pixels in their class color (not the old `#8ed6ff` player-blue).
- [ ] HUD HP orb stays `#CC2222` (pulsing to `#FF4444` below 25% HP) per [hud-layout.md](hud-layout.md), regardless of class.
- [ ] HUD MP orb stays `#2244CC` per [hud-layout.md](hud-layout.md), regardless of class.
- [ ] Damage numbers, heal text, crit indicators stay on their semantic slots regardless of class.
- [ ] Accessibility: every class-coded surface has text or icon redundancy so the color is not the only identity signal.

## Implementation Notes

- **`UiTheme.cs` update:** add the three class Color constants + the `ForClass` helper. Follow the existing `Colors.*` static class pattern.
- **Class sprite accent update:** the live sprites (Warrior / Ranger / Mage under `assets/characters/player/`) currently use the `#8ed6ff` accent. Redraw batch (ART-SPEC-PC-01 paired with SPEC-PC-ART-01 update) replaces those accents with the per-class hex. This is Bucket A in the asset redraw per [asset-inventory.md](../assets/asset-inventory.md) — already queued; adding the per-class-color note to the ticket.
- **Class-select scene update:** find every card-border / card-header style override in `ClassSelect.cs` (or equivalent) and route through `UiTheme.Colors.ForClass()` instead of the hardcoded `Player` color.
- **NpcPanel / Pause-menu label updates:** `{Class} Guildmaster` renderers substitute the class name + wrap in a `<color>` rich-text tag using the class color, OR set `font_color` on the label. Match the codebase's existing per-text coloring pattern.
- **Supersession note in SPEC-PC-ART-01:** edit `docs/world/player-classes-art.md` §Color to point at this spec as the canonical per-class accent.

## Open Questions

None — spec is locked.

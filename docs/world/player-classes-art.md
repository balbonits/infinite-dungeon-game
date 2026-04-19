# Player Classes — Visual Identity & Sprite Spec (SPEC-PC-ART-01)

## Summary

The game-facing visual identity spec for the three playable classes — **Warrior**, **Ranger**, **Mage**. Defines the silhouette, fiction beat, starter equipment, color contract, scale, and animation set for the **single canonical world sprite** each class ships with. Equipment changes (armor tier, weapon swaps) are reflected in the inventory paperdoll UI, **not** on the world sprite. Paired with [ART-SPEC-PC-01 — player class sprite pipeline](../assets/player-class-pipeline.md), which specifies the PixelLab generation half. Co-lock criterion: neither half reaches "Ready-for-impl" alone.

## Current State

The repo ships three player class sprites today (`assets/characters/player/{warrior,ranger,mage}/rotations/*.png`, referenced by `Constants.PlayerClassPreviews` and the live `Player.cs` scene). They were authored ad-hoc before this spec existed and are slated for re-generation under [ART-SPEC-01 v2](../assets/prompt-templates.md) (the iso pivot rewrite at commit `5e9e70f`). The class roster — Warrior, Ranger, Mage — is locked in `scripts/logic/PlayerClass.cs` and `scripts/ui/ClassSelect.cs`; this spec extends but does not change that roster.

This spec mirrors the eight-section rigor of [`docs/world/species-template.md`](species-template.md) (SPEC-SPECIES-01) but adapted for player characters: PCs do not have AI patterns, drop tables, or level-relative tint behavior, so those sections are dropped or reframed. Identity, silhouette, scale, color contract, and art-pairing carry over directly.

Related specs this one pulls from:
- [docs/overview.md](../overview.md) — game vision, Diablo-1-inspired tone, frontier-settlement framing
- [docs/world/species-template.md](species-template.md) — sibling rigor for monster authoring (mirrored here)
- [docs/assets/prompt-templates.md](../assets/prompt-templates.md) — `CHAR-HUM-ISO` block (the generation foundation art-lead's paired spec extends)
- [docs/systems/color-system.md](../systems/color-system.md) — level-relative tint system (PCs are exempt; see §Color-Coding Contract)
- [docs/systems/iso-rendering.md](../systems/iso-rendering.md) — sprite anchor + 128×128 canvas contract
- [docs/assets/player-class-pipeline.md](../assets/player-class-pipeline.md) — paired art half (ART-SPEC-PC-01)

**Locked product-owner decision (2026-04-17): single default sprite per class.** Warrior in starter plate + sword + shield; Ranger in leather + shortbow + hood; Mage in robes + staff + pointed hat. Equipment changes are surfaced in the inventory paperdoll UI; they do **not** swap the world sprite. No per-armor-tier sprite variants. No per-weapon sprite variants. This decision is the entire reason the per-class spec below has only one "starter equipment" row instead of a tiered matrix.

## Design

### 1. Per-Class Identity Template

Each class fills the same nine-field template below. The template structure is identical across classes so cross-class differentiation is checkable side-by-side. Per the locked PO decision, every class has exactly one row — no tiered variants.

| Field | What it pins |
|---|---|
| **Fiction beat** | One sentence: who this character is in the world, fitting the frontier-settlement / first-explorer framing in `docs/overview.md`. |
| **Role in party fantasy** | Which classic ARPG axis (tanky melee / agile ranged DPS / squishy caster). |
| **Silhouette readability constraint** | What MUST read at a glance from 8 tiles away. The single hardest field to get right; this is the gameplay-visual hook. |
| **Starter equipment (world sprite)** | Exact items rendered on the canonical 128×128 sprite. This is what PixelLab generates. |
| **Color-coding contract** | Which surfaces carry the player-blue accent (`#8ed6ff` per `docs/assets/ui-theme.md`); which pixels (if any) are exempt from any future tint. |
| **Scale + anchor** | Scale multiplier (player = 1.00 canonical reference), Z-offset, canvas size. |
| **Locked animation set** | Which animations ship with the sprite at v1: walking + fight-stance idle + class-appropriate attack. |
| **Build / age / gender beat** | Per-class skew on the shared rules in §4. |
| **Naming coupling** | How this class is addressed in-game (per `project_npc_naming` memory). |

Sections 2–4 below specialize this template for each class. Section 6 is the worked example for Warrior with full narrative depth; Sections 7–8 fill every field for Ranger and Mage with less prose but no TBDs.

---

### 2. Class-vs-Class Differentiation Rules

The three class silhouettes MUST be distinguishable at 64-pixel thumbnail scale (half of the 128×128 native canvas) from a neutral idle pose, with no equipment hint beyond what each silhouette inherently carries. This is a hard gameplay constraint — the player must identify their own class instantly when alt-tabbing back, when respawning, when watching another player's character on a future co-op screen.

**Locked silhouette differentiation rules:**

| Class | Silhouette dominant axis | Concrete hook |
|---|---|---|
| **Warrior** | **Widest** | Shoulders + shield extend the horizontal bounding box. Pauldrons exaggerated. Shield held at the side (not on back) so the silhouette is asymmetric and reads "armored mass". |
| **Mage** | **Tallest** | Pointed wizard hat extends above the head silhouette by ~12–16 px. Staff held vertically, head of the staff rises near hat-tip height. |
| **Ranger** | **Narrowest** | No shoulder armor. Bow held close to the body (not extended outward). Hood drawn up but not flared. Compact, lean profile. |

**Why these three axes specifically.** Width vs. height vs. compactness are the three orthogonal silhouette levers a 2D pixel artist has. Picking each class on a different lever guarantees no two classes can be confused from any rotation, even at thumbnail scale — they don't compete on the same dimension.

**Acceptance test for this rule:** export the south-facing idle frame of all three classes at 64×64. Convert each to a pure-black silhouette (alpha-mask). A reviewer who has never seen the three classes can name which is which with 100% accuracy.

---

### 3. Gender + Age Beats (shared across all classes)

These rules apply to every class so the art-pipeline spec doesn't have to re-derive them per class.

- **Age range**: all three classes read as **adults aged roughly 20s–40s**. Not children, not elders. Frontier guild members are working-age — the village chief is the only NPC who reads as elder, and there are no child characters in the game per current `docs/world/` scope.
- **Build**: per the §2 silhouette rules (Warrior bulky, Mage average build under robes, Ranger lean). Build is a silhouette differentiator, not a body-type prescription — the underlying skeleton is the same humanoid template.
- **Gender**: **not prescribed**. PixelLab's default for the `humanoid` body type with `mannequin` template is a hero archetype of indeterminate / androgynous gender, and that is the intended look. Faces are mostly hidden by helmet/hood/hat anyway, which makes the question moot for the world sprite. The class portrait (deferred to ART-SPEC-09) may go further in either direction; the world sprite stays neutral.

This neutrality is a deliberate fantasy-gameplay choice: the player projects themselves onto the silhouette. Pinning a gender narrows that projection without adding gameplay value. (If a future ticket wants gender-selectable variants, that's a new spec — not this one.)

---

### 4. Naming + Portrait Coupling

Per the `project_npc_naming` memory (NPCs are title-only in-game; PC is addressed as `{Class} Guildmaster`):

- **In-world dialogue addresses the PC by class title**: "Warrior Guildmaster," "Ranger Guildmaster," "Mage Guildmaster." Never by a personal name.
- **The world sprite carries no name label.** No floating nameplate, no ID text. The sprite is the identity.
- **The class portrait** (a future asset deferred to ART-SPEC-09 — not in scope for this spec) shows the **full archetype** of the class at higher detail (256×384 canvas per `prompt-templates.md` §1a). The world sprite is the **compact 128×128 version of the same archetype**: same silhouette, same colors, same defining features. A player who sees the portrait and then sees the world sprite must instantly recognize them as the same character.

The portrait/sprite coupling is what makes the locked single-sprite decision work: the portrait is the "full" expression of the class fantasy; the sprite is the in-world handle. Equipment changes show up in the paperdoll UI panel, which has its own slot icons — neither the portrait nor the world sprite needs to mutate when the player swaps gear.

---

### 5. Color-Coding Contract (shared rules, per-class accents below)

How player class sprites interact with [color-system.md](../systems/color-system.md):

- **PC sprites are NEVER modulated by the level-relative tint system.** That system exists to color-code monsters by player-vs-monster level gap (grey/blue/cyan/green/yellow/gold/orange/red). It does not apply to the player. The player's `Modulate` stays `Color.White` always.
- **Base tint surface**: full sprite, no exempt-pixel mechanism needed (since no tint is ever applied).
- **Player-blue accent contract**: each class carries a small amount of player-blue (`#8ed6ff` per `docs/assets/ui-theme.md`) as the visual signature that this is "the player." Per-class location of the accent is specified below in §6–8. The accent is **baked into the sprite pixels** (not a runtime modulate), so it survives forever.
- **Class-identity colors** (warrior steel + silver, ranger forest leather, mage robe color) are also baked into pixels and are part of the locked spec — no class-color customization at v1.

---

### 6. Worked Example — Warrior (full narrative depth)

This is the canonical reference instance of the §1 template. Ranger and Mage in §7–8 follow the same shape with less prose.

> **REVISED 2026-04-18 — lighter armor, not plate.** PO direction + reference image (sprite-sheet style, classic fighter archetype: tunic + hauberk + round shield + sword, no pauldrons or plate). Spec below updated accordingly. The accent-location rule unchanged — brick-red surcoat is still the class-color anchor.

**Fiction beat.** A frontier guild fighter trained in blade and shield, sent into the dungeon as the Guild's first-line combatant: the one expected to close the gap and trade hits where the lighter classes can't.

**Role in party fantasy.** Melee frontline — not a heavy tank, more a classic fighter. The classic ARPG "hold the line" archetype with the mobility to flank. High STR, high STA per `ClassSelect.cs:25` (STR 3 / STA 2). Rewards closing distance + shield-timing.

**Silhouette readability constraint.** From 8 tiles away, the Warrior must read as **a sword-and-shield fighter in a tunic + hauberk**. Specifically: visibly wider than a baseline humanoid silhouette (shield held at the side extends the horizontal bounding box), sword visible in the off-hand, head visible (bareheaded or simple circlet — NOT a horned full helm, NOT a hood, NOT a pointed hat). Rationale: the player is the one entity on screen the camera centers on; their identity must read first, even when the screen is busy with monsters and FX. The "widest class" axis from §2 still holds — the shield is what extends the bounding box, not pauldrons.

**Starter equipment (world sprite).** **Tunic over light hauberk or leather chest armor** (NOT plate cuirass, NOT steel pauldrons). Visible belt with pouches. Cloth leggings or leather breeches. Short boots. Right hand: one-handed steel longsword. Left hand: round wooden shield with iron rim (smaller than a plate-era tower shield). Bareheaded or simple circlet — face is VISIBLE (key differentiator from the prior horned-helm spec). The tunic itself carries the brick-red class accent.

**Color-coding contract.**
- Base tint surface: full sprite, never modulated (per §5).
- Class accent location: **the tunic / surcoat** over the hauberk — a visible `#b53238` brick-red garment covering the chest / waist area (larger surface area than the prior "small panel beneath plate"). Optional small brick-red shield-boss insignia if the artist has the pixels for it.
- Class-identity colors: warm leather brown (belt, shoulder straps), iron grey (hauberk, sword, shield rim), desaturated red-brown (tunic body), cream / neutral (linen underlayer). NO `#8ed6ff` player-blue anywhere (per SPEC-CLASS-COLOR-CODING-01 supersession in §5).

**Scale + anchor.** Scale multiplier = **1.00** (Warrior is the canonical reference scale; all other character entities — including monsters via `species-template.md` §6 and the other two PCs — are scaled relative to him). Z-offset = 0 (ground-walking). Canvas = 128×128 per `CHAR-HUM-ISO` in `prompt-templates.md` §1a. Sprite import offset `Vector2(0, -80)` per the same.

**Locked animation set (MVP):** per `feedback_mvp_animation_scope.md` — 3 animations only at MVP, weapons baked in, no equipped/unequipped variants.
- `walk` — 8-directional walk cycle. **The default PixelLab humanoid walk templates (`walking`, `walking-6-frames`) drop the shield + sword in the first half of the cycle** (observed 2026-04-18) — the mannequin template does not track held equipment during the back-swing arm phase. Custom `action_description` walk is required; see SPEC-PC-WALK-01 (if/when authored) or the in-flight dispatch 2026-04-18.
- `attack` — 8-directional. **SUPERSEDED by SPEC-PC-ATK-01 ([`docs/ui/class-attack-animations.md §3`](../ui/class-attack-animations.md))** — locked as 4-frame downward cleave, sword raised overhead then swung straight down, hit-tick on frame 2. Custom `action_description` path. This replaces the prior "downward overhead sword slash (map to `slash` or `hammer-down` template)" line — the template path was rejected across all three classes in the attack spec.
- `death` — south-facing only.
- `idle_combat`, running, hit-react, etc. deferred to v1.

**Build / age / gender beat.** Per §3: 20s–40s, broad-shouldered build (the silhouette differentiator), **bareheaded or simple circlet** per the 2026-04-18 REVISED banner at the top of this section — face is visible (NOT hidden by a helmet; the prior "hero-archetype face mostly hidden by the helmet anyway" line was stale against the tunic + hauberk direction). Adult male proportions match the canonical human reference (this sprite IS the reference).

**Naming coupling.** Addressed in-game as **"Warrior Guildmaster."** No personal name. World sprite carries no name label.

---

### 7. Worked Example — Ranger (every field filled)

> **REVISED 2026-04-18** after theme-review generation. Three accepted changes: (1) bow slung on back / shoulder rather than held vertically in-hand — PixelLab's mannequin template defaults this at 92×92 and the archer silhouette still reads cleanly via the quiver + fletching; (2) inner-hood-lining forest-green strip didn't render as a distinct stripe — instead, the whole hood exterior reads as forest-green, which satisfies SPEC-CLASS-COLOR-CODING-01's "accent renders on the class identity surface"; (3) **PO locked the Ranger as a woman (v6 theme-review sprite, file `assets/characters/player/ranger/_theme-review/south.png`).** Proportions use the canonical human recipe (`realistic_male` preset with feminine-presentation description — the preset name is a PixelLab proportion label, not a gender prescription).

- **Fiction beat.** A frontier scout who hunted in the woods around the settlement long before the dungeon was found, now drawn into it as the Guild's eyes and ranged blade.
- **Role in party fantasy.** Agile DPS at range. High DEX per `ClassSelect.cs:28` (DEX 3, STR 1). Rewards positioning and clean line-of-sight, punished for getting cornered.
- **Silhouette readability constraint.** From 8 tiles away, the Ranger must read as **a hooded lean archer with a visible quiver and bow**. Bow placement can be on-back or in-hand — both read as "archer" at 92×92 and PixelLab's mannequin template tends toward on-back at small scale. The quiver + arrow fletching over the shoulder is the load-bearing identity cue.
- **Starter equipment (world sprite).** Leather jerkin + leather bracers + soft hood drawn up + cloth leggings + soft boots. Shortbow carried either held at the side OR slung on the back/shoulder (art-lead's call; PixelLab's default at 92×92 is on-back). Quiver visible on the back with arrow fletching at the shoulder — this is the key "archer" silhouette cue, not the bow itself.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Class accent location: **the hood exterior** is the primary forest-green (`#3a7a4d`) surface. Optional forest-green inner-lining strip at the face opening + arrow fletching if pixel budget allows, but the exterior alone satisfies SPEC-CLASS-COLOR-CODING-01 identity requirements.
  - Class-identity colors: leather brown jerkin (palette-clamp warm browns), forest-green hood (`#3a7a4d` per SPEC-CLASS-COLOR-CODING-01 — a desaturated muted green that fits the "gritty dungeon" palette, not bright spring green), darker brown boots and bracers. NO `#8ed6ff` player-blue anywhere.
- **Scale + anchor.** Scale multiplier = **1.00** (same as Warrior — the silhouette differentiation is via shape, not size). Z-offset = 0. Canvas 128×128. Sprite import offset `Vector2(0, -80)`.
- **Locked animation set (MVP):** per `feedback_mvp_animation_scope.md` — 3 animations only at MVP, weapons baked in, no equipped/unequipped variants.
  - `walk` — 8-directional walk cycle.
  - `attack` — 8-directional. **SUPERSEDED by SPEC-PC-ATK-01 ([`docs/ui/class-attack-animations.md §4`](../ui/class-attack-animations.md))** — locked as single-stage bow pull-and-release, 4 frames, arrow projectile spawns at frame 2 (release). Custom `action_description` path.
  - `death` — south-facing only.
  - `idle_combat`, running, hit-react, etc. deferred to v1.
- **Build / age / gender beat.** Per §3: 20s–40s, **a woman** (PO direction 2026-04-18), **lean** build (the silhouette differentiator), face partially shadowed by the hood. Proportions match the Warrior v2 canonical reference (adult human ratio, not chibi).
- **Naming coupling.** Addressed in-game as **"Ranger Guildmaster."** No personal name. World sprite carries no name label.

---

### 8. Worked Example — Mage (every field filled)

> **REVISED 2026-04-18** after theme-review generation. Three accepted changes: (1) **PO locked the Mage as a Black man (v4 theme-review sprite, file `assets/characters/player/mage/_theme-review/south.png`, PixelLab character ID `4703290e-3779-40d3-b101-50ee8cf4fd3c`).** (2) Robe color shifted from deep blue-gray `#24314a` to neutral warm chocolate/walnut brown (keeps the muted-dungeon palette, reads as earthy-scholar rather than arcane-clichéd, and isolates the royal-violet accent to a single pixel cluster on the gem). (3) Class accent moved from `#8ed6ff` player-blue to royal-violet `#5b47a0` per SPEC-CLASS-COLOR-CODING-01 — the staff-gem at the top of the staff is now the sole violet surface on the sprite. The hat-tip extension above the head silhouette (12–16 px) remains locked and was rendered as expected.

- **Fiction beat.** A frontier scholar who studied the magicule-warped strangeness of the dungeon from the outside before deciding to walk into it; carries a staff that channels what the scholarship can't yet explain.
- **Role in party fantasy.** Squishy caster. High INT per `ClassSelect.cs:32` (INT 3). Rewards range and timing; punished by direct contact with anything dangerous.
- **Silhouette readability constraint.** From 8 tiles away, the Mage must read as **a robed figure with a pointed hat and a staff held vertically**. Specifically: pointed wizard-hat tip extends above the head silhouette by 12–16 px (this is the locked tallest-class differentiator from §2), staff held vertically along the right side of the body with the staff-head reaching near hat-tip height, robes flare outward at the bottom (wider hem than torso) so the silhouette tapers downward from the hat-tip. Rationale: the pointed hat and staff combination is the universal ARPG caster shorthand; deviating from it costs more in instant readability than it gains in originality.
- **Starter equipment (world sprite).** Long hooded robes that brush the ground (full-length, not knee-length) + pointed wizard hat (the iconic cone, not a soft hood) worn over the hood + leather belt + visible spellbook or pouch on the belt. Right hand: tall wooden staff with a carved head (a small crystal, orb, or stylized knot at the top — palette-neutral, not glowing). Left hand: empty / casting-ready.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Class accent location: **the staff-gem at the top of the staff** is the primary royal-violet (`#5b47a0`) surface per SPEC-CLASS-COLOR-CODING-01. The gem sits at the silhouette's literal apex for maximum visibility. No other violet on the sprite; no `#8ed6ff` player-blue on the sprite.
  - Class-identity colors: neutral warm chocolate/walnut brown robe body (palette-clamp warm browns, fitting the muted dungeon palette and reading as earthy-scholar rather than arcane-cliché), darker warm-brown hat, weathered wood for the staff. The only violet on the entire sprite is the gem cluster.
- **Scale + anchor.** Scale multiplier = **1.00** (the hat extends the visible silhouette upward but the underlying scale is unchanged — the canvas just uses more of its vertical pixels). Z-offset = 0. Canvas 128×128. Sprite import offset `Vector2(0, -80)`. **Note for art-lead**: the hat tip approaches but must NOT exceed the canvas top edge — leave 4–6 px clearance so the silhouette is not clipped and so future hat-related VFX have room.
- **Locked animation set (MVP):** per `feedback_mvp_animation_scope.md` — 3 animations only at MVP, weapons baked in, no equipped/unequipped variants.
  - `walk` — 8-directional walk cycle. Robes sway slightly with stride (artist's discretion within the PixelLab template).
  - `attack` — 8-directional. **SUPERSEDED by SPEC-PC-ATK-01 ([`docs/ui/class-attack-animations.md §5`](../ui/class-attack-animations.md))** — locked as 5-frame forward staff swing with gem flare, projectile spawns at frame 3 (peak flare). Custom `action_description` path.
  - `death` — south-facing only.
  - `idle_combat`, running, hit-react, etc. deferred to v1.
- **Build / age / gender beat.** Per §3: 20s–40s, **a Black man** (PO direction 2026-04-18), build hidden under robes (no silhouette signal from build — the hat does the work), face shadowed by the hat brim. Proportions match the Warrior v2 canonical reference (adult human ratio, not chibi).
- **Naming coupling.** Addressed in-game as **"Mage Guildmaster."** No personal name. World sprite carries no name label.

---

### 9. Art-Spec Pairing

Paired art ticket: **ART-SPEC-PC-01** ([docs/assets/player-class-pipeline.md](../assets/player-class-pipeline.md)) — the generation-side half (PixelLab prompts, palette implementation, batch plan, manifest update for the three class sprites).

Pairing status: `[ ] design spec locked  [ ] art spec locked  [ ] sprites delivered`

A class's sprite reaches **"Ready-for-impl"** only when:
1. This design spec's per-class fields (§6 / §7 / §8) have no TBDs (all already filled — see Acceptance Criteria).
2. The paired art-pipeline spec is also locked.
3. This spec's Open Questions section is empty (it is — see below).

Neither half ships without the other. If a class's design fields change (e.g. a new starter weapon is decided), the art spec re-renders. If the art spec discovers a generation constraint that breaks a design field (e.g. PixelLab cannot produce a hat-tip extension that survives at thumbnail scale), this spec edits the affected field and re-locks.

## Acceptance Criteria

- [ ] An art-lead reading this spec plus the paired [ART-SPEC-PC-01](../assets/player-class-pipeline.md) can author the Warrior, Ranger, and Mage PixelLab prompts without asking design-lead a clarifying question.
- [ ] Every field in the §1 template has a concrete value for all three classes (no TBDs in §6 / §7 / §8). Verifiable by a row-by-row reviewer.
- [ ] The three classes' silhouettes remain distinguishable at 64×64 thumbnail scale per the §2 Acceptance Test (export south-facing idle, alpha-mask to silhouette, name correctly). If the first art batch fails this test, regenerate before locking the art spec.
- [ ] No class's color-coding contract requires the player sprite to be runtime-modulated. (PCs are exempt from `color-system.md` per §5.)
- [ ] The locked single-sprite-per-class decision is reflected in this spec; no §1 row mentions per-armor or per-weapon variants.
- [ ] The PC is referred to in any future spec or UI string as `{Class} Guildmaster`, never by personal name (per `project_npc_naming` memory and §4).

## Implementation Notes

- **Pairing with [ART-SPEC-PC-01](../assets/player-class-pipeline.md) is load-bearing.** The two specs are a single logical unit. Any change to fields in §6 / §7 / §8 (starter equipment, silhouette constraint, color-accent location, animation set) triggers a regeneration cycle on the art side.
- **Equipment-on-paperdoll, not on sprite.** The locked PO decision (2026-04-17) means the inventory UI is now the sole surface where equipment changes are visible to the player. Spec-side, this means [docs/inventory/](../inventory/) and any UI spec for the paperdoll panel must show equipped items at full fidelity; no upgrade path exists for sprite-side equipment until and unless this spec is reopened.
- **Scale = 1.00 is the canonical reference.** Monsters are scaled in `species-template.md` §6 relative to this 1.00. If a future ticket changes the player canvas size, every monster scale multiplier revalidates against it. (Currently safe: 128×128 player canvas is locked in `prompt-templates.md` `CHAR-HUM-ISO`.)
- **Animation set v1 is intentionally minimal.** Walk + combat-idle + attack are the three animations the live combat loop actually consumes. Hit-react, death, victory pose, town-idle (parade-rest) are deferred to a later animation-pass ticket — they are nice-to-have, not blockers. Authoring all of them now would burn PixelLab credits on frames the engine doesn't yet read.
- **Player-blue accent placement is the one easy win for class identity.** Each class's accent is in a different anatomical zone (Warrior surcoat / Ranger hood-lining / Mage staff-head) so even at thumbnail scale the eye picks up "the player blue is here = this is my class." Art-lead should treat the accent location as a hard pixel placement, not a soft suggestion.
- **Mage hat-tip clearance.** The pointed hat is the silhouette differentiator and also the canvas-edge risk. Leave 4–6 px clearance from the canvas top per §8. If PixelLab consistently clips the hat, the art spec may move to a 128×144 canvas for the Mage only — but this requires a sprite-anchor audit because the standard offset assumes 128×128.
- **No glow / particle FX baked into the sprite** except the small player-blue charge cue on the Mage attack frames per §8. Runtime FX (hit sparks, projectile trails, ability VFX) belong to the engine FX layer, not the sprite atlas. This keeps the sprite re-usable across animation states.

## Open Questions

None — spec is locked. The product-owner decision on single-default-sprite (2026-04-17) closed the only outstanding tradeoff. Per-class fields are all concrete. Art-pairing checkbox status is the only remaining state, and that resolves when ART-SPEC-PC-01 locks.

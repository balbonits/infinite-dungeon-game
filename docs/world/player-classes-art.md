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

> **SUPERSESSION NOTICE (2026-04-18):** the "single player-blue accent for all three classes" rule below is **superseded by [SPEC-CLASS-COLOR-CODING-01](../ui/class-color-coding.md)**. Each class now has its own accent color: **Warrior brick red `#b53238`, Ranger forest green `#3a7a4d`, Mage royal violet `#5b47a0`**. The accent-location guidance in §6–8 below still applies — only the hex value per class changes. The exempt-pixel mechanism + "baked into sprite pixels" rule are unchanged. Art redraw (Bucket A) must use the per-class hex values.

How player class sprites interact with [color-system.md](../systems/color-system.md):

- **PC sprites are NEVER modulated by the level-relative tint system.** That system exists to color-code monsters by player-vs-monster level gap (grey/blue/cyan/green/yellow/gold/orange/red). It does not apply to the player. The player's `Modulate` stays `Color.White` always.
- **Base tint surface**: full sprite, no exempt-pixel mechanism needed (since no tint is ever applied).
- **Per-class accent contract (supersession noted above)**: each class carries a small amount of its **class color** (Warrior `#b53238`, Ranger `#3a7a4d`, Mage `#5b47a0`) as the visual signature that this is "the player." Per-class location of the accent is specified below in §6–8 (locations unchanged; only the hex values shift from the prior `#8ed6ff` player-blue). The accent is **baked into the sprite pixels** (not a runtime modulate), so it survives forever.
- **Class-identity colors** (warrior steel + silver, ranger forest leather, mage robe color) are also baked into pixels and are part of the locked spec — no class-color customization at v1.

---

### 6. Worked Example — Warrior (full narrative depth)

This is the canonical reference instance of the §1 template. Ranger and Mage in §7–8 follow the same shape with less prose.

**Fiction beat.** A frontier guild fighter trained in plate and shield, sent into the dungeon as the Guild's first-line heavy: the one expected to absorb the hits so the lighter classes can land theirs.

**Role in party fantasy.** Tanky melee. The classic ARPG "stand in front" archetype. High STR, high STA per `ClassSelect.cs:25` (STR 3 / STA 2). Rewards trading hits and closing distance.

**Silhouette readability constraint.** From 8 tiles away, the Warrior must read as **a bulky armored mass with a horned helmet**. Specifically: visibly wider than a baseline humanoid silhouette (shield + pauldrons account for the extra width), helmet outline shows two upward horns or a single visible crest above the headline, no skin visible (face is fully covered by the helmet visor). Rationale: the player is the one entity on screen the camera centers on; their identity must read first, even when the screen is busy with monsters and FX.

**Starter equipment (world sprite).** Plate cuirass + steel pauldrons + greaves + horned full helm covering the face. Right hand: one-handed steel longsword. Left hand: round metal shield with iron rim. Surcoat (cloth tabard) underneath the plate carries the player-blue accent.

**Color-coding contract.**
- Base tint surface: full sprite, never modulated (per §5).
- Player-blue accent location: **the surcoat / tabard** beneath the plate (a small visible panel of `#8ed6ff` cloth between the cuirass and the belt, plus an optional small shield-boss insignia if the artist has the pixels for it).
- Class-identity colors: dark steel plate (palette-clamp `#3c4664` deep blue-gray), silver trim on armor edges, leather straps in palette-clamp brown.

**Scale + anchor.** Scale multiplier = **1.00** (Warrior is the canonical reference scale; all other character entities — including monsters via `species-template.md` §6 and the other two PCs — are scaled relative to him). Z-offset = 0 (ground-walking). Canvas = 128×128 per `CHAR-HUM-ISO` in `prompt-templates.md` §1a. Sprite import offset `Vector2(0, -80)` per the same.

**Locked animation set (v1):**
- `walk` — 8-directional walk cycle (the standard PixelLab humanoid walk template).
- `idle_combat` — fight-stance idle: knees slightly bent, shield raised to chest height, sword angled forward. Not the default neutral parade-rest — this is a "ready to swing" pose because the Warrior is in a dungeon, not a town square.
- `attack` — **downward overhead sword slash**. The signature Warrior swing: sword raised over the head, brought down in front of the body, ending in a low follow-through. Reads as "heavy committed strike" not "quick jab." Maps to PixelLab humanoid template animation: use the closest "slash" or "hammer-down" preset; if none is a clean fit, generate a custom 4-frame action via `animate_with_skeleton`.

(Idle-out-of-combat / death / hit-react are deferred to a later ticket — v1 ships walk + combat-idle + attack only, sufficient for the live combat loop.)

**Build / age / gender beat.** Per §3: 20s–40s, broad-shouldered build (the silhouette differentiator), gender-neutral / hero-archetype face (mostly hidden by the helmet anyway).

**Naming coupling.** Addressed in-game as **"Warrior Guildmaster."** No personal name. World sprite carries no name label.

---

### 7. Worked Example — Ranger (every field filled)

- **Fiction beat.** A frontier scout who hunted in the woods around the settlement long before the dungeon was found, now drawn into it as the Guild's eyes and ranged blade.
- **Role in party fantasy.** Agile DPS at range. High DEX per `ClassSelect.cs:28` (DEX 3, STR 1). Rewards positioning and clean line-of-sight, punished for getting cornered.
- **Silhouette readability constraint.** From 8 tiles away, the Ranger must read as **a hooded lean archer with a bow silhouette held close to the body**. Specifically: drawn-up hood with a visible point or fold above the head (but lower than the Mage's hat), bow held vertically along the side of the body (not extended outward in idle), no shoulder armor — the silhouette is markedly narrower than the Warrior's. Rationale: the bow is the one piece of equipment that visually differentiates this class from any future light-armor melee class; it must be visible in idle, not just when attacking.
- **Starter equipment (world sprite).** Leather jerkin + leather bracers + soft hood drawn up + cloth leggings + soft boots. Right hand: shortbow held vertically along the right side of the body, string facing forward. Left hand: empty (free for an arrow draw on attack frame). Quiver visible on the back as a strap detail (not a full back-pack to keep the silhouette narrow).
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Player-blue accent location: **the inner lining of the hood** (visible as a small `#8ed6ff` strip where the hood meets the face), plus an optional player-blue arrow-fletching color on the equipped quiver.
  - Class-identity colors: leather brown jerkin (palette-clamp warm browns), forest-green hood (a desaturated muted green that fits the "gritty dungeon" palette — not bright spring green), darker brown boots and bracers.
- **Scale + anchor.** Scale multiplier = **1.00** (same as Warrior — the silhouette differentiation is via shape, not size). Z-offset = 0. Canvas 128×128. Sprite import offset `Vector2(0, -80)`.
- **Locked animation set (v1):**
  - `walk` — 8-directional walk cycle.
  - `idle_combat` — fight stance: bow raised slightly, weight on back foot, ready to draw.
  - `attack` — **bow draw + release**. Two-stage: frame 1–2 draws the bowstring back, frame 3–4 releases. The arrow projectile spawns at release frame. Maps to PixelLab humanoid template "bow" or "ranged" preset; custom skeleton animation if no preset fits.
- **Build / age / gender beat.** Per §3: 20s–40s, **lean** build (the silhouette differentiator), gender-neutral / hero-archetype face (partially shadowed by the hood).
- **Naming coupling.** Addressed in-game as **"Ranger Guildmaster."** No personal name. World sprite carries no name label.

---

### 8. Worked Example — Mage (every field filled)

- **Fiction beat.** A frontier scholar who studied the magicule-warped strangeness of the dungeon from the outside before deciding to walk into it; carries a staff that channels what the scholarship can't yet explain.
- **Role in party fantasy.** Squishy caster. High INT per `ClassSelect.cs:32` (INT 3). Rewards range and timing; punished by direct contact with anything dangerous.
- **Silhouette readability constraint.** From 8 tiles away, the Mage must read as **a robed figure with a pointed hat and a staff held vertically**. Specifically: pointed wizard-hat tip extends above the head silhouette by 12–16 px (this is the locked tallest-class differentiator from §2), staff held vertically along the right side of the body with the staff-head reaching near hat-tip height, robes flare outward at the bottom (wider hem than torso) so the silhouette tapers downward from the hat-tip. Rationale: the pointed hat and staff combination is the universal ARPG caster shorthand; deviating from it costs more in instant readability than it gains in originality.
- **Starter equipment (world sprite).** Long hooded robes that brush the ground (full-length, not knee-length) + pointed wizard hat (the iconic cone, not a soft hood) worn over the hood + leather belt + visible spellbook or pouch on the belt. Right hand: tall wooden staff with a carved head (a small crystal, orb, or stylized knot at the top — palette-neutral, not glowing). Left hand: empty / casting-ready.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Player-blue accent location: **the staff-head** (a small `#8ed6ff` crystal or carved facet at the top of the staff — this is the most visually prominent player-blue spot of any class because the staff-head sits at the silhouette's literal apex), plus an optional player-blue trim on the robe hem.
  - Class-identity colors: deep robe color (palette-clamp deep blue-gray `#24314a` for the robe body, fitting the muted dungeon palette and avoiding bright wizard-purple cliché), darker hat, weathered wood for the staff.
- **Scale + anchor.** Scale multiplier = **1.00** (the hat extends the visible silhouette upward but the underlying scale is unchanged — the canvas just uses more of its vertical pixels). Z-offset = 0. Canvas 128×128. Sprite import offset `Vector2(0, -80)`. **Note for art-lead**: the hat tip approaches but must NOT exceed the canvas top edge — leave 4–6 px clearance so the silhouette is not clipped and so future hat-related VFX have room.
- **Locked animation set (v1):**
  - `walk` — 8-directional walk cycle. Robes sway slightly with stride (artist's discretion within the PixelLab template).
  - `idle_combat` — staff planted, slight forward lean, weight on the staff. Ready stance.
  - `attack` — **staff overhead cast** culminating in a magic bolt projectile spawning forward. Two-stage: frame 1–2 raises the staff overhead with a charge cue (artist may add a small player-blue glow at the staff-head as part of the bake — this glow is part of the animation, not a runtime FX), frame 3–4 brings the staff forward and the projectile spawns at release. Maps to PixelLab humanoid template "cast" or "magic" preset; custom skeleton animation if no preset fits.
- **Build / age / gender beat.** Per §3: 20s–40s, build hidden under robes (no silhouette signal from build — the hat does the work), gender-neutral / hero-archetype face (shadowed by the hat brim).
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

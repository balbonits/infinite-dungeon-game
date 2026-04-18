# Species Sprite Pipeline (ART-SPEC-02)

## Summary

Generation-side half of SPEC-SPECIES-01. Binds every field in `docs/world/species-template.md` to concrete PixelLab params, prompt tokens, and engine-side `scale` / `offset.y` numbers. This spec does not redefine the preamble, palette clause, or negative prompts — those live in [ART-SPEC-01](prompt-templates.md) §1 / §1c and every prompt here inherits them verbatim. Every species spec cites one `CHAR-MON-ISO` sub-variant (`.quad` / `.wing` / `.arach` / `.biped-mon`) from [ART-SPEC-01 §2 Block `CHAR-MON-ISO`](prompt-templates.md#block-char-mon-iso--monster--creature-non-humanoid-body-plans); this doc fills in what each sub-variant demands.

Paired with [SPEC-SPECIES-01](../world/species-template.md). Neither locks alone — see §10.

## Current State

Seven species ship today (`bat`, `dark_mage`, `goblin`, `orc`, `skeleton`, `spider`, `wolf` under `assets/characters/enemies/`) — all authored pre-template, most with the wrong perspective per ART-SPEC-01 §Current State. ART-14 (Bat / Spider / Wolf re-art) is the first batch to consume this pipeline; future species in P2 + the existing four untouched (goblin, orc, skeleton, dark_mage) retrofit into it opportunistically.

**Design-half status:** all seven species now have filled, locked `SPEC-SPECIES-*` design specs under `docs/world/species/`:

| Species | Design spec | Paired art ticket |
|---------|-------------|-------------------|
| Bat | [bat.md](../world/species/bat.md) | ART-14 |
| Skeleton | [skeleton.md](../world/species/skeleton.md) | ART-SKELETON (placeholder) |
| Wolf | [wolf.md](../world/species/wolf.md) | ART-14 |
| Spider | [spider.md](../world/species/spider.md) | ART-14 |
| Dark Mage | [darkmage.md](../world/species/darkmage.md) | ART-DARKMAGE (placeholder) |
| Orc | [orc.md](../world/species/orc.md) | ART-ORC (placeholder) |
| Goblin | [goblin.md](../world/species/goblin.md) | ART-GOBLIN (placeholder) |

ART-14 consumes the Bat/Spider/Wolf design specs directly — no longer blocked on design-half work. Future non-ART-14 redraws use the other four placeholders as they're authored.

## Design

### 1. Sub-Variant Trigger Table (AI pattern × body plan → block slot)

SPEC-SPECIES-01 §3 locks five AI patterns. Each has a recommended body plan and a default scale band. **Exceptions allowed** when §5 silhouette-readability forces a different body plan (e.g. a `melee-chase` species authored as `.wing` because "airborne" is the load-bearing silhouette cue — the Bat).

| AI pattern | Recommended body plan | Sub-variant | Scale-band default | Notes |
|---|---|---|---|---|
| `melee-chase` | Bipedal humanoid or quadruped | `.biped-mon` or `.quad` | Standard | Skeleton / Goblin = `.biped-mon`; Wolf = `.quad`. |
| `ranged-kite` | Bipedal humanoid (holds projectile weapon) | `.biped-mon` | Standard | Goblin Archer archetype. |
| `ambush` | Arachnid or winged (low/hidden silhouette) | `.arach` or `.wing` | Small | Spider = `.arach`; future gargoyle ambusher = `.wing`. |
| `pack` | Small airborne or small quadruped | `.wing` (4-dir, see §4) or `.quad` | Small | Bat swarm = `.wing`; rat pack = `.quad`. |
| `caster` | Bipedal humanoid (robe + staff) | `.biped-mon` | Standard or Large | Dark Mage = `.biped-mon` Standard; boss lich = Large. |

Override rule: if §5 silhouette readability names "airborne" or "8 legs" as the binding cue, body plan follows the cue, not the table.

### 2. Slot Table Per Body Plan

All slots inherit ART-SPEC-01 §1a mandatory params (`view="low top-down"`, `outline="single color black outline"`, preamble + palette + negative-prompt clauses verbatim). Values below are slot-specific overrides/additions.

| Param | `.quad` | `.wing` | `.arach` | `.biped-mon` |
|---|---|---|---|---|
| `body_type` | `quadruped` | `humanoid` | `quadruped` | `humanoid` |
| `template` | `wolf` / `bear` / `cat` / `dog` / `horse` / `lion` (nearest match) | `mannequin` | `cat` (low-slung scaffold only; prompt overrides to 8 legs) | `mannequin` |
| `size` | 64 | 64 | 64 | 64 |
| `n_directions` | 8 | 8 (4 only for airborne Small `pack` — see §4) | 8 | 8 |
| `shading` | `medium` (Standard) / `detailed` (Boss) | same | same | same |
| `detail` | `medium` (Standard) / `high` (Boss) | same | same | same |
| Body-plan tokens (inserted in the `[BODY-PLAN]` and `[POSE]` slots of the `CHAR-MON-ISO` skeleton) | `"four legs, low-slung body, fur texture, [tail cue]"` | `"airborne, leathery wings spread wide, body not touching the ground, hovering mid-flight"` | `"eight segmented legs, bulbous body, spider-like articulation, scuttling low to the ground"` | `"bipedal, two legs, upright humanoid stance, [weapon held in hand]"` |
| Silhouette-readability cue (pick one per species, see SPEC-SPECIES-01 §5) | `"forward-leaning"`, `"powerful haunches"`, `"low slung"` | `"wings spread wide"`, `"hovering"`, `"dropping from above"` | `"8 legs splayed"`, `"low carapace"`, `"compact clustered body"` | `"hulking"`, `"lean"`, `"staff raised"`, `"robe trailing"` |

### 3. 8-Direction Coverage Rules

Default = **8 directions, all species, no exceptions by default.** The engine's `DirectionalSprite.cs` picker maps 8 visual rotations (see ART-SPEC-01 §1a table) to sprite frames; a 4-dir sheet breaks the contract and forces ugly nearest-neighbor direction snap.

**Sole carve-out:** a species may ship as **4 directions + single hover-frame idle** if and only if all three conditions hold:
1. Body plan is `.wing` (airborne).
2. Scale band is Small.
3. SPEC-SPECIES-01 §1 intended reaction is `pack-management` (swarm-feel mobs — the Bat is the canonical case).

Anything outside that carve-out = 8-dir mandatory. Boss-tier = 8-dir always, regardless of body plan or reaction.

### 4. Animation Set Requirements

Minimum viable set per species (template animation IDs from PixelLab's humanoid template catalog, consumed via `animate_character`):

| AI pattern | Minimum animations | Template IDs |
|---|---|---|
| `melee-chase` | walk + idle + melee attack | `walking`, `fight-stance-idle-8-frames`, `cross-punch` (or `high-kick` / `roundhouse-kick` for kickers) |
| `ranged-kite` | walk + idle + ranged attack | `walking`, `fight-stance-idle-8-frames`, `fireball` (projectile throw) |
| `ambush` | walk + idle + burst attack | `walking`, `breathing-idle`, `surprise-uppercut` or `flying-kick` |
| `pack` | walk + idle (individual attack optional — pack volume is the threat) | `walking`, `breathing-idle` |
| `caster` | walk + idle + cast | `walking`, `fight-stance-idle-8-frames`, `fireball` |

**Boss tier** adds `falling-back-death` on top of the AI-pattern minimum, so first-kill cutscenes and dramatic death beats have a frame set to play.

Quadruped template animations differ per PixelLab animal template — `get_character()` reveals the per-template animation list once the character is generated. Use the nearest semantic match (e.g. wolf `walk` + `run` + `attack`).

### 5. Palette-Clamp vs. Per-Species Tint

Tension: ART-SPEC-01 §1c clamps every baked pixel to the dungeon palette (`#24314a`, `#3c4664`, `#f5c86b`, `#ff6f6f`) so Godot `Modulate` tints work cleanly across level gaps. SPEC-SPECIES-01 §7 says some species need identity-color pixels (Dark Mage purple glow, Bat yellow-gold eyes) that *must* stay unmodulated.

**Resolution rule.** Three layers:

1. **Body pixels — always palette-clamped.** No species-identity color baked on general body surfaces. The body eats runtime `Modulate` and color-shifts with level gap, as intended by the color system.
2. **Exempt pixels — allowed off-palette, but always on a separate sprite sub-node with `modulate = Color.White`.** Named in SPEC-SPECIES-01 §7 exempt list. These are identity beats (eye gleam, elemental core, rune glow). Implementation is scene-side: a child `Sprite2D` sits atop the tinted body sprite and is explicitly excluded from level-gap tinting.
3. **Whole-species chromatic shifts (e.g. fire variant of an existing species) — runtime `Modulate` only, never baked.** A fire wolf and an ice wolf share one baked sprite + two `Modulate` colors.

**Bat worked example.** Body baked in clamped dungeon-blue tones (`#3c4664` leather wings, `#24314a` underbelly shadow). Two eye-highlight pixels baked in `#f5c86b` yellow-gold, placed on a child `Sprite2D` with `modulate = Color.White` so the gleam survives any level-gap tint. At a +10 level gap (red tint) the body reads red; the eyes still read gold — species identity preserved.

### 6. Scale Multiplier + Z-Offset Mapping (Godot-side numbers)

Default/example `scale` baselines per SPEC-SPECIES-01 §6 band. **The authoritative multiplier for each species is the per-species value locked in that species spec's §6, not the table below.** Use this table as a starting point for new species; existing species override per their locked specs.

| Band | Default `scale` | Per-species locked values | Hitbox radius (px) |
|---|---|---|---|
| Small | **0.70** default | Bat 0.70× / Spider 0.75× / Goblin 0.80× | 8 |
| Standard | **1.00** default | Skeleton 1.00× / Dark Mage 1.00× | 12 |
| Large | **1.30** default | Wolf 1.25× / Orc 1.40× | 16 |
| Boss | **2.00** default | Floor bosses (see per-boss specs under `docs/world/bosses/`); 1.8×–2.2× in current roster | 24 |

Implementers set the final value on the species root `Node2D` using the per-species locked number. The band defaults above are authoring guidance only.

**Z-offset (airborne only).** Per SPEC-SPECIES-01 §6 and [iso-rendering.md](../systems/iso-rendering.md). This doc uses Godot screen-space `Sprite2D.position.y` / `offset.y` sign convention: **negative values lift the visual upward, positive values push it downward**.

- Ground species (`.quad`, `.arach`, `.biped-mon`): z-offset = **0 px**.
- Airborne species (`.wing`): z-offset = **-28 px** applied to the sprite's `position.y` (matches `SpeciesDatabase.SpriteOffsetY = -28f` for Bat in the current code; not applied to the iso anchor — the iso anchor math stays per ART-SPEC-01 §1a; the `-28` is a visual hover lift on top).

A child shadow decal `Sprite2D` at the unmodified anchor point (0 offset) preserves the correct cell footprint for iso Y-sort and collision — the hovering body visually floats above its shadow because the body sprite is lifted by a negative Y offset, but the entity still occupies and sorts from its ground cell.

**Sign-convention guardrail:** if a future refactor changes this convention (e.g., to store z-offset as a positive "height above ground" and invert at render time), update this note AND `SpeciesDatabase.SpriteOffsetY` at the same time so spec and code stay aligned.

### 7. Handoff Checklist (per-species redraw ticket)

Every species redraw ticket (ART-14 Bat/Spider/Wolf + every future species) is authored by filling these eight items. No new prompt engineering per species.

```
Handoff — Species Redraw

1. Species name:               ______________________________
2. Paired design ticket:       SPEC-SPECIES-____________-01
3. AI pattern (§3 of design):  [melee-chase | ranged-kite | ambush | pack | caster]
4. Body plan / sub-variant:    [CHAR-MON-ISO.quad | .wing | .arach | .biped-mon]
5. Scale band + scale value:   [Small 0.70 | Standard 1.00 | Large 1.30 | Boss 2.00]
6. Exempt-pixel list:          (from design spec §7 — e.g. "eye gleam, staff head")
7. Animation set:              walking + fight-stance-idle + ___________  (+ falling-back-death if boss)
8. Expected PixelLab duration: ~3 min character gen + ~4 min per animation
9. Download target path:       assets/characters/enemies/<species>/
```

Implementer confirms item 2 design spec is locked before submitting a PixelLab job.

## Acceptance Criteria

- [ ] Every existing species folder under `assets/characters/enemies/` (`bat`, `skeleton`, `goblin`, `wolf`, `orc`, `spider`, `dark_mage`) can be authored by filling the §7 handoff checklist with zero new prompt engineering beyond this spec and ART-SPEC-01.
- [ ] The §8 Bat worked example, if pasted into `create_character` today with the §2 `.wing` param overrides, produces a sprite that satisfies SPEC-SPECIES-BAT-01 §5 (reads as airborne from 8 tiles away) and the ART-14 "true winged flyer, not bipedal" constraint.
- [ ] Zero IP-violating terms in any prompt in this spec (no licensed game / studio / franchise-character names). Per ART-SPEC-01 §11 rule 6 — a named IP is a spec bug.
- [ ] Sub-variant trigger table (§1) covers all five SPEC-SPECIES-01 AI patterns.
- [ ] Slot table (§2) covers all four `CHAR-MON-ISO` sub-variants.
- [ ] 8-direction coverage carve-out (§3) is unambiguous — three conjunctive conditions, else 8-dir mandatory.
- [ ] Animation set (§4) maps every AI pattern to at least one concrete PixelLab template-animation ID from the known humanoid catalog (`walking`, `fight-stance-idle-8-frames`, `cross-punch`, `fireball`, `surprise-uppercut`, `flying-kick`, `breathing-idle`, `falling-back-death` — all present in the `animate_character` tool's documented ID set).
- [ ] Scale + z-offset numbers (§6) are copy-paste-ready — `scale = 0.70` etc., with the +28 hover offset documented for `.wing`.
- [ ] Handoff checklist (§7) has exactly 8–9 items that together specify a complete generation job.
- [ ] Pairing with SPEC-SPECIES-01 is called out in Implementation Notes; this spec is not shippable without the design half and vice versa.

## Implementation Notes

- **Pairing with SPEC-SPECIES-01 is load-bearing.** Co-lock criterion: both specs move to "Ready-for-impl" together. SPEC-SPECIES-01 (design side) is already locked per today's tag-team; this spec completes the pair. A species redraw ticket with only a design spec, or only an art spec, is not lockable.
- **Inheritance, not redefinition.** This spec inherits ART-SPEC-01 §1 preamble, §1c palette clause, and universal negative-prompt tokens verbatim. If any of those drift, fix them in ART-SPEC-01 and this spec updates by reference, not by copy.
- **Block `CHAR-MON-ISO` is the durable handle.** Every species ticket cites this block (and one of its four sub-variants). ART-SPEC-01's block IDs are stable even as block text evolves — so a species ticket written today against `CHAR-MON-ISO.wing` remains correct as the block text refines.
- **Sub-variant slots match PixelLab templates, not game-world taxonomy.** The `.arach` slot uses PixelLab's `cat` quadruped template as a scaffold and overrides via the prompt — 8 segmented legs don't match any shipped PixelLab template, so the prompt carries the anatomy. Expect iteration; worst case, bump `ai_freedom` up to 850 for arachnids to give PixelLab latitude on leg count.
- **8-dir carve-out is deliberately narrow** so the engine contract stays simple. The Bat earns 4-dir only because all three conditions hold (airborne + Small + `pack-management`). The Spider does NOT qualify (ground-based `.arach`) — Spider ships 8-dir.
- **Runtime `Modulate` is the first-class tool for color-system tinting.** Baked species-identity color is the exception path, always paired with the exempt-pixel sub-node rule in §5.
- **No `create_isometric_tile` calls in this pipeline** — that's a tile-family concern (ART-SPEC-07), not species. Species use `create_character` + `animate_character` exclusively.

## 8. Worked Example — Bat (SPEC-SPECIES-BAT-01)

### Design binding (from SPEC-SPECIES-01 worked example)

- AI pattern: `melee-chase` (swooping), reaction `kite-from-range`.
- Body plan: `CHAR-MON-ISO.wing` (airborne; silhouette readability demands "reads as airborne from 8 tiles" per §5).
- Scale band: Small → `scale = 0.70`, hitbox 8 px, z-offset +28.
- Exempt pixels: eye highlights only (two bright pixels, yellow-gold).
- 8-dir carve-out: qualifies (`.wing` + Small + — wait — SPEC-SPECIES-BAT-01 reaction is `kite-from-range`, not `pack-management`). **Bat ships 8-dir, not 4-dir.** (The 4-dir carve-out would apply only if Bat were re-authored as a `pack-management` swarm unit; the shipped design is `kite-from-range`, so default 8-dir wins.)

### PixelLab params

```
Tool:          create_character
description:   (see prompt below — preamble + body + palette + negative)
name:          "Bat (SPEC-SPECIES-BAT-01)"
size:          64
n_directions:  8
view:          "low top-down"
body_type:     "humanoid"                    # .wing slot uses humanoid+mannequin, not quadruped
template:      "mannequin"
outline:       "single color black outline"
shading:       "medium shading"
detail:        "medium detail"
ai_freedom:    750
```

### Full PixelLab prompt (copy-paste)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A small cartoonish chittering bat rendered in cartoonish isometric pixel-art style, with a winged airborne body plan — leathery flappy wings spread wide, small compact furry body, short legs tucked underneath, body not touching the ground, hovering mid-flight. Wings spread wide to read clearly as airborne from a distance. Glowing yellow-gold eyes (#f5c86b) on a small dark face, sharp little fangs visible. Leathery dark-charcoal wings with visible finger-bone ribs, deep blue-gray body fur. Hovering aggressive swooping pose, wings mid-flap. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

### Animation queue (after character completes)

```
animate_character(character_id, template_animation_id="walking")
animate_character(character_id, template_animation_id="fight-stance-idle-8-frames")
animate_character(character_id, template_animation_id="cross-punch")   # Bat dive-attack stand-in
```

### Godot scene wiring

```
root Node2D:
  scale = Vector2(0.70, 0.70)
  position.y += -28           # airborne hover lift
child Sprite2D "Body":
  offset = Vector2(0, -80)    # per ART-SPEC-01 §1a for H=128
  modulate = <level-gap color from color-system>
child Sprite2D "EyeHighlights":
  offset = Vector2(0, -80)
  modulate = Color.White      # exempt from level-gap tint
child Sprite2D "Shadow":
  offset = Vector2(0, 0)      # at anchor, not hover — preserves iso sort + collision footprint
```

### Handoff checklist (filled)

```
1. Species name:               Bat
2. Paired design ticket:       SPEC-SPECIES-BAT-01
3. AI pattern:                 melee-chase (swooping) / reaction kite-from-range
4. Body plan / sub-variant:    CHAR-MON-ISO.wing
5. Scale band + value:         Small, scale = 0.70
6. Exempt-pixel list:          eye highlights (two pixels, #f5c86b yellow-gold)
7. Animation set:              walking + fight-stance-idle-8-frames + cross-punch
8. Expected PixelLab duration: ~3 min character + ~4 min × 3 animations = ~15 min total
9. Download target path:       assets/characters/enemies/bat/
```

The prompt contains zero named-IP terms — "cartoonish chittering bat", "flappy leather wings", "glowing yellow-gold eyes" are generic beast vocabulary. Per ART-SPEC-01 §11 this is IP-clean.

## Open Questions

None.

# Boss Sprite Pipeline (ART-SPEC-BOSS-01)

## Summary

The generation-facing half of the zone-boss roster. Locks the PixelLab
recipe — prompt text, params, animation templates, download layout, and
review checks — for the eight starter bosses (one per zone, zones 1–8),
so an implementer can produce all eight directories under
`assets/characters/bosses/` without further design input.

Pairs with `docs/world/boss-art.md` (SPEC-BOSS-ART-01, design lead,
authored in parallel). This spec **EXTENDS** — not replaces — the
`CHAR-MON-ISO` block from
[prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit
`5e9e70f`). The universal preamble (§1), palette clause (§1c), and
negative-prompt tokens (§1) are inherited verbatim. The boss canvas
size (160×160, per ART-SPEC-01 §1a row "Large/boss character") and
the offset formula (`Sprite2D.offset = Vector2(0, -96)`) are
inherited from the same spec. IP Protection rules from
ART-SPEC-01 §11 are binding here, full stop.

Structural parity with [species-pipeline.md](species-pipeline.md)
(ART-SPEC-02) is intentional — same section order, same sub-variant
model (`.quad` / `.wing` / `.arach` / `.biped-mon`), same handoff
discipline. Bosses are not a new body plan; they are a **scale +
animation + silhouette tier** on top of the existing four plans.

**Product-owner constraint.** One boss per zone. Zones 1–8 covered by
the starter roster (~8 bosses). Zone 8 is the current late-P2 ceiling
per `dungeon.md` stat floors (3 / 28 / 75). First-kill drops are
guaranteed unique items per FORGE-01 and
[monster-drops.md § Boss First-Kill Drops](../systems/monster-drops.md).

## Current State

Bucket E of [asset-inventory.md](asset-inventory.md) is empty for
bosses — no boss sprite has ever been authored. This spec's first
consumption creates `assets/characters/bosses/` and the eight
per-boss subdirectories. There is nothing to delete; the
delete-before-regen protocol applies only to subsequent iterations
of already-shipped bosses.

Dungeon-side wiring exists (per `dungeon.md § Boss Rooms`: every 10th
floor, 3× stat multiplier, exit-room spawn, first-kill rare-material
drop, 5× XP bonus), so boss sprites will plug directly into the
existing boss-floor spawn hook once delivered.

## Design

### 1. Boss vs. Standard Species — Art-Side Differences

The generation recipe for a boss is `CHAR-MON-ISO` (whichever
sub-variant the boss's body plan demands) with five overrides. These
five deltas are the entirety of "boss-ness" at the art layer:

| Knob | Standard species | Boss override | Source |
|---|---|---|---|
| Canvas size | 128×128 | **160×160** | ART-SPEC-01 §1a table, row "Large/boss character" |
| `Sprite2D.offset.y` | −80 px | **−96 px** | ART-SPEC-01 §1a derived-offset formula (`−(H/2) − 16` with H=160) |
| Scale band | Small / Standard / Large | **Boss: 1.7×–2.5×, default 1.8×, Final-zone 2.2×** | SPEC-SPECIES-01 §6 Size/Scale Rule |
| Directions | 4-dir carve-out possible for Small-airborne | **8-dir always, no carve-out** | SPEC-SPECIES-01 §3 + ART-SPEC-02 §3 — Boss-tier is 8-dir regardless of body plan |
| Animation set | AI-pattern minimum (walk + idle + primary attack) | AI-pattern minimum **+ `falling-back-death`** | SPEC-SPECIES-01 Boss-tier rule; first-kill dramatic death beat |
| `shading` / `detail` | medium / medium | **detailed / high** | ART-SPEC-01 §1b defaults, boss row |

**Phase-shift visual cue (if the boss has phases).** The pipeline
handles a second-phase visual state in one of two ways, picked by the
paired SPEC-BOSS-ART-01 spec, **not** by this doc:

1. **Palette-shift only (default, zero extra generations).** Runtime
   `Modulate` swap on the existing sprite — e.g. the boss tints red
   in phase 2. Uses the color-system tint channel. No second sprite.
2. **Second sprite variant (heavy, +1 character generation per
   boss).** A distinct rotation set authored as a sibling directory
   (`assets/characters/bosses/{boss_id}/phase2/`). Only used when the
   silhouette itself must change (armor sheds, wings unfurl). Not
   recommended for the starter roster — if SPEC-BOSS-ART-01 does not
   explicitly name a boss as phase-sprite-requiring, use the
   palette-shift path.

Particle / aura layers are a scene-side concern (per-boss `.tscn`
with a child `GPUParticles2D`), not a PixelLab concern. Do not bake
auras into the sprite — they will fight the color-system tint.

### 2. Boss Prompt Skeleton (Extension of `CHAR-MON-ISO`)

The skeleton below is the `CHAR-MON-ISO` template from
ART-SPEC-01 §2 with boss-tier slot substitutions. Preamble and
closing clauses are inherited **verbatim**.

```
[PREAMBLE (verbatim, ART-SPEC-01 §1)]

A [SCALE-WORD — towering / hulking / imposing / looming / massive] [ZONE-FLAVOR] boss of the [SPECIES-FAMILY — undead / goblinoid / lupine / orc-kin / arachnid / cavern-dweller / arcane / elder] kind, rendered in cartoonish isometric pixel-art style, with [BODY-PLAN TOKENS — per sub-variant slot, see ART-SPEC-02 §2 slot table]. [UNIQUE-SILHOUETTE-CUE — the single outline beat that separates this boss from the zone's standard species: towering crown / trailing cloak / spreading wings / segmented tail / horned skull mask / staff of bone]. [SCALE-ACCENT — what the extra canvas real estate shows that a 128-canvas species cannot: spreading wingspan, trailing cape, rising staff, pauldrons jutting beyond the body line]. [READABILITY CUE from ART-SPEC-02 §2 — exactly one]. [DEFINING FEATURE — glowing eye color, signature weapon, elemental core]. [POSE — commanding / threatening / arms-raised / weapon-presented]. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE CLAUSE (verbatim, ART-SPEC-01 §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, ART-SPEC-01 §1)]
```

**Slot-fill discipline.** A boss prompt that does not fill all five
of [SCALE-WORD], [SPECIES-FAMILY], [UNIQUE-SILHOUETTE-CUE],
[SCALE-ACCENT], and [READABILITY CUE] is not reviewable. Empty slots
produce PixelLab output that reads as a re-skinned regular species.

### 3. Boss-vs-Species Differentiation Rules

A boss must read as **"bigger and meaner"** at first sight, not as a
re-skinned zone species. Four enforceable differentiators:

1. **Scale (always).** Boss band = 1.7×+ with default 1.8×, Final-zone
   (zone 8) = 2.2×. Standard-band and Large-band species cap at 1.6×.
   Scale alone puts a boss outside the silhouette envelope of its
   zone roster.
2. **Silhouette (mandatory unique beat).** A boss must have one
   outline cue its zone's standard species lack. Example: zone-1
   regular skeletons are compact bipeds with short weapons; the
   zone-1 boss has a **towering silhouette with cape + crown +
   two-handed staff** — the outline reads "monarch," not "warrior."
3. **Color (heightened palette within the clamp).** Deeper shadows,
   a brighter signature accent pixel (eyes, weapon glow, rune core).
   The §1c palette clamp still binds — identity colors live on the
   exempt-pixel layer (per ART-SPEC-02 §5, separate `Sprite2D`
   sub-node with `modulate = Color.White`).
4. **Animation (`falling-back-death` is boss-only).** Standard
   species do not receive a scripted death animation — they fade or
   play a generic hurt frame on kill. Bosses always receive
   `falling-back-death` so the first-kill moment gets a dramatic
   beat. This is a contract: if a boss ships without it, the spec is
   not locked.

### 4. Standard Spec Params (Boss Overrides on `CHAR-MON-ISO`)

Inherited from ART-SPEC-02 §2 slot table; overrides per §1 above.

| Param | Value |
|---|---|
| Tool | `create_character` |
| `body_type` | Per sub-variant: `humanoid` for `.biped-mon` / `.wing`; `quadruped` for `.quad` / `.arach` |
| `template` | Per sub-variant: `mannequin` (humanoid) or nearest animal (`wolf` / `bear` / `cat` / `dog` / `horse` / `lion`) |
| `size` | `64` (PixelLab internal; canvas target 160×160 via prompt clause) |
| `n_directions` | `8` (mandatory — no carve-out for bosses) |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `detailed shading` (boss override) |
| `detail` | `high detail` (boss override) |
| `ai_freedom` | `750` (default); bump to `850` for arachnid or unique-silhouette bosses if first-gen reads muddy |
| `proportions` | Humanoid bosses: `{"type": "preset", "name": "heroic"}` (oversized pauldrons + weapons), quadruped bosses: N/A |

### 5. Starter Roster — Full PixelLab Prompts (Eight Bosses)

**Roster synced 2026-04-18 from SPEC-BOSS-ART-01 canonical lock. Prior
art-lead-derived roster superseded.** Names, base species, reaction,
and scale values below are authoritative from
`docs/world/boss-art.md` §5. Silhouette cues, phase beats, and
exempt-pixel lists trace back to design-lead's §6–13 per-boss fills.

Each boss ID matches the pattern `boss_{zone}_{species}`.
SPEC-BOSS-ART-01 locks the per-boss identity, stat snapshot, and
phase convention; this pipeline authors a PixelLab-ready prompt for
each. **If SPEC-BOSS-ART-01 changes, that spec wins — update this
file by reference.**

Every prompt opens with the ART-SPEC-01 §1 preamble, closes with the
§1c palette clause + §1 negative prompt. For brevity, those two
clauses are abbreviated below as `[PREAMBLE]` and `[PALETTE+NEG]` —
in the actual PixelLab call, they are pasted in verbatim from
ART-SPEC-01. The middle paragraph is the only part authored per
boss.

---

#### Boss 1 — Zone 1 (Stone) · `boss_z1_skeleton`

Title: **Bone Overlord**. Base species: Skeleton. Reaction:
`burst-down-fast`. Sub-variant: `CHAR-MON-ISO.biped-mon`.
Scale: 1.8×. Phase: palette-shift (dim purple → bright red at 50%).

```
[PREAMBLE]

A towering Bone Overlord boss of the undead kind, rendered in cartoonish isometric pixel-art style, with a bipedal humanoid body plan — two legs planted, upright heavy stance, a massive rib cage arching open at the chest. A fused bone-club left arm grotesquely larger than its own torso contrasted against an ordinary skeleton right arm (the asymmetry cue absent from the symmetric zone-1 skeleton regulars), plus a rib-furnace cavity at chest-center that reads as a bone-altar. The rib cage splays wide past the shoulder line and the oversized club juts past the body outline — scale accent that fills the extra 160 canvas. Commanding aggressive charge-pose, club cocked to swing. Deep bone-white hide 2 steps darker than standard skeletons in the recesses, every joint — ribs, shoulders, pelvis — flaring with a bright magicule-purple glow, eye sockets burning as two hollow pale-amber (#f5c86b) points. Bone-dust particles trail from the rib-furnace (source planned for engine particles — do not bake into sprite heavily). Threatening charging pose, ready to close distance. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 2 — Zone 2 (Wood / Mine) · `boss_z2_wolf`

Title: **Howling Pack-Father**. Base species: Wolf (leads goblin
pack). Reaction: `burst-down-fast`. Sub-variant:
`CHAR-MON-ISO.quad` (wolf template). Scale: 1.8×. Phase:
palette-shift (neutral grey → blood-red at 50%).

```
[PREAMBLE]

A massive Howling Pack-Father boss of the lupine kind, rendered in cartoonish isometric pixel-art style, with a quadruped four-legged body plan — the size of a small cave bear, four planted legs, low-slung predatory head-carriage. A massively overgrown right shoulder hump swelling higher than the other (the asymmetry cue absent from the symmetric zone-2 regular wolves), plus a muzzle drawn back in a pack-lord snarl. The grotesque shoulder ridge and a thick hackled spine push the silhouette past the standard-wolf outline — scale accent filling the extra canvas. Aggressive charge-pose, head lowered, haunches coiled mid-lunge. Deep grey-brown fur 1 step darker than standard wolves, the back-ridge fur a saturated signature grey-brown 2 steps brighter than species baseline, both eyes burning a bright yellow, a slow red breath-steam emission wisping from the open muzzle (source: 3×2 pixel cluster at muzzle-tip). Threatening burst-attack pose, ready to leap. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 3 — Zone 3 (Ice / Frozen Depths) · `boss_z3_spider`

Title: **Chitin Matriarch**. Base species: Spider. Reaction:
`kite-from-range`. Sub-variant: `CHAR-MON-ISO.arach`.
Scale: 1.8×. Phase: palette-shift (pale green → toxic green at 50%).
`ai_freedom: 850` (arachnid leg-count latitude per ART-SPEC-02
Implementation Notes).

```
[PREAMBLE]

A massive Chitin Matriarch boss of the arachnid kind, rendered in cartoonish isometric pixel-art style, with a quadruped body plan overridden in prompt to eight segmented arachnid legs — bulbous egg-sac abdomen, low carapace articulated fortress-heavy on eight legs. Fossilized egg-sac armor plates visibly layered on the back like tiled scales (the additional-body-part cue absent from the smooth-shelled zone-3 regular spiders), plus a forward-thrust pair of chitinous fangs. The egg-sac armor stacks raise the silhouette into a "fortress with legs" profile, legs splaying wide past the canvas midline — scale accent that fills the 160×160 space. Rear-guarded ranged stance, body angled for retreat-and-spit, fangs forward in a threat-display. Deep chitin-black carapace 1 step brighter than species baseline along the ridge highlights, with iridescent green accent pixels packed 2× as densely as the standard Spider, an 8-pixel compound-eye cluster burning a saturated toxic-green, and a slow toxic-green venom drip from the fang-tips (source: 2×1 pixel cluster per fang). Threatening ranged stance, ready to launch web-globules. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 4 — Zone 4 (Bone / Catacombs) · `boss_z4_darkmage`

Title: **Hollow Archon**. Base species: Dark Mage. Reaction:
`kite-from-range`. Sub-variant: `CHAR-MON-ISO.biped-mon` (arcane
family, robed caster). Scale: 1.8×. Phase: palette-shift (dim
purple → bright violet at 50%). Z-offset +24 (airborne hover per
SPEC-BOSS-ART-01 §Zone 4).

```
[PREAMBLE]

A looming Hollow Archon boss of the arcane kind, rendered in cartoonish isometric pixel-art style, with a bipedal humanoid body plan — an empty floor-length robe held up by a magicule-current with no body inside, upright caster stance, hem hovering a hand-span above the ground. A broken staff drifting in its own orbit independent of the robe (the additional-body-part cue absent from the zone-4 regular dark mages), plus a hollow void where the face should be under the hood. The untethered orbiting staff and the billowing sleeves extend the silhouette far past the robe outline — scale accent filling the extra canvas width. Ranged caster stance, sleeves spread wide in a channeling pose, staff raised in orbit. Deep blue-gray (#3c4664) robes with the Dark Mage signature purple accent 2 steps brighter than species baseline and extending into a violet magicule-haze along the staff orbit's trailing pixels, a hollow 4×4 void at chest-center where the body should be, 4 pixels of eye-void deep inside the cowl, and a 3×3 staff-head gem glowing bright violet. Threatening distance-keeping casting pose, robe trailing. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 5 — Zone 5 (Metal / Forge) · `boss_z5_orc`

Title: **Warlord of the Fifth**. Base species: Orc. Reaction:
`kite-from-range`. Sub-variant: `CHAR-MON-ISO.biped-mon` (orc-kin
family). Scale: 2.0×. Phase: palette-shift (iron-grey →
hot-iron-orange at 50%).

```
[PREAMBLE]

A towering Warlord of the Fifth boss of the orc-kin kind, rendered in cartoonish isometric pixel-art style, with a bipedal humanoid body plan — two tree-trunk legs, massive broad shoulders, a body weighed down by layered stacked-trophy regalia. A visibly mismatched stack of plate pauldrons and back-trophies from every previous warlord it killed (the crown/regalia cue absent from the zone-5 regular orcs), plus a heavy throwing-axe cocked behind the shoulder. The layered regalia stack and the cocked axe push the silhouette past the standard-orc outline — scale accent at full 160 height and width. Mid-range throw-stance, weight on the back foot, arm cocked to hurl. Dark rusted iron plate over orc green-brown skin 1 step darker than standard orcs (this warlord is *older*), with bright hot-iron-orange glow pixels seeping through the thin seams between every regalia layer, 2 pixels of highlight on each of two tusks, and 2 pixels of eye-glow. Threatening cornered-ranger pose, ready to throw or close. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 6 — Zone 6 (Stone-dark cycle) · `boss_z6_bat`

Title: **The Screaming Flight**. Base species: Bat (swarm-fused).
Reaction: `close-the-gap`. Sub-variant: `CHAR-MON-ISO.wing`.
Scale: 2.0×. Phase: palette-shift (pale grey → screeching yellow at
50% → raw white-hot at 25%). Z-offset +40 (airborne Phase 1+2;
engine drops to 0 at Phase 3 per SPEC-BOSS-ART-01 §Zone 6).

```
[PREAMBLE]

A looming The Screaming Flight boss of the cavern-dweller kind, rendered in cartoonish isometric pixel-art style, with a winged airborne body plan — a cloud of bats fused at the edges into a single many-winged many-mouthed mass, hovering mid-flight with no single body distinguishable from the swarm. Multiple independent wing-pairs radiating outward from a central core (the additional-body-part cue absent from the zone-6 regular single-winged bats), plus a cluster of 8+ mismatched eyes at the core. The radiating wingspan reaches the full 160 canvas width and the core cluster gives the silhouette a menacing "cloud that has teeth" read at tile distance — scale accent that makes the airborne form unmistakable. Close-the-gap diving pose, wings folding mid-swoop to plunge at the player. Leathery wings in deep charcoal 1 step darker than standard bats on the core, the fused many-eye cluster carrying a new saturated signature yellow (source: 8+ pixel cluster at core), and a wing-fringe yellow shimmer sparsely emitted along every wing edge (source pixels: sparse cluster along wing outlines). Threatening diving close-the-gap pose, mid-swoop. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 7 — Zone 7 (Wood-dark cycle) · `boss_z7_goblin`

Title: **Iron-Gut Goblin King**. Base species: Goblin. Reaction:
`close-the-gap`. Sub-variant: `CHAR-MON-ISO.biped-mon` (goblinoid
family). Scale: 2.0×. Phase: palette-shift (dim-iron-grey →
molten-orange at 50% → raw white-hot at 25%).

```
[PREAMBLE]

A hulking Iron-Gut Goblin King boss of the goblinoid kind, rendered in cartoonish isometric pixel-art style, with a bipedal humanoid body plan — two squat powerful legs, menacing forward-lumbering posture, a grotesquely distended iron-scab belly bulging far wider than the torso. A crude iron-circlet crown melted directly into the skull plus the iron-armored belly that visibly swallowed the goblin's ribcage (both crown and belly are cues absent from the zone-7 regular goblin grunts), plus a horizontal belly-seam glowing with a magicule-forge heat-line. The distended belly pushes the silhouette into a siege-engine profile that dominates the 160×160 canvas — scale accent filling the lower half with sheer mass. Close-the-gap lumbering pose, weight on the front foot, ready to belly-slam. Sickly yellow-green goblin skin 1 step sicklier than standard goblins, a new molten-orange accent carried by the horizontal belly-seam iron-glow (source: thin bright horizontal line across belly), 6 pixels of circlet highlight, 2 pixels of eye-glow, and iron-heat shimmer wisping from the belly seams. Threatening close-the-gap advance pose. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

#### Boss 8 — Zone 8 (Final / Ice-dark cycle) · `boss_z8_mix`

Title: **Volcano Tyrant**. Base species: deep-zone mix, orc-form.
Reaction: `close-the-gap`. Sub-variant: `CHAR-MON-ISO.biped-mon`
(orc-form, deep-zone mix). Scale: **2.2× (Final-zone default per
§1)**. Phase: palette-shift (magma-red → incandescent-orange at 50%
→ raw white-gold at 25%); design-lead may upgrade to sprite-variant
at Phase 3 in a future SPEC-BOSS-ART-01 review.

```
[PREAMBLE]

A massive Volcano Tyrant boss of the elder kind, rendered in cartoonish isometric pixel-art style, with a bipedal humanoid body plan on an orc-form frame — two stone-fused legs, upright dominant stance, a body cracked open in places revealing a magma core beneath the char-black exterior. One shoulder is a chunk of raw erupting rock visibly larger and uneven-shaped against the other (the asymmetry cue that separates it from every prior boss in the roster), plus a jagged obsidian mantle spanning the shoulders as a crown/regalia. The erupting shoulder-rock and mantle spread the silhouette to fill the full 160×160 canvas — scale accent matching the deepest-boss scale band. Close-the-gap menacing advance pose, a heavy hammer held low, both feet planted. Char-black + magma-red duotone supplanting standard orc green-brown, shadows 3 steps darker than species baseline (deepest boss in the roster), multiple thin bright magma-red body-crack lines running across torso and limbs (source pixels: thin bright lines), scattered obsidian mantle highlights, and 4 pixels of bright eye-glow each side. Threatening close-the-gap advance pose, chest cracks bright. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette. Exaggerated boss proportions — chunky, oversized, bold — but still cartoonish (not photorealistic, not gritty-realistic, not anime).

[PALETTE+NEG]
```

---

### 6. Animation Recipe (Per Boss)

After `create_character` completes, queue four `animate_character`
calls **sequentially** (rate-limit discipline — 8 concurrent PixelLab
jobs cap, and batched character+animation runs hit that ceiling
fast). Order:

```
animate_character(character_id, template_animation_id="walking")
animate_character(character_id, template_animation_id="fight-stance-idle-8-frames")
animate_character(character_id, template_animation_id="<primary-attack per AI pattern>")
animate_character(character_id, template_animation_id="falling-back-death")
```

**Primary-attack template selection** (from ART-SPEC-02 §4):

| Boss | AI pattern | `template_animation_id` for attack |
|---|---|---|
| `boss_z1_skeleton` (Bone Overlord) | melee-chase (heavy windup) | `roundhouse-kick` (ground-slam stand-in) |
| `boss_z2_wolf` (Howling Pack-Father) | melee-chase (lunge) | wolf-template `attack` (via `get_character()` catalog) |
| `boss_z3_spider` (Chitin Matriarch) | ranged-kite | `fireball` (web-globule stand-in) |
| `boss_z4_darkmage` (Hollow Archon) | caster | `fireball` |
| `boss_z5_orc` (Warlord of the Fifth) | ranged-kite (throw-and-close) | `throw-object` (axe-throw) |
| `boss_z6_bat` (The Screaming Flight) | ranged-kite (dive-diagonal) | `flying-kick` (dive attack) |
| `boss_z7_goblin` (Iron-Gut Goblin King) | melee-chase (belly-slam) | `roundhouse-kick` (belly-slam stand-in) |
| `boss_z8_mix` (Volcano Tyrant) | melee-chase (delayed heavy swings) | `roundhouse-kick` (hammer-swing stand-in) |

Quadruped animation IDs vary per PixelLab template — once
`boss_z2_wolf` character generation completes, call
`get_character()` to see the wolf-template animation catalog and
pick the nearest semantic match for "attack."

Boss-tier `falling-back-death` is **required** for all eight. The
humanoid template IDs are present in the `animate_character` tool's
documented set. For the one quadruped boss (Howling Pack-Father),
verify via `get_character()` — if the wolf template does not expose
a death-equivalent, fall back to whatever "hurt" or "idle" template
reads as a death frame and flag in the PR for design-lead review.

Expected PixelLab duration per boss:
- Character generation: ~5 min (160×160 + 8-dir + detailed shading)
- 4 animations × ~4 min = ~16 min
- **Total per boss: ~21 min.** All 8 bosses: ~3 hours of queued
  generation time (assuming serial queueing to stay under the
  8-concurrent-job cap).

### 7. Download + Extraction

After all animations complete:

```bash
# For each boss_id in the eight above:
mkdir -p assets/characters/bosses/<boss_id>/
curl --fail -o /tmp/<boss_id>.zip "<pixellab_download_url>"
unzip -d assets/characters/bosses/<boss_id>/ /tmp/<boss_id>.zip
rm /tmp/<boss_id>.zip

# Verify:
ls assets/characters/bosses/<boss_id>/
# expect: 8 rotation PNGs + 4 animation subdirs + metadata.json
sips -g pixelWidth -g pixelHeight assets/characters/bosses/<boss_id>/south.png
# expect: 160 × 160
```

**`metadata.json` per boss** — authored by implementer (PixelLab's
ZIP export does not include the game-side scale/z-offset/AI-pattern
fields; add them):

```json
{
  "boss_id": "boss_z1_skeleton",
  "boss_title": "Bone Overlord",
  "zone": 1,
  "base_species": "skeleton",
  "sub_variant": "CHAR-MON-ISO.biped-mon",
  "scale": 1.8,
  "z_offset": 0,
  "ai_pattern": "melee-chase",
  "reaction": "burst-down-fast",
  "exempt_pixels": ["rib-furnace core", "eye sockets", "club bone-dust shimmer"],
  "phase_convention": "palette-shift",
  "paired_design_spec": "SPEC-BOSS-ART-01",
  "paired_species_spec": null,
  "first_kill_drop": "FORGE-01 Tier 1 pool"
}
```

`z_offset` is `+24` for `boss_z4_darkmage` (airborne hover) and
`+40` for `boss_z6_bat` (airborne Phase 1+2; engine drops to 0 at
Phase 3). All other bosses = `0`. Scale values per §5 roster.
Exempt-pixel lists per each boss's `[DEFINING FEATURE]` slot and
SPEC-BOSS-ART-01 §7 per-boss color-coding section.

Target layout on disk:

```
assets/characters/bosses/
├── boss_z1_skeleton/           (Bone Overlord)
│   ├── metadata.json
│   ├── south.png  south-east.png  east.png  ... north-west.png  (8 rotations, 160×160)
│   ├── walking/             (8 dirs × N frames)
│   ├── fight-stance-idle-8-frames/
│   ├── roundhouse-kick/     (primary attack)
│   └── falling-back-death/
├── boss_z2_wolf/               (Howling Pack-Father)
│   └── ...
├── boss_z3_spider/             (Chitin Matriarch)
├── boss_z4_darkmage/           (Hollow Archon)
├── boss_z5_orc/                (Warlord of the Fifth)
├── boss_z6_bat/                (The Screaming Flight)
├── boss_z7_goblin/             (Iron-Gut Goblin King)
└── boss_z8_mix/                (Volcano Tyrant)
    └── ...
```

### 8. Boss-at-Thumbnail Gate (PR Checklist)

Before any boss PR merges, the implementer must render each of the
eight boss `south.png` at **96×96 downsample** and paste the 8-tile
grid into the PR description. The acceptance test:

1. Each boss silhouette is **distinct from its zone's standard
   species** at 96×96. A reviewer squinting at the thumbnail can
   name the boss without reading the filename.
2. The four differentiators from §3 are visible at 96×96 — scale
   (fills more of the thumbnail), unique silhouette beat, heightened
   palette accent, and pose readability.
3. No two bosses in the 8-tile grid read as "the same boss in a
   different color." If any pair looks like re-skins, the weaker of
   the two goes back to the prompt bench.

This gate catches the most common boss-pipeline failure mode: a boss
that looks like a scaled-up version of its zone species instead of a
distinct entity.

### 9. Delete-Before-Regen Protocol

[asset-inventory.md](asset-inventory.md) Bucket E is currently
**empty** for bosses (no sprites have ever been authored). The
first consumption of this spec creates
`assets/characters/bosses/` — nothing to delete on first run.

Subsequent iterations (reworking a boss's art after the initial
ship) follow the standard four-step protocol from
asset-inventory.md:

1. Open a dedicated branch.
2. Delete the old boss's directory (`rm -rf
   assets/characters/bosses/<boss_id>/`) as the first commit.
3. Regenerate via this spec.
4. Commit the new assets + updated `metadata.json` as the second
   commit.

## Acceptance Criteria

- [ ] All eight boss prompts in §5 are fully copy-paste-able: the
  middle paragraph is authored; `[PREAMBLE]` and `[PALETTE+NEG]` are
  substituted verbatim from ART-SPEC-01 §1 and §1c at PixelLab-call
  time.
- [ ] Every boss in §5 reads as distinct from its zone's standard
  species at 96×96 thumbnail per §8 gate (reviewer-verified at PR
  time).
- [ ] Every boss in §6 has a `falling-back-death` animation queued
  (this is a hard contract per SPEC-SPECIES-01 Boss-tier rule).
- [ ] Zero named-IP terms in any of the eight prompts — "Bone
  Overlord," "Howling Pack-Father," "Chitin Matriarch," "Hollow
  Archon," "Warlord of the Fifth," "The Screaming Flight,"
  "Iron-Gut Goblin King," and "Volcano Tyrant" are all
  generic-archetype titles authored fresh for this world. Per
  ART-SPEC-01 §11 rule 6.
- [ ] Canvas size 160×160 is stated for every boss in §5 (implicitly
  via inheritance from §4 + ART-SPEC-01 §1a row "Large/boss
  character").
- [ ] `Sprite2D.offset = Vector2(0, -96)` is stated for every boss
  scene (inherited from ART-SPEC-01 §1a derived-offset formula with
  H=160).
- [ ] Scale values in §5 match the Boss band from SPEC-SPECIES-01 §6
  (1.7×–2.5×, default 1.8×, Final-zone 2.2×).
- [ ] 8-direction rotation is stated as mandatory (§1 table) with no
  carve-out for bosses.
- [ ] Phase-shift handling (§1) is documented with the default
  "palette-shift only" path and the "second sprite variant" escape
  hatch, and explicitly defers per-boss phase-type selection to
  SPEC-BOSS-ART-01.
- [ ] Download + extraction (§7) produces a per-boss
  `metadata.json` with scale, z-offset, and AI-pattern reference
  fields (game-side wiring depends on these).
- [ ] Bucket E (asset-inventory.md) is cited as the target bucket,
  and the "empty — first regen creates" protocol is stated.
- [ ] Pairing with SPEC-BOSS-ART-01 is called out in Summary — this
  spec is not shippable without the design half, and vice versa.

## Implementation Notes

- **Pairing with SPEC-BOSS-ART-01 is load-bearing.** Co-lock
  criterion: both specs move to "Ready-for-impl" together. If the
  design-lead renames a boss, changes its zone assignment, or
  introduces a phase-sprite-requiring boss, update §5 of this doc
  by reference, not by re-authoring.
- **Inheritance, not redefinition.** This spec inherits ART-SPEC-01
  §1 preamble, §1c palette clause, §1 negative-prompt tokens, §1a
  canvas rule (160×160 + `offset.y = -96`), and ART-SPEC-02's
  sub-variant slot table (§2) verbatim. If any drift, fix upstream
  and update by reference.
- **Boss-tier is an override-stack on `CHAR-MON-ISO`, not a new
  block.** The five boss deltas (§1 table) fully describe the
  difference. Do not author a new `CHAR-BOSS-ISO` block unless a
  future boss cannot be expressed via overrides on `CHAR-MON-ISO`
  — prompt-templates.md §9 implementation notes leave room for this
  reassessment at ART-10 time but current evidence says overrides
  suffice.
- **Rate-limit discipline.** 8 bosses × 4 animations = 32 PixelLab
  jobs per regen cycle. PixelLab caps at 8 concurrent. Queue one
  boss's character + animations as a serial chain, and batch across
  bosses only if the queue has headroom. The ~3-hour total from §6
  assumes serial per-boss queueing.
- **IP Protection audit.** ART-SPEC-01 §11 is binding. The eight
  boss titles in §5 trace back to SPEC-BOSS-ART-01 §5 canonical
  lock — none names a specific licensed game, studio, or franchise
  character. "Bone Overlord" / "Howling Pack-Father" / "Chitin
  Matriarch" / "Hollow Archon" / "Warlord of the Fifth" / "The
  Screaming Flight" / "Iron-Gut Goblin King" / "Volcano Tyrant"
  are all genre-generic descriptive titles. If a future
  SPEC-BOSS-ART-01 update introduces a boss title that reads as
  IP-adjacent, it is a spec bug per §11 rule 6 — flag in PR
  review, not here.
- **First-kill drops.** Per `dungeon.md § Boss Rooms` and FORGE-01,
  each boss's first kill drops a unique item. The drop table lives
  in FORGE-01 / SPEC-BOSS-ART-01, not here. Art-side impact: the
  unique item's sprite lives in Bucket G (equipment catalog,
  ART-SPEC-06), not in this pipeline. This spec does not author
  drop sprites.
- **Phase-sprite escape hatch.** If SPEC-BOSS-ART-01 promotes a
  boss from palette-shift to sprite-variant phasing, that boss's
  per-boss directory gains a `phase2/` subdirectory (per §1). This
  is +1 character generation and +4 animation generations per
  upgraded boss — budget accordingly. Recommend deferring any
  phase-sprite upgrades to a post-P2 iteration.

## Open Questions

None.

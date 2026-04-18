# Player Class Sprite Pipeline (ART-SPEC-PC-01)

## Summary

The generation-facing half of the player-class redraw. Locks the PixelLab
recipe — prompt text, params, animation templates, download layout, and
review checks — for the three shipped classes (Warrior / Ranger / Mage),
so an implementer can open a terminal and produce all three character
dirs under `assets/characters/player/` without further design input.

Pairs with `docs/world/player-classes-art.md` (SPEC-PC-ART-01, design
lead). This spec EXTENDS the `CHAR-HUM-ISO` block from
[prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit
`5e9e70f`) — it does not redefine the universal preamble, the palette
clause, or the negative-prompt tokens. Those are cited verbatim from §1
/ §1c of that spec.

**Product-owner decision (locked 2026-04-17).** One default sprite per
class. Warrior = plate + sword + shield. Ranger = leather + shortbow +
hood. Mage = robes + staff + hat. Gear permutations are rendered in the
inventory UI, never baked into world sprites. The three sprites below
are the only world-space character art for the PC slot.

## Current State

Three existing player sprites (`assets/characters/player/warrior/`,
`/ranger/`, `/mage/`) are authored for the wrong perspective (low
top-down, not true 2:1 iso) and anchor to the wrong point (diamond
center, not diamond top vertex). They are on the
[asset-inventory.md](asset-inventory.md) **Bucket A** redraw list.
Delete-before-regen is mandatory — see §7 below.

This spec supersedes any per-class prompt scraps that may have lived in
older ART tickets. It is the single authoritative generation recipe
until SPEC-PC-ART-01 or ART-SPEC-01 is revised.

## Design

### 1. Class → CHAR-HUM-ISO Prompt Skeleton Table

All three classes use block `CHAR-HUM-ISO` from ART-SPEC-01 §2. Locked
values shared across all classes:

| Param | Value | Source |
|---|---|---|
| Tool | `create_character` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `body_type` | `humanoid` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `template` | `mannequin` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `size` | `64` (PixelLab internal; canvas extended to 128×128 on download) | ART-SPEC-01 §1a |
| `n_directions` | `8` | ART-SPEC-01 §1a + SPEC-PC-ART-01 §2 |
| `view` | `low top-down` (iso intent carried by preamble + negatives) | ART-SPEC-01 §1a |
| `outline` | `single color black outline` | ART-SPEC-01 §1b |
| `shading` | `detailed shading` (player hero tier) | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `detail` | `high detail` (player hero tier) | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `ai_freedom` | `500` (3-asset batch — consistency across classes) | ART-SPEC-01 §1b |
| Canvas (authoring target) | **128×128** | ART-SPEC-01 §1a |
| Anchor | bottom-center (x=64, y=128) → diamond top vertex | ART-SPEC-01 §1a + §3 |
| `Sprite2D.offset` | `Vector2(0, -80)` | ART-SPEC-01 §1a derived-offset formula (H=128) |
| Animation set | walking + fight-stance-idle + class attack | SPEC-PC-ART-01 §2 |

Per-class fill-ins for the `CHAR-HUM-ISO` skeleton. Silhouette
constraints and `proportions` overrides come from SPEC-PC-ART-01 §2
(design-side silhouette contract). Each row is one fill-in of the
skeleton in ART-SPEC-01 §2 `CHAR-HUM-ISO`.

#### Warrior

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "heroic"}` |
| BUILD | stocky, broad-shouldered |
| ROLE | warrior |
| OUTFIT | bold chunky plate armor over mail, slightly oversized shoulder pauldrons, steel greaves, a layered surcoat under the breastplate |
| HELD-ITEM(S) | right hand always holds a large steel longsword with a broad flat blade; left hand always holds a round metal shield with a thick iron rim |
| DEFINING-FEATURE | exaggerated horned helmet that covers the upper face, two curved horns sweeping back |
| COLOR-ACCENT | silver trim on the armor edges, faint player-blue (`#8ed6ff`) cloth on the surcoat beneath the plate |
| STANCE | combat-ready, slight forward lean, shield held at the chest, sword ready at the side |

#### Ranger

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "default"}` |
| BUILD | lean, wiry |
| ROLE | ranger |
| OUTFIT | layered leather jerkin with chunky stitching, bracers on both forearms, a full-cover cloth hood pulled up over the head, a slung quiver visible behind the right shoulder |
| HELD-ITEM(S) | both hands always hold a wooden shortbow held across the body; bow visible in both hands, arrow nocked at mid-draw |
| DEFINING-FEATURE | pulled-up hood that shadows the upper face, only chin and mouth readable |
| COLOR-ACCENT | dark forest-brown leather, subtle player-blue (`#8ed6ff`) dye on the hood lining and bow-grip wrap |
| STANCE | poised and half-crouched, bow drawn to mid-pull, weight on the back foot |

#### Mage

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "stylized"}` |
| BUILD | slight, tall |
| ROLE | sorcerer |
| OUTFIT | flowing dark robes with a wide collared mantle, belt with a small pouch, exaggerated oversized sleeves that taper at the wrists |
| HELD-ITEM(S) | right hand always holds a tall wooden staff with a glowing orb at the tip; left hand always held open, empty, fingers slightly curled as if channeling |
| DEFINING-FEATURE | exaggerated pointed wide-brim wizard hat, tip curling slightly, broad brim casting a shadow over the face |
| COLOR-ACCENT | deep blue-gray robes (`#24314a` / `#3c4664`), player-blue (`#8ed6ff`) glow on the staff orb, gold (`#f5c86b`) trim at the hem and mantle edge |
| STANCE | neutral casting stance, staff planted, free hand raised toward the viewer |

### 2. Full Copy-Paste PixelLab Prompts

The following three prompts are complete — they can be fed to
`create_character` verbatim (as the `description` argument) without any
further edits. Each opens with the universal preamble from ART-SPEC-01
§1, inserts the fill-ins from §1 above, and closes with the palette
clause (§1c) and universal negative-prompt tokens (§1).

Every prompt is IP-clean per ART-SPEC-01 §11: no named game titles, no
studio names, no signature character names. The cartoonish qualifier
(ART-SPEC-01 §11 rule 2) is load-bearing and is embedded in the
preamble — do NOT soften it to "realistic" or "gritty" when copying.

**Companion `create_character` invocation args** (same for all three, per §1 table):

```
name: <"Warrior" | "Ranger" | "Mage">
body_type: humanoid
template: mannequin
size: 64
n_directions: 8
view: low top-down
outline: single color black outline
shading: detailed shading
detail: high detail
ai_freedom: 500
proportions: <per-class, see §1>
```

#### Prompt A — Warrior

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A stocky, broad-shouldered warrior rendered in cartoonish isometric pixel-art style. Bold chunky plate armor over mail, slightly oversized shoulder pauldrons, steel greaves, a layered surcoat under the breastplate. Right hand always holds a large steel longsword with a broad flat blade, left hand always holds a round metal shield with a thick iron rim. Exaggerated horned helmet that covers the upper face, two curved horns sweeping back. Silver trim on the armor edges, faint player-blue (#8ed6ff) cloth on the surcoat beneath the plate. Combat-ready stance with a slight forward lean, shield held at the chest, sword ready at the side. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

#### Prompt B — Ranger

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A lean, wiry ranger rendered in cartoonish isometric pixel-art style. Layered leather jerkin with chunky stitching, bracers on both forearms, a full-cover cloth hood pulled up over the head, a slung quiver visible behind the right shoulder. Both hands always hold a wooden shortbow held across the body, bow visible in both hands with an arrow nocked at mid-draw. Pulled-up hood that shadows the upper face — only chin and mouth readable. Dark forest-brown leather, subtle player-blue (#8ed6ff) dye on the hood lining and bow-grip wrap. Poised and half-crouched, bow drawn to mid-pull, weight on the back foot. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

#### Prompt C — Mage

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A slight, tall sorcerer rendered in cartoonish isometric pixel-art style. Flowing dark robes with a wide collared mantle, a belt with a small pouch, exaggerated oversized sleeves that taper at the wrists. Right hand always holds a tall wooden staff with a glowing orb at the tip, left hand always held open and empty with fingers slightly curled as if channeling. Exaggerated pointed wide-brim wizard hat, tip curling slightly, broad brim casting a shadow over the face. Deep blue-gray robes (#24314a, #3c4664), player-blue (#8ed6ff) glow on the staff orb, gold (#f5c86b) trim at the hem and mantle edge. Neutral casting stance, staff planted, free hand raised toward the viewer. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

### 3. Animation Generation Recipe

Per SPEC-PC-ART-01 §2 the shipped animation set per class is
**walking + fight-stance-idle + class attack**. `animate_character` is
called once per animation per class, AFTER the character's 8-dir
rotations have completed (sequence `create_character` → poll until
`get_character` returns `completed` → queue animations). Each
`animate_character` call covers all 8 directions of its class by
default; do not pass `directions` explicitly.

All three animation IDs listed below are from PixelLab's humanoid
template catalog as documented in the
[agent tool surface](../../.claude/agents/art-lead.md)
(`mcp__pixellab__animate_character`).

| Class | Animation purpose | `template_animation_id` | `animation_name` |
|---|---|---|---|
| Warrior | locomotion | `walking` | `walking` |
| Warrior | stationary idle (combat-ready) | `fight-stance-idle-8-frames` | `fight-stance-idle` |
| Warrior | class attack (sword cleave) | `cross-punch` | `attack` |
| Ranger | locomotion | `walking` | `walking` |
| Ranger | stationary idle (bow-ready) | `fight-stance-idle-8-frames` | `fight-stance-idle` |
| Ranger | class attack (bow draw-release) | `lead-jab` | `attack` |
| Mage | locomotion | `walking` | `walking` |
| Mage | stationary idle (casting stance) | `fight-stance-idle-8-frames` | `fight-stance-idle` |
| Mage | class attack (staff cast) | `fireball` | `attack` |

Rationale for the attack-template choices:

- **Warrior → `cross-punch`.** Two-frame lead-hand swing; the mannequin
  rig plays it as a lateral strike the drawn-in sword reads correctly as
  a cleave without re-authoring keyframes.
- **Ranger → `lead-jab`.** Single lead-arm extend; the drawn-in shortbow
  reads as a bow loose with the rear arm holding position.
- **Mage → `fireball`.** PixelLab's dedicated casting template; staff +
  empty hand read as a spell-release. Native fit for the class.

All three attack IDs are template animations (1 PixelLab generation per
direction × 8 directions = 8 generations per animation, no custom-action
cost). `walking` and `fight-stance-idle-8-frames` are likewise
templates. Total animation generation cost: 9 × 8 = **72 generations**
for the full player-class batch after character rotations land.

### 4. Download + Extraction Recipe

After all animations report `completed` via `get_character`, download
the ZIP once per class and extract into the directory convention below.

**Target layout** (per class, one subtree under `assets/characters/player/`):

```
assets/characters/player/
├── warrior/
│   ├── rotations/
│   │   ├── south.png
│   │   ├── south-east.png
│   │   ├── east.png
│   │   ├── north-east.png
│   │   ├── north.png
│   │   ├── north-west.png
│   │   ├── west.png
│   │   └── south-west.png
│   ├── animations/
│   │   ├── walking-<hash>/
│   │   │   ├── south/frame_00.png ... frame_NN.png
│   │   │   ├── south-east/…
│   │   │   └── (all 8 directions)
│   │   ├── fight-stance-idle-<hash>/
│   │   │   └── (same 8-dir pattern)
│   │   └── attack-<hash>/
│   │       └── (same 8-dir pattern)
│   └── metadata.json             # preserved verbatim from PixelLab ZIP
├── ranger/    (same structure)
└── mage/      (same structure)
```

`<hash>` is the animation-job hash suffix that PixelLab emits; match
the existing convention in the other character directories. The hash
is whatever the ZIP already contains — do not rename.

**Extraction commands** (reference; implementer adjusts paths):

```bash
# Per class, after get_character reports all artifacts completed:
curl --fail -o /tmp/warrior.zip "<download_url_from_get_character>"
unzip -q /tmp/warrior.zip -d assets/characters/player/warrior/

# Sanity checks:
ls assets/characters/player/warrior/rotations/ | wc -l          # expect 8
ls assets/characters/player/warrior/animations/ | wc -l         # expect 3
test -f assets/characters/player/warrior/metadata.json          # must exist
sips -g pixelWidth -g pixelHeight \
    assets/characters/player/warrior/rotations/south.png        # 128×128
```

Repeat for `ranger` and `mage`. `curl --fail` is required per the
[art-lead agent card](../../.claude/agents/art-lead.md) ("verify
downloads"). PixelLab returns HTTP 423 + JSON when assets are still
generating; `--fail` turns that into a non-zero exit instead of writing
a junk file.

`metadata.json` is preserved verbatim — it is the PixelLab-authored
record of generation params and is the reference-of-record for future
regens per the [art-lead agent card](../../.claude/agents/art-lead.md)
("Match existing style: read metadata.json for reference parameters").

### 5. Silhouette-at-Thumbnail Verification

Per SPEC-PC-ART-01, the three classes must be distinguishable at
**64×64 thumbnail scale** — the silhouette alone must read the class.

**PR-checklist item (bundled into the redraw PR's description):**

> [ ] Render `rotations/south.png` from Warrior, Ranger, and Mage at
> 64×64 side-by-side (three 128×128 source PNGs, each downscaled to
> 64×64 via nearest-neighbor). Paste the composite into the PR. At
> 64×64 the three must read unambiguously as: (a) a plate-armored
> hero with a shield, (b) a hooded archer with a drawn bow, (c) a
> wide-brim-hatted caster with a staff. If any silhouette reads
> ambiguously, re-gen with DEFINING-FEATURE emphasis turned up (bigger
> horns / more pulled-up hood / longer hat brim) before the PR merges.

This gates the PR per ART-SPEC-01 §6 item 5 ("silhouette readability,
south-facing rotation pasted at thumbnail size"), extended here with
the three-up comparison specific to the player class set.

### 6. Pairing Note

This spec is the generation-facing half of the player-class redraw.
Design-facing counterpart: **`docs/world/player-classes-art.md`
(SPEC-PC-ART-01)**, authored by `@design-lead` in parallel. That spec
owns silhouette constraints, scale rules (PC = scale 1.0 per art-lead
agent card), color contracts, and the animation-set decision that this
spec implements. Co-lock: neither ships "Ready-for-impl" until both are
locked. Dev-tracker Notes cross-link both ways.

## Acceptance Criteria

- [x] **Copy-paste parity.** Any of the three prompts in §2 can be
      pasted into `create_character` as `description` and produces a
      sprite matching the SPEC-PC-ART-01 silhouette constraints.
- [x] **IP-clean.** All three prompts contain zero named-IP
      references — no game titles, studio names, or franchise-character
      names. The cartoonish qualifier is present in all three preambles
      per ART-SPEC-01 §11 rule 2.
- [x] **Animation template IDs are real.** The four template IDs used
      (`walking`, `fight-stance-idle-8-frames`, `cross-punch`,
      `lead-jab`, `fireball`) all appear in the humanoid template list
      documented on the `mcp__pixellab__animate_character` tool.
- [x] **Anchor convention agrees with ART-SPEC-01 §3.** Canvas is
      128×128; anchor is bottom-center; `Sprite2D.offset = (0, -80)`
      is derived from the §1a formula for H=128.
- [x] **Delete-before-regen is documented.** §7 below states the
      commit-1 deletion of the three existing dirs per
      [asset-inventory.md](asset-inventory.md) Bucket A protocol.
- [x] **64×64 silhouette check is a PR gate.** §5 states the
      three-up-thumbnail PR-checklist item.
- [x] **Block-ID citation.** This spec extends `CHAR-HUM-ISO` from
      [prompt-templates.md](prompt-templates.md) §2; the block ID is
      cited in §1 and again in the impl ticket per ART-SPEC-01 §6
      item 1.

## Implementation Notes

### 7. Delete-Before-Regen Hook

Per [asset-inventory.md](asset-inventory.md) **Bucket A** (Player
classes), the redraw PR executes in this order:

1. **Commit 1 (deletion).** `git rm -r` the following dirs as the first
   commit of the redraw branch:
   ```
   assets/characters/player/warrior/
   assets/characters/player/ranger/
   assets/characters/player/mage/
   ```
   This is not optional. The render pipeline breaks if old + new
   co-exist (parallel `metadata.json` files, conflicting animation
   hashes, stale scene-file references mixing anchor conventions).
2. **Commit 2 (regen + scene fixes).** Extract the new PixelLab ZIPs
   per §4, commit new rotations + animations + metadata, and fix any
   `.tscn` files whose `Sprite2D.texture = ExtResource("…player/warrior/south.png")`
   paths were invalidated by the deletion. The new `Sprite2D.offset`
   value (`Vector2(0, -80)`) lands in the same commit.
3. **Verify in-engine.** Before merging, boot the game, confirm all
   three classes spawn, confirm feet align to diamond top vertex on
   the dungeon entrance floor tile. Any floating or sunken sprite is
   an `offset` bug, not a sprite bug (per ART-SPEC-01 §6 item 6).

Git history preserves the old v1 sprites at the pre-deletion commit.
No backup branch or `assets/_old/` shadow copy — history is enough.

### Other notes

- **Batch pacing.** `ai_freedom = 500` is set across the three-character
  batch for consistency per ART-SPEC-01 §1b. All three
  `create_character` calls can be queued concurrently (PixelLab's
  8-concurrent-job limit accommodates them easily). Animations queue
  after rotations complete — do not start animations until
  `get_character` returns completed for all three.
- **Canvas extend.** PixelLab's `size=64` produces ~96×96 output; if
  the shipped PNG is not already 128×128, the implementer canvas-extends
  on download (padded transparent, content re-aligned to bottom-center).
  ART-SPEC-01 §1b documents this as a known friction.
- **Manifest.** This spec does not author a per-family manifest — the
  player class slot is too small to earn one (3 sprites, fixed roster).
  The authoritative record of what exists lives in `metadata.json`
  per directory (PixelLab-preserved) plus [asset-inventory.md](asset-inventory.md)
  Bucket A.
- **Tint compatibility.** Sprites stay palette-neutral (no baked level
  tint) so Godot `Modulate` runtime tints per `docs/systems/color-system.md`
  apply cleanly. Player-blue accent (`#8ed6ff`) on class cloth is the
  only exempt pixel family for PC sprites per ART-SPEC-01 §1c
  exempt-pixel list.

## Open Questions

None.

# PixelLab Skeleton-Animation Research (pre-Bucket A)

## Summary

Research-only note documenting how PixelLab's skeleton-based animation
pipeline actually works, ahead of Bucket A animation generation. No
assets generated. Informs the animation plan for the three player
classes (Warrior / Ranger / Mage) — walk cycle + fight-stance idle +
class-specific attack.

Grounded in direct MCP inspection of four in-repo characters
(`Warrior v2 Theme Review` `50fa6ac2…`, `Skeleton Enemy` `ca84a6e3…`,
`Human Base` `1b27dde3…`, `Warrior (v0)` from
`assets/characters/player/warrior/metadata.json`) and the tool surface
exposed by `mcp__pixellab__animate_character`. Nothing in this doc is
derived from external web search — it all reads off live PixelLab data
already in our account.

---

## 1. What is a "skeleton" in PixelLab?

Not a user-editable 2D bone rig (not Spine / not Live2D). It is an
**internal keypoint graph** baked into the character when
`create_character` runs. From the tool surface:

- Every character created via `create_character` with
  `body_type=humanoid` is built on the **`mannequin`** template
  (confirmed in all four inspected characters' `metadata.json:
  template_id="mannequin"`). Quadruped characters use one of the
  animal templates (`bear`, `cat`, `dog`, `horse`, `lion`).
- Template animations (listed in the tool surface, 49 of them for
  humanoids — `walking`, `cross-punch`, `fireball`, etc.) are
  precomputed keyframe trajectories expressed against the `mannequin`
  skeleton. When you call `animate_character(..., template_animation_id=X)`
  PixelLab drives the character's sprite through those precomputed
  keypoint positions, one frame at a time, per-direction.
- We get **zero direct control over the skeleton itself**. We cannot
  re-rig, expose bones to Godot, or author keyframes against the
  keypoints. The skeleton is a service-side implementation detail.
- What we DO get: for every animation frame, PixelLab emits keypoint
  metadata (`"Collision Detection: Includes keypoints for all frames"`
  per the tool-output banner on every `get_character` call). These
  keypoints are available for pixel-perfect collision / hit-detection
  but NOT for animation authoring from our side.

**Implication for us:** we author at the *animation-selection* level,
not the *keyframe* level. Our two levers are picking the
`template_animation_id` and (for custom actions) writing a text
`action_description` prompt. No keyframe editor is in scope.

---

## 2. `animate_character` with template vs. custom (`action_description`) mode

`animate_character` has two modes, switched by which argument is set:

### Template mode — `template_animation_id="walking"` (cheap)

- Uses one of the 49 humanoid presets (`walking`,
  `fight-stance-idle-8-frames`, `cross-punch`, `lead-jab`,
  `fireball`, `high-kick`, `backflip`, …).
- Cost: **1 PixelLab generation per direction**. For our standard
  8-direction characters: **8 generations per animation**.
- Frame count per direction is determined by the template (e.g.
  `walking` → 6 frames, `fight-stance-idle-8-frames` → 8 frames).
  We cannot alter the frame count.
- Speed / cadence is controlled at playback time in Godot
  (`SpriteFrames.set_animation_speed` / `AnimatedSprite2D.speed_scale`),
  NOT at generation time. The frames themselves are evenly spaced;
  we time them however we want on the Godot side.
- `confirm_cost` flag is NOT required for template animations.

### Custom mode — `action_description="walking stealthily"` (expensive)

- Free-form text prompt describing the motion. PixelLab synthesizes
  new keypoint trajectories against the `mannequin` skeleton guided
  by the prompt.
- Cost: **20–40 PixelLab generations per direction** (depending on
  character size — the tool surface quotes `20-40 generations per direction`
  explicitly; our 92×92 / 128×128 canvas sits near the high end).
  For all 8 directions: **160–320 generations per animation**.
  That is 20–40× more expensive than a template.
- The tool enforces an opt-in: the FIRST call must be made without
  `confirm_cost=true` so the tool returns the cost estimate; the PO
  must explicitly approve before the second call uses
  `confirm_cost=true` to run it. We never surprise-spend custom-action
  credits.
- Output is the same format as template mode — per-direction,
  per-frame PNGs + a metadata entry named
  `custom-<first 30 chars of prompt>` (confirmed in `Human Base`
  metadata — the 5-frame `custom-The character begins in a neut`
  entry). Frame count is prompt-dependent (usually 4–8).

**Is `animate_with_skeleton` a separate tool?** No. The name appears
in `docs/world/player-classes-art.md:122` as shorthand for "custom
animation mode" but the actual MCP tool is just `animate_character`
with `action_description` instead of `template_animation_id`. There
is no distinct `animate_with_skeleton` tool call in our tool surface.
(The spec's reference should be read as "use `animate_character` in
`action_description` mode.")

---

## 3. Walk animation — template, frame count, cadence

**Template to use:** `walking` (preferred). The plain `walking`
template produces **6 frames per direction** (confirmed by the
Skeleton Enemy metadata — `walking-04c73796` with
`frame_000.png`…`frame_005.png` in all 8 direction folders).

Alternatives exist in the catalog but are NOT what we want:

- `walking-4-frames` / `walking-6-frames` / `walking-8-frames` —
  specific frame-count variants if we ever need to match a stride
  to a gameplay timing constraint.
- `walk`, `walk-1`, `walk-2`, `walking-2`..`walking-10` — style
  variants with subtle gait differences (confident stride vs.
  trudging vs. sauntering). Default `walking` is the safe choice
  for all three PCs; a differentiated gait would require one
  regeneration per class with a different template.
- `running-4-frames` / `running-6-frames` / `running-8-frames` —
  kept in reserve for a future sprint action (not needed for
  Bucket A; we don't have a sprint mechanic).
- `sad-walk` / `scary-walk` / `crouched-walking` — niche mood
  variants; skip for player classes.

**Speed control:** set on the Godot side via `SpriteFrames`. 6 frames
at 10 fps = 0.6s per cycle — reasonable for a brisk walk. If the
game-feel tuning wants slower (exhausted) or faster (energized) it's
a `speed_scale` tweak, not a regen.

---

## 4. Attack animations — template-mappable vs. custom-authored

The pipeline spec `player-class-pipeline.md §3` already proposes
template mappings. Auditing them against what the templates actually
produce:

| Class | Spec-proposed template | Reality check | Verdict |
|---|---|---|---|
| Warrior | `cross-punch` | 2-frame lead-hand lateral swing. Sword is baked-in to the rotation, so the swing reads as a lateral cleave — not a "downward overhead slash" (the `player-classes-art.md §6` design ask). | **Acceptable if PO accepts lateral cleave over overhead.** If design insists on overhead, custom-authored. |
| Ranger | `lead-jab` | Single lead-arm extend-and-return. Bow baked in reads as "forward bow-push" — not the classic "draw → hold → release" two-stage action design §7 calls out. | **Acceptable as first swing.** If design insists on draw+release, custom-authored. |
| Mage | `fireball` | PixelLab's dedicated casting template — staff raised, forward thrust, held hand extended. Native fit for a staff-cast ability. | **Clean fit. Ship as template.** |

Other templates worth knowing about when retro-fitting attacks:

- `high-kick`, `roundhouse-kick`, `flying-kick`, `hurricane-kick` —
  not applicable (PCs don't kick).
- `surprise-uppercut` — could sub for Warrior if we want a rising
  strike instead of a lateral cleave.
- `throw-object` — potential fit for a future Ranger thrown-item
  ability.
- `taking-punch`, `falling-back-death`, `getting-up` — hit-react
  + death animations, deferred per SPEC-PC-ART-01 §6.

**Recommendation:** ship **all three attacks on templates** for
Bucket A v1. Note the design-vs-template mismatches as "known
simplifications" in the PR description. If the PO reviews the
rendered attacks and rejects the lateral cleave or forward jab,
THAT is when we spend custom-action credits — not before.

Custom-action budget if we have to fall back on all three attacks:
3 classes × 1 attack × 8 directions × ~30 generations = **~720
generations**. That is a significant spend we should not commit to
blind; template-first is the fiscally-responsible path.

---

## 5. Cost model

**Create cost (`create_character`, standard mode):**
- **1 generation per character**, regardless of direction count.
  (Verified: no per-direction cost is mentioned in the tool surface
  for standard mode.)
- Pro mode (`mode="pro"`): 20–40 generations per character, always
  8 directions. Higher quality. We have not used pro mode on any
  player class; not needed for Bucket A.

**Animate cost (`animate_character`):**

| Mode | Cost per direction | Cost per animation (8 dir) | Cost per class (3 anims) | Total for 3 classes |
|---|---|---|---|---|
| Template | 1 | 8 | 24 | **72** |
| Custom (`action_description`) | 20–40 | 160–320 | 480–960 | 1440–2880 |

**Bucket A total (template-only plan):** create 3 characters (3
generations) + animate all 9 animations on templates (72
generations) = **~75 total PixelLab generations**.

**Bucket A total (fully custom attacks, worst case):** 3 + 72
template + 3 × 8 × 40 custom attacks = 3 + 72 + 960 = **~1035**.
**That is ~14× the template-only cost** and is the decision we want
to avoid making on spec.

The locked plan in `player-class-pipeline.md §3` is already
template-only and the prompt spec there annotates the attack-template
choices as approximations. This research confirms that plan is the
cost-responsible one.

---

## 6. Output format

PixelLab returns animations as **per-direction, per-frame PNGs**
inside the character's ZIP download, with a `metadata.json` index.
Confirmed structure from `assets/characters/enemies/skeleton/metadata.json`:

```
assets/characters/enemies/skeleton/
├── rotations/
│   ├── south.png, south-east.png, ... (8 files)
├── animations/
│   └── walking-<hash>/
│       ├── south/
│       │   ├── frame_000.png
│       │   ├── frame_001.png
│       │   └── ... (6 frames)
│       ├── south-east/  (6 frames)
│       └── ... (8 direction folders)
└── metadata.json  (references all PNGs by relative path)
```

**Not a spritesheet.** Each frame is its own PNG file. No atlas, no
composite image. The `<hash>` is PixelLab's per-job ID suffix — we
preserve it verbatim (never rename).

**Godot binding.** The per-frame PNG layout maps cleanly to a
`SpriteFrames` resource for `AnimatedSprite2D`:

- One `SpriteFrames` animation per `(anim_name, direction)` pair →
  that's 9 animations × 8 directions = **72 animation slots per
  class** in the `SpriteFrames` resource. The existing enemy
  scenes already use this convention; we follow the same pattern.
- At runtime we switch animation by joining name + direction (e.g.
  `"walking_south-east"`) and call `AnimatedSprite2D.play()`.
- Frame cadence is set per-slot via `set_animation_speed`; we
  default to 10 fps and tune from there.
- Texture filter stays nearest-neighbor (`texture_filter = 0`) per
  the art-lead agent card — pixel art must not smooth.

**Alternatives we are NOT using.** We could pre-atlas the frames
into a single PNG spritesheet + a `TileSet`-derived `SpriteFrames`,
but the per-frame layout already works, regen diffs are cleaner
(only the changed PNG lands in git), and there is no measurable
runtime cost on our sprite count.

---

## 7. Skeleton reuse across characters

**No.** Each `create_character` call builds a fresh character with
its own sprite + its own `template_id="mannequin"` reference (the
template is a shared PixelLab internal, but the character's rendered
frames are unique to it). There is no "generate Warrior, then clone
its rig onto Ranger" workflow.

**What this means for our 3-class batch:**

- Warrior / Ranger / Mage are three independent `create_character`
  calls. Each gets its own 8-rotation set + its own animation
  generations. No reuse discount.
- `ai_freedom=500` (per `player-class-pipeline.md §1`) is the
  cross-class consistency lever — setting it the same across all
  three calls biases the generator toward visual coherence but
  does NOT share a skeleton.
- The PO cannot say "give Ranger the same walk rhythm as Warrior."
  Each direction's walk frames for each class are synthesized
  independently from the `walking` template applied to that
  specific sprite. If rhythms feel mismatched, that's a playback-speed
  issue (Godot side) not a regen issue.

**Implication for future work:** if we ever want true per-class
walk differentiation (drunken mage, loping warrior, stealth ranger),
each class swaps to a different `walking-*` variant — 1 generation
per direction × 8 = 8 generations per class retro-fit. Cheap enough
to experiment with post-Bucket A.

---

## 8. Recommendation for Bucket A animation generation

### Walk cycle — all three classes

- Template: **`walking`** (6 frames / direction).
- Animation name in the ZIP: `walking`.
- Cost: 8 generations per class × 3 classes = **24 generations**.
- Godot binding: `SpriteFrames` animation `walking_<direction>`,
  playback 10 fps (tune in-engine).

### Fight-stance idle — all three classes

- Template: **`fight-stance-idle-8-frames`** (8 frames / direction).
- Animation name in the ZIP: `fight-stance-idle`.
- Cost: 8 × 3 = **24 generations**.
- Godot binding: `SpriteFrames` animation
  `fight_stance_idle_<direction>`, playback 8 fps (subtle
  breathing cadence — adjust if frames read jittery).

### Class attack — template-first, custom only on PO rejection

- Warrior: **`cross-punch`** (lateral sword cleave reading). 8 gens.
- Ranger: **`lead-jab`** (forward bow-push reading). 8 gens.
- Mage: **`fireball`** (staff-cast, clean fit). 8 gens.
- Cost: **24 generations** for all three attacks on template.
- Godot binding: `SpriteFrames` animation `attack_<direction>`,
  playback tuned to the ability's windup timing.

### Total Bucket A animation generation budget

**72 PixelLab generations** for all three classes × three
animations × 8 directions (plus 3 generations for the initial
characters, though those may already exist from the theme-review
batch — `Warrior v2 Theme Review` is in the account). Grand total
if we redo everything from scratch: **75 generations**. If we
reuse the existing `Warrior v2 Theme Review` character ID and add
the two missing PCs, it's **~50 generations**.

### Risk + contingency

- **Attack-template rejection (highest risk).** If the PO reviews
  the lateral cleave / forward jab and rejects them, we burn the
  custom-action budget on affected classes. Worst case (all three
  rejected): +960 generations. Mitigation: include a PR-review
  thumbnail of each south-facing attack frame 3 (the peak swing
  frame) so the PO flags rejects before we commit to animating all
  8 directions.
- **Walk-rhythm mismatch across classes.** Low risk. If it's an
  issue, Godot `speed_scale` fixes it without re-gen.
- **Fight-stance-idle reading as too-active for a town.** If the
  PC hangs out in town and the idle animation reads jittery, we
  swap one class to `breathing-idle` (gentler, NPC-standard) for
  the town-stance use case — adds one animation slot per class +
  8 generations. Defer decision until we have the town-state
  feature spec.

---

## 9. Open questions for PO

1. **Attack-template choices.** `cross-punch` reads as lateral
   cleave (not overhead slash per design §6). `lead-jab` reads as
   forward bow-push (not draw+release per design §7). Acceptable
   simplifications or must we custom-author? (Cost impact: +160–320
   per custom class, up to +960 worst case.)
2. **Reuse `Warrior v2 Theme Review`?** The theme-review character
   `50fa6ac2-f601-4893-b82e-d13076f9b05e` is already in the account
   with 8 rotations. Do we extend it with animations directly, or
   regen a "v2 final" under a new character ID?
3. **Town-idle vs. combat-idle split.** Is `fight-stance-idle-8-frames`
   the correct idle for ALL states (town, dungeon, pause menu), or
   do we also need a gentler `breathing-idle` for the town scene?
   (Cost impact: +24 generations total if we add a second idle.)

---

## References

- `docs/assets/pixellab.md` — tool surface documentation.
- `docs/assets/player-class-pipeline.md §3` — animation recipe
  (this research confirms the plan and adds the cost + output-format
  detail).
- `docs/world/player-classes-art.md §6/§7/§8` — design-side
  animation set definitions (walk + fight-stance + attack).
- `assets/characters/enemies/skeleton/metadata.json` — reference
  metadata format for animation output.
- `assets/characters/player/warrior/metadata.json` — example of a
  custom-action animation (`idle_combat_stance_with_sword_held…`
  8 frames).
- MCP tool surface `mcp__pixellab__animate_character` — authoritative
  source for cost model (`20-40 generations per direction` for
  custom) and `confirm_cost` opt-in protocol.

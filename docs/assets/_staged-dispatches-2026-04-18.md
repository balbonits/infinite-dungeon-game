# Staged PixelLab Dispatches — 2026-04-18 (work-in-progress)

Staging doc for animation dispatches authored while PO is away. Once a dispatch is fired and its output is committed / approved, strike through or remove the corresponding block here. This file is `_`-prefixed to signal it is scratch work, not a spec — delete once all blocks are executed.

**Character IDs (PixelLab) for all dispatch calls:**

| Character | ID | Canonical file |
|---|---|---|
| Warrior v2 | `50fa6ac2-f601-4893-b82e-d13076f9b05e` | `assets/characters/player/warrior/_theme-review/south.png` |
| Ranger v6 woman | `2c4f2261-6eef-4c2a-b727-cd6a19159db6` | `assets/characters/player/ranger/_theme-review/south.png` |
| Mage v4 black man | `4703290e-3779-40d3-b101-50ee8cf4fd3c` | `assets/characters/player/mage/_theme-review/south.png` |

**Walk custom `action_description` pattern — VALIDATED by Warrior walk south success (4 frames, 20 gens, zero equipment-drop):**

> The character walks forward with natural stride; the `<WEAPON-1>` stays in the `<HAND-1>` at the side `<WEAPON-1-ORIENTATION>` throughout every frame; the `<WEAPON-2>` stays on the `<HAND-2>` at the side throughout every frame; both weapons remain fully visible and correctly positioned in all frames including the back-swing phase; natural alternating feet, body rotates slightly with stride.

**Attack custom `action_description` pattern — UNDER VALIDATION on Warrior attempt 2 (pose-based, anti-locomotion):**

> The character stands in place facing the camera (south direction) throughout all frames — does NOT walk, does NOT turn, does NOT change facing direction, does NOT pivot. The feet stay planted on the ground in every single frame. The character holds `<WEAPON(S)>`. Pose progression across the animation:
> - Frame 0 (Ready): `<frame 0 pose>`.
> - Frame 1 (`<beat>`): `<frame 1 pose>`.
> - Frame 2 (`<beat>`): `<frame 2 pose>`.
> - Frame 3 (`<beat>`): `<frame 3 pose>`.
> - (Frame 4 if 5-frame attack): `<frame 4 pose>`.
>
> The `<WEAPON(S)>` stay gripped in `<HAND(S)>` in every frame. The character faces south (toward the camera) in every frame — no back-turn, no side-profile, no rotation. Static pose-based animation with the feet stationary.

---

## BLOCK 1 — Warrior walk, 7 remaining directions (FIRE after PO approves south)

**Trigger:** PO visually confirms `walking-south-v3-custom-spritesheet.png` (already rendered) reads correctly.

**Dispatch:**
```
mcp__pixellab__animate_character(
    character_id="50fa6ac2-f601-4893-b82e-d13076f9b05e",
    action_description="<Warrior walk prompt, verbatim from walking-south-v3-custom dispatch>",
    animation_name="walking-v3-custom-full",
    directions=["north", "east", "west", "north-east", "north-west", "south-east", "south-west"],
    confirm_cost=true  # pre-approved — 7 × ~20 = ~140 gens
)
```

**Save layout:** `assets/characters/player/warrior/animations/walking/<direction>/frame_NN.png` (final production path, not `_theme-review/`).

**Post-render:** move / copy south frames from `_theme-review/walking-south-v3-custom/` into `animations/walking/south/`.

---

## BLOCK 2 — Ranger walk, south-only one-first (FIRE after Warrior walk approved)

**Trigger:** PO confirms Warrior walk batch looks good across all 8 directions (Block 1 complete).

**Dispatch:**
```python
mcp__pixellab__animate_character(
    character_id="2c4f2261-6eef-4c2a-b727-cd6a19159db6",
    action_description="""The character walks forward with natural stride; the shortbow stays slung on the back across the shoulder throughout every frame; the quiver stays on the back with arrow fletching visible at the shoulder throughout every frame; both bow and quiver remain fully visible and correctly positioned in all frames including the back-swing phase; natural alternating feet, body rotates slightly with stride.""",
    animation_name="walking-south-custom",
    directions=["south"],
    confirm_cost=true
)
```

**Save layout:** `assets/characters/player/ranger/_theme-review/walking-south-custom/frame_NN.png`. Batch remaining 7 directions only after south verified.

---

## BLOCK 3 — Mage walk, south-only one-first (FIRE after Warrior walk approved)

**Dispatch:**
```python
mcp__pixellab__animate_character(
    character_id="4703290e-3779-40d3-b101-50ee8cf4fd3c",
    action_description="""The character walks forward with natural stride; the staff stays gripped in the right hand held vertically at the side with the staff-gem at the top throughout every frame; the wizard hat stays on the head with the pointed tip visible above the silhouette throughout every frame; robes sway slightly with stride; both staff and hat remain fully visible and correctly positioned in all frames including the back-swing phase; natural alternating feet.""",
    animation_name="walking-south-custom",
    directions=["south"],
    confirm_cost=true
)
```

**Save layout:** `assets/characters/player/mage/_theme-review/walking-south-custom/frame_NN.png`.

---

## BLOCK 4 — Warrior attack, 7 remaining directions (FIRE after Warrior attack south validated)

**Trigger:** PO confirms the `attack-south-v2-literal` (or later attempt) output reads as a clean downward cleave.

**Dispatch:**
```python
mcp__pixellab__animate_character(
    character_id="50fa6ac2-f601-4893-b82e-d13076f9b05e",
    action_description="<validated attack prompt, verbatim from the approved attempt>",
    animation_name="attack-v1-full",
    directions=["north", "east", "west", "north-east", "north-west", "south-east", "south-west"],
    confirm_cost=true  # pre-approved — 7 × ~20 = ~140 gens
)
```

**Note:** attack custom per-frame pose language needs directional adaptation — "facing south" in the south prompt becomes "facing north" / "facing east" / etc. for each direction. If PixelLab auto-adapts pose per direction (custom-mode uses completed sides as reference) this may not be an issue; verify with the first one or two non-south directions before firing all 7 at once.

---

## BLOCK 5 — Ranger attack (pull-and-release), south-only one-first (FIRE after Warrior attack pattern validated)

**Trigger:** Warrior attack pose-based prompt pattern is validated (attempt 2 or later) AND PO approves firing Ranger.

**Dispatch:**
```python
mcp__pixellab__animate_character(
    character_id="2c4f2261-6eef-4c2a-b727-cd6a19159db6",
    action_description="""The character stands in place facing the camera (south direction) throughout all frames — does NOT walk, does NOT turn, does NOT change facing direction, does NOT pivot. The feet stay planted on the ground in every single frame. The character holds a shortbow with an arrow nocked.

Pose progression across the animation:
- Frame 0 (Ready): bow held at chest level, rear hand resting on the bowstring, arrow visible nocked on the bow.
- Frame 1 (Full draw): rear hand is at the cheek holding the bowstring back, front arm extended forward holding the bow, the bow is at full tension, arrow still visible on the bow.
- Frame 2 (Release): the bowstring is straight (no longer pulled back), the arrow is leaving the bow in the forward direction, front arm still extended.
- Frame 3 (Recovery): rear arm lower near the body, bow tilted downward.

The bow stays gripped in the front hand in every frame. The character faces south (toward the camera) in every frame — no back-turn, no side-profile, no rotation. Static pose-based animation with the feet stationary and the body not moving forward or backward.""",
    animation_name="attack-south-v1",
    directions=["south"],
    confirm_cost=true
)
```

**Save layout:** `assets/characters/player/ranger/_theme-review/attack-south-v1/frame_NN.png`.

**Success criteria:** character faces south in all frames, feet planted, bow drawn then released, arrow visible leaving on frame 2, no locomotion / pivot / back-turn.

---

## BLOCK 6 — Mage attack (forward staff swing + gem flare), south-only one-first (FIRE after Warrior attack pattern validated)

**Trigger:** same as Block 5.

**Dispatch:**
```python
mcp__pixellab__animate_character(
    character_id="4703290e-3779-40d3-b101-50ee8cf4fd3c",
    action_description="""The character stands in place facing the camera (south direction) throughout all frames — does NOT walk, does NOT turn, does NOT change facing direction, does NOT pivot. The feet stay planted on the ground in every single frame. The character holds a staff vertically in the dominant hand with the staff-gem at the top.

Pose progression across the animation:
- Frame 0 (Ready): staff held vertical at the side of the body, gem dim.
- Frame 1 (Charge): staff still vertical but drawn back slightly, gem glowing with a subtle royal-violet light.
- Frame 2 (Forward-lean): staff tilted forward at an angle in front of the body, gem glowing brighter.
- Frame 3 (Peak flare): staff at the forward peak position extended in front of the body, gem flares bright royal-violet light.
- Frame 4 (Recovery): staff returning toward vertical at the side, gem glow fading.

The staff stays gripped in the hand in every frame. The wizard hat stays on the head in every frame. The character faces south (toward the camera) in every frame — no back-turn, no side-profile, no rotation. Static pose-based animation with the feet stationary and the body not moving forward or backward.""",
    animation_name="attack-south-v1",
    directions=["south"],
    confirm_cost=true
)
```

**Save layout:** `assets/characters/player/mage/_theme-review/attack-south-v1/frame_NN.png`.

**Success criteria:** character faces south in all frames, feet planted, staff goes from vertical-at-side through forward arc with gem brightening, peak flare at frame 3, staff returning in frame 4, no locomotion.

---

## BLOCK 7-9 — 7-direction batches for Ranger walk, Mage walk, Ranger attack, Mage attack (FIRE after each south one-first approved)

Mirror of Block 1 / Block 4 structure — once the south one-first is PO-approved, dispatch the remaining 7 directions for that character + animation. Estimated 140 gens per batch (7 × 20).

---

## BLOCK 11 — Ranger walk escalation (PO decision required)

**Status: BLOCKED — two custom attempts both failed with identical back-equipment drop (bow + quiver vanish in feet-crossing frames 1/3 despite both the base pose prompt and a frame-by-frame anchored retry). PixelLab's walk template is confirmed to not track back-attached equipment through arm-swing phases. Prompt engineering has been exhausted.**

### PO options (pick one to unblock Ranger walk)

**Option A — Post-composite the back equipment onto walk frames.**
Render a Ranger walk with the character wearing NO back equipment (clean arm-swing animation with no drop risk). Extract the bow + quiver region from `south.png` as a transparent PNG mask. Stamp that mask onto each walk frame at a fixed torso anchor. Because the walk is south-facing and the south.png is camera-locked to south, the back equipment occupies the same screen-space region across all frames — a single stamp layer applied to all 4 frames will read coherently.
- **Cost:** ~20 gens to re-render the walk without equipment + 0 gens for compositing (PIL/ImageMagick, local).
- **Pros:** keeps the canonical Ranger v6 sprite intact; deterministic (no more generation roulette); reusable per-direction once the mask is built.
- **Cons:** requires an image-processing step; mask needs building per-direction (8 masks total); compositing may show edge artifacts if the walk's torso rotation shifts the anchor point.
- **Implementation notes:** this is the first real post-composite pipeline we'd build. Worth documenting in `pixellab-animation-research.md` as a recurring fallback for back-attached equipment across future enemy/NPC sprites.

**Option B — Regenerate Ranger with bow IN HAND.**
Create a new Ranger character where the bow is held at the side in-hand (like Warrior's sword and Mage's staff) instead of slung on the back. In-hand equipment has been proven to persist through walk animations (Warrior + Mage walks both clean). The quiver stays on the back (smaller; may still drop but is less critical to the silhouette than the bow).
- **Cost:** ~1 gen for the new character (standard mode + `realistic_male` preset per canonical recipe) + ~20 gens for the new walk + possible re-do of other poses that were already approved against the current Ranger v6.
- **Pros:** solves the problem at the root — all future Ranger animations benefit from in-hand equipment stability.
- **Cons:** replaces the current canonical Ranger v6 sprite; PO needs to re-approve the new silhouette; the bow-in-hand silhouette reads differently ("bow drawn" rather than "archer carrying bow").
- **Implementation notes:** SPEC-PC-ART-01 §7's revised-2026-04-18 banner explicitly accepted bow-on-back as a PixelLab interpretation at 92×92. This option reverses that decision.

**Option C — Accept broken walk as a known v1 limitation.**
Ship the Ranger walk with bow+quiver dropping in frames 1/3. The drop is visible but not catastrophic (the character is still recognizably walking; only the back equipment flickers).
- **Cost:** 0 additional gens.
- **Pros:** unblocks v1 quickly; 2/4 frames are correct.
- **Cons:** visibly inconsistent; violates the MVP "weapons baked into sprites" rule.
- **Implementation notes:** not recommended — this is a visual defect that will register to players as "the ranger's bow disappears while walking."

**Option D — Defer Ranger walk; ship Warrior + Mage walks for MVP.**
The Warrior and Mage walks are validated. Ranger's walk can ship as a known "working on it" item; the game can spawn the Ranger with the static south rotation until this is unblocked.
- **Cost:** 0 additional gens.
- **Pros:** keeps MVP moving without Ranger blocking the whole class pipeline.
- **Cons:** Ranger has no walk animation at MVP; the class feels less alive than Warrior/Mage.
- **Implementation notes:** acceptable ONLY if MVP release pressure is high and Option A is slated for v1-patch.

**Recommendation:** **Option A (post-composite).** Low risk, reusable mechanism, preserves approved canonical sprite. Cost is manageable and the mechanism will recur across other back-attached-equipment sprites (e.g., bosses with shoulder-mounted weapons, enemies with quivers).

---

## BLOCK 10 — Death animations (south-only each, MVP scope)

**Trigger:** all walks + attacks landed. Motion intents below are DRAFT; PO lock before dispatch.

### Death motion MC options per class (pending PO lock)

Each class plays its death once when HP reaches 0. South-facing only for MVP (per `feedback_mvp_animation_scope.md`). All deaths end with the character prone / on the ground so the sprite stays readable as "dead" after the last frame loops-hold. Weapons stay on body (consistent with MVP weapons-baked rule).

**Warrior death** — pick one:
- **A) Fall backward** — hit takes the Warrior down onto their back. Shield still on the arm, sword still in hand. Arms splay slightly outward. Classic "defeated soldier" read. [rec — unambiguous, consistent with heavy-strike aesthetic]
- **B) Stagger forward, drop to one knee, collapse sideways** — three-beat slower death. More dramatic.
- **C) Straight-down slump** — body folds at the knees, character drops to a seated position, head falls forward. Quieter.

**Ranger death** — pick one:
- **A) Knees buckle forward** — body folds at the knees, hands instinct-brace the ground, head drops forward. Bow stays slung on the back. [rec — reads clearly, the archer's balance failing under lethal damage is the readable beat]
- **B) Stumble backward** — character staggers back, falls onto their back. Hood partially falls off.
- **C) Spin and fall sideways** — body rotates as if struck, falls on its side.

**Mage death** — pick one:
- **A) Staff drops, slow collapse** — staff slips from the hand and falls to the ground, arms reach up briefly then fall, body collapses straight down in place with robes bunching. Quiet, scholarly. [rec — the staff-drop is the most readable "a mage has died" cue because the staff = class identity]
- **B) Kneel then topple forward** — body drops to its knees first, then topples forward face-to-ground. More dramatic.
- **C) Backward tip** — arms fall to sides, body tips straight backward, staff falls separately.

### Dispatch template (apply after PO picks intent per class)

```python
mcp__pixellab__animate_character(
    character_id="<class character id>",
    action_description="""<pose-based prompt matching the picked intent — use the attack
    anti-locomotion template but adapted for the death motion's specific pose progression.
    Emphasize: character faces south throughout, feet planted until the collapse, weapons
    stay on the body, final frame is the character prone/kneeling/slumped in a stable
    pose that reads as 'dead' when held.>""",
    animation_name="death-south-v1",
    directions=["south"],
    confirm_cost=true  # pre-approved — 1 × ~20 = ~20 gens per class
)
```

**Save layout per class:** `assets/characters/player/<class>/_theme-review/death-south-v1/frame_NN.png`.

**Success criteria:**
1. Final frame reads as "dead" when held statically (no loop-back needed for the sprite to read as non-animate).
2. Weapons remain on the body per MVP bake-in rule.
3. Motion matches PO-locked intent per class.
4. Character faces south throughout (until the collapse orientation shifts for the final frame).

**Cost estimate:** 3 × ~20 gens = **~60 gens total** for the three south-only death animations. V1 can add other directions later if PO wants fallbacks for camera-reveals / death-from-above beats.

---

## Estimated total remaining credit spend (worst case, all blocks fire)

| Block | Purpose | Gens |
|---|---|---|
| 1 | Warrior walk 7-dir batch | ~140 |
| 2 | Ranger walk south | ~20 |
| 3 | Mage walk south | ~20 |
| 4 | Warrior attack 7-dir batch | ~140 |
| 5 | Ranger attack south | ~20 |
| 6 | Mage attack south | ~20 |
| 7 | Ranger walk 7-dir batch | ~140 |
| 8 | Mage walk 7-dir batch | ~140 |
| 9a | Ranger attack 7-dir batch | ~140 |
| 9b | Mage attack 7-dir batch | ~140 |
| 10 | Deaths (3 × south-only) | ~60 |
| **Total** | | **~980** |

Already sunk: 40 gens (Warrior walk south + Warrior attack attempt 1). Plus attempt 2 in flight (~20).

**Grand total MVP animation budget estimate: ~1040 gens.**

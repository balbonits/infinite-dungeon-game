# Class Attack Animations (SPEC-PC-ATK-01)

## Summary

Locks the per-class attack-animation **motion intent** for Warrior /
Ranger / Mage ahead of any PixelLab rendering. Per PO direction
2026-04-18, all three classes ship with **custom `action_description`**
animations — no template path survived the motion-intent review. This
spec locks the semantic read, frame count, per-frame role, hit-tick
alignment, and (for Ranger + Mage) projectile-spawn frame so the
combat-tuning team has a single reference when ability damage numbers
land.

**MVP scope:** this is the **single attack animation per class** for
MVP. Per `feedback_mvp_animation_scope.md`, MVP ships 3 animations per
class (walk / attack / death). Additional attack variants, cast-charge,
hit-react, etc. are deferred to v1.

Walking animations are handled separately via the "render one, review,
then batch" path. Fight-stance idle is also out of scope for MVP.

## Current State

- **User locked motion intents 2026-04-18** (per
  `feedback_mvp_animation_scope.md`):
  - **Warrior:** downward cleave — sword raised overhead, swung
    straight down. Reads as "heavy committed strike."
  - **Ranger:** bow shot — single-stage pull-and-release. The bow
    draws back and immediately releases. Arrow projectile spawns at
    release frame.
  - **Mage:** forward staff swing with gem flare — the staff is
    brought forward in an arc, the staff-gem flares bright royal-
    violet, reading as "launched an attack" (the magical projectile
    spawns at the gem-flare frame).
- **This spec's locked intents take precedence over** any older
  draft in `docs/world/player-classes-art.md §6/§7/§8` that describes
  the attack differently.
- `docs/assets/pixellab-animation-research.md §4` audited the
  49-template humanoid catalog; **no template is a clean fit for any
  of the three locked intents.**
  - `cross-punch` → lateral cleave, not downward. Rejected.
  - `lead-jab` → single-arm forward push, not bow draw. Rejected.
  - `fireball` → overhead cast with forward arm extension, not
    forward staff-arc-swing with gem flare. Rejected.
- No attack animations have been generated yet. The three existing
  player sprites have no `attack` animation under
  `assets/characters/player/*/animations/`.

## Design

### 1. Plan — Custom `action_description` for all three classes

The path for each class is locked to custom `action_description`
because no template matches the motion intent:

| Class | Path | Generation method | Estimated cost |
|---|---|---|---|
| Warrior | **Custom** | `action_description` (see §3) | ~20-40 gens |
| Ranger | **Custom** | `action_description` (see §4) | ~20-40 gens |
| Mage | **Custom** | `action_description` (see §5) | ~20-40 gens |

**Total estimated generation budget:** ~60-120 PixelLab generations for
all three attacks (covers all 8 directions per animation).

Cost framing uses the `animate_character` call-count model: one
`animate_character` call produces the full animation across all 8
directions. Custom `action_description` calls cost ~20-40 gens each
(depending on frame count and size). Template calls would have been ~1
gen each but none match the locked intents.

**Why no template path survived.** The three motion intents each hit a
different template-catalog gap:
- Downward cleave requires a vertical sword arc from overhead to
  low-forward. `cross-punch` and `slash` templates are lateral
  motions; `chop` exists but maps to axe-overhead-to-midline, not a
  full vertical follow-through.
- Bow draw-and-release requires two-arm coordination (front arm
  braces, rear arm pulls the string). No humanoid template models
  this — `lead-jab` and `right-cross` are single-arm punches.
- Forward staff swing with gem flare requires a specific arc-then-
  flare beat where the gem visual is the "hit moment." `fireball`
  template keeps the staff vertical and uses the free hand for the
  cast, not the gem.

Templates would save ~180-360 gens (~$20-40 in credits) but all three
ship wrong at MVP. That's a bad trade — players see the attack
animation every ~2 seconds for the entire game. Ship custom.

### 2. Per-Class Attack Specification

Each class's attack is authored via a custom `action_description`
prompt. Semantics, frame count, timing, and hit-tick alignment are
authoritative — the combat-tuning team reads this table when wiring
ability damage.

### 3. Warrior Attack — Custom `action_description` (downward cleave)

**Path:** Custom (`action_description`).
**Frame count:** **4 frames** (gives ready / raise / strike / recovery
without dead frames).
**Reads as:** a heavy two-handed downward sword cleave — the Warrior
raises the longsword overhead in a committed wind-up, then brings it
down in a straight vertical slash, ending with the blade tip forward
and low. Commitment weight reads clearly; this is not a quick combat-
practical slash, it is a single heavy strike.

**`action_description` prompt to feed to `animate_character`** (VALIDATED 2026-04-18 via attempt 2 — see Implementation Notes "Prompt-interpretation failure mode"):

```
The character stands in place facing the camera (south direction)
throughout all frames — does NOT walk, does NOT turn, does NOT change
facing direction, does NOT pivot. The feet stay planted on the ground
in every single frame. The character holds a longsword in a two-hand
grip.

Pose progression across the animation:
- Frame 0 (Ready): the sword is held at the side with the blade
  pointing downward.
- Frame 1 (Overhead): both arms raise the sword vertically so the
  blade is held overhead pointing straight up.
- Frame 2 (Forward-low): the arms have brought the sword downward in
  a vertical path so the blade ends pointing forward and low in front
  of the body.
- Frame 3 (Recovery): arms returning toward neutral with the sword
  at the side.

The shield stays on the left forearm in every frame. The sword stays
gripped in the hands in every frame. The character faces south
(toward the camera) in every frame — no back-turn, no side-profile,
no rotation. Pose-based animation with the feet stationary.
```

**Timing (at 10 fps playback = 0.4s total):**
| Frame | Role | Duration |
|---|---|---|
| 0 | **Ready.** Sword at side, blade pointing down. | 0.10s |
| 1 | **Overhead / wind-up peak.** Sword held overhead, blade pointing up. | 0.10s |
| 2 | **Forward-low / hit-tick.** Blade ends pointing forward-low in front of body (the cleave follow-through). | 0.10s |
| 3 | **Recovery.** Arms return to neutral, sword at side. | 0.10s |

**Hit-tick frame:** **frame 2**. Damage fires when the blade occupies
the space in front of the Warrior. At 10 fps, damage lands 0.2s after
the player presses attack — fast enough to feel responsive, slow
enough to preserve the commitment weight that a heavy cleave requires.

**Projectile spawn:** none. Melee.

### 4. Ranger Attack — Custom `action_description` (bow pull-and-release)

**Path:** Custom (`action_description`).
**Frame count:** **4 frames** (ready / draw / release / recovery —
single-stage, no separate hold beat).
**Reads as:** a single fluid bow draw-and-release. The bow is raised
and drawn back in one motion, then immediately releases — no held
full-draw pose. The arrow leaves on the release frame.

**`action_description` prompt to feed to `animate_character`** (pose-based pattern validated on Warrior cleave 2026-04-18):

```
The character stands in place facing the camera (south direction)
throughout all frames — does NOT walk, does NOT turn, does NOT change
facing direction, does NOT pivot. The feet stay planted on the ground
in every single frame. The character holds a shortbow with an arrow
nocked.

Pose progression across the animation:
- Frame 0 (Ready): bow held at chest level, rear hand resting on the
  bowstring, arrow visible nocked on the bow.
- Frame 1 (Full draw): rear hand is at the cheek holding the
  bowstring back, front arm extended forward holding the bow, the
  bow is at full tension, arrow still visible on the bow.
- Frame 2 (Release): the bowstring is straight (no longer pulled
  back), the arrow is leaving the bow in the forward direction,
  front arm still extended.
- Frame 3 (Recovery): rear arm lower near the body, bow tilted
  downward.

The bow stays gripped in the front hand in every frame. The character
faces south (toward the camera) in every frame — no back-turn, no
side-profile, no rotation. Static pose-based animation with the feet
stationary and the body not moving forward or backward.
```

**Timing (at 10 fps playback = 0.4s total):**
| Frame | Role | Duration |
|---|---|---|
| 0 | **Ready.** Bow at chest, arrow nocked, rear hand on string. | 0.10s |
| 1 | **Full draw.** Rear hand at cheek, front arm extended, bow at full tension. | 0.10s |
| 2 | **Release / hit-tick / projectile spawn.** Bowstring straight, arrow leaving bow. | 0.10s |
| 3 | **Recovery.** Rear arm lower, bow tilted down. | 0.10s |

**Hit-tick frame:** **frame 2** (release). The arrow is a projectile —
the hit-tick on the Ranger frame fires the projectile spawn, NOT the
damage event. Damage happens when the projectile hits a target, which
is a separate engine event.

**Projectile spawn:**
- **Frame index:** 2.
- **Spawn offset from sprite origin:** the projectile spawns at the
  front hand's position on frame 2 (bow extended forward). The engine
  reads the front-hand keypoint from PixelLab's per-frame keypoint
  metadata and spawns the arrow entity there.
- **Fallback:** if the keypoint is missing for a given direction, use
  a per-direction hardcoded offset (worse experience; avoid by
  verifying keypoints before committing the animation).

### 5. Mage Attack — Custom `action_description` (forward staff swing + gem flare)

**Path:** Custom (`action_description`).
**Frame count:** **4 frames** (PixelLab chose 4 on the validated
one-first render 2026-04-18 — the spec originally called for 5 with
a separate Charge beat, but PixelLab collapsed Charge + Forward-lean
into a single minimal-motion frame. The peak flare on frame 2 is the
hit-tick regardless of whether PixelLab renders 4 or 5 frames).
**Reads as:** the Mage brings the staff forward in an arc from a
ready pose while the staff-gem glows brighter, culminating in a peak
flare on the release frame where the projectile exits. The gem
visual IS the release cue.

**`action_description` prompt to feed to `animate_character`** (pose-based pattern validated on Warrior cleave 2026-04-18):

```
The character stands in place facing the camera (south direction)
throughout all frames — does NOT walk, does NOT turn, does NOT change
facing direction, does NOT pivot. The feet stay planted on the ground
in every single frame. The character holds a staff vertically in the
dominant hand with the staff-gem at the top.

Pose progression across the animation:
- Frame 0 (Ready): staff held vertical at the side of the body, gem
  dim.
- Frame 1 (Charge): staff still vertical but drawn back slightly, gem
  glowing with a subtle royal-violet light.
- Frame 2 (Forward-lean): staff tilted forward at an angle in front
  of the body, gem glowing brighter.
- Frame 3 (Peak flare): staff at the forward peak position extended
  in front of the body, gem flares bright royal-violet light (this is
  the projectile-spawn frame).
- Frame 4 (Recovery): staff returning toward vertical at the side,
  gem glow fading.

The staff stays gripped in the hand in every frame. The wizard hat
stays on the head in every frame. The character faces south (toward
the camera) in every frame — no back-turn, no side-profile, no
rotation. Static pose-based animation with the feet stationary and
the body not moving forward or backward.
```

**Timing (at 10 fps playback = 0.4s total for 4-frame output):**
| Frame | Role | Duration |
|---|---|---|
| 0 | **Ready.** Staff vertical at side, gem dim. | 0.10s |
| 1 | **Charge (minimal motion).** Staff still at side, subtle draw-back; PixelLab renders this as near-identical to Ready. | 0.10s |
| 2 | **Peak flare / hit-tick / projectile spawn.** Staff tilted forward across body, gem flares bright royal-violet. | 0.10s |
| 3 | **Recovery.** Staff returns to vertical at side, gem glow fades. | 0.10s |

**Hit-tick frame:** **frame 2** (peak flare, per the 4-frame output).
Like the Ranger, the hit-tick on the Mage frame is the projectile
spawn event, not the damage event.

**Projectile spawn:**
- **Frame index:** 2.
- **Spawn offset:** at the staff-tip / gem keypoint on frame 2. The
  bright royal-violet flare baked into the sprite frame is the
  visual cue for spawn — the spell visually "launches" from exactly
  where the gem is. Engine reads the staff-tip keypoint from
  PixelLab metadata and spawns the projectile there.
- **Fallback:** same as Ranger — per-direction hardcoded offset if
  keypoint missing.

**Gem color observation (2026-04-18).** The canonical Mage v4 sprite
has the staff-gem at royal-violet `#5b47a0` per SPEC-CLASS-COLOR-
CODING-01. In both the walk-cycle and attack animations, the gem
renders slightly **blue-violet / indigo at rest** and shifts to a
**brighter magenta-violet at the flare peak**. This is a character-
level palette quirk that propagates through all animations of this
character (animations inherit the character's palette), not a prompt
issue. Acceptable at MVP; if PO wants exact `#5b47a0` fidelity, the
only fix is regenerating the Mage character with a more pigment-
explicit description ("deep royal-violet, specifically the hex
#5b47a0, NOT blue, NOT indigo"). Not recommended unless PO flags it.

### 6. Cost Summary

| Class | Path | Estimated gens |
|---|---|---|
| Warrior | Custom (downward cleave) | ~20-40 |
| Ranger | Custom (pull-and-release) | ~20-40 |
| Mage | Custom (forward staff + gem flare) | ~20-40 |
| **Total** | | **~60-120** |

All three animations cover all 8 directions per
`animate_character` semantics. Actual cost depends on frame count and
size; the ~20-40 range is the `confirm_cost` estimate band observed
for 4-6 frame humanoid customs at size 64.

### 7. Escalation Paths (If Rendered Animation Rejected in Playtesting)

For each class, if the rendered custom animation reads wrong to the PO
after first playthrough, the remediation path is locked here so we
don't re-debate it mid-render:

- **Custom prompt misses the intent.** Re-author the
  `action_description` with more prescriptive language on the
  specific beat that read wrong (e.g., "the sword path is vertical,
  not diagonal" for Warrior). Re-run `animate_character`. Budget: two
  additional custom attempts per class. If three custom attempts all
  fail, the attack gets kicked to a dedicated art-direction session
  before spending more credits.
- **Keypoint metadata missing / wrong.** If projectile spawn lands in
  the wrong screen position (Ranger arrow / Mage spell exits from the
  character's chest instead of the bow/gem), the fallback is a per-
  direction hardcoded spawn offset table in engine code. Does not
  require regeneration. Worse than keypoint-driven spawn, but
  ship-acceptable.
- **No template fallback exists.** Unlike the prior spec draft, we
  do not keep a template fallback. The templates were rejected for
  motion-intent reasons; falling back to them would ship the animation
  the PO specifically rejected. If custom fails repeatedly, the
  correct response is deadline-pressure escalation (delay attack
  animation to post-MVP, ship with a placeholder), not template
  fallback.

### 8. Engine-Side Consumption Contract

Notes for the implementer agent whenever it wires these animations
into Godot:

- **Playback speed:** 10 fps default (one frame per 100ms). Per-class
  tuning happens via `AnimatedSprite2D.speed_scale` in the combat
  system, NOT by regenerating frames. Each class's ability cooldown
  and animation runtime should be tuned together: if the combat
  designer wants the Warrior attack to take 0.3s instead of 0.4s,
  `speed_scale = 1.33` — no regen.
- **Hit-tick frame is an engine constant per class**, exported from
  this spec into `scripts/logic/PlayerClass.cs` (or wherever class
  data lives) as a `HitFrame` field:
  - Warrior: `HitFrame = 2`
  - Ranger: `HitFrame = 2` (also the `ProjectileSpawnFrame`)
  - Mage: `HitFrame = 2` (also the `ProjectileSpawnFrame`) — per the
    4-frame output PixelLab produced on the validated 2026-04-18
    render. All three class attack hit-ticks landed on frame 2 by
    coincidence; this may simplify engine wiring.
- **Projectile spawn is a signal**, fired from the AnimatedSprite2D's
  `frame_changed` callback when `frame == ProjectileSpawnFrame`. The
  signal payload carries the spawn position (read from the keypoint
  metadata for the current frame + direction).
- **Animation doesn't cancel mid-playback.** Once the player commits
  to an attack, all frames play out. Movement is locked during attack
  frames. Commitment weight matters more than cancel-fluidity for a
  Diablo-1-inspired game. If the combat designer wants cancel-on-hit
  or cancel-on-move, that is a separate spec change.
- **Attack animation triggers on skill/ability activation ONLY, not
  on auto-attack.** Per PO direction 2026-04-18, the full attack
  animation is reserved for deliberate skill/ability casts. Basic
  auto-attacks use damage-number floaters + brief hit-flash for
  feedback but must NOT call `play("attack")`. If every auto-attack
  tick plays the animation, the character is perpetually mid-swing
  and the silhouette read collapses. The animation must stay a
  deliberate "ability fired" signal.
- **Weapons are baked into the sprite** per
  `feedback_mvp_animation_scope.md`. The attack animation must render
  with the Warrior's sword + shield, Ranger's bow + quiver, Mage's
  staff present in all frames — the custom prompt includes "Weapon
  held throughout all frames" specifically to prevent the
  equipment-drop failure mode observed on the Warrior walk animation.

## Acceptance Criteria

- [ ] Each class has a locked `action_description` prompt in §3/§4/§5.
- [ ] Frame count, per-frame role, hit-tick frame, and (for Ranger +
      Mage) projectile-spawn frame are concrete integers for all
      three classes.
- [ ] `player-classes-art.md §6-§8` gains a supersession pointer to
      this spec in its "Locked animation set" subsection for the
      attack animation specifically. Walk and death remain owned by
      that spec (subject to the MVP 3-animation scope per
      `feedback_mvp_animation_scope.md`).
- [ ] `player-class-pipeline.md §3`'s attack row updates to reflect
      the all-custom path for all three classes (or gains a cross-
      reference that the attack animations are locked elsewhere).
- [ ] Escalation paths in §7 remain documented even if playtesting
      never triggers them — they are the "if something breaks" runbook.
- [ ] All three custom `action_description` prompts include explicit
      "Weapon held throughout all frames" language to prevent the
      equipment-drop failure mode.

## Implementation Notes

- **Render sequencing.** When the rendering agent runs, render one
  attack first (recommend Warrior as the simplest beat-structure) and
  review the south-facing frames with the PO before queueing the other
  two. Per `feedback_one_image_first.md`, this protects against
  discovering a PixelLab-side glitch before committing all three
  custom generations.
- **Prompt-interpretation failure mode (discovered 2026-04-18).**
  Warrior attack attempt 1 used prose like "swings the sword straight
  down in a vertical arc." PixelLab interpreted "swings" as a
  character-locomotion verb and generated a turn-and-walk cycle — the
  character pivoted 180° mid-sequence (back to camera) and equipment
  dropped in back-facing frames. **Do NOT use motion verbs that could
  describe character movement** ("swings," "strikes," "travels,"
  "pulls," "releases," "brings down," "throws") in attack prompts.
  The prose-style animation prompt that works for *locomotion*
  (e.g., the Warrior walk success) does not work in reverse — for
  stationary attacks, use explicit per-frame pose descriptions
  ("Frame 0: sword at side pointing down. Frame 1: sword overhead
  pointing up. Frame 2: sword low and forward in front of body.") +
  an explicit anti-locomotion lock ("character stands in place facing
  the camera (south direction) throughout, does NOT walk, does NOT
  turn, does NOT change facing direction, does NOT pivot. The feet
  stay planted on the ground in every single frame"). **VALIDATED
  2026-04-18 via Warrior attempt 2** — pose-based + anti-locomotion
  prompt produced a clean 4-frame downward cleave (all frames
  south-facing, feet planted, vertical sword path, equipment
  preserved throughout). The §3/§4/§5 prompts above now reflect the
  validated pattern.
- **Custom-action confirm_cost protocol.** Each `action_description`
  call must go through the `confirm_cost` opt-in protocol per
  research §2 — first call without `confirm_cost=true` returns the
  estimate, PO approves, second call runs. Do not ever skip this gate.
- **Keypoint metadata is load-bearing.** The per-frame keypoint data
  PixelLab emits is what the engine uses to place projectile spawns
  (§4, §5). Verify the Ranger's frame 2 front-hand keypoint and the
  Mage's frame 3 staff-tip keypoint are valid before committing the
  metadata.json into git. If a keypoint is missing, the engine falls
  back to a hard-coded offset per direction (see §7 escalation).
- **Equipment-drop mitigation.** The Warrior walk animation failed
  twice with `walking` and `walking-6-frames` templates — the
  mannequin template does not track held equipment during certain arm
  phases. Custom `action_description` calls include "Weapon held
  throughout all frames" as positive-prompt insurance. If custom
  attacks also exhibit equipment drop, the fallback is post-
  composite: overlay the weapon from the rotation sprite onto each
  frame. Document as escalation §7.4 if hit.
- **In-hand vs back-slung equipment observation (2026-04-18).**
  Custom walks dispatched under the same prompt pattern produced
  different results based on *where* the equipment sits:
  - **In-hand equipment persists reliably.** Warrior (sword in right
    hand + shield on left arm) and Mage (staff in right hand) both
    rendered 4-frame walk cycles with zero equipment-drop using the
    base "weapon stays in hand" prompt pattern.
  - **Back-slung / torso-attached equipment drops under the same
    prompt.** Ranger attempt 1 (bow slung on back + quiver on back)
    rendered the bow and quiver in frames 0/2 but dropped them in the
    back-swing frames 1/3 — the same failure mode as the template
    walks exhibited on Warrior.
  - **Mitigation:** use frame-by-frame pose anchors (describe
    equipment position explicitly per frame) rather than global
    "stays on the back throughout" language. If that also fails, the
    fallback is regenerating the character with equipment in hand or
    post-compositing the equipment from the rotation sprite.
- **Playback tuning room.** The 10 fps default is a starting point,
  not a lock. Combat-tuning may speed the Warrior up or slow the Mage
  down. Frames are rendered once, speed is free to adjust.
- **The supersession edit to `player-classes-art.md`.** Do not
  rewrite §6/§7/§8 from scratch when pointing them at this spec —
  add a one-line supersession note inside each section's "Locked
  animation set" bullet pointing to `class-attack-animations.md`.
  The rest of those sections (silhouette, colors, fiction beat)
  remains untouched and owned by that spec.

## Open Questions

One decision the PO should weigh in on before rendering runs:

1. **Does combat use no-cancel animation commitment (this spec's
   §8 position) or cancel-on-input?** The frame-count and timing
   tables above assume no-cancel. If cancel-on-input is selected,
   the windup/recovery frames become "interruptible regions" and
   the hit-tick frame becomes the "commit point" — all still
   derivable from the tables but with different engine wiring.

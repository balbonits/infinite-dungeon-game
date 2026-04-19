# Town NPCs — Visual Identity & Sprite Spec (SPEC-NPC-ART-01)

## Summary

The game-facing visual identity spec for the three town NPCs — **Blacksmith**, **Guild Maid**, **Village Chief**. Defines each NPC's fiction beat, services-owned, silhouette readability constraint, defining features, color contract, scale, pose, age range, and direction coverage. Paired with [ART-SPEC-NPC-01 — NPC sprite pipeline](../assets/npc-pipeline.md), which specifies the PixelLab generation half. Co-lock criterion: neither half reaches "Ready-for-impl" alone.

## Current State

> **⚠ ADR-007 reconciliation (2026-04-18):** this spec was authored during the PixelLab + iso era. Per [ADR-007](../decisions/007-top-down-oga-pivot.md), PixelLab is retired for MVP and iso rendering is reverted to top-down. The NPC **roster**, **south-only direction rule**, **NPC-vs-PC silhouette constraints**, and **pose conventions** below remain canon and apply equally to the LPC art pipeline. References to `prompt-templates.md`, `iso-rendering.md`, PixelLab modes, and the `mannequin` template are **superseded**; treat them as historical notes. Replacement pipeline lives at `assets/md/lpc-sprite-recipes.md`.

The NPC roster was reduced and re-mapped on 2026-04-17 (PO decision, captured in the `NPC-ROSTER-REWIRE-01` ticket and the `project_npc_naming` agent memory). The roster is now exactly three: Blacksmith (forge + craft + shop), Guild Maid (bank + teleport / floor-select), Village Chief (quest giver). The earlier roster (Guild Master, Shopkeeper, Banker, Teleporter, Blacksmith, Guild Maid, Village Chief) is deprecated — the dropped roles fold into these three NPCs as services, not separate sprites.

All NPCs are **title-only in-game** (no personal names) per `project_npc_naming` (homage to *Maoyuu Maou Yuusha*). The PC is addressed in dialogue as `{Class} Guildmaster` and is not an NPC.

**Sprite delivery state (updated 2026-04-18 after south-only batch shipped):**
- `assets/characters/npcs/blacksmith/rotations/south.png` — **shipped** (see §9).
- `assets/characters/npcs/guild_maid/rotations/south.png` — **shipped** (see §9).
- `assets/characters/npcs/village_chief/rotations/south.png` — **shipped** (see §9); authored fresh from the §5 design.

This spec mirrors the section discipline of [`docs/world/species-template.md`](species-template.md) (SPEC-SPECIES-01) and the per-character structure of [`docs/world/player-classes-art.md`](player-classes-art.md) (SPEC-PC-ART-01) — adapted for town NPCs, who do not have AI patterns, drop tables, level-relative tint behavior, or combat animations.

Related specs this one pulls from:
- [docs/overview.md](../overview.md) — game vision, frontier-settlement framing.
- [docs/world/town.md](town.md) — town layout, NPC station placement.
- [docs/world/player-classes-art.md](player-classes-art.md) — sibling per-character spec (mirrored here).
- [docs/world/species-template.md](species-template.md) — sibling rigor for monster authoring (mirrored here).
- [docs/assets/prompt-templates.md](../assets/prompt-templates.md) — `CHAR-HUM-ISO` block (the generation foundation art-lead's paired spec extends; current text references the deprecated 7-NPC roster and will be updated by art-lead under ART-SPEC-NPC-01).
- [docs/systems/color-system.md](../systems/color-system.md) — level-relative tint system (NPCs are exempt; see §6 Color-Coding Contract).
- [docs/systems/iso-rendering.md](../systems/iso-rendering.md) — sprite anchor + 128×128 canvas contract.
- [docs/flows/npc-interaction.md](../flows/npc-interaction.md) — dialogue + interaction flow (voice/tone reference for §2 voice-compatibility checks).
- [docs/assets/npc-pipeline.md](../assets/npc-pipeline.md) — paired art half (ART-SPEC-NPC-01).

## Design

### 1. Per-NPC Identity Template

Each NPC fills the same eight-field template below. The structure is identical across NPCs so cross-NPC differentiation is checkable side-by-side.

| Field | What it pins |
|---|---|
| **Fiction beat** | One sentence: who this NPC is in the frontier-guild world, fitting the first-explorer framing. |
| **Services owned** | Explicit list of dialogue-menu services this NPC handles. Must match the locked service mapping. |
| **Silhouette readability constraint** | What MUST read at a glance from 6–8 tiles away. The single hardest field; this is the gameplay-visual hook. |
| **Defining features** | Unique visual cues that distinguish this NPC from any other humanoid on screen (player classes included). |
| **Color-coding contract** | Base palette band + any exempt pixels (signature glow, accent, etc.) per `color-system.md`. |
| **Scale + anchor** | Scale multiplier (player = 1.00 reference), Z-offset, canvas size. |
| **Pose convention** | The single canonical service-stance the NPC holds while idle in town. |
| **Age range + voice compatibility** | Age band + a one-line check that the visual identity matches the dialogue voice in `npc-interaction.md`. |

§3 specifies the direction coverage recommendation (shared across all three NPCs). §4 specifies the NPC-vs-PC differentiation rules. §5 covers Village Chief's fresh-design choice in detail. §§6–8 fill the template completely for all three NPCs with no TBDs.

---

### 2. Service Mapping (locked 2026-04-17)

The full town-services catalog maps onto the three NPCs as follows. This is the contract dialogue and quest systems must read against:

| NPC | Services owned |
|---|---|
| **Blacksmith** | Forge (repair / reinforce equipment), Craft (combine materials → equipment), Shop (buy / sell consumables + base equipment) |
| **Guild Maid** | Bank (stash / withdraw items + currency), Teleport / Floor-Select (warp to unlocked floors) |
| **Village Chief** | Quest giver (per [SYS-04](../systems/quests.md) — issues, accepts, and rewards quests) |

**Why this collapses the prior 7-NPC roster cleanly.** The dropped NPCs (Guild Master / Shopkeeper / Banker / Teleporter) were each single-service kiosks. Folding shop into Blacksmith, bank+teleport into Guild Maid, and quests into Village Chief gives every remaining NPC a coherent identity (craftsperson / hospitality + logistics / authority figure) instead of a function-name label. It also removes the code-vs-spec gap where quest-giver wiring had drifted onto "Guild Master" in implementation while design always intended Village Chief.

---

### 3. Direction Coverage — South-facing only (LOCKED 2026-04-18)

**Locked decision: ONE south-facing idle frame per NPC. No rotations. No walk cycle.**

PO direction 2026-04-18: *"NPCs do not move, so, let's skip generating the rest of the angles/directions ... we're gonna save pixellab renders/tokens this way."*

Rationale:
- NPCs are stationary in town — they occupy a fixed station tile and never path. No walk cycle, no turn-to-face-player.
- The player approaches the NPC from the front; south-facing is the canonical camera angle, and other rotations are never seen.
- Only the south frame is extracted, committed to the repo, and referenced from the scene. Non-south frames (if the underlying art source produces them as a spritesheet) are discarded at import, not stored.
- "Turn to face the player" UX feedback is replaced by the existing NpcPanel interaction (panel fades in centered on screen) — no sprite turn needed.

*(Original PO framing cited PixelLab credit savings; post-[ADR-007](../decisions/007-top-down-oga-pivot.md) the savings realized are storage + scene-wiring simplicity rather than generation credits, but the south-only rule and its behavioral justification stand.)*

**Frame budget per NPC:** 1 idle frame (south). No rotations, no walk, no attack, no death. **Total: 1 frame × 3 NPCs = 3 NPC frames** for the entire town roster.

**Global rule — who gets rotations + animations at all:** only entities that actually move in the game. Players, enemies, bosses, projectiles = 8-direction + animation sets. NPCs, containers, map objects, tiles, buildings = single-frame. (This rule is carried in the `feedback_animation_scope` agent memory; no standalone docs/conventions file has been authored.)

**Code-side implication:** `Npc.cs` uses a single `Sprite2D` with `SpritePath` pointing at `rotations/south.png` directly — no `DirectionalSprite` hookup needed. The `rotations/` subfolder is named speculatively but contains only `south.png` for each stationary NPC. Don't rename the subfolder unless a broader refactor demands it.

---

### 4. NPC-vs-PC Differentiation Rules (hard constraints)

NPCs must never be mistaken for player classes at a glance. The player must always be able to find their own sprite in town instantly. Locked silhouette rules:

| Rule | Why |
|---|---|
| **No NPC wears plate armor.** Leather aprons (Blacksmith), cloth uniforms (Guild Maid), and robes (Village Chief) are allowed; plate cuirass / pauldrons / greaves are reserved for the Warrior. | Distinguishes from Warrior at thumbnail scale. |
| **No NPC carries a bow or quiver.** | Distinguishes from Ranger. |
| **No NPC wears a pointed wizard hat.** Soft caps, headscarves, hoods, or bareheaded are fine. | Distinguishes from Mage. |
| **No NPC carries a staff with a glowing crystal / orb head.** Village Chief's walking staff is allowed but must be a plain carved-wood staff with no glow, no exempt-pixel accent, no crystal head. | Distinguishes from Mage. |
| **No NPC carries the player-blue (`#8ed6ff`) accent.** That color is the PC's identity signature per [SPEC-PC-ART-01 §5](player-classes-art.md). NPCs use warm/neutral/earth-tone accents only (bronze, brass, silver, deep red, sage green) so the eye reads "this is not me." | Reinforces PC identity through color, not just silhouette. |
| **Default NPC pose is non-combat.** No fight-stance, no weapon held aggressively. Service-stance only (see per-NPC §6–8 poses). | Reinforces "this is not a thing I fight or compete with" at a glance. |

**Acceptance test for these rules:** export each NPC's south-facing idle frame at 64×64 alongside the three PC class south-facing idles. A reviewer who has not seen the labels can sort the six sprites into "PC" vs "NPC" with 100% accuracy.

---

### 5. Village Chief Fresh-Design Section

Village Chief had no existing sprite at the time this section was authored; it was a from-scratch design, and the resulting south-facing sprite has since shipped at `assets/characters/npcs/village_chief/rotations/south.png` (see §9). The locked archetype below remains the reference for any future regeneration (including under the LPC pipeline per ADR-007):

> **An elderly figure in muted-green hooded robes with a silver chain-of-office across the chest, leaning on a gnarled wooden walking staff, with a long white beard or weathered kindly features.**

Why this archetype:
- **Silhouette differentiator vs. Mage.** The Mage is the tallest PC class (pointed hat extends 12–16 px above headline per [SPEC-PC-ART-01 §2](player-classes-art.md)). Village Chief is roughly the same overall height but with a **stooped, forward-leaning** posture (leaning on the staff) and a **rounded soft-hood** silhouette (no pointed apex). Compare and contrast at thumbnail: Mage is "vertical line with a triangular top"; Village Chief is "comma-shaped with a rounded top."
- **Silhouette differentiator vs. Warrior / Ranger.** Robe hem flares at the bottom (read as "civic / ceremonial dress"); no shoulder armor; no compact lean profile.
- **Service-readability ("quest giver").** The chain-of-office + robes signal authority and ceremony — the player reads "this person dispenses tasks" the same way they'd read a mayor or village elder in any classic ARPG. The walking staff signals "settled / immobile" rather than "adventuring," reinforcing that quests come *from* him, the player executes them.
- **Frontier-lore consistency.** Per the `project_frontier_lore` memory: the town is a frontier settlement; the Village Chief is the eldest pioneer and de facto authority. Elderly + civic robes + chain-of-office reads as "founder-elder," not "wandering wizard."
- **Beard / gender.** Long white beard is the recommendation because it is the single most legible "elderly authority figure" cue at 64×64 — but if PixelLab's `mannequin` template defaults to a beardless face that still reads as elderly via wrinkles + posture + white hair, that is acceptable. Gender is not prescribed (per [SPEC-PC-ART-01 §3](player-classes-art.md) shared neutrality rule); art-lead picks the face that best meets the silhouette goals.

---

### 6. Worked Spec — Blacksmith

> **Direction coverage (revised 2026-04-18):** south-facing only. NPCs are stationary in the town scene — no north/east/west frames generated. See §3 for the global rule.

> **Defining-features update (revised 2026-04-18 after theme-review generation):** the single horn-scale pauldron and bald-top/braided-beard details did not render reliably in the first PixelLab pass and the PO accepted the sprite as-is. Those two details are dropped from this spec; the approved sprite carries a hammer-on-shoulder pose, leather apron, belt buckle, and medium-hair + short beard. The identifying visual is now the hammer pose + apron silhouette, not the pauldron memento.

- **Fiction beat.** A retired adventurer turned smith, the one who first hammered out the guildmasters' starter gear and now teaches each new arrival how to shape their own.
- **Services owned.** Forge (repair / reinforce equipment), Craft (combine materials → equipment), Shop (buy / sell consumables + base equipment).
- **Silhouette readability constraint.** From 6–8 tiles away, the Blacksmith must read as **a stout, broad-shouldered figure with a hammer over one shoulder and a heavy leather apron**. Specifically: visibly broader at the shoulders than a baseline humanoid silhouette (apron + rolled sleeves account for the breadth), one arm raised to rest a long-hafted hammer-head against the shoulder so the hammer adds a vertical dark mass to the silhouette's right side, the apron extends from chest to mid-thigh as a single dark trapezoid that anchors the figure to the ground. Rationale: "smith" must be readable without the player walking up to see the anvil — the hammer-on-shoulder is the universal shorthand and the silhouette must commit to it.
- **Defining features.**
  - Leather apron trapezoid extending from chest to mid-thigh (the primary silhouette anchor).
  - Belt buckle (brass / gold) centered at the waist — small exempt-pixel metallic accent.
  - Rolled-sleeve forearms visible below the apron — reads as "active workman."
  - Hammer resting against the right shoulder, head up — the universal smith shorthand.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated (NPCs are exempt from `color-system.md`).
  - Base palette band: warm browns (apron leather), iron grey (hammer head, hammer haft furniture), skin tone with soot accents.
  - **Exempt pixels:** gold belt buckle (~3-5 pixels) — warm metallic accent at the waist. No player-blue anywhere on the sprite (per §4).
- **Scale + anchor.** Scale multiplier = **0.95×** (NPCs slightly smaller than PC reference 1.00 per `species-template.md` §6 small-scale convention — reads as non-threatening, signals "townsfolk not adventurer"). Z-offset = 0. Canvas = 92×92 (PixelLab's actual output at `size=64`; downstream scene import handles the scale without a canvas-extend step).
- **Pose convention.** **Hammer-on-shoulder lean:** weight on one foot, one hand resting the hammer-head on the shoulder, free hand at side. South-facing canonical pose only.
- **Age range + voice compatibility.** Age **45–55** (mid-life craftsman, retired-but-still-strong). Gender not prescribed. Voice in `npc-interaction.md` is direct, blunt, paternal — compatible with a weathered mid-life craftsman silhouette.

---

### 7. Worked Spec — Guild Maid

> **Direction coverage:** south-facing only (see §3). **Defining-features update (revised 2026-04-18 after theme-review generation):** the teleport pendant, keyring-at-hip, and ledger book did not resolve cleanly at 92×92 — PixelLab rendered them as clasped hands + plain apron. The PO accepted the sprite as-is. Those accessory details are dropped from this spec; identity now rests on the hourglass apron silhouette + low-bun hair + navy-over-cream palette.

- **Fiction beat.** The guild's quartermaster and gatekeeper of the deep-floor passages — keeps the ledger, holds the keys, and operates the teleport pendant that returns adventurers to floors they've already cleared. (Fiction retained; the visible sprite conveys her role via apron silhouette + professional pose rather than visible accessories.)
- **Services owned.** Bank (stash / withdraw items + currency), Teleport / Floor-Select (warp to unlocked floors).
- **Silhouette readability constraint.** From 6–8 tiles away, the Guild Maid must read as **a neat figure in a maid-uniform silhouette — long apron over a knee-length dress, hair tied back**. The hourglass waist-narrowing apron line is the silhouette feature that no PC class or other NPC shares. The "service professional" read is carried by the apron + bun combination, not by held accessories.
- **Defining features.**
  - **Neat trimmed apron** (white/cream, full-length over the dress, tied at the waist — the hourglass-silhouette anchor).
  - **Hair tied back** (low bun) — reads as "professional / on-duty."
  - Dress in deep navy or charcoal under the apron — distinguishes from Warrior grey and Mage robes.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Base palette band: cream / off-white (apron), deep navy or charcoal (dress under the apron).
  - **Exempt pixels:** none required at v1. If a future regen surfaces a pendant or keys clearly, they can be added as exempt-color accents. **No player-blue anywhere on the sprite** (per §4).
- **Scale + anchor.** Scale multiplier = **0.95×**. Z-offset = 0. Canvas = 92×92 (PixelLab `size=64` actual).
- **Pose convention.** **South-facing neutral stance** — hands together or at sides, weight evenly distributed, slight forward inclination as if attentively listening. South-facing only per §3.
- **Age range + voice compatibility.** Age **20–30** (early-career professional, hospitable but disciplined). Gender presents feminine via the maid-uniform silhouette (this is a costume / role identifier in the *Maoyuu* homage tradition, not a body-type prescription — face / build remain in PixelLab's neutral default range). Voice in `npc-interaction.md` is polite, formal, slightly reserved — compatible with a young-professional-quartermaster silhouette.

---

### 8. Worked Spec — Village Chief

> **Direction coverage:** south-facing only (see §3). **Defining-features update (revised 2026-04-18 after theme-review generation):** the stooped posture and silver chain-of-office did not render cleanly — PixelLab produced a fairly upright stance and a small neck accent that reads as a pendant rather than a chain. The PO accepted the sprite as-is. Those two details are dropped from this spec; identity now rests on sage-green robes + white hair + beard + wooden walking staff.

- **Fiction beat.** The eldest pioneer of the frontier settlement, the one who buried the founders and now decides which dungeon dangers the guild should answer next; speaks for the village and hands the guildmasters their tasks.
- **Services owned.** Quest giver (per [SYS-04](../systems/quests.md) — issues, accepts, and rewards quests).
- **Silhouette readability constraint.** From 6–8 tiles away, the Village Chief must read as **an elderly authority figure in long muted-green robes with a tall walking staff and visible white hair + beard**. The robe-hem trapezoid brushes the ground (distinguishes from any PC class hem); the staff reaches head-height (not above, to distinguish from a wizard staff). Rationale: "elder-with-staff" is the universal civic-elder shorthand. The elder read is carried by the white hair + beard + sage-green robes combination; the stooped posture that was previously the load-bearing differentiator has been dropped per the sprite accepted above.
- **Defining features.**
  - **Long white beard + long white hair or cream tones** — the primary "elder" cue.
  - **Sage-green full-length robe** — the dominant silhouette feature. Muted green fits the frontier-village palette.
  - **Gnarled wooden walking staff** — knotted plain wood, head-height, no crystal / no orb / no glow (per §4 hard constraint).
  - **Small neck accent** (pendant or small chain cluster) at the throat — rendered by PixelLab as a single-cluster accent rather than a full chain-of-office. Reads as "civic mark" even if not legible as a linked chain.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Base palette band: muted sage green or deep forest green (robe body), cream/ivory (beard, hair), warm brown (staff wood).
  - **Exempt pixels:** the neck accent (~3–5 pixels) — stays silver/metallic regardless of any future tint. No player-blue anywhere (per §4).
- **Scale + anchor.** Scale multiplier = **0.95×** (NPC standard — the town reads as a consistent population scale relative to the player). Z-offset = 0. Canvas = 92×92 (PixelLab `size=64` actual).
- **Pose convention.** **South-facing neutral stance** with staff held vertically to the side. Upright posture acceptable (stooped-posture direction was tried but didn't render — dropped). South-facing only per §3.
- **Age range + voice compatibility.** Age **60–80** (visibly elderly — the only NPC outside the working-age band; Blacksmith and Guild Maid are explicitly NOT in this band to keep the elder read unique to the Chief). Gender not prescribed; long beard recommendation skews masculine but is not required. Voice in `npc-interaction.md` is wise, measured, slow-cadenced — compatible with the elderly-authority silhouette.

---

### 9. Art-Spec Pairing

Paired art ticket: **ART-SPEC-NPC-01** ([docs/assets/npc-pipeline.md](../assets/npc-pipeline.md)) — the generation-side half. Updated 2026-04-18 scope: **south-facing-only sprites, 1 frame per NPC, 3 total frames for the town**.

Pairing status: `[x] design spec locked  [x] art spec delivered  [x] sprites delivered (2026-04-18)`

All three NPC sprites shipped on 2026-04-18 at `assets/characters/npcs/{blacksmith,guild_maid,village_chief}/rotations/south.png`. Placeholder rotation frames in other directions have been deleted per the south-only rule (§3).

## Acceptance Criteria

- [ ] An art-lead reading this spec plus the paired [ART-SPEC-NPC-01](../assets/npc-pipeline.md) can author the Blacksmith, Guild Maid, and Village Chief PixelLab prompts and animation recipes without asking design-lead a clarifying question.
- [ ] Every field in the §1 template has a concrete value for all three NPCs (no TBDs in §§6 / 7 / 8). Verifiable by row-by-row review.
- [ ] All three NPC silhouettes are distinguishable from all three PC class silhouettes at 64×64 thumbnail scale per the §4 acceptance test (export south-facing idles, sort PC vs NPC with 100% accuracy).
- [ ] All three NPCs' services read from their visual identity: a player who has never seen these NPCs can guess Blacksmith handles forge/craft/shop, Guild Maid handles bank/teleport, and Village Chief handles quests, by silhouette + defining-features alone.
- [ ] No NPC sprite carries the player-blue (`#8ed6ff`) accent (per §4 hard constraint).
- [ ] No NPC sprite is runtime-modulated by `color-system.md` (NPCs are exempt; same as PCs per [SPEC-PC-ART-01 §5](player-classes-art.md)).
- [x] South-only single-frame budget per NPC (1 × 3 = 3 frames total) honored.
- [ ] Every NPC dialogue label and quest-system reference uses the title-only form (`Blacksmith`, `Guild Maid`, `Village Chief`) — never a personal name (per `project_npc_naming` memory).
- [ ] The Village Chief fresh-design archetype (§5) is implemented as specified — no substitution to a different archetype without re-opening this spec.

## Implementation Notes

- **Pairing with [ART-SPEC-NPC-01](../assets/npc-pipeline.md) is load-bearing.** The two specs are a single logical unit. Any change to fields in §§6–8 (defining features, silhouette constraint, color-accent location, pose) triggers a regeneration cycle on the art side.
- **NPCs are static. No rotations, no walk cycle, no combat animation.** The 1-frame-south budget per NPC (3 frames total for the town) is the entire animation set v1, per the "rotations + animations only for moving entities" rule. Talking-head animation, gesture cues, and reactive expressions are deferred to a later ticket and are nice-to-have, not blockers.
- **Service mapping is the contract for dialogue / quest wiring.** Engineer-side, the dialogue-menu builder and the quest-system entrypoint must read against the §2 table — Blacksmith owns forge/craft/shop, Guild Maid owns bank/teleport, Village Chief owns quests. Any code that wires a service to the wrong NPC is a bug against this spec, not a design choice. (The historical "Guild Master = quest giver" wiring was such a bug; `NPC-ROSTER-REWIRE-01` corrects it.)
- **Scale = 0.95× is deliberate.** NPCs are slightly smaller than the PC reference (1.00) so the town reads as "the player is the protagonist; these are townsfolk." A 1.00 NPC scale would make the player feel like one-of-many; a 0.85× would make NPCs read as children. 0.95× is the goldilocks point.
- **No player-blue on NPCs is the hardest rule to remember.** Art-lead's instinct on the Guild Maid's teleport pendant will be to make it cyan/blue (because "teleport = magic = blue" is the genre cliché). It must not be. Sage green or deep red is the locked alternative. This is what protects PC identity in town: the player's own sprite is the only blue-accented humanoid on screen.
- **Direction coverage is south-only, not 4-dir, not 8-dir.** PixelLab's standard mode requires `n_directions=4` minimum, so the generation cost is 4 frames even though only south is saved; the other three are simply not downloaded. This saves commit + storage cost but not PixelLab generation cost. Do not let the template default trick the pipeline into saving all 4 — extract south.png only.
- **Village Chief's staff is plain wood, NOT the Mage staff.** This is the easiest visual confusion to introduce by accident — both are "robed figure with vertical staff." The differentiators (white beard + hair, sage-green robe, plain wooden staff, no hat, head-height staff not above-head) must hold. Cutting them risks a Mage-confusion bug at thumbnail scale.
- **Voice / dialogue is referenced, not authored here.** The voice-compatibility line in §§6–8 is a sanity check that the silhouette and the dialogue voice agree. The actual dialogue text lives in `docs/flows/npc-interaction.md` and the per-NPC dialogue strings file when one is authored.

## Open Questions

None — spec is locked. The PO decisions on the 3-NPC roster, the service mapping, the title-only naming convention, and the Village Chief fresh-design archetype (all 2026-04-17) closed the outstanding tradeoffs. Per-NPC fields are concrete. Art-pairing checkbox status is the only remaining state, and that resolves when ART-SPEC-NPC-01 locks.

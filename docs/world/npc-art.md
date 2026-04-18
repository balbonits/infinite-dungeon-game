# Town NPCs — Visual Identity & Sprite Spec (SPEC-NPC-ART-01)

## Summary

The game-facing visual identity spec for the three town NPCs — **Blacksmith**, **Guild Maid**, **Village Chief**. Defines each NPC's fiction beat, services-owned, silhouette readability constraint, defining features, color contract, scale, pose, age range, and direction coverage. Paired with [ART-SPEC-NPC-01 — NPC sprite pipeline](../assets/npc-pipeline.md), which specifies the PixelLab generation half. Co-lock criterion: neither half reaches "Ready-for-impl" alone.

## Current State

The NPC roster was reduced and re-mapped on 2026-04-17 (PO decision, captured in the `NPC-ROSTER-REWIRE-01` ticket and the `project_npc_naming` agent memory). The roster is now exactly three: Blacksmith (forge + craft + shop), Guild Maid (bank + teleport / floor-select), Village Chief (quest giver). The earlier roster (Guild Master, Shopkeeper, Banker, Teleporter, Blacksmith, Guild Maid, Village Chief) is deprecated — the dropped roles fold into these three NPCs as services, not separate sprites.

All NPCs are **title-only in-game** (no personal names) per `project_npc_naming` (homage to *Maoyuu Maou Yuusha*). The PC is addressed in dialogue as `{Class} Guildmaster` and is not an NPC.

Sprite directory state at spec-lock (verify before generation per art-pipeline):
- `assets/characters/npcs/blacksmith/` — exists, slated for redraw under the iso pivot.
- `assets/characters/npcs/guild_maid/` — exists, slated for redraw under the iso pivot.
- `assets/characters/npcs/village_chief/` — **does not exist**; this spec authors a fresh design (see §5).

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

### 3. Direction Coverage Recommendation

**Recommendation to art-lead: 4-direction (N / S / E / W) with one idle frame per direction. No walk cycle.**

Rationale:
- NPCs are static in town. They occupy a fixed station tile and never path. The walk cycle that the player sprite needs is dead weight for an NPC that never walks.
- The only directional behavior NPCs need is "turn to face the player when interaction begins." A 4-direction set covers this — when the player is approached from NE, the NPC picks the closest cardinal (N or E) and faces it. The visual pop of "the NPC turned to look at me" is the load-bearing feel beat; sub-cardinal precision is not.
- 8-direction (the player-class default) doubles asset cost and PixelLab credits for zero gameplay gain, since NPCs never traverse iso space.
- 2-direction (face / back) would technically work — NPCs could simply face south by default and rotate to face the player on interaction — but loses the "the shopkeeper is busy at the anvil and turns to greet you" beat that 4-direction enables, and breaks down at the station tile when the player approaches from behind.

**Frame budget per NPC (recommended):** 4 idle frames (N / S / E / W) × 1 pose. No walk, no attack, no death. **Total: 4 frames × 3 NPCs = 12 NPC frames** for the entire town roster.

**If art-lead must reduce further to fit credits:** drop to single-facing-south + an "interact-turn" cue handled in code (e.g., a brief sprite flash or a small "!" indicator). This is acceptable as a v1 fallback but loses the directional-greeting beat. Flag this tradeoff in the art-pipeline spec rather than silently dropping below 4-direction.

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

Village Chief has no existing sprite directory; this is a from-scratch design. The locked archetype:

> **An elderly figure in muted-green hooded robes with a silver chain-of-office across the chest, leaning on a gnarled wooden walking staff, with a long white beard or weathered kindly features.**

Why this archetype:
- **Silhouette differentiator vs. Mage.** The Mage is the tallest PC class (pointed hat extends 12–16 px above headline per [SPEC-PC-ART-01 §2](player-classes-art.md)). Village Chief is roughly the same overall height but with a **stooped, forward-leaning** posture (leaning on the staff) and a **rounded soft-hood** silhouette (no pointed apex). Compare and contrast at thumbnail: Mage is "vertical line with a triangular top"; Village Chief is "comma-shaped with a rounded top."
- **Silhouette differentiator vs. Warrior / Ranger.** Robe hem flares at the bottom (read as "civic / ceremonial dress"); no shoulder armor; no compact lean profile.
- **Service-readability ("quest giver").** The chain-of-office + robes signal authority and ceremony — the player reads "this person dispenses tasks" the same way they'd read a mayor or village elder in any classic ARPG. The walking staff signals "settled / immobile" rather than "adventuring," reinforcing that quests come *from* him, the player executes them.
- **Frontier-lore consistency.** Per the `project_frontier_lore` memory: the town is a frontier settlement; the Village Chief is the eldest pioneer and de facto authority. Elderly + civic robes + chain-of-office reads as "founder-elder," not "wandering wizard."
- **Beard / gender.** Long white beard is the recommendation because it is the single most legible "elderly authority figure" cue at 64×64 — but if PixelLab's `mannequin` template defaults to a beardless face that still reads as elderly via wrinkles + posture + white hair, that is acceptable. Gender is not prescribed (per [SPEC-PC-ART-01 §3](player-classes-art.md) shared neutrality rule); art-lead picks the face that best meets the silhouette goals.

---

### 6. Worked Spec — Blacksmith

- **Fiction beat.** A retired adventurer turned smith, the one who first hammered out the guildmasters' starter gear and now teaches each new arrival how to shape their own.
- **Services owned.** Forge (repair / reinforce equipment), Craft (combine materials → equipment), Shop (buy / sell consumables + base equipment).
- **Silhouette readability constraint.** From 6–8 tiles away, the Blacksmith must read as **a stout, broad-shouldered figure with a hammer over one shoulder and a heavy leather apron**. Specifically: visibly broader at the shoulders than a baseline humanoid silhouette (apron + rolled sleeves account for the breadth), one arm raised to rest a long-hafted hammer-head against the shoulder so the hammer adds a vertical dark mass to the silhouette's right side, the apron extends from chest to mid-thigh as a single dark trapezoid that anchors the figure to the ground. Rationale: "smith" must be readable without the player walking up to see the anvil — the hammer-on-shoulder is the universal shorthand and the silhouette must commit to it.
- **Defining features.**
  - Soot-streaked face / forearms (texture detail; need not be palette-clamp — palette deviation here is acceptable per art-spec).
  - **Single horn-scale pauldron** on the off-shoulder (the one without the hammer): a curved bone/horn fragment lashed with leather straps — a memento from the Blacksmith's old adventuring days. This is the one identifying flourish that makes the Blacksmith specifically this Blacksmith and not a generic smith icon.
  - Bareheaded (no hat / hood / helmet) — distinguishes from all PC classes, all other NPCs, and reinforces "working at the forge."
  - Rolled-sleeve forearms visible (not covered by apron) — reads as "active workman."
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated (NPCs are exempt from `color-system.md`).
  - Base palette band: warm browns (apron leather), iron grey (hammer head, hammer haft furniture), skin tone with soot accents.
  - **Exempt pixels (small, non-modulated runtime accents permitted):** the horn-scale pauldron carries a small **bronze / brass tie-band** (warm metallic accent, ~6–10 pixels) — this is the color signature for the Blacksmith specifically and must remain readable. No player-blue anywhere on the sprite (per §4).
- **Scale + anchor.** Scale multiplier = **0.95×** (NPCs slightly smaller than PC reference 1.00 per `species-template.md` §6 small-scale convention — reads as non-threatening, signals "townsfolk not adventurer"). Z-offset = 0. Canvas = 128×128. Sprite import offset `Vector2(0, -80)` per `CHAR-HUM-ISO`.
- **Pose convention.** **Hammer-on-shoulder lean:** weight on one foot, one hand resting the hammer-head on the shoulder, free hand on hip or at side. This is the canonical idle pose held in all 4 directions per §3.
- **Age range + voice compatibility.** Age **45–55** (mid-life craftsman, retired-but-still-strong). Gender not prescribed. Voice in `npc-interaction.md` is direct, blunt, paternal — compatible with a weathered mid-life craftsman silhouette.

---

### 7. Worked Spec — Guild Maid

- **Fiction beat.** The guild's quartermaster and gatekeeper of the deep-floor passages — keeps the ledger, holds the keys, and operates the teleport pendant that returns adventurers to floors they've already cleared.
- **Services owned.** Bank (stash / withdraw items + currency), Teleport / Floor-Select (warp to unlocked floors).
- **Silhouette readability constraint.** From 6–8 tiles away, the Guild Maid must read as **a neat figure in a maid-uniform silhouette — long apron over a knee-length dress, holding a ledger book and a keyring at the hip, with a small pendant at the throat**. Specifically: hourglass silhouette (apron tied at the waist creates a clear waist-narrowing line that no other character on screen has), one hand cradling a closed ledger at chest height, the other hand visible at the hip with a keyring hanging from the belt that breaks the silhouette's left edge with 3–5 dangling key shapes. Rationale: "ledger + keys" is the universal shorthand for "this person handles your stuff and your access." The waist-narrowing apron line is the silhouette feature that no PC class shares.
- **Defining features.**
  - **Neat trimmed apron** (white/cream, full-length over the dress, with a tied bow visible at the back when facing N — single visible apron strap when facing E/W).
  - **Teleport pendant at the throat** — a small carved disc or geometric symbol on a cord. The pendant is the visual signature for the teleport service; it is the smallest and most central exempt-color accent on the sprite (see color contract).
  - **Keyring at the left hip** — 3–5 visible keys hanging from a hoop ring, dangling 8–12 px below the belt line. Reads as "she has access to things you don't."
  - **Ledger book** held closed at chest height with one hand. Bookmark ribbon visible.
  - Hair tied back (low bun or simple tie) — reads as "professional / on-duty."
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Base palette band: cream / off-white (apron), deep navy or charcoal (dress under the apron), brass (keyring, ledger corners).
  - **Exempt pixels:** the **teleport pendant** is a small **sage-green or deep-red exempt accent** (3–5 pixels max) — distinct from any PC accent color so it reads as "this is *her* signature, not the player's." The keyring brass is a normal palette-clamp metallic, not exempt. **No player-blue anywhere on the sprite** (per §4) — the pendant must NOT be `#8ed6ff` or any near-cyan, even though "teleport" might suggest a cool color; this is a hard constraint to preserve PC identity.
- **Scale + anchor.** Scale multiplier = **0.95×**. Z-offset = 0. Canvas = 128×128. Sprite import offset `Vector2(0, -80)`.
- **Pose convention.** **Hands-folded-with-ledger:** ledger held at chest height with the off-hand, dominant hand resting on top of the ledger or at the hip near the keyring, weight evenly distributed, slight forward inclination as if attentively listening. Held in all 4 directions per §3.
- **Age range + voice compatibility.** Age **20–30** (early-career professional, hospitable but disciplined). Gender presents feminine via the maid-uniform silhouette (this is a costume / role identifier in the *Maoyuu* homage tradition, not a body-type prescription — face / build remain in PixelLab's neutral default range). Voice in `npc-interaction.md` is polite, formal, slightly reserved — compatible with a young-professional-quartermaster silhouette.

---

### 8. Worked Spec — Village Chief

- **Fiction beat.** The eldest pioneer of the frontier settlement, the one who buried the founders and now decides which dungeon dangers the guild should answer next; speaks for the village and hands the guildmasters their tasks.
- **Services owned.** Quest giver (per [SYS-04](../systems/quests.md) — issues, accepts, and rewards quests).
- **Silhouette readability constraint.** From 6–8 tiles away, the Village Chief must read as **an elderly authority figure in long muted-green robes, leaning forward on a tall gnarled walking staff, with a silver chain-of-office across the chest and a long white beard or weathered features**. Specifically: forward stoop angled ~10–15° from vertical (weight on the staff, not on the legs — distinguishes from the Mage's upright stance), staff held vertically along one side reaching head-height (NOT extending above the head — distinguishes from a wizard staff), robe hem brushes the ground in a wide trapezoid (distinguishes from any PC class hem). Rationale: "elder-with-staff" is the universal civic-elder shorthand; the stoop is the load-bearing differentiator from the Mage and must commit. See §5 for the full archetype rationale.
- **Defining features.**
  - **Silver chain-of-office** across the chest — a visible row of 5–7 linked metallic discs/links at collarbone height. This is the single most legible "civic authority" cue.
  - **Long white beard** OR weathered/lined elder face if PixelLab's template defaults beardless (art-lead picks; either reads as "elder" at 64×64). Long white hair past the shoulders is also acceptable.
  - **Gnarled wooden walking staff** — knotted/twisted plain wood, head-height, no crystal, no orb, no glow (per §4 hard constraint). The staff has a carved grip wrap (leather or rope at the hand-hold) but is otherwise unadorned.
  - **Soft hood or cowl** drawn back from the head (rests on the shoulders, not raised over the head) — reveals the white hair / beard and avoids any pointed-hat read.
  - Robe is full-length, single-color body with a contrasting trim at the hem and cuffs.
- **Color-coding contract.**
  - Base tint surface: full sprite, never modulated.
  - Base palette band: muted sage green or deep forest green (robe body — fits the frontier-village palette; avoids royal purple wizard cliché), cream/ivory (beard, hair, hood lining), warm brown (staff wood), tarnished silver (chain-of-office).
  - **Exempt pixels:** the **silver chain-of-office** is a small **silver metallic accent** (~10–14 pixels across the chest) — exempt because the chain is the service-readability signature for "authority / quest giver" and must not be washed out at any future tint. The staff has no exempt pixels (plain wood, palette-clamp brown). **No player-blue anywhere** (per §4).
- **Scale + anchor.** Scale multiplier = **0.95×** (NPC standard — even though the Village Chief is read as elderly and may visually feel slightly shorter due to the stoop, the underlying scale matches the other two NPCs so the town reads as a consistent population scale relative to the player). Z-offset = 0. Canvas = 128×128. Sprite import offset `Vector2(0, -80)`.
- **Pose convention.** **Lean-on-staff:** both hands on the staff at chest height, weight forward, slight forward stoop, free shoulder relaxed. Held in all 4 directions per §3.
- **Age range + voice compatibility.** Age **60–80** (visibly elderly — the only NPC outside the working-age band; Blacksmith and Guild Maid are explicitly NOT in this band to keep the elder read unique to the Chief). Gender not prescribed; long beard recommendation skews masculine but is not required (a long-white-haired matriarchal elder reads equally well at thumbnail). Voice in `npc-interaction.md` is wise, measured, slow-cadenced — compatible with the elderly-authority silhouette.

---

### 9. Art-Spec Pairing

Paired art ticket: **ART-SPEC-NPC-01** ([docs/assets/npc-pipeline.md](../assets/npc-pipeline.md)) — the generation-side half (PixelLab prompts per NPC, palette implementation, animation recipe for the 4-direction idle set, batch plan for 12 frames, manifest update).

Pairing status: `[ ] design spec locked  [ ] art spec locked  [ ] sprites delivered`

An NPC's sprite reaches **"Ready-for-impl"** only when:
1. This design spec's per-NPC fields (§§6 / 7 / 8) have no TBDs — verified at lock time.
2. The paired art-pipeline spec is also locked.
3. This spec's Open Questions section is empty — see below.

Neither half ships without the other. If a per-NPC defining feature changes (e.g., the horn-scale pauldron is dropped from the Blacksmith), the art spec re-renders. If the art spec discovers a generation constraint that breaks a design field (e.g., PixelLab cannot produce a 4-direction set with consistent ledger placement at 128×128), this spec edits the affected field and re-locks.

## Acceptance Criteria

- [ ] An art-lead reading this spec plus the paired [ART-SPEC-NPC-01](../assets/npc-pipeline.md) can author the Blacksmith, Guild Maid, and Village Chief PixelLab prompts and animation recipes without asking design-lead a clarifying question.
- [ ] Every field in the §1 template has a concrete value for all three NPCs (no TBDs in §§6 / 7 / 8). Verifiable by row-by-row review.
- [ ] All three NPC silhouettes are distinguishable from all three PC class silhouettes at 64×64 thumbnail scale per the §4 acceptance test (export south-facing idles, sort PC vs NPC with 100% accuracy).
- [ ] All three NPCs' services read from their visual identity: a player who has never seen these NPCs can guess Blacksmith handles forge/craft/shop, Guild Maid handles bank/teleport, and Village Chief handles quests, by silhouette + defining-features alone.
- [ ] No NPC sprite carries the player-blue (`#8ed6ff`) accent (per §4 hard constraint).
- [ ] No NPC sprite is runtime-modulated by `color-system.md` (NPCs are exempt; same as PCs per [SPEC-PC-ART-01 §5](player-classes-art.md)).
- [ ] The locked 4-direction idle set (per §3) is reflected in the paired art spec; the frame-count budget (4 × 3 = 12) is honored.
- [ ] Every NPC dialogue label and quest-system reference uses the title-only form (`Blacksmith`, `Guild Maid`, `Village Chief`) — never a personal name (per `project_npc_naming` memory).
- [ ] The Village Chief fresh-design archetype (§5) is implemented as specified — no substitution to a different archetype without re-opening this spec.

## Implementation Notes

- **Pairing with [ART-SPEC-NPC-01](../assets/npc-pipeline.md) is load-bearing.** The two specs are a single logical unit. Any change to fields in §§6–8 (defining features, silhouette constraint, color-accent location, pose) triggers a regeneration cycle on the art side.
- **NPCs are static. No walk cycle, no combat animation.** The 4-direction × 1-frame budget per NPC (12 frames total for the town) is the entire animation set v1. Talking-head animation, gesture cues, and reactive expressions are deferred to a later ticket and are nice-to-have, not blockers.
- **Service mapping is the contract for dialogue / quest wiring.** Engineer-side, the dialogue-menu builder and the quest-system entrypoint must read against the §2 table — Blacksmith owns forge/craft/shop, Guild Maid owns bank/teleport, Village Chief owns quests. Any code that wires a service to the wrong NPC is a bug against this spec, not a design choice. (The historical "Guild Master = quest giver" wiring was such a bug; `NPC-ROSTER-REWIRE-01` corrects it.)
- **Scale = 0.95× is deliberate.** NPCs are slightly smaller than the PC reference (1.00) so the town reads as "the player is the protagonist; these are townsfolk." A 1.00 NPC scale would make the player feel like one-of-many; a 0.85× would make NPCs read as children. 0.95× is the goldilocks point.
- **No player-blue on NPCs is the hardest rule to remember.** Art-lead's instinct on the Guild Maid's teleport pendant will be to make it cyan/blue (because "teleport = magic = blue" is the genre cliché). It must not be. Sage green or deep red is the locked alternative. This is what protects PC identity in town: the player's own sprite is the only blue-accented humanoid on screen.
- **Direction coverage is 4-dir, not 8-dir, by design.** Do not let PixelLab's default humanoid template trick the pipeline into generating 8 frames per NPC. The art-pipeline spec must override the template default to 4-direction explicitly.
- **Village Chief's staff is plain wood, NOT the Mage staff.** This is the easiest visual confusion to introduce by accident — both are "robed figure with vertical staff." The differentiators (forward stoop, no hat / soft hood, plain wood, head-height not above-head, chain-of-office, white beard / hair) must all hold simultaneously. Cutting any one of them risks a Mage-confusion bug at thumbnail scale.
- **Voice / dialogue is referenced, not authored here.** The voice-compatibility line in §§6–8 is a sanity check that the silhouette and the dialogue voice agree. The actual dialogue text lives in `docs/flows/npc-interaction.md` and the per-NPC dialogue strings file when one is authored.

## Open Questions

None — spec is locked. The PO decisions on the 3-NPC roster, the service mapping, the title-only naming convention, and the Village Chief fresh-design archetype (all 2026-04-17) closed the outstanding tradeoffs. Per-NPC fields are concrete. Art-pairing checkbox status is the only remaining state, and that resolves when ART-SPEC-NPC-01 locks.

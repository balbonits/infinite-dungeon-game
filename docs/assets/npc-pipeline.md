# Town NPC Sprite Pipeline (ART-SPEC-NPC-01)

> **⚠ SUPERSEDED BY ADR-007 (2026-04-18) — retained as historical reference.** This file describes the PixelLab-based NPC art pipeline. PixelLab is retired for MVP under the top-down / OGA-LPC pivot (ADR-007 lands on main separately); replacement pipeline is the LPC Universal Sprite Character Generator recipes. In particular, the 4-direction contract (§3, §4, `n_directions: 4`, per-NPC direction lock table) is **overridden** by the south-only rule in [SPEC-NPC-ART-01 §3](../world/npc-art.md) — shipped NPCs are 1 south frame each, not 4 rotations. Do not follow this file as the source of truth for new NPC work. A follow-up ticket will rewrite this pipeline doc against LPC.

## Summary

The generation-facing half of the Town NPC roster. Locks the PixelLab
recipe — prompt text, params, animation templates, download layout, and
review checks — for the three shipped NPCs (Blacksmith / Guild Maid /
Village Chief), so an implementer can open a terminal and produce all
three directories under `assets/characters/npcs/` without further
design input.

Pairs with `docs/world/npc-art.md` (SPEC-NPC-ART-01, design lead,
authored in parallel). This spec EXTENDS the `CHAR-HUM-ISO` block from
[prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit
`5e9e70f`) — it does not redefine the universal preamble, the palette
clause, or the negative-prompt tokens. Those are cited verbatim from
§1 / §1c of that spec. Structural parity with
[player-class-pipeline.md](player-class-pipeline.md) (ART-SPEC-PC-01)
is intentional — same section order, same table shape, same
commit-ordering discipline.

**Product-owner decision (locked 2026-04-17).** NPC roster is exactly
three: Blacksmith, Guild Maid, Village Chief. Service mapping is
locked per [asset-inventory.md](asset-inventory.md) Bucket C:

- **Blacksmith** — forge, crafting, shop (absorbs old Shopkeeper service).
- **Guild Maid** — bank (absorbs old Banker), teleport (absorbs old
  Teleporter).
- **Village Chief** — quest giver (was mis-wired to the now-deprecated
  Guild Master NPC).

"Guild Master" is not an NPC — it is the PC's title per
`project_npc_naming.md` ("{Class} Guildmaster"). No sprite authored.
The four orphan dirs (`banker/`, `shopkeeper/`, `guild_master/`,
`teleporter/`) are deleted with no redraw per §8.

## Current State

Two existing NPC sprite dirs (`assets/characters/npcs/blacksmith/`,
`assets/characters/npcs/guild_maid/`) are authored for the wrong
perspective (low top-down, not true 2:1 iso) and anchor to the wrong
point (diamond center, not diamond top vertex). They are on
[asset-inventory.md](asset-inventory.md) **Bucket C** redraw list.
Delete-before-regen is mandatory — see §8 below.

Four other dirs (`banker/`, `shopkeeper/`, `guild_master/`,
`teleporter/`) belong to roles that have been consolidated away.
They are deleted with no new sprite. Their services are rewired in the
separate `NPC-ROSTER-REWIRE-01` code ticket, which must land before
this redraw PR so new sprites bind to the right service handlers.

Village Chief has never had a sprite; this is a net-new creation.

This spec supersedes any per-NPC prompt scraps that may have lived in
older ART tickets. It is the single authoritative generation recipe
until SPEC-NPC-ART-01 or ART-SPEC-01 is revised.

## Design

### 1. NPC → CHAR-HUM-ISO Prompt Skeleton Table

All three NPCs use block `CHAR-HUM-ISO` from ART-SPEC-01 §2. Locked
values shared across all NPCs (diverges from player classes in the
bolded rows — NPC tier is visibly softer than PC hero tier so
silhouettes read "townsfolk, not adventurer"):

| Param | Value | Source |
|---|---|---|
| Tool | `create_character` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `body_type` | `humanoid` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `template` | `mannequin` | ART-SPEC-01 §2 CHAR-HUM-ISO |
| `size` | `64` (PixelLab internal; canvas extended to 128×128 on download) | ART-SPEC-01 §1a |
| **`n_directions`** | **`4`** (south / east / north / west — see §4) | SPEC-NPC-ART-01 §3 default |
| `view` | `low top-down` (iso intent carried by preamble + negatives) | ART-SPEC-01 §1a |
| `outline` | `single color black outline` | ART-SPEC-01 §1b |
| **`shading`** | **`medium shading`** (NPC tier, not hero tier) | ART-SPEC-01 §1b + CHAR-HUM-ISO NPC row |
| **`detail`** | **`medium detail`** (NPC tier, not hero tier) | ART-SPEC-01 §1b + CHAR-HUM-ISO NPC row |
| `ai_freedom` | `500` (3-asset batch — cohesion across the NPC set) | ART-SPEC-01 §1b |
| Canvas (authoring target) | **128×128** | ART-SPEC-01 §1a |
| **Render scale** | **0.95×** (NPC band — slightly below PC's 1.0, clearly above standard-enemy 0.7) | art-lead agent card Scale Rules |
| Anchor | bottom-center (x=64, y=128) → diamond top vertex | ART-SPEC-01 §1a + §3 |
| `Sprite2D.offset` | `Vector2(0, -80)` | ART-SPEC-01 §1a derived-offset formula (H=128) |
| Animation set | `breathing-idle` only (service-stance) — see §5 | SPEC-NPC-ART-01 §3 |

Per-NPC fill-ins for the `CHAR-HUM-ISO` skeleton. Silhouette constraints
and `proportions` overrides derive from SPEC-NPC-ART-01 §2
(design-side silhouette contract — distinct-from-PC at thumbnail per
SPEC-NPC-ART-01 §4). Each row is one fill-in of the skeleton in
ART-SPEC-01 §2 `CHAR-HUM-ISO`.

#### Blacksmith

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "default"}` |
| BUILD | burly, barrel-chested, thick-armed |
| ROLE | blacksmith |
| OUTFIT | heavy leather apron over rolled-up linen undershirt, rugged canvas trousers, sturdy work boots, leather gauntlets on both forearms, a thick belt with small tool loops |
| HELD-ITEM(S) | right hand always holds a heavy iron blacksmith hammer with a wooden haft; left hand always held open and empty at the side |
| DEFINING-FEATURE | thick braided beard falling past the collarbone, bald-topped head with a sweat-cloth tied at the brow |
| COLOR-ACCENT | scorched-brown leather apron, dull iron-gray hammer head, warm gold (`#f5c86b`) hammer-band and belt buckle |
| STANCE | service stance, feet planted shoulder-width, hammer resting on the shoulder, weight centered — the forge-ready pose of a townsman, not a combatant |

#### Guild Maid

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "default"}` |
| BUILD | slender, upright |
| ROLE | maid |
| OUTFIT | long ankle-length dark-blue service dress with a high collar, a clean white half-apron tied at the waist, lace cuffs at the wrists, a small silver brooch at the throat |
| HELD-ITEM(S) | both hands held clasped together in front of the waist, empty, palms resting one over the other |
| DEFINING-FEATURE | neat white frilled headpiece pinned to dark hair tied back in a low bun — the unmistakable maid silhouette |
| COLOR-ACCENT | deep navy dress (`#24314a` anchor), white apron and headpiece (the only bright white in the NPC set), faint gold (`#f5c86b`) brooch |
| STANCE | poised service stance, back straight, feet together, chin slightly level — the polite receiving pose of a guild attendant |

#### Village Chief

| Slot | Fill-in |
|---|---|
| `proportions` | `{"type": "preset", "name": "default"}` |
| BUILD | older, slightly stooped, lean |
| ROLE | elder |
| OUTFIT | layered woolen cloak with fur-trimmed collar over a plain long tunic, a sash at the waist, sensible trousers, soft leather boots, no armor and no weapons |
| HELD-ITEM(S) | right hand always holds a gnarled wooden walking staff (unadorned, no orb, no glow — clearly a walking aid, not a mage staff); left hand always held open and empty, resting near the waist |
| DEFINING-FEATURE | long white beard reaching mid-chest, bushy white eyebrows, a simple embroidered skullcap (no pointed hat, no crown — village elder, not royalty or wizard) |
| COLOR-ACCENT | earthy brown and muted moss-green cloak, fur trim in off-white, warm gold (`#f5c86b`) stitching on the sash |
| STANCE | calm elder stance, weight resting on the staff, free hand at the waist, slight forward tilt as if listening |

**Design notes on distinction from PC classes (SPEC-NPC-ART-01 §4
contract):**

- **Blacksmith vs. Warrior.** Warrior has horned helmet + longsword +
  round shield. Blacksmith has no helmet (bald + beard + brow-cloth),
  no shield, and a blunt hammer — not a blade. Apron silhouette
  replaces plate silhouette.
- **Guild Maid vs. any PC class.** No PC class has a dress silhouette,
  a frilled headpiece, or a high-collar service uniform. The maid
  headpiece is the hard silhouette tell at thumbnail scale.
- **Village Chief vs. Mage.** Mage has pointed wide-brim wizard hat +
  staff with glowing orb + flowing robes. Chief has a flat skullcap
  (not pointed), a plain walking staff (no orb, no glow), and a
  cloak+tunic (not robes). Beard + age cues replace wizard cues. The
  "no glowing orb" clause in the HELD-ITEM fill-in is load-bearing
  against PixelLab defaulting any staff to a wizard prop.

### 2. Full Copy-Paste PixelLab Prompts

The following three prompts are complete — they can be fed to
`create_character` verbatim (as the `description` argument) without any
further edits. Each opens with the universal preamble from ART-SPEC-01
§1, inserts the fill-ins from §1 above, and closes with the palette
clause (§1c) and universal negative-prompt tokens (§1).

Every prompt is IP-clean per ART-SPEC-01 §11: no named game titles,
no studio names, no signature character names. The cartoonish qualifier
(ART-SPEC-01 §11 rule 2) is present in all three preambles and is
load-bearing — do NOT soften it to "realistic" or "gritty" when copying.

**Companion `create_character` invocation args** (same for all three, per §1 table):

```
name: <"Blacksmith" | "Guild Maid" | "Village Chief">
body_type: humanoid
template: mannequin
size: 64
n_directions: 4
view: low top-down
outline: single color black outline
shading: medium shading
detail: medium detail
ai_freedom: 500
proportions: {"type": "preset", "name": "default"}
```

#### Prompt A — Blacksmith

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A burly, barrel-chested, thick-armed blacksmith rendered in cartoonish isometric pixel-art style. Heavy leather apron over a rolled-up linen undershirt, rugged canvas trousers, sturdy work boots, leather gauntlets on both forearms, a thick belt with small tool loops. Right hand always holds a heavy iron blacksmith hammer with a wooden haft, left hand always held open and empty at the side. Thick braided beard falling past the collarbone, bald-topped head with a sweat-cloth tied at the brow. Scorched-brown leather apron, dull iron-gray hammer head, warm gold (#f5c86b) hammer-band and belt buckle. Service stance with feet planted shoulder-width, hammer resting on the shoulder, weight centered — the forge-ready pose of a townsman, not a combatant. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

#### Prompt B — Guild Maid

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A slender, upright maid rendered in cartoonish isometric pixel-art style. Long ankle-length dark-blue service dress with a high collar, a clean white half-apron tied at the waist, lace cuffs at the wrists, a small silver brooch at the throat. Both hands held clasped together in front of the waist, empty, palms resting one over the other. Neat white frilled headpiece pinned to dark hair tied back in a low bun — the unmistakable maid silhouette. Deep navy dress (#24314a anchor), white apron and headpiece, faint gold (#f5c86b) brooch. Poised service stance with back straight, feet together, chin slightly level — the polite receiving pose of a guild attendant. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

#### Prompt C — Village Chief

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An older, slightly stooped, lean elder rendered in cartoonish isometric pixel-art style. Layered woolen cloak with a fur-trimmed collar over a plain long tunic, a sash at the waist, sensible trousers, soft leather boots, no armor and no weapons. Right hand always holds a gnarled wooden walking staff that is unadorned with no orb and no glow — clearly a walking aid and not a mage staff; left hand always held open and empty, resting near the waist. Long white beard reaching mid-chest, bushy white eyebrows, a simple embroidered skullcap — no pointed hat and no crown, a village elder rather than royalty or a wizard. Earthy brown and muted moss-green cloak, fur trim in off-white, warm gold (#f5c86b) stitching on the sash. Calm elder stance with weight resting on the staff, free hand at the waist, slight forward tilt as if listening. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

### 3. Direction Coverage

**Locked: 4 directions per NPC — `south`, `east`, `north`, `west`.**

NPCs are stationary service actors; the Town scene places each one at a
fixed spot facing a fixed direction (Blacksmith at the forge, Guild
Maid at the guildhall counter, Village Chief at the town center). The
player orbits them. The iso camera only ever shows the NPC from the
four cardinal facings as the player walks around, and the game has no
mechanic that rotates an NPC to a diagonal. The four extra diagonals
(`south-east` / `south-west` / `north-east` / `north-west`) would cost
~50% more generations for zero gameplay readability gain.

This matches SPEC-NPC-ART-01 §3's default recommendation (4-dir +
idle). No NPC in the locked roster deviates — all three get the same
4-direction coverage. If a future NPC is added that moves on its own
(e.g., a town-crier walking a route), that spec revision promotes its
coverage to 8-direction and explains why; the default stays 4.

**Per-NPC direction lock table:**

| NPC | `n_directions` | Rotations generated |
|---|---|---|
| Blacksmith | 4 | south, east, north, west |
| Guild Maid | 4 | south, east, north, west |
| Village Chief | 4 | south, east, north, west |

### 4. Animation Recipe

Per SPEC-NPC-ART-01 §3 the shipped animation set per NPC is a single
**stationary idle** — NPCs do not walk, do not attack, do not die.
The only motion the player ever sees is a subtle breathing/sway cycle
while the NPC stands at their post. `animate_character` is called
once per NPC, AFTER the NPC's 4-dir rotations have completed (sequence
`create_character` → poll until `get_character` returns `completed` →
queue animations). Each `animate_character` call covers all 4
directions by default; do not pass `directions` explicitly.

PixelLab's humanoid template catalog (per `mcp__pixellab__animate_character`
tool docs in the [art-lead agent card](../../.claude/agents/art-lead.md))
has no exact "static service-stance idle" template. Two candidates:

- `breathing-idle` — subtle chest-rise / weight-shift cycle, NO combat
  telegraph. The closest native fit for a townsman standing at a
  counter / forge / square.
- `fight-stance-idle-8-frames` — explicit combat ready, used for the
  three PC classes. WRONG tier for NPCs — would make the Blacksmith
  read as a combatant at thumbnail scale and dilute the PC-vs-NPC
  silhouette gate (§7).

**Decision:** all three NPCs use `breathing-idle`. No description
override is needed — the template is already stance-neutral. The
per-NPC `STANCE` fill-in in §1 (service stance, poised service stance,
calm elder stance) travels with the character's rotations and is
re-used by the animation engine as the neutral pose around which
`breathing-idle` oscillates.

| NPC | Animation purpose | `template_animation_id` | `animation_name` |
|---|---|---|---|
| Blacksmith | service-stance idle | `breathing-idle` | `idle` |
| Guild Maid | service-stance idle | `breathing-idle` | `idle` |
| Village Chief | service-stance idle | `breathing-idle` | `idle` |

All three are template animations (1 PixelLab generation per direction
× 4 directions = 4 generations per animation, no custom-action cost).
Total animation generation cost: 3 × 4 = **12 generations** for the
full NPC batch after character rotations land. Character rotations
themselves run at `standard` mode / 1 generation each × 4 directions
× 3 NPCs = 12 generations. **Total NPC-batch cost: 24 generations**
(vs. 72 for the PC batch — cheaper because of fewer directions + no
attack + no fight-stance-idle).

### 5. Download + Extraction Recipe

After all animations report `completed` via `get_character`, download
the ZIP once per NPC and extract into the directory convention below.

**Target layout** (per NPC, one subtree under `assets/characters/npcs/`):

```
assets/characters/npcs/
├── blacksmith/
│   ├── rotations/
│   │   ├── south.png
│   │   ├── east.png
│   │   ├── north.png
│   │   └── west.png
│   ├── animations/
│   │   └── idle-<hash>/
│   │       ├── south/frame_00.png ... frame_NN.png
│   │       ├── east/…
│   │       ├── north/…
│   │       └── west/…
│   └── metadata.json             # preserved verbatim from PixelLab ZIP
├── guild_maid/      (same structure)
└── village_chief/   (same structure — net-new dir)
```

`<hash>` is the animation-job hash suffix that PixelLab emits; match
the existing convention in the player-class directories. The hash is
whatever the ZIP already contains — do not rename.

**Extraction commands** (reference; implementer adjusts paths):

```bash
# Per NPC, after get_character reports all artifacts completed:
curl --fail -o /tmp/blacksmith.zip "<download_url_from_get_character>"
unzip -q /tmp/blacksmith.zip -d assets/characters/npcs/blacksmith/

# Sanity checks:
ls assets/characters/npcs/blacksmith/rotations/ | wc -l          # expect 4
ls assets/characters/npcs/blacksmith/animations/ | wc -l         # expect 1
test -f assets/characters/npcs/blacksmith/metadata.json          # must exist
sips -g pixelWidth -g pixelHeight \
    assets/characters/npcs/blacksmith/rotations/south.png        # 128×128
```

Repeat for `guild_maid` and `village_chief`. `curl --fail` is required
per the [art-lead agent card](../../.claude/agents/art-lead.md) ("verify
downloads"). PixelLab returns HTTP 423 + JSON when assets are still
generating; `--fail` turns that into a non-zero exit instead of writing
a junk file.

`metadata.json` is preserved verbatim — it is the PixelLab-authored
record of generation params and the reference-of-record for future
regens per the art-lead agent card ("Match existing style: read
metadata.json for reference parameters").

### 6. Render Scale

NPC render scale is **0.95×**, applied at the Godot scene level via
`Sprite2D.scale = Vector2(0.95, 0.95)` (or the equivalent parent
`Node2D.scale`). Rationale per art-lead agent card Scale Rules:

- Player classes: 1.0× (hero tier).
- NPCs: 0.9× "slightly smaller than player, non-threatening." This
  spec tightens the band to **0.95×** — closer to 1.0 than 0.9 —
  because NPCs are adult townsfolk standing alongside the PC in the
  Town scene, and 0.9× reads as child-scale at thumbnail. 0.95× keeps
  the non-threatening tone while preserving adult proportions beside
  the PC at scene scale. The three NPCs share the same scale so the
  Town scene reads as a consistent crowd tier.
- Standard enemies: 0.7× (smaller-than-PC silhouette band).

This scale is a scene-side author decision, not a PixelLab param. The
authoring canvas stays 128×128 so metadata.json matches PC convention.

### 7. NPC-vs-PC Thumbnail Gate (PR Checklist)

Per SPEC-NPC-ART-01 §4, the three NPCs must be distinguishable from
the three PC classes at **64×64 thumbnail scale** — a player glancing
at the Town scene should know at once which figure is their PC and
which are the townsfolk service actors.

**PR-checklist item (bundled into the redraw PR's description):**

> [ ] Render `rotations/south.png` from all six characters side-by-side
> at 64×64: Warrior, Ranger, Mage (from `assets/characters/player/`)
> and Blacksmith, Guild Maid, Village Chief (from
> `assets/characters/npcs/`). Each 128×128 source PNG is downscaled
> to 64×64 via nearest-neighbor and pasted into a single composite
> image. At 64×64 the six must read unambiguously as six distinct
> silhouettes:
>
> 1. Warrior — horned helmet + round shield
> 2. Ranger — pulled-up hood + drawn bow
> 3. Mage — wide-brim pointed hat + staff with glowing orb
> 4. Blacksmith — bald + braided beard + apron + hammer-on-shoulder
> 5. Guild Maid — frilled headpiece + floor-length dress silhouette
> 6. Village Chief — walking staff + long white beard + flat skullcap
>
> Paste the composite into the PR description. If any NPC silhouette
> reads as a PC class (most likely failure: Chief reading as a Mage
> because of the staff), re-gen the offender with its DEFINING-FEATURE
> emphasized harder (explicit "no pointed hat, no glowing orb" already
> in the Chief prompt — escalate by dropping the staff entirely if
> a second re-gen still reads as a Mage).

This gates the PR per ART-SPEC-01 §6 item 5 ("silhouette readability,
south-facing rotation pasted at thumbnail size"), extended here with
the six-up PC+NPC comparison specific to the Town scene.

### 8. Delete-Before-Regen Hook (Bucket C Full Sweep)

Per [asset-inventory.md](asset-inventory.md) **Bucket C**, the redraw
PR executes in a **three-commit sequence** so scene-file breakage and
code-rewire land in the right order.

> [!IMPORTANT]
> The code rewire lives in a separate ticket
> (`NPC-ROSTER-REWIRE-01`) and **must merge before this redraw PR
> opens**, so Town.cs / Npc.cs already reference the new service
> mapping (QuestPanel → Village Chief, Bank + Teleport → Guild Maid,
> Shop → Blacksmith) by the time new sprites arrive. If the rewire
> has not merged, this PR blocks — do not work around it by editing
> code under this ticket.

**Commit 1 — deletions (full Bucket C sweep).** The first commit of
the redraw branch executes:

```bash
# Redraw slots — old sprites deleted to make room for regen:
git rm -r assets/characters/npcs/blacksmith/
git rm -r assets/characters/npcs/guild_maid/

# No-redraw orphans — services consolidated, dir deleted forever:
git rm -r assets/characters/npcs/banker/
git rm -r assets/characters/npcs/shopkeeper/
git rm -r assets/characters/npcs/guild_master/
git rm -r assets/characters/npcs/teleporter/
```

All six deletions land in a single commit. The render pipeline breaks
if old + new co-exist for the two redraw slots (parallel metadata.json,
mixed anchor conventions); the four orphan dirs simply have no
referencing code after `NPC-ROSTER-REWIRE-01` merges, so their
deletion is pure cleanup.

**Commit 2 — regenerate + create new.** Extract the three new PixelLab
ZIPs per §5 into:

```
assets/characters/npcs/blacksmith/     (redraw)
assets/characters/npcs/guild_maid/     (redraw)
assets/characters/npcs/village_chief/  (net-new dir — never existed)
```

Commit the new rotations + animations + metadata.json for all three.

**Commit 3 — scene + node fixes.** Fix any `.tscn` files whose
`Sprite2D.texture = ExtResource("…npcs/blacksmith/south.png")` paths
were invalidated by the commit-1 deletions. Land the new
`Sprite2D.offset = Vector2(0, -80)` and `Sprite2D.scale = Vector2(0.95, 0.95)`
values. Add the new Village Chief Npc node to the Town scene at the
town-center spawn per SPEC-NPC-ART-01 §5 placement table (if that
section exists; otherwise co-lock with design-lead before merging
commit 3).

**Verify in-engine.** Before merging, boot the game, confirm all three
NPCs spawn at the right Town locations, confirm feet align to diamond
top vertex, confirm clicking Blacksmith opens Forge+Shop, Guild Maid
opens Bank+Teleport, Village Chief opens Quests. Any floating or
sunken sprite is an `offset` bug, not a sprite bug (ART-SPEC-01 §6
item 6). Any mis-wired service is a merge-order bug — confirm
`NPC-ROSTER-REWIRE-01` actually landed first.

Git history preserves the old v1 Blacksmith + Guild Maid sprites and
the four orphan dirs at the pre-deletion commit. No backup branch or
`assets/_old/` shadow copy — history is enough.

### 9. Pairing Note

This spec is the generation-facing half of the Town NPC redraw.
Design-facing counterpart: **`docs/world/npc-art.md` (SPEC-NPC-ART-01)**,
authored by `@design-lead` in parallel. That spec owns NPC identity
(who they are in the world, what services they front, how they greet
the player), silhouette constraints (the "must look different from
PC class X" contract this spec's §1 design notes implement),
placement in the Town scene (where each NPC stands), and any
per-NPC dialogue/portrait decisions (out of scope for this spec).

Co-lock: neither ships "Ready-for-impl" until both are locked.
Dev-tracker Notes cross-link both ways.

## Acceptance Criteria

- [x] **Copy-paste parity.** Any of the three prompts in §2 can be
      pasted into `create_character` as `description` and produces a
      sprite matching the SPEC-NPC-ART-01 identity constraints.
- [x] **IP-clean.** All three prompts contain zero named-IP references
      — no game titles, studio names, or franchise-character names.
      The cartoonish qualifier is present in all three preambles per
      ART-SPEC-01 §11 rule 2.
- [x] **Animation template IDs are real.** The single template ID used
      (`breathing-idle`) appears in the humanoid template list
      documented on the `mcp__pixellab__animate_character` tool in the
      [art-lead agent card](../../.claude/agents/art-lead.md).
- [x] **Anchor convention agrees with ART-SPEC-01 §3.** Canvas is
      128×128; anchor is bottom-center; `Sprite2D.offset = (0, -80)`
      is derived from the §1a formula for H=128. Scene-side scale
      adds `Vector2(0.95, 0.95)` per §6, independent of the anchor
      contract.
- [x] **Direction coverage is justified.** §3 locks 4-direction and
      explains why (stationary NPCs, player orbits, no diagonal
      facing needed). Default matches SPEC-NPC-ART-01 §3
      recommendation.
- [x] **Delete-before-regen is documented, including the four orphans.**
      §8 states the commit-1 deletion of all six dirs (two redraws +
      four consolidated-away), the commit-2 regeneration of the three
      surviving NPCs, and the commit-3 scene/offset fix.
- [x] **NPC-vs-PC thumbnail gate is a PR gate.** §7 states the
      six-up-thumbnail PR-checklist item distinguishing all three
      NPCs from all three PC classes at 64×64.
- [x] **Block-ID citation.** This spec extends `CHAR-HUM-ISO` from
      [prompt-templates.md](prompt-templates.md) §2; the block ID is
      cited in §1 and is the durable handle per ART-SPEC-01 §6 item 1.
- [x] **Merge-order coupling is called out.** §8 flags
      `NPC-ROSTER-REWIRE-01` as a hard pre-req — new sprites must bind
      to code that already routes services correctly.

## Implementation Notes

### Other notes

- **Batch pacing.** `ai_freedom = 500` is set across the three-NPC
  batch for cohesion per ART-SPEC-01 §1b. All three `create_character`
  calls can be queued concurrently (PixelLab's 8-concurrent-job limit
  accommodates them easily). Animations queue after rotations complete
  — do not start animations until `get_character` returns completed
  for all three.
- **Canvas extend.** PixelLab's `size=64` produces ~96×96 output; if
  the shipped PNG is not already 128×128, the implementer canvas-extends
  on download (padded transparent, content re-aligned to bottom-center).
  ART-SPEC-01 §1b documents this as a known friction.
- **Manifest.** This spec does not author a per-family manifest — the
  NPC slot is too small to earn one (3 sprites, fixed roster). The
  authoritative record of what exists lives in `metadata.json` per
  directory (PixelLab-preserved) plus
  [asset-inventory.md](asset-inventory.md) Bucket C.
- **Tint compatibility.** NPC sprites stay palette-neutral — no baked
  level tint — so Godot `Modulate` runtime tints per
  `docs/systems/color-system.md` apply cleanly if the Town scene ever
  needs a time-of-day or event tint. The Guild Maid's white apron +
  headpiece is the most tint-sensitive pixel family in the NPC set;
  if color-system tinting is ever applied in Town, expect that family
  to be the first to demand an exempt-pixel carve-out per ART-SPEC-01
  §1c.
- **Why no `walking` template.** NPCs never move at runtime. Baking a
  walk cycle would be 12 unused generations and would invite future
  regressions where an NPC animation gets accidentally wired to
  movement logic. Absence is a feature.

## Open Questions

None.

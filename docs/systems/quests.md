# Quest System

## Summary

Radiant (procedurally generated) quests offered by the Guild in town. The Guild displays an offer board of candidate quests; the player chooses which ones to accept, up to an active-quest cap. Completing a quest rewards gold and materials. Intro quests guide new players through core mechanics without interrupting them. No time limits, no abandonment penalty, no "world-ending" stakes — quests are checklists and fetch/hunt research tasks, not emergencies.

## Current State

**Spec status: IN REVIEW.** Quest data model, generation formulas, tracking signals, completion flow, UI layout, save format, player-selection model, intro-quest track, and level/floor scaling (`quest_scale`) are defined. A handful of ambiguities opened by the PO's Quest Log expansion are still pending — see [Open Questions](#open-questions). Returns to LOCKED once those are resolved.

Not yet implemented. Quests are delivered by the **Village Chief** NPC per [npc-interaction.md](../flows/npc-interaction.md) (the earlier "Adventure Guild / Guild Master" naming in this doc is historical — all references to "Guild" below mean the Village Chief's quest board). The Guild Maid owns Bank/Teleport, not quests. The tutorial that these quests reinforce lives in the Pause menu (see [Intro Quests](#intro-quests) below).

## Design

### Player Selection Model

Quests are not auto-installed into slots. The Guild maintains two structures:

- **Offer board** — up to **6** candidate quests the player can browse and pick from. Refilled on Guild interaction (see [Quest Refresh Rules](#quest-refresh-rules)).
- **Active quests** — quests the player has accepted. Active-quest cap: **3** (same as the prior 3-slot model — preserves the locked UI layout and reward pacing).

```
Player walks up to Village Chief
  -> Opens Quest Board (Offer + Active tabs)
  -> Offer tab: browse 6 candidates, [Accept] to move one into Active
  -> Active tab: view progress, claim completed, or [Refuse] to remove
```

**Refusal is free.** Declining an offered quest or removing an active one has no penalty and no cooldown — quests are research requests and bounty checklists, not emergencies. No time limits anywhere in the system.

**Equivalent reward tiers.** All offered quests at a given scaling level are balanced to equivalent value (gold + material value roughly equal per [Reward Formulas](#reward-formulas)), so picking is about *what the player feels like doing tonight* (kill fast, push depth, hunt a boss) — not about chasing the best payout.

### Intro Quests

A curated, non-radiant track that teaches core mechanics by putting the player *into* them. Intro quests sit in the offer board alongside radiant quests until completed. Each is one-shot per character; refusing them is allowed (they just stay on the offer board until accepted or the player moves on).

**Design stance — no forced tutorials.** The game assumes the player can read the written tutorial available anytime in the Pause menu, and can think. Intro quests are a *hand on the shoulder*, not a Ubisoft-style interruption loop. They do not pop tooltips, lock input, or gate progression.

**Each intro quest grants a starter bonus on completion** — small, one-time, meaningful early but irrelevant by mid-game. Starter bonuses are additive to the normal gold+materials reward, not a replacement.

| # | Quest | Teaches | Starter Bonus |
|---|-------|---------|---------------|
| 1 | "Kill 5 enemies on floor 1" | Basic combat, floor targeting | +50 gold, 1x Health Potion |
| 2 | "Reach floor 2" | Staircase / floor transition | +1 backpack slot token |
| 3 | "Return to town and claim a reward" | Town hub, NPC interaction, claim loop | 1x Sacrificial Idol |
| 4 | "Equip an item via the Blacksmith" | Equipment / forge flow | +100 gold |
| 5 | "Reach floor 5" | Mid-early progression gate | 1x rare Tier 1 material |

Intro quests scale *minimally* (they target specific early floors, so scaling isn't applied). Their gold+material rewards use the normal formula at the referenced floor.

### Quest Types

```
enum QuestType {
  Kill,       // "Slay N enemies on floor F"
  Boss,       // "Defeat the Zone Z boss"
  ClearFloor, // "Clear all enemies on floor F"
  DepthPush   // "Reach floor F for the first time"
}
```

| Type | Target | Difficulty | Reward Tier |
|------|--------|------------|-------------|
| Kill | Kill N enemies on a specific floor | Low-medium | Common materials + gold |
| Boss | Kill the boss on a boss floor (multiple of 10) | High | Rare materials + gold |
| ClearFloor | Reduce active enemies on a floor to 0 at once | Medium-high | Common materials + gold + bonus XP |
| DepthPush | Step onto a floor deeper than `deepest_floor` | Variable | Rare materials + gold |

### Quest Data Model

```
QuestDef {
  id:           string      // Deterministic hash: "{type}_{target_floor}_{target_count}_{seed}"
  type:         QuestType   // Kill | Boss | ClearFloor | DepthPush
  target_floor: int         // The floor this quest references
  target_count: int         // Kill quest: enemy count. Others: 1 (implicit).
  description:  string      // Player-facing text (generated from template)
  gold_reward:  int         // Gold payout on completion
  material_reward: MaterialReward  // Materials payout on completion
  xp_reward:    int         // Bonus XP (ClearFloor only; 0 for others)
}

MaterialReward {
  tier:  int   // Material tier (matches floor-based affix tiers from items.md)
  count: int   // Number of materials awarded
  rare:  bool  // true = rare materials (Boss/DepthPush), false = common (Kill/ClearFloor)
}

QuestState {
  quest:      QuestDef   // The quest definition
  progress:   int        // Current progress toward target_count
  completed:  bool       // true once progress >= target_count
  slot:       int        // 0, 1, or 2 (which of the 3 quest slots)
}
```

### Quest Generation

Quests are generated when the offer board needs refilling (game start, or after offers expire / are accepted). Generation is seeded from **`quest_scale`** to keep quests appropriate to the player's progression.

#### Scaling Reference: `quest_scale`

```
quest_scale = floor((player_level + deepest_floor) / 2)
quest_scale = max(1, quest_scale)
```

**Why the average, not one or the other:**
- Player-level-only: a L20 player farming F3 would get L20-scale quests that F3 monsters can't feasibly satisfy (kill targets, material tiers, boss floors out of reach).
- Deepest-floor-only (the previous locked model): a lucky L5 who got yanked to F10 by a friend would get trivial L5-feeling quests; an L50 who has never gone below F10 would never see Tier-3+ material rewards.
- Average: quests stay reachable *and* rewarding for the player's actual current capability, whichever of the two is lagging. One full level/floor of lead pulls the scale up half a step — gentle and predictable.

Existing places in this spec that previously read `deepest_floor` for quest target-floor selection still use `deepest_floor` (you can only fight on floors you've unlocked). The *scale* of rewards, kill counts, and material tiers uses `quest_scale`. The two inputs are listed explicitly in each formula below.

#### Slot Allocation

Each time a quest is generated, the type is chosen by weighted random roll:

| Type | Weight | Approx Chance |
|------|--------|---------------|
| Kill | 45 | ~45% |
| ClearFloor | 25 | ~25% |
| DepthPush | 20 | ~20% |
| Boss | 10 | ~10% |

**Constraint:** Boss quests can only appear if the player has not yet killed the boss on the target floor (checked against `dungeon.boss_kills` in the save). If a Boss quest fails this check, reroll as a Kill quest.

**Constraint:** DepthPush quests always target `deepest_floor + 1`. Only one DepthPush quest can exist across the 3 slots at a time. If a DepthPush would duplicate, reroll as a Kill quest.

#### Target Floor Selection

```
Kill:       random floor in [max(1, deepest_floor - 5), deepest_floor]
ClearFloor: random floor in [max(1, deepest_floor - 3), deepest_floor]
Boss:       lowest uncompleted boss floor <= deepest_floor + 10
            (i.e., nearest boss floor the player hasn't beaten yet)
DepthPush:  deepest_floor + 1 (always)
```

This keeps Kill/ClearFloor quests on floors the player can already reach, while Boss/DepthPush quests push toward new content.

#### Target Count (Kill Quests Only)

```
kill_target(floor) = 15 + floor * 1
```

| Floor | Kill Target |
|-------|-------------|
| 1 | 16 |
| 5 | 20 |
| 10 | 25 |
| 20 | 35 |
| 50 | 65 |
| 100 | 115 |

This scales gently. At floor 10, killing 25 enemies takes roughly 2-3 minutes of active combat. At floor 100, killing 115 enemies takes roughly 8-12 minutes, which is appropriate for the higher reward.

### Reward Formulas

#### Gold Reward

```
base_gold(type):
  Kill       = 50
  ClearFloor = 75
  Boss       = 150
  DepthPush  = 120

gold_reward(type, floor) = floor(base_gold(type) * (1 + (floor - 1) * 0.15))
```

| Floor | Kill Gold | ClearFloor Gold | Boss Gold | DepthPush Gold |
|-------|-----------|-----------------|-----------|----------------|
| 1 | 50 | 75 | 150 | 120 |
| 5 | 80 | 120 | 240 | 192 |
| 10 | 117 | 176 | 352 | 282 |
| 20 | 192 | 288 | 577 | 462 |
| 50 | 417 | 626 | 1,252 | 1,002 |
| 100 | 792 | 1,188 | 2,377 | 1,902 |

#### Material Reward

Material tier is keyed to **`quest_scale`** (not raw floor), so a high-level player farming shallow floors still receives materials worth their progression:

```
material_tier(quest_scale):
  1-9:    Tier 1
  10-24:  Tier 2
  25-49:  Tier 3
  50-74:  Tier 4
  75-99:  Tier 5
  100+:   Tier 6

material_count(type):
  Kill       = 2
  ClearFloor = 3
  Boss       = 3
  DepthPush  = 2

material_rare(type):
  Kill       = false
  ClearFloor = false
  Boss       = true
  DepthPush  = true
```

Rare materials are the same type that boss first-kills drop. Common materials are the same type enemies drop on the floor. This keeps the quest reward pipeline consistent with the existing loot system.

**Fictional framing.** The materials the Guild hands out are not summoned from thin air — they come from the Guild's own processing of the monsters the player has already killed. How exactly they harvest, preserve, and extract the useful bits from a corpse the player never dragged home is the Guild's trade secret. The player doesn't ask. The Guild doesn't tell. It reads as *our bounty cut, cleaned and packaged* — a believable economic loop without forcing the player to manage carcasses in the backpack.

#### XP Reward (ClearFloor Only)

```
xp_reward(floor) = floor(xp_to_next_level(player_level) * 0.10)
```

This awards 10% of the player's current level-up requirement. At level 10, that is `floor(4500 * 0.10) = 450` XP, roughly equivalent to killing 9 even-level monsters. Enough to feel rewarding without replacing kill XP as the primary progression source.

Kill, Boss, and DepthPush quests award 0 bonus XP. Their value comes from gold and materials.

### Quest Description Templates

```
Kill:       "Slay {target_count} enemies on floor {target_floor}"
ClearFloor: "Clear all enemies on floor {target_floor}"
Boss:       "Defeat the Zone {zone} boss on floor {target_floor}"
DepthPush:  "Reach floor {target_floor} for the first time"
```

Zone number for Boss quests: `zone = ceil(target_floor / 10)`.

### Quest Tracking

Progress is tracked via EventBus signals. The quest system listens for events and updates matching active quests.

| Quest Type | Signal | Condition | Progress Update |
|------------|--------|-----------|-----------------|
| Kill | `EnemyDefeated(position, tier, floor)` | `floor == quest.target_floor` | `progress += 1` |
| Boss | `BossDefeated(floor)` | `floor == quest.target_floor` | `progress = 1` (complete) |
| ClearFloor | `FloorCleared(floor)` | `floor == quest.target_floor` | `progress = 1` (complete) |
| DepthPush | `FloorChanged(floor)` | `floor == quest.target_floor && floor > previous_deepest` | `progress = 1` (complete) |

**Required signal additions to EventBus:**

| Signal | Parameters | Emitted By |
|--------|------------|------------|
| `EnemyDefeated` | `Vector2 position, int tier, int floor` | Enemy.TakeDamage (add floor param to existing signal) |
| `BossDefeated` | `int floor` | Boss death handler (new signal) |
| `FloorCleared` | `int floor` | Spawn system when active enemy count hits 0 (new signal) |
| `FloorChanged` | `int floor` | GameState when floor_number changes (existing, may need param) |

`EnemyDefeated` already exists but needs the `floor` parameter added. `BossDefeated` and `FloorCleared` are new signals.

**ClearFloor tracking detail:** A floor is "cleared" when the active enemy count on the current floor reaches 0 at any point after the quest was accepted. Since enemies respawn infinitely, the player must kill all currently active enemies fast enough to hit 0 simultaneously. This is intentionally harder than a Kill quest — it requires speed and area coverage, not just a body count.

### Quest Completion Flow

```
Quest progress reaches target
  -> QuestState.completed = true
  -> HUD notification: "Quest Complete: {description}" (brief toast, 3 seconds)
  -> Rewards held in escrow (not yet given)
  -> Player must visit Adventure Guild to claim rewards
  -> On Guild interaction:
     -> Completed quests show "Claim" button
     -> Clicking Claim:
        -> Gold added to character.gold
        -> Materials added to backpack (overflow: materials are lost if backpack is full)
        -> XP awarded (ClearFloor only)
        -> Quest slot cleared
        -> New quest generated for the empty slot immediately
```

**Why claim at Guild, not auto-reward:** Returning to town is a core loop anchor. Forcing the player to visit the Guild to collect rewards keeps them cycling through town, interacting with NPCs, and making decisions about what to do next. It also prevents inventory surprises mid-combat.

**Backpack overflow:** If the player's backpack is full when claiming materials, the materials are lost. The claim UI shows a warning: "Backpack full! Materials will be lost." The player can cancel, visit the Banker to free up space, then return. Gold and XP are never lost (gold has no inventory slot; XP is applied directly).

### Quest Refresh Rules

| Event | What Happens |
|-------|-------------|
| Game start (new character) | Fill offer board with 6 radiant quests seeded from `quest_scale = 1`, plus all 5 intro quests pinned to the top |
| Player visits Guild | If offer board has < 6 slots, generate new radiant quests to refill. Intro quests persist until completed. |
| Player accepts a quest | Quest moves from Offer to Active (up to active-cap of 3). Offer slot empties. |
| Player refuses an offered quest | Offered quest is removed from the board. Slot empties. No penalty. Will be re-rolled on next Guild visit. |
| Quest claimed at Guild | Quest is removed from Active. Active slot opens up for the next Accept. |
| Player abandons an active quest | Active slot opens up. Progress lost. No penalty. |
| Player reaches new deepest floor | Existing active quests are NOT refreshed (avoid invalidating progress). New offers generated at the new `quest_scale` on next Guild visit. |

**Abandonment / refusal.** Both are free, instant, and never punished. Quests are checklists, not contracts. The only "cost" of churning quests is the player's own time.

**No forced refresh.** Quests are never automatically replaced or invalidated. A Kill quest targeting floor 5 remains valid even if the player is now on floor 50. The rewards will feel small by then, but the player can still complete it or abandon it at their discretion.

### UI: Quest Board Panel

The Quest Board panel appears when the player walks up to the Village Chief NPC (proximity interaction, same as all town NPCs — see [npc-interaction.md](../flows/npc-interaction.md)). Tabbed window (Q/E cycles tabs per [hud-layout.md](../ui/hud-layout.md)).

**Tab 1: Offer Board** — up to 6 candidate quests, plus any uncompleted intro quests pinned at top.

```
+--------------------------------------------------+
|  QUEST BOARD         [ Offers ] [ Active 2/3 ]   |
+--------------------------------------------------+
|  INTRO                                            |
|  Kill 5 enemies on floor 1                        |
|      Reward: 50 gold, 1x Tier 1 material          |
|      Starter bonus: +50 gold, 1x Health Potion    |
|      [ Accept ]                                   |
|                                                    |
|  RADIANT                                          |
|  Slay 25 enemies on floor 10                      |
|      Reward: 117 gold, 2x Tier 2 materials        |
|      [ Accept ]  [ Refuse ]                       |
|                                                    |
|  Defeat the Zone 2 boss on floor 20               |
|      Reward: 577 gold, 3x rare Tier 2 materials   |
|      [ Accept ]  [ Refuse ]                       |
|                                                    |
|  ... (up to 6 radiant offers)                     |
+--------------------------------------------------+
```

**Tab 2: Active Quests** — up to 3 accepted quests. Mirrors the previously-locked 3-slot layout.

```
+--------------------------------------------------+
|  QUEST BOARD         [ Offers ] [ Active 2/3 ]   |
+--------------------------------------------------+
|  [1] Slay 25 enemies on floor 10       12/25     |
|      Reward: 117 gold, 2x Tier 2 materials       |
|      [ Abandon ]                                  |
|                                                    |
|  [2] Reach floor 11 for the first time   DONE!   |
|      Reward: 282 gold, 2x rare Tier 2 materials  |
|      [ Claim ]                                    |
|                                                    |
|  [3] (empty — accept from Offers tab)            |
+--------------------------------------------------+
```

**Layout rules:**
- Offer tab shows up to 6 radiant offers + all uncompleted intro quests (intro pinned, labeled "INTRO", not counted against the 6).
- Active tab shows 3 slots; empty slots display `(empty — accept from Offers tab)`.
- `[ Accept ]` is disabled when Active is full (3/3). Tooltip: "Active quests full — abandon or claim one first."
- Each offer/active entry shows: description, reward summary, progress (active only, Kill quests only), and action buttons.
- Completed active quests show `[ Claim ]` with reward highlight.
- Kill quests show `progress / target_count`; ClearFloor / Boss / DepthPush are binary.

### HUD Quest Indicator

A minimal HUD element surfaces active quest state without cluttering combat. See [hud-layout.md](../ui/hud-layout.md) for placement.

- Shows count of active quests (e.g., `Quests: 2/3`) in a corner of the HUD.
- If any active quest has `completed = true` but unclaimed, the indicator pulses or gets a dot marker — hint to return to town.
- No per-quest tracker on screen (avoids overlay clutter in the isometric view). The player opens the Quest Board to see details.

### Save/Load Format

Quest state is stored inside the existing save structure (see [save.md](save.md)):

```json
{
  "quests": {
    "active": [
      {
        "id": "kill_10_25_a3f9",
        "type": "kill",
        "target_floor": 10,
        "target_count": 25,
        "description": "Slay 25 enemies on floor 10",
        "gold_reward": 117,
        "material_reward": { "tier": 2, "count": 2, "rare": false },
        "xp_reward": 0,
        "progress": 12,
        "completed": false,
        "slot": 0
      },
      {
        "id": "boss_20_1_b7c2",
        "type": "boss",
        "target_floor": 20,
        "target_count": 1,
        "description": "Defeat the Zone 2 boss on floor 20",
        "gold_reward": 577,
        "material_reward": { "tier": 2, "count": 3, "rare": true },
        "xp_reward": 0,
        "progress": 0,
        "completed": false,
        "slot": 1
      },
      {
        "id": "depthpush_11_1_c4e1",
        "type": "depth_push",
        "target_floor": 11,
        "target_count": 1,
        "description": "Reach floor 11 for the first time",
        "gold_reward": 282,
        "material_reward": { "tier": 2, "count": 2, "rare": true },
        "xp_reward": 0,
        "progress": 1,
        "completed": true,
        "slot": 2
      }
    ],
    "completed_count": 7,
    "offers": [
      {
        "id": "kill_8_23_d4e7",
        "type": "kill",
        "target_floor": 8,
        "target_count": 23,
        "description": "Slay 23 enemies on floor 8",
        "gold_reward": 95,
        "material_reward": { "tier": 1, "count": 2, "rare": false },
        "xp_reward": 0
      }
    ],
    "intro_completed": ["intro_kill_5_f1", "intro_reach_f2"]
  }
}
```

**Field descriptions:**

| Field | Type | Purpose |
|-------|------|---------|
| `quests.active` | array (max 3) | Currently active quest states |
| `quests.active[].id` | string | Unique quest identifier for deduplication |
| `quests.active[].type` | string | "kill", "boss", "clear_floor", "depth_push" |
| `quests.active[].target_floor` | int | The floor this quest references |
| `quests.active[].target_count` | int | Target to reach (kill count or 1 for one-shot) |
| `quests.active[].description` | string | Player-facing text |
| `quests.active[].gold_reward` | int | Gold payout |
| `quests.active[].material_reward` | object | `{ tier, count, rare }` |
| `quests.active[].xp_reward` | int | Bonus XP (0 except ClearFloor) |
| `quests.active[].progress` | int | Current progress |
| `quests.active[].completed` | bool | Whether target has been met |
| `quests.active[].slot` | int | Slot index (0-2) |
| `quests.completed_count` | int | Lifetime quests completed (for future achievements) |
| `quests.offers` | array (max 6) | Candidate quests shown on the Offer tab; does not include intro quests (those are derived from `intro_completed`) |
| `quests.intro_completed` | array of string | IDs of intro quests the player has claimed. Used to suppress them from the offer board on subsequent visits. |

Save version must be incremented when quest data is added. Migration for existing saves: initialize `quests` with `{ "active": [], "completed_count": 0, "offers": [], "intro_completed": [] }`. On next Guild interaction, the offer board fills with 6 radiant quests at the current `quest_scale`, and all 5 intro quests appear pinned at the top.

## Acceptance Criteria

- [ ] On new character creation, the offer board contains 5 intro quests pinned at top and 6 radiant quests at `quest_scale = 1`
- [ ] Accepting a quest moves it from Offers to Active; active cap is 3
- [ ] `[ Accept ]` is disabled (with tooltip) when 3 quests are already active
- [ ] Refusing an offered quest removes it from the board; no penalty, no cooldown
- [ ] Abandoning an active quest opens the slot; no penalty, no cooldown
- [ ] Radiant quest rewards and material tiers scale with `quest_scale = floor((player_level + deepest_floor) / 2)`, not raw `deepest_floor`
- [ ] Intro quests are one-shot per character; once claimed, never re-appear on the offer board
- [ ] Completing an intro quest grants its starter bonus in addition to the normal gold+materials reward
- [ ] HUD quest indicator shows active count and pulses when any active quest is completed-unclaimed
- [ ] Kill quest progress increments when enemies are killed on the target floor
- [ ] Kill quest progress does NOT increment on a different floor
- [ ] Boss quest completes when the specified boss is defeated
- [ ] ClearFloor quest completes when active enemy count on the target floor hits 0
- [ ] DepthPush quest completes when the player reaches the target floor for the first time
- [ ] Completed quests show "Claim" button at the Guild
- [ ] Claiming a quest awards gold, materials, and XP (ClearFloor) to the player
- [ ] Claiming a quest generates a new quest in the empty slot
- [ ] Abandoning a quest clears progress and leaves the slot empty until next Guild visit
- [ ] Backpack-full warning appears when claiming with insufficient space; materials lost if confirmed
- [ ] Gold and XP rewards are never lost regardless of backpack state
- [ ] Quest state persists across save/load correctly
- [ ] Boss quests do not generate for already-killed bosses
- [ ] Only one DepthPush quest can exist at a time across all 3 slots
- [ ] Quest rewards scale with floor depth per the formulas above
- [ ] HUD toast notification appears when a quest completes in the dungeon

## Implementation Notes

- The quest manager should be an Autoload singleton (`QuestManager`) that listens to EventBus signals and updates quest state.
- Quest generation is deterministic given `deepest_floor` and a seed, but the seed should be randomized per generation event (not per save) so repeat visits to the Guild produce varied quests.
- The `EnemyDefeated` signal on EventBus needs a `floor` parameter added. This is a breaking change to the existing signal signature -- all current listeners must be updated.
- `FloorCleared` is a new signal. The spawn system should emit it when `GetTree().GetNodesInGroup("enemies").Count == 0` on the current floor, but only once per floor visit (to avoid re-triggering when all enemies are dead briefly during respawn windows). Use a flag that resets on floor entry.
- ClearFloor detection must account for the respawn system. The check should run after an enemy dies and before the respawn timer fires. Since enemy death uses `QueueFree()` (deferred to end of frame) and respawn uses a 1.4s timer, there is a natural window where the count genuinely hits 0 before respawns begin.
- Quest UI is part of the Adventure Guild NPC interaction panel, not a standalone screen. It follows the same proximity-based panel system as all town NPCs.
- The `completed_count` field enables future achievement tracking ("Complete 100 quests") without needing to retroactively scan history.

## Open Questions

The spec was previously locked under a pure-radiant, auto-fill, 3-slot model. The PO's Quest Log additions (player selection, intro quests, scaling, material fiction) have re-opened a handful of genuine ambiguities. Please pick an option (or edit) for each:

### Q1. Offer-board refresh cadence

When (if ever) should offered radiant quests that weren't accepted get rerolled?

- A. Refresh only when empty slots exist on next Guild visit (current spec — offers persist forever until accepted/refused). `[rec]` Simplest; honors "no pressure" stance; matches fetch/research framing.
- B. Soft refresh: on each Guild visit, up to 2 stale offers (not seen in prior session) get rerolled so the board feels alive.
- C. Full refresh: any unaccepted offer is replaced on every Guild visit (most variety, but players lose bookmark-like "I'll grab that next time" behavior).

### Q2. Intro-quest visibility for returning players

We ship this system mid-life-cycle. For a character that predates intro quests and has already done (e.g.) "kill 5 on F1" naturally, do intro quests still show up?

- A. Always show all 5 intro quests until the player explicitly completes or refuses each one — starter bonuses are too valuable to skip. `[rec]` Simplest; the player opts in; the bonuses are tiny by mid-game anyway.
- B. Auto-mark intro quests as completed (no bonus) if the save's telemetry proves the objective was already met organically (e.g., `deepest_floor >= 2` satisfies intro #2). Zero starter bonuses awarded retroactively.
- C. Offer them but badge with "already done organically, accept for bonus" — honest but confusing.

### Q3. Intro-quest refusal semantics

What happens if the player hits `[ Refuse ]` on an intro quest?

- A. Refusing an intro quest is a soft-hide until the player clicks "Show Tutorials Again" in settings. `[rec]` Respects player autonomy; recoverable.
- B. Refusing an intro quest is permanent — same as claiming without the bonus. Clean but unforgiving.
- C. Intro quests cannot be refused, only accepted or ignored. Breaks the "no penalties / no pressure" stance.

### Q4. Gold-reward scaling input

Material tier now uses `quest_scale` (the averaged input). Should gold use `quest_scale` too, or stay keyed to `target_floor`?

- A. Keep gold keyed to `target_floor` (current spec). A high-level player farming shallow floors gets shallow gold — matches the floor they're actually fighting on. `[rec]` Gold is already abundant late-game; tying it to target_floor prevents a gold faucet from farming easy quests.
- B. Switch gold to `quest_scale` so rewards feel uniformly worth the player's time. Risk: creates an incentive to grind trivial floor-3 quests at level 40 for level-40 gold.
- C. Hybrid: `gold = base * floor_multiplier * min(1, quest_scale / target_floor)` — caps at quest_scale, floors at target_floor. Correct but complex.

### Q5. Material identity for quest rewards

The spec says "common materials are the type enemies drop on the floor, rare materials are boss-drop type." Loot tables (SPEC-06b) aren't locked yet.

- A. Defer to loot-drops spec — quest materials reuse whatever that spec defines, name-for-name. `[rec]` Avoids parallel truth; quest reward descriptions stay generic ("2x Tier 2 materials") until loot is locked.
- B. Define a distinct "Guild Salvage" material type that only drops from quests — separates quest-earned crafting currency from dungeon-earned, giving quests a unique reward identity.
- C. Quest materials are always the *rarest* material on the target floor (always-premium rewards). Risk: makes dungeon farming feel worse than quest farming.

### Q6. Doc rename: "Adventure Guild" → "Village Chief's Quest Board"

The locked spec repeatedly says "Adventure Guild" / "Guild Master." The town only has Village Chief, Guild Maid, and Blacksmith — there is no separate Guild NPC. Fix this doc?

- A. Rename all references to "Village Chief" / "Quest Board" across quests.md now (follow-up commit). `[rec]` Source-of-truth hygiene; matches npc-interaction.md.
- B. Leave "Guild" as the in-fiction name of the quest institution (even though the Village Chief is its only representative in town) — lets the lore breathe and makes future expansion (more Guild NPCs) easy.
- C. Clarify once at the top and keep both names — minimal churn. Risk: future readers get confused.

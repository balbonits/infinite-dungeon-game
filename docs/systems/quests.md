# Quest System

## Summary

Radiant (procedurally generated) quests offered by the Adventure Guild NPC in town. Three quests are available at a time. Completing a quest rewards gold and materials, then immediately refreshes that slot with a new quest. No time limits, no abandonment penalty.

## Current State

**Spec status: LOCKED.** Quest data model, generation formulas, tracking signals, completion flow, refresh rules, UI layout, and save format are fully defined.

Not yet implemented. The Adventure Guild NPC is defined in [town.md](../world/town.md) (SPEC-01d) but does not exist in-game.

## Design

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

Quests are generated when a slot needs filling (game start, or after a quest is completed). Generation is seeded from `deepest_floor` to keep quests appropriate to the player's progression.

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

Material tier matches the affix tier gates from [items.md](../inventory/items.md):

```
material_tier(floor):
  floor 1-9:    Tier 1
  floor 10-24:  Tier 2
  floor 25-49:  Tier 3
  floor 50-74:  Tier 4
  floor 75-99:  Tier 5
  floor 100+:   Tier 6

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
| Game start (new character) | Generate 3 quests based on `deepest_floor = 1` |
| Quest claimed at Guild | Generate 1 new quest for the empty slot |
| Quest abandoned | Slot stays empty until player visits Guild; Guild auto-fills empty slots on interaction |
| Player reaches new deepest floor | Existing quests are NOT refreshed (avoid invalidating progress) |

**Abandonment:** The player can abandon any active quest from the Guild panel. Progress is lost. The empty slot is refilled the next time the player interacts with the Guild. There is no cooldown or penalty.

**No forced refresh.** Quests are never automatically replaced or invalidated. A Kill quest targeting floor 5 remains valid even if the player is now on floor 50. The rewards will feel small by then, but the player can still complete it or abandon it at their discretion.

### UI: Guild Master Panel

The Guild panel appears when the player walks up to the Adventure Guild NPC (proximity interaction, same as all town NPCs -- see [town.md](../world/town.md)).

```
+--------------------------------------------------+
|  ADVENTURE GUILD                                  |
|  "Another day, another descent."                  |
+--------------------------------------------------+
|                                                    |
|  [1] Slay 25 enemies on floor 10       12/25     |
|      Reward: 117 gold, 2x Tier 2 materials       |
|      [ Abandon ]                                  |
|                                                    |
|  [2] Defeat the Zone 2 boss on floor 20          |
|      Reward: 577 gold, 3x rare Tier 2 materials  |
|      [ Abandon ]                                  |
|                                                    |
|  [3] Reach floor 11 for the first time   DONE!   |
|      Reward: 282 gold, 2x rare Tier 2 materials  |
|      [ Claim ]                                    |
|                                                    |
+--------------------------------------------------+
```

**Layout rules:**
- 3 quest slots, always visible (empty slots show "No quest -- check back later" until Guild auto-fills)
- Each slot shows: description, progress (e.g., "12/25" for Kill quests, nothing for one-shot quests until complete), reward summary, and an action button
- Active quests show `[ Abandon ]` button
- Completed quests show `[ Claim ]` button with reward highlight
- Kill quests show `progress / target_count`
- ClearFloor, Boss, and DepthPush show no progress bar (binary: incomplete or complete)

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
    "completed_count": 7
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

Save version must be incremented when quest data is added. Migration for existing saves: initialize `quests` with `{ "active": [], "completed_count": 0 }` and generate 3 fresh quests on next Guild interaction.

## Acceptance Criteria

- [ ] 3 quests are generated on new character creation, scaled to `deepest_floor`
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

None. Spec is locked.

# Quest System

## Summary

Radiant (procedurally generated) quests offered by the Village Chief in town. The Village Chief maintains a **Quests Board** that displays an offer list of candidate quests; the player chooses which ones to accept, up to an active-quest cap. Completing a quest rewards gold and materials. Intro quests guide new players through core mechanics without interrupting them. No time limits, no abandonment penalty, no "world-ending" stakes — quests are checklists and fetch/hunt research tasks, not emergencies.

## Current State

**Spec status: Locked (v1.1).** Quest data model (base `Quest` type + sub-types), generation formulas, tracking signals, completion flow, UI layout, save format, player-selection model, intro-quest track, and level/floor scaling (`quest_scale`) are defined. All prior Open Questions from v1.0 have been resolved by the PO and folded into the body of this spec.

Not yet implemented. Quests are delivered by the **Village Chief** NPC per [npc-interaction.md](../flows/npc-interaction.md). The UI window the Village Chief opens is titled **"Quests Board"**. There is no in-fiction "Adventure Guild" — the Village Chief personally curates the board on behalf of the frontier settlement. The Guild Maid owns Bank/Teleport, not quests. The written tutorial that these quests reinforce lives in the Pause menu (see [hud-layout.md](../ui/hud-layout.md) for hotkey and placement).

## Design

### Player Selection Model

Quests are not auto-installed into slots. The Village Chief's Quests Board maintains two structures:

- **Offer board** — up to **6** candidate radiant quests the player can browse and pick from. Persists between visits (see [Quest Refresh Rules](#quest-refresh-rules)).
- **Active quests** — quests the player has accepted. Active-quest cap: **3** (same as the prior 3-slot model — preserves the locked UI layout and reward pacing).

```
Player walks up to Village Chief
  -> Opens Quests Board (Offers + Active tabs)
  -> Offers tab: browse up to 6 candidates, [Accept] to move one into Active
  -> Active tab: view progress, claim completed, or [Abandon] to remove
```

**Refusal / abandonment are free.** Declining an offered quest or abandoning an active one has no penalty and no cooldown — quests are research requests and bounty checklists, not emergencies. No time limits anywhere in the system.

**Equivalent reward tiers.** All offered quests at a given scaling level are balanced to equivalent value (gold + material value roughly equal per [Reward Formulas](#reward-formulas)), so picking is about *what the player feels like doing tonight* (kill fast, push depth, hunt a boss) — not about chasing the best payout.

### Intro Quests

A curated, non-radiant track that teaches core mechanics by putting the player *into* them. Intro quests sit in the offer board alongside radiant quests until completed, refused, or auto-resolved as organically satisfied. Each is one-shot per character.

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

**Intro-quest lifecycle (extended state machine).** Intro quests carry two states that radiant quests do not:

- **`OrganicallySatisfied`** — the save's telemetry proves the player already met the objective before the intro quest could be accepted (e.g., `deepest_floor >= 2` satisfies "Reach floor 2"; `character.has_ever_equipped_item == true` satisfies "Equip an item via the Blacksmith"). When this is detected at Quests Board open, the intro quest is auto-moved into `OrganicallySatisfied` with **no starter bonus** awarded. It does not occupy an offer slot and does not re-appear. This exclusively serves returning players who predate the intro-quest track; new characters encounter each intro quest before the telemetry could trigger.
- **`Refused`** — the player explicitly hit `[ Refuse ]` on an intro quest. Refusal is **permanent** for that character: same effect as claiming without the starter bonus. The quest is gone, will not re-appear, and there is no "Show Tutorials Again" toggle. This is a deliberate opt-out for experienced players who do not want the shoulder-tap.

Neither state applies to radiant quests. Radiant quests that the player dismisses from the offer board are simply removed (the slot empties and will re-roll on the next Village Chief visit); radiant quests the player abandons from Active use the standard `Abandoned` state.

**Organic-satisfaction predicates (per intro quest).** The check runs when the Quests Board is opened, before the board is rendered:

| Intro # | Objective | Organic-satisfied predicate |
|---|---|---|
| 1 | Kill 5 enemies on floor 1 | `total_enemies_killed_on_floor_1 >= 5` |
| 2 | Reach floor 2 | `deepest_floor >= 2` |
| 3 | Return to town and claim a reward | `quests.completed_count >= 1` (any quest ever claimed) |
| 4 | Equip an item via the Blacksmith | `character.has_ever_equipped_item == true` |
| 5 | Reach floor 5 | `deepest_floor >= 5` |

If the save lacks the telemetry field (pre-migration saves), the predicate evaluates to `false` and the quest surfaces normally — worst case is a returning player sees an intro quest they already organically fulfilled and can `[ Refuse ]` or `[ Accept ]` it as they prefer.

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

Quests share a common **base `Quest` type**. Sub-types extend the base with kind-specific fields and (for intro quests) a richer state machine. New quest families added later (story quests, event quests, seasonal bounties) are new sub-types of `Quest`, not new top-level shapes.

#### Base `Quest`

All quests, radiant or intro, carry these fields:

```
abstract record Quest {
  id:              string        // Deterministic hash: "{kind}_{type}_{target_floor}_{target_count}_{seed}"
  kind:            QuestKind     // Radiant | Intro (extensible: Story, Event, ...)
  type:            QuestType     // Kill | Boss | ClearFloor | DepthPush
  title:           string        // Short label (e.g., "Cull the Lowlands")
  description:     string        // Player-facing text (generated from template)
  target_floor:    int           // The floor this quest references
  target_count:    int           // Kill quest: enemy count. Others: 1 (implicit).
  quest_scale:     int           // Scaling input frozen at generation time (see formulas below)
  progress:        int           // Current progress toward target_count
  state:           QuestState    // See state machines below
  rewards:         QuestRewards  // Gold + materials + bonus XP
  created_at:      timestamp     // For sort order and staleness heuristics
}

record QuestRewards {
  gold:     int              // Gold payout on completion
  material: MaterialReward   // Materials payout on completion
  xp:       int              // Bonus XP (ClearFloor only; 0 for others)
}

record MaterialReward {
  tier:  int   // Material tier (matches floor-based affix tiers from items.md)
  count: int   // Number of materials awarded
  rare:  bool  // true = rare materials (Boss/DepthPush), false = common (Kill/ClearFloor)
}

enum QuestType  { Kill, Boss, ClearFloor, DepthPush }
enum QuestKind  { Radiant, Intro }
```

#### Sub-type: `RadiantQuest`

Procedurally generated by the Village Chief's offer board. Standard 4-state lifecycle; nothing intro-specific.

```
record RadiantQuest : Quest {
  kind  = QuestKind.Radiant     // fixed
  state : RadiantQuestState     // restricted state machine (see below)
  slot  : int?                  // 0, 1, or 2 when Accepted; null while on the offer board
}

enum RadiantQuestState {
  Offered,     // On the offer board; not yet accepted
  Accepted,    // In an Active slot; progress > 0 allowed
  Completed,   // progress >= target_count; awaiting Claim at Village Chief
  Abandoned    // Player hit [ Abandon ] or [ Refuse ]; removed from board/slot, terminal
}
```

#### Sub-type: `IntroQuest`

Curated one-shot tutorial track. Extends the base state machine with two intro-only states.

```
record IntroQuest : Quest {
  kind            = QuestKind.Intro   // fixed
  state           : IntroQuestState   // extended state machine (see below)
  starter_bonus   : StarterBonus      // Additive bonus applied on Completed (not OrganicallySatisfied/Refused)
  organic_check   : OrganicPredicate  // Evaluated on Quests Board open; may auto-promote to OrganicallySatisfied
  slot            : int?              // 0, 1, or 2 when Accepted; null otherwise
}

record StarterBonus {
  gold:  int                  // e.g., +50
  items: list<ItemGrant>      // e.g., [{ id: "health_potion", qty: 1 }]
}

enum IntroQuestState {
  Offered,                // Pinned on the offer board
  Accepted,               // In an Active slot
  Completed,              // Claimed at Village Chief; starter bonus awarded; terminal
  Abandoned,              // Abandoned from Active after being accepted (edge case; terminal, no bonus)
  OrganicallySatisfied,   // Auto-resolved by telemetry; no starter bonus awarded; terminal, never re-appears
  Refused                 // Player hit [ Refuse ] on the offer board; permanent for this character; terminal
}
```

`OrganicallySatisfied` and `Refused` are **intro-exclusive**. They do not apply to radiant quests. `Abandoned` is the shared terminal state for "player rejected this quest"; the terms do not collapse — `Refused` specifically means "intro quest rejected from the offer board before acceptance," while `Abandoned` means "quest rejected after being moved into an active slot" (and for radiant quests, also covers offer-board dismissal since radiant offers are simply re-rolled).

#### Why a base type at all

- One definition for `id`, `description`, `target_floor`, `rewards`, `progress`, `created_at` — quest-generic systems (HUD indicator, save format, reward-claim pipeline) traffic in `Quest` and don't need to know sub-types.
- Sub-types own only the differences: their state enum, any sub-type-specific fields (`starter_bonus`, `organic_check`).
- New quest families (story arcs, seasonal events) slot in as new `Quest` sub-types without reshaping existing systems.

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

Gold scales with the **target floor** (depth difficulty of the work), but is **capped by `quest_scale`** so a high-level player farming a shallow floor cannot turn trivial quests into a gold faucet:

```
base_gold(type):
  Kill       = 50
  ClearFloor = 75
  Boss       = 150
  DepthPush  = 120

floor_multiplier(floor) = 1 + (floor - 1) * 0.15

gold_reward(type, target_floor, quest_scale) =
  floor(
    base_gold(type)
    * floor_multiplier(target_floor)
    * min(1, quest_scale / target_floor)
  )
```

The `min(1, quest_scale / target_floor)` term is 1 whenever the player's `quest_scale` meets or exceeds the target floor (the normal case — you do quests near your current depth), so the payout matches the floor-driven table below. It only bites when `quest_scale < target_floor`, which never occurs in practice because Boss/DepthPush are the only quest types that can target floors beyond `deepest_floor`, and their floor-over-scale ratio is bounded. The cap's real job is on the opposite side: it prevents runaway payouts when a level-40 character farms a floor-3 Kill quest, because `quest_scale` also sits around the mid-20s+ while `target_floor` is 3 — the ratio clamps at 1, gold stays at the floor-3 value, and the floor-3 quest never out-pays floor-3 combat.

| Target Floor | Kill Gold | ClearFloor Gold | Boss Gold | DepthPush Gold |
|-------|-----------|-----------------|-----------|----------------|
| 1 | 50 | 75 | 150 | 120 |
| 5 | 80 | 120 | 240 | 192 |
| 10 | 117 | 176 | 352 | 282 |
| 20 | 192 | 288 | 577 | 462 |
| 50 | 417 | 626 | 1,252 | 1,002 |
| 100 | 792 | 1,188 | 2,377 | 1,902 |

(Values shown assume `quest_scale >= target_floor`, i.e., the cap is not active — the most common case.)

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

**Material identity is owned by SPEC-06b (loot-drops), not this spec.** Quest reward descriptions in the Quests Board UI stay generic ("2x Tier 2 materials", "3x rare Tier 2 materials") until loot-drops is locked. Once SPEC-06b names the common and rare material families per floor/zone, the quest system references them by name-for-name lookup — there is no parallel "Guild Salvage" or quest-exclusive material. This avoids parallel truth and keeps dungeon loot and quest rewards on the same economy.

**Fictional framing.** The materials the Village Chief hands out are not summoned from thin air — they come from the settlement's own processing of the monsters the player has already killed. How exactly the frontier outpost harvests, preserves, and extracts the useful bits from a corpse the player never dragged home is the settlement's trade secret. The player doesn't ask. The Village Chief doesn't tell. It reads as *our bounty cut, cleaned and packaged* — a believable economic loop without forcing the player to manage carcasses in the backpack.

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
  -> Quest.state = Completed
  -> HUD notification: "Quest Complete: {description}" (brief toast, 3 seconds)
  -> Rewards held in escrow (not yet given)
  -> Player must visit the Village Chief to claim rewards
  -> On Village Chief interaction (Quests Board opens):
     -> Completed quests show "Claim" button on the Active tab
     -> Clicking Claim:
        -> Gold added to character.gold
        -> Materials added to backpack (overflow: materials are lost if backpack is full)
        -> XP awarded (ClearFloor only)
        -> IntroQuest only: starter_bonus applied (gold + items)
        -> Quest slot cleared
        -> For radiant: a replacement offer is re-rolled onto the offer board if there is free offer-board space (NOT auto-accepted)
```

**Why claim at the Village Chief, not auto-reward:** Returning to town is a core loop anchor. Forcing the player to visit the Village Chief to collect rewards keeps them cycling through town, interacting with NPCs, and making decisions about what to do next. It also prevents inventory surprises mid-combat.

**Backpack overflow:** If the player's backpack is full when claiming materials, the materials are lost. The claim UI shows a warning: "Backpack full! Materials will be lost." The player can cancel, visit the Guild Maid's Bank tab to free up space, then return. Gold and XP are never lost (gold has no inventory slot; XP is applied directly). IntroQuest starter-bonus *items* follow the same overflow rule; starter-bonus *gold* is never lost.

### Quest Refresh Rules

**Offered quests persist forever until the player acts on them.** Empty offer slots refill only when the player opens the Quests Board (i.e., interacts with the Village Chief). The board never silently re-rolls between visits, never auto-ages out stale offers, and never pressures the player to accept anything. If a floor-5 Kill quest has been sitting on the board since session 1, it's still there in session 20 — the rewards just feel small by then.

| Event | What Happens |
|-------|-------------|
| Game start (new character) | Fill offer board with 6 radiant quests seeded from `quest_scale = 1`, plus the 5 intro quests pinned to the top. |
| Player opens Quests Board (Village Chief interaction) | Evaluate each `IntroQuest.organic_check`; any that pass flip to `OrganicallySatisfied` (terminal, no bonus) and are removed from the board. If the offer board has < 6 radiant slots, generate new radiant quests at the current `quest_scale` to refill. Existing offers are never re-rolled. |
| Player accepts a quest | Quest moves from Offers to Active (up to active-cap of 3). Offer slot empties; refills on next Quests Board open. |
| Player refuses a radiant offer | Offered quest is removed from the board (`state = Abandoned`, terminal for that specific generated instance). Slot empties and refills on next Quests Board open with a freshly generated quest. |
| Player refuses an intro offer | Intro quest is permanently removed for this character (`state = Refused`, terminal). Does not re-appear, does not occupy an offer slot, and there is no "Show Tutorials Again" toggle. |
| Quest claimed at Village Chief | Quest `state = Completed`. Active slot opens up; replacement offer re-rolled onto the board if offer-board has free slots. |
| Player abandons an active quest | Active slot opens up. Progress lost. No penalty. `state = Abandoned`, terminal. |
| Player reaches new deepest floor | Existing active quests are NOT refreshed (avoid invalidating progress). New offers generated at the new `quest_scale` on next Quests Board open. |

**Abandonment / refusal.** Both are free, instant, and never punished. Quests are checklists, not contracts. The only "cost" of churning quests is the player's own time.

**No forced refresh.** Quests are never automatically replaced or invalidated. A Kill quest targeting floor 5 remains valid even if the player is now on floor 50. The rewards will feel small by then, but the player can still complete it or abandon it at their discretion.

### UI: Quests Board Window

The **Quests Board** window opens when the player walks up to the Village Chief NPC (proximity interaction, same as all town NPCs — see [npc-interaction.md](../flows/npc-interaction.md)). Tabbed window (Q/E cycles tabs per [hud-layout.md](../ui/hud-layout.md)). The window's title bar reads "Quests Board".

**Tab 1: Offers** — up to 6 candidate radiant quests, plus any still-active intro quests pinned at top.

```
+--------------------------------------------------+
|  QUESTS BOARD        [ Offers ] [ Active 2/3 ]   |
+--------------------------------------------------+
|  INTRO                                            |
|  Kill 5 enemies on floor 1                        |
|      Reward: 50 gold, 1x Tier 1 material          |
|      Starter bonus: +50 gold, 1x Health Potion    |
|      [ Accept ]  [ Refuse ]                       |
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

**Tab 2: Active** — up to 3 accepted quests. Mirrors the previously-locked 3-slot layout.

```
+--------------------------------------------------+
|  QUESTS BOARD        [ Offers ] [ Active 2/3 ]   |
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
- Offers tab shows up to 6 radiant offers + all not-yet-terminal intro quests (intro pinned, labeled "INTRO", not counted against the 6).
- Active tab shows 3 slots; empty slots display `(empty — accept from Offers tab)`.
- `[ Accept ]` is disabled when Active is full (3/3). Tooltip: "Active quests full — abandon or claim one first."
- `[ Refuse ]` on a radiant offer re-rolls its slot on next Quests Board open. `[ Refuse ]` on an intro quest is **permanent for the character** — confirm with a short inline warning on the button: "Refuse? This tutorial won't re-appear."
- Each offer/active entry shows: description, reward summary, progress (active only, Kill quests only), and action buttons.
- Completed active quests show `[ Claim ]` with reward highlight.
- Kill quests show `progress / target_count`; ClearFloor / Boss / DepthPush are binary.

### HUD Quest Indicator

A minimal HUD element surfaces active quest state without cluttering combat. See [hud-layout.md](../ui/hud-layout.md) for placement.

- Shows count of active quests (e.g., `Quests: 2/3`) in a corner of the HUD.
- If any active quest has `completed = true` but unclaimed, the indicator pulses or gets a dot marker — hint to return to town.
- No per-quest tracker on screen (avoids overlay clutter in the isometric view). The player opens the Quests Board to see details.

### Save/Load Format

Quest state is stored inside the existing save structure (see [save.md](save.md)). Each serialized quest carries a `kind` discriminator so the loader can reconstruct the right sub-type; intro-specific fields only appear on `kind: "intro"` entries.

```json
{
  "quests": {
    "active": [
      {
        "kind": "radiant",
        "id": "radiant_kill_10_25_a3f9",
        "type": "kill",
        "title": "Cull the Lowlands",
        "description": "Slay 25 enemies on floor 10",
        "target_floor": 10,
        "target_count": 25,
        "quest_scale": 12,
        "progress": 12,
        "state": "accepted",
        "rewards": {
          "gold": 117,
          "material": { "tier": 2, "count": 2, "rare": false },
          "xp": 0
        },
        "created_at": 1714320000,
        "slot": 0
      },
      {
        "kind": "radiant",
        "id": "radiant_boss_20_1_b7c2",
        "type": "boss",
        "title": "Break the Zone 2 Tyrant",
        "description": "Defeat the Zone 2 boss on floor 20",
        "target_floor": 20,
        "target_count": 1,
        "quest_scale": 16,
        "progress": 0,
        "state": "accepted",
        "rewards": {
          "gold": 577,
          "material": { "tier": 2, "count": 3, "rare": true },
          "xp": 0
        },
        "created_at": 1714320000,
        "slot": 1
      },
      {
        "kind": "radiant",
        "id": "radiant_depthpush_11_1_c4e1",
        "type": "depth_push",
        "title": "Into the Unknown",
        "description": "Reach floor 11 for the first time",
        "target_floor": 11,
        "target_count": 1,
        "quest_scale": 12,
        "progress": 1,
        "state": "completed",
        "rewards": {
          "gold": 282,
          "material": { "tier": 2, "count": 2, "rare": true },
          "xp": 0
        },
        "created_at": 1714320000,
        "slot": 2
      }
    ],
    "offers": [
      {
        "kind": "radiant",
        "id": "radiant_kill_8_23_d4e7",
        "type": "kill",
        "title": "Thin the Pack",
        "description": "Slay 23 enemies on floor 8",
        "target_floor": 8,
        "target_count": 23,
        "quest_scale": 10,
        "progress": 0,
        "state": "offered",
        "rewards": {
          "gold": 95,
          "material": { "tier": 1, "count": 2, "rare": false },
          "xp": 0
        },
        "created_at": 1714320000
      },
      {
        "kind": "intro",
        "id": "intro_kill_5_f1",
        "type": "kill",
        "title": "First Blood",
        "description": "Kill 5 enemies on floor 1",
        "target_floor": 1,
        "target_count": 5,
        "quest_scale": 1,
        "progress": 0,
        "state": "offered",
        "rewards": {
          "gold": 50,
          "material": { "tier": 1, "count": 1, "rare": false },
          "xp": 0
        },
        "starter_bonus": {
          "gold": 50,
          "items": [{ "id": "health_potion", "qty": 1 }]
        },
        "created_at": 1714320000
      }
    ],
    "intro_history": {
      "intro_reach_f2": "completed",
      "intro_return_to_town": "organically_satisfied",
      "intro_equip_item": "refused"
    },
    "completed_count": 7
  }
}
```

**Field descriptions (shared):**

| Field | Type | Purpose |
|-------|------|---------|
| `quests.active` | array (max 3) | Currently accepted quests (state ∈ {accepted, completed}) |
| `quests.offers` | array (max 6 radiant + up to 5 intro) | Quests shown on the Offers tab; intro quests live here until they reach a terminal state |
| `quests.active[] / .offers[].kind` | string | `"radiant"` or `"intro"` — discriminator for sub-type reconstruction |
| `quests.active[] / .offers[].id` | string | Unique quest identifier |
| `quests.active[] / .offers[].type` | string | `"kill"`, `"boss"`, `"clear_floor"`, `"depth_push"` |
| `quests.active[] / .offers[].title` | string | Short label for the Quests Board entry |
| `quests.active[] / .offers[].description` | string | Player-facing objective text |
| `quests.active[] / .offers[].target_floor` | int | The floor this quest references |
| `quests.active[] / .offers[].target_count` | int | Target to reach |
| `quests.active[] / .offers[].quest_scale` | int | Scaling input frozen at generation time |
| `quests.active[] / .offers[].progress` | int | Current progress toward `target_count` |
| `quests.active[] / .offers[].state` | string | One of `offered` / `accepted` / `completed` / `abandoned` (radiant) plus `organically_satisfied` / `refused` (intro only) |
| `quests.active[] / .offers[].rewards` | object | `{ gold, material: { tier, count, rare }, xp }` |
| `quests.active[] / .offers[].created_at` | int (unix) | Creation timestamp for sort / heuristic staleness |
| `quests.active[].slot` | int | Slot index (0-2); present only on quests in the active array |
| `quests.completed_count` | int | Lifetime quests claimed (for future achievements) |

**Intro-only fields (present only when `kind: "intro"`):**

| Field | Type | Purpose |
|-------|------|---------|
| `offers[].starter_bonus` | object | `{ gold, items: [{ id, qty }] }` awarded on Completed only |
| `quests.intro_history` | map<string, string> | Terminal status of each intro quest this character has resolved. Keys are intro-quest IDs; values are one of `completed` / `organically_satisfied` / `refused` / `abandoned`. Used to suppress terminal intro quests from ever re-appearing. |

**Migration for saves predating v1.1:** initialize `quests` with `{ "active": [], "offers": [], "intro_history": {}, "completed_count": 0 }`. On the next Quests Board open, all 5 intro quests are materialized into `offers`, then each `organic_check` is evaluated against save telemetry — any intro quest that passes flips to `organically_satisfied`, is recorded in `intro_history`, and is removed from `offers` immediately (no starter bonus). Remaining intro quests stay pinned on the board for the player. The radiant offer list then fills to 6 at the current `quest_scale`.

Pre-v1.1 saves that recorded intro-quest completions in the old `intro_completed` array are migrated into `intro_history` with value `"completed"`.

## Acceptance Criteria

- [ ] Data model implements a base `Quest` record with `RadiantQuest` and `IntroQuest` sub-types; quest-generic systems (save, HUD, reward claim) traffic in `Quest`, not sub-types
- [ ] Radiant quests use only the `Offered / Accepted / Completed / Abandoned` state machine
- [ ] Intro quests extend radiant states with `OrganicallySatisfied` and `Refused`; neither state is reachable for radiant quests
- [ ] On new character creation, the offer board contains the 5 intro quests pinned at top and 6 radiant quests at `quest_scale = 1`
- [ ] Opening the Quests Board evaluates each `IntroQuest.organic_check`; passing quests flip to `OrganicallySatisfied` with no starter bonus awarded and do not appear on the board
- [ ] Refusing an intro quest sets `state = Refused`, is permanent for that character, and the quest never re-appears — no "Show Tutorials Again" toggle exists
- [ ] Refusing a radiant offer empties its slot; a fresh radiant quest re-rolls into that slot on the next Quests Board open (not sooner)
- [ ] Offered quests are never auto-refreshed between Quests Board opens; a stale offer from session 1 is still offered in session 20 until the player acts on it
- [ ] Accepting a quest moves it from Offers to Active; active cap is 3
- [ ] `[ Accept ]` is disabled (with tooltip) when 3 quests are already active
- [ ] Abandoning an active quest opens the slot; no penalty, no cooldown; `state = Abandoned`
- [ ] Radiant quest material tiers scale with `quest_scale = floor((player_level + deepest_floor) / 2)`, not raw `deepest_floor`
- [ ] Gold reward uses the hybrid formula `base * floor_multiplier(target_floor) * min(1, quest_scale / target_floor)`; farming a shallow floor with a high `quest_scale` does not inflate gold above the floor's baseline
- [ ] Quest reward material identity defers to SPEC-06b (loot-drops); reward descriptions stay generic ("2x Tier 2 materials") until loot is locked
- [ ] Window title of the quest UI reads "Quests Board"; the NPC it opens from is the Village Chief
- [ ] Completing an intro quest grants its `starter_bonus` (gold + items) in addition to the normal `rewards`; `OrganicallySatisfied` and `Refused` paths do not grant the starter bonus
- [ ] HUD quest indicator shows active count and pulses when any active quest is completed-unclaimed
- [ ] Kill quest progress increments when enemies are killed on the target floor
- [ ] Kill quest progress does NOT increment on a different floor
- [ ] Boss quest completes when the specified boss is defeated
- [ ] ClearFloor quest completes when active enemy count on the target floor hits 0
- [ ] DepthPush quest completes when the player reaches the target floor for the first time
- [ ] Completed quests show "Claim" button on the Active tab of the Quests Board
- [ ] Claiming a quest awards gold, materials, and XP (ClearFloor) to the player
- [ ] Claiming an active radiant quest empties its slot; the offer board re-rolls a replacement offer only if it has free offer-slots
- [ ] Backpack-full warning appears when claiming with insufficient space; materials lost if confirmed
- [ ] Gold, XP, and intro starter-bonus gold are never lost regardless of backpack state
- [ ] Quest state persists across save/load correctly, including sub-type discriminator and state enum
- [ ] Boss quests do not generate for already-killed bosses
- [ ] Only one DepthPush quest can exist at a time across all 3 active slots
- [ ] HUD toast notification appears when a quest completes in the dungeon
- [ ] Saves predating v1.1 migrate: `intro_completed[]` becomes `intro_history{id: "completed"}`; the offer board re-materializes on next Quests Board open

## Implementation Notes

- The quest manager should be an Autoload singleton (`QuestManager`) that listens to EventBus signals and updates quest state.
- Quest generation is deterministic given `deepest_floor` and a seed, but the seed should be randomized per generation event (not per save) so repeat visits to the Village Chief produce varied quests.
- The `EnemyDefeated` signal on EventBus needs a `floor` parameter added. This is a breaking change to the existing signal signature -- all current listeners must be updated.
- `FloorCleared` is a new signal. The spawn system should emit it when `GetTree().GetNodesInGroup("enemies").Count == 0` on the current floor, but only once per floor visit (to avoid re-triggering when all enemies are dead briefly during respawn windows). Use a flag that resets on floor entry.
- ClearFloor detection must account for the respawn system. The check should run after an enemy dies and before the respawn timer fires. Since enemy death uses `QueueFree()` (deferred to end of frame) and respawn uses a 1.4s timer, there is a natural window where the count genuinely hits 0 before respawns begin.
- Quest UI is part of the Village Chief's NPC interaction panel (the "Quests Board" window), not a standalone screen. It follows the same proximity-based panel system as all town NPCs (see [npc-interaction.md](../flows/npc-interaction.md)).
- The `Quest` base record and `RadiantQuest` / `IntroQuest` sub-types live in a shared quest-data module consumed by `QuestManager`, the save layer, and the Quests Board UI. Runtime polymorphism is preferred over discriminated-union gymnastics — the `kind` discriminator in the save JSON is for serialization only; in-memory code can switch on actual C# types.
- Intro-quest `organic_check` predicates read from existing save telemetry (`deepest_floor`, `character.gold` history, equip history). The one predicate that needs new telemetry is `total_enemies_killed_on_floor_1`. Rather than build a per-floor kill counter, implementers may choose the cheaper equivalent: "on intro-quest-1 `Offered` creation, if `deepest_floor >= 1` AND `character.created_at < now - 1 minute`, treat as organically satisfied." Choose whichever is simpler — the behavior is the same from the player's perspective.
- The `completed_count` field enables future achievement tracking ("Complete 100 quests") without needing to retroactively scan history.


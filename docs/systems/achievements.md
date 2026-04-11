# Achievement System — The Fated Ledger

## Summary

A permanent record of player accomplishments, tracked per save slot. Achievements are called entries in the **Fated Ledger** — a magical record that writes itself as the player acts, kept by the Adventure Guild. Five categories, 42 achievements, each with a concrete trigger condition and a reward (gold, title, or unique item). Achievements are never lost, even on death.

## Current State

**Spec status: LOCKED.** All achievement definitions, trigger conditions, reward values, data model, UI placement, save format, and notification flow are defined. Individual reward item stats are deferred to the unique items spec (SYS-08) — the achievement system only references item IDs.

---

## Design

### Lore

The Fated Ledger is a magical tome maintained by the Adventure Guild. It was brought to the frontier settlement specifically to document the first expedition into the infinite dungeon. The book writes itself — ink appears on its pages the moment an adventurer accomplishes something noteworthy. The Guild Master keeps the original copy; the player can review their own entries at any time.

Lore justification for permanence: the Ledger records what *happened*, not what the adventurer currently possesses. The dungeon can eat memories (XP) and steal belongings (items), but it cannot rewrite history. Once ink appears on the page, it stays.

### Data Model

#### AchievementDef (static, read-only)

Defines what an achievement is. Loaded once at startup. Never changes at runtime.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique key, snake_case (e.g., `first_blood`) |
| `name` | string | Display name shown in UI (e.g., "First Blood") |
| `description` | string | Flavor text describing the feat |
| `category` | AchievementCategory | One of: Combat, Exploration, Progression, Economy, Mastery |
| `tier` | int | 1 = Bronze, 2 = Silver, 3 = Gold. Determines reward scale. |
| `trigger_type` | TriggerType | Which signal/event to listen for (see Tracking section) |
| `trigger_threshold` | int | Numeric threshold to meet or exceed |
| `reward_type` | RewardType | One of: Gold, Title, UniqueItem |
| `reward_value` | string | Gold amount, title string, or unique item ID |
| `hidden` | bool | If true, name and description show as "???" until earned |

#### AchievementState (per save slot, mutable)

Tracks player progress toward each achievement.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Matches `AchievementDef.id` |
| `unlocked` | bool | Whether this achievement has been earned |
| `progress` | int | Current count toward `trigger_threshold` |
| `unlock_timestamp` | string | ISO-8601 timestamp when earned, empty string if locked |

### Achievement Categories

| Category | Icon Color | Theme |
|----------|-----------|-------|
| **Combat** | Red (#FF6F6F) | Killing, damage, combat feats |
| **Exploration** | Cyan (#4AE8E8) | Floors reached, areas discovered |
| **Progression** | Green (#6BFF89) | Levels, stats, skills |
| **Economy** | Gold (#F5C86B) | Gold earned, items bought/sold/crafted |
| **Mastery** | Purple (#9B6BFF) | Class-specific feats, advanced play |

### Tier System

| Tier | Label | Gold Reward Multiplier | Rarity |
|------|-------|----------------------|--------|
| 1 | Bronze | 1x | Common milestones |
| 2 | Silver | 3x | Significant feats |
| 3 | Gold | 10x | Exceptional accomplishments |

---

### Achievement List (42 Total)

#### Combat (10 achievements)

| ID | Name | Tier | Trigger | Threshold | Reward |
|----|------|------|---------|-----------|--------|
| `first_blood` | First Blood | 1 | EnemyDefeated (cumulative) | 1 | 50 gold |
| `century_slayer` | Century Slayer | 1 | EnemyDefeated (cumulative) | 100 | 200 gold |
| `thousand_cuts` | A Thousand Cuts | 2 | EnemyDefeated (cumulative) | 1,000 | 1,000 gold |
| `ten_thousand_strong` | Ten Thousand Strong | 3 | EnemyDefeated (cumulative) | 10,000 | Title: "Exterminator" |
| `floor_sweep` | Clean Sweep | 1 | FloorWiped (cumulative) | 1 | 100 gold |
| `ten_sweeps` | Serial Sweeper | 2 | FloorWiped (cumulative) | 10 | 500 gold |
| `fifty_sweeps` | Absolute Zero | 3 | FloorWiped (cumulative) | 50 | Title: "The Thorough" |
| `boss_slayer` | Boss Slayer | 2 | BossDefeated (cumulative) | 1 | 300 gold |
| `five_bosses` | Boss Hunter | 2 | BossDefeated (cumulative) | 5 | 1,500 gold |
| `deathless_floor` | Untouchable | 3 | FloorClearedNoHit | 1 | Title: "Untouchable" |

#### Exploration (9 achievements)

| ID | Name | Tier | Trigger | Threshold | Reward |
|----|------|------|---------|-----------|--------|
| `first_descent` | Into the Dark | 1 | FloorReached | 2 | 50 gold |
| `floor_10` | The First Abyss | 1 | FloorReached | 10 | 200 gold |
| `floor_25` | Deep Dweller | 2 | FloorReached | 25 | 500 gold |
| `floor_50` | Abyssal Pioneer | 2 | FloorReached | 50 | 1,500 gold |
| `floor_100` | Centurion of Depth | 3 | FloorReached | 100 | Title: "Abyssal" |
| `floor_200` | Beyond the Pale | 3 | FloorReached | 200 | UniqueItem: `ledger_lantern` |
| `ten_floors_one_session` | Marathon Runner | 2 | FloorsInSession | 10 | 500 gold |
| `return_to_town` | Homecoming | 1 | TownReturns (cumulative) | 1 | 25 gold |
| `fifty_town_returns` | Frequent Flyer | 2 | TownReturns (cumulative) | 50 | 500 gold |

#### Progression (9 achievements)

| ID | Name | Tier | Trigger | Threshold | Reward |
|----|------|------|---------|-----------|--------|
| `level_5` | Awakening | 1 | LevelReached | 5 | 100 gold |
| `level_10` | Ascendant | 1 | LevelReached | 10 | 250 gold |
| `level_25` | Adept | 2 | LevelReached | 25 | 750 gold |
| `level_50` | Veteran | 2 | LevelReached | 50 | 2,000 gold |
| `level_100` | Centurion | 3 | LevelReached | 100 | Title: "Centurion" |
| `first_stat_point` | Self-Improvement | 1 | StatPointAllocated (cumulative) | 1 | 25 gold |
| `hundred_stat_points` | Sculptor of Flesh | 2 | StatPointAllocated (cumulative) | 100 | 500 gold |
| `first_skill_level` | New Trick | 1 | SkillLevelReached (any skill) | 1 | 50 gold |
| `skill_25` | Specialist | 2 | SkillLevelReached (any single skill) | 25 | 1,000 gold |

#### Economy (8 achievements)

| ID | Name | Tier | Trigger | Threshold | Reward |
|----|------|------|---------|-----------|--------|
| `first_gold` | Breadwinner | 1 | GoldEarned (cumulative lifetime) | 1 | 10 gold |
| `thousand_gold` | Prospector | 1 | GoldEarned (cumulative lifetime) | 1,000 | 200 gold |
| `ten_thousand_gold` | Tycoon | 2 | GoldEarned (cumulative lifetime) | 10,000 | 1,000 gold |
| `hundred_thousand_gold` | Midas | 3 | GoldEarned (cumulative lifetime) | 100,000 | Title: "Golden" |
| `first_purchase` | First Purchase | 1 | ItemsPurchased (cumulative) | 1 | 25 gold |
| `first_craft` | Forgeborn | 1 | ItemsCrafted (cumulative) | 1 | 100 gold |
| `ten_crafts` | Artisan | 2 | ItemsCrafted (cumulative) | 10 | 500 gold |
| `first_recycle` | Nothing Wasted | 1 | ItemsRecycled (cumulative) | 1 | 50 gold |

#### Mastery (6 achievements)

| ID | Name | Tier | Trigger | Threshold | Reward |
|----|------|------|---------|-----------|--------|
| `warrior_100` | Blade Saint | 3 | LevelReached + ClassIs(Warrior) | 100 | UniqueItem: `warriors_mark` |
| `ranger_100` | Deadshot | 3 | LevelReached + ClassIs(Ranger) | 100 | UniqueItem: `rangers_mark` |
| `mage_100` | Archmage | 3 | LevelReached + ClassIs(Mage) | 100 | UniqueItem: `mages_mark` |
| `survive_ten_deaths` | Unkillable | 2 | DeathCount (cumulative) | 10 | 500 gold |
| `survive_fifty_deaths` | Cockroach | 3 | DeathCount (cumulative) | 50 | Title: "Undying" |
| `no_death_floor_25` | Flawless Run | 3 | FloorReached + DeathCount == 0 | 25 | Title: "Flawless" |

---

### Achievement Tracking

#### Trigger Types and Signal Mapping

Each trigger type maps to an existing EventBus signal or GameState change. Counters are incremented when the mapped event fires, and then checked against thresholds.

| TriggerType | Source Signal / Check | Counter Stored In |
|-------------|----------------------|-------------------|
| `EnemyDefeated` | `EventBus.EnemyDefeated` | `stats.total_kills` |
| `FloorWiped` | `EventBus.FloorWiped` (all enemies on floor killed) | `stats.total_floor_wipes` |
| `BossDefeated` | `EventBus.BossDefeated` | `stats.total_boss_kills` |
| `FloorClearedNoHit` | `EventBus.FloorWiped` + `stats.damage_taken_this_floor == 0` | Check on event, no persistent counter |
| `FloorReached` | `GameState.FloorNumber` change | `character.deepest_floor` (already saved) |
| `FloorsInSession` | Floor transition counter, reset on game load | Transient session counter (not saved) |
| `TownReturns` | Town scene loaded | `stats.total_town_returns` |
| `LevelReached` | `GameState.Level` change | `character.level` (already saved) |
| `StatPointAllocated` | Stat allocation event | `stats.total_stat_points_spent` |
| `SkillLevelReached` | Skill level-up event | Check highest level across all skills |
| `GoldEarned` | Gold awarded (kills, quests, sales) | `stats.total_gold_earned` |
| `ItemsPurchased` | Shop buy event | `stats.total_items_purchased` |
| `ItemsCrafted` | Blacksmith craft event | `stats.total_items_crafted` |
| `ItemsRecycled` | Blacksmith recycle event | `stats.total_items_recycled` |
| `DeathCount` | Player death event | `stats.total_deaths` |

#### Evaluation Flow

```
Event fires (e.g., EnemyDefeated)
  -> AchievementManager receives signal
  -> Increment relevant counter in AchievementState
  -> For each locked achievement with matching trigger_type:
       if progress >= trigger_threshold:
         -> Mark unlocked = true
         -> Set unlock_timestamp = now (ISO-8601)
         -> Grant reward (gold added to GameState, title stored, or item added to inventory)
         -> Emit AchievementUnlocked signal with achievement ID
         -> Trigger auto-save
```

#### New Signals Required

| Signal | Emitted By | Payload |
|--------|-----------|---------|
| `AchievementUnlocked` | AchievementManager | `string achievementId` |
| `FloorWiped` | Dungeon (already exists) | — |
| `BossDefeated` | Enemy (on boss death) | `int floorNumber` |

#### New Persistent Counters

These counters live in the save file under a new `stats` block and are never decremented.

| Counter | Type | Default |
|---------|------|---------|
| `total_kills` | int | 0 |
| `total_floor_wipes` | int | 0 |
| `total_boss_kills` | int | 0 |
| `total_town_returns` | int | 0 |
| `total_stat_points_spent` | int | 0 |
| `total_gold_earned` | int | 0 |
| `total_items_purchased` | int | 0 |
| `total_items_crafted` | int | 0 |
| `total_items_recycled` | int | 0 |
| `total_deaths` | int | 0 |
| `damage_taken_this_floor` | int | 0 (reset on floor transition) |

---

### Achievement Rewards

#### Gold Rewards

Base gold values are listed per achievement in the tables above. Gold is added directly to `GameState.Gold` on unlock.

#### Titles

Titles are cosmetic strings displayed after the character name in the HUD and pause menu. Format: `"CharacterName, the {Title}"`. Only one title is active at a time. The player can change their active title in the Fated Ledger UI.

Earned titles are stored as a string array in the save file. The active title is a separate field.

| Title | Source Achievement |
|-------|-------------------|
| "Exterminator" | Ten Thousand Strong |
| "The Thorough" | Absolute Zero |
| "Untouchable" | Untouchable |
| "Abyssal" | Centurion of Depth |
| "Centurion" | Centurion |
| "Golden" | Midas |
| "Undying" | Cockroach |
| "Flawless" | Flawless Run |

#### Unique Items

Three class-specific mark items and one universal item are awarded by achievements. These are the only way to obtain these items — they cannot drop, be purchased, or be crafted.

| Item ID | Source Achievement | Effect (summary) |
|---------|--------------------|-----------------|
| `warriors_mark` | Blade Saint | Accessory. +5% melee damage, +3% block chance. Cosmetic glow on character. |
| `rangers_mark` | Deadshot | Accessory. +5% projectile damage, +3% evasion chance. Cosmetic glow on character. |
| `mages_mark` | Archmage | Accessory. +5% spell damage, +3% mana regen. Cosmetic glow on character. |
| `ledger_lantern` | Beyond the Pale | Accessory. +10% XP from kills. Faint light aura around character. |

Full item stat definitions are deferred to the unique items spec (SYS-08). The values above are targets for that spec to implement.

---

### UI — The Fated Ledger Panel

#### Access Points

| Location | How to Open |
|----------|-------------|
| **Pause Menu** | "Fated Ledger" button in the pause menu list |
| **Guild Master NPC** | "View Ledger" service option in the Guild Master's NPC panel |

Both open the same panel. The Guild Master route is the lore-canonical way; the pause menu route is for convenience.

#### Panel Layout

```
┌─────────────────────────────────────────────┐
│  THE FATED LEDGER           [X Close]       │
│  ─────────────────────────────────────────  │
│  [Combat] [Exploration] [Progression]       │
│  [Economy] [Mastery]                        │
│  ─────────────────────────────────────────  │
│  Progress: 12 / 42  (28%)                   │
│  ─────────────────────────────────────────  │
│                                             │
│  [Bronze] First Blood              [done]   │
│    "Drew blood for the first time."         │
│    Reward: 50 gold (claimed)                │
│                                             │
│  [Silver] A Thousand Cuts        734/1000   │
│    "Defeat 1,000 enemies."                  │
│    Reward: 1,000 gold                       │
│                                             │
│  [Gold]  ??? (hidden)               ???     │
│    "???"                                    │
│    Reward: ???                              │
│                                             │
│  ─────────────────────────────────────────  │
│  Active Title: [dropdown] "the Centurion"   │
└─────────────────────────────────────────────┘
```

#### UI Rules

- Category tabs filter the list. "All" is not a tab — the default view is Combat.
- Each entry shows: tier icon (bronze/silver/gold circle), name, progress bar or "done" checkmark.
- Hidden achievements (where `hidden == true`) show "???" for name, description, and reward until unlocked.
- Unlocked entries show the unlock date beneath the description.
- A progress bar fills based on `progress / trigger_threshold` for cumulative achievements.
- Non-cumulative achievements (e.g., FloorReached) show current value vs threshold.
- The title dropdown at the bottom only appears if the player has earned at least one title. It lists all earned titles plus "None".
- The panel uses the global theme colors and fonts defined in `GlobalTheme.cs` / `UiTheme.cs`.

#### Notification Toast

When an achievement unlocks during gameplay:

1. A toast notification appears using the existing `Toast.cs` system.
2. Toast type: `success` (green border).
3. Toast text: `"Ledger Entry: {achievement name}"`.
4. Toast duration: 4 seconds (longer than standard 3s toasts to ensure the player sees it).
5. If multiple achievements unlock simultaneously (e.g., kill count crosses 100 and 1,000 at once from a batch), toasts stack with 0.5s delay between them.
6. No popup or modal — the toast is non-intrusive. The player can review details in the Ledger later.

---

### Save / Load Format

Achievement data is stored per save slot, inside the existing save JSON structure. Two new top-level keys are added.

```json
{
    "version": 3,
    "character": { ... },
    "skills": { ... },
    "inventory": { ... },
    "dungeon": { ... },
    "stats": {
        "total_kills": 734,
        "total_floor_wipes": 8,
        "total_boss_kills": 2,
        "total_town_returns": 15,
        "total_stat_points_spent": 87,
        "total_gold_earned": 12450,
        "total_items_purchased": 23,
        "total_items_crafted": 4,
        "total_items_recycled": 7,
        "total_deaths": 6,
        "damage_taken_this_floor": 0
    },
    "achievements": {
        "unlocked": {
            "first_blood": "2026-04-11T14:30:00Z",
            "century_slayer": "2026-04-11T16:45:00Z",
            "first_descent": "2026-04-11T14:32:00Z"
        },
        "active_title": "Centurion",
        "earned_titles": ["Centurion"]
    }
}
```

#### Save Format Notes

- `achievements.unlocked` is a dictionary mapping achievement ID to unlock timestamp. Only unlocked achievements appear.
- Progress values are not stored separately — they are derived from `stats` counters and `character` fields at load time. This avoids data duplication and ensures counters stay consistent.
- `damage_taken_this_floor` is transient — reset to 0 on floor transition. Saved only to survive mid-floor auto-saves.
- Adding `stats` and `achievements` blocks bumps the save version from 2 to 3. The migration function initializes both blocks with defaults (all counters at 0, no unlocked achievements).
- On load, the AchievementManager re-evaluates all thresholds against current stats. This catches any achievements that should have been unlocked but were missed (e.g., due to a version upgrade adding new achievements to an existing save).

---

## Acceptance Criteria

1. All 42 achievements are defined as `AchievementDef` records and loadable at startup.
2. Each achievement's progress updates correctly when its mapped signal fires.
3. Unlocking an achievement grants the specified reward (gold, title, or item) exactly once.
4. The Fated Ledger panel opens from both the pause menu and the Guild Master NPC.
5. Category tabs filter achievements correctly.
6. Hidden achievements display "???" until unlocked.
7. Progress bars show `progress / threshold` for cumulative achievements.
8. Achievement state persists across save/load cycles.
9. Loading a v2 save migrates cleanly to v3 with empty stats and achievements.
10. Re-evaluation on load catches retroactively earned achievements (e.g., a player already at level 50 when the system is added).
11. Toast notifications appear on unlock using the existing Toast system.
12. Simultaneous unlocks produce stacked toasts with 0.5s delay.
13. Title selector in the Ledger UI changes the active title, which displays in the HUD.
14. Unique item rewards are added to the player's backpack (or bank if backpack is full).
15. No achievement can be unlocked more than once per save slot.

## Implementation Notes

- `AchievementManager` should be an autoload singleton that subscribes to EventBus signals and GameState changes in `_Ready()`.
- Achievement definitions should be static data, not loaded from files. A `Dictionary<string, AchievementDef>` initialized in code is sufficient — 42 entries do not warrant a JSON data file.
- The `stats` block counters must be incremented *before* the achievement check runs, so the check sees the updated value.
- `FloorsInSession` is the only transient counter. It resets when the game loads (not on death — death does not end a session). It does not need to be saved.
- The Fated Ledger panel is a `Control` node added to the pause menu scene. It should be a separate scene instanced into the pause menu to keep the scene tree clean.
- For the `FloorClearedNoHit` achievement, `damage_taken_this_floor` is reset to 0 in the floor transition handler, before enemies spawn. Any `PlayerDamaged` signal increments it. When `FloorWiped` fires, check if it is still 0.
- Save version migration: the v2 -> v3 migration adds `stats` (all zeros) and `achievements` (empty unlocked dict, empty title, empty earned_titles array). Existing v2 data is untouched.

## Open Questions

None. Spec is **LOCKED**.

# Town Hub

## Summary

A compact, walkable safe zone — a frontier settlement founded beside a newly discovered infinite dungeon. The player is the first resident adventurer, and the NPCs are early settlers who came to support the expedition. No combat in town. All NPCs available from game start. Inspired by Diablo 1's Tristram — purely functional, minimal walking.

## Current State

**Spec status: LOCKED (layout and scene flow).** Individual NPC specs (SPEC-01b-f) are separate tickets.

Not yet implemented. The prototype only has the dungeon scene.

## Lore

The town is a **frontier settlement** — recently founded as an "adventurers' town" beside a newly discovered cave mouth that leads into an infinite dungeon. Nobody knows how deep it goes, what made it, or why it exists. The cave is natural, not man-made — something ancient and possibly alive.

### The Guild Branch

This town is the site of a **newly-opened branch of The Guild**. The player character is the branch's **Guildmaster** — a young but capable adventurer assigned to plant a flag in the frontier and lead the exploration effort. The NPCs in town are **senior guild personnel and allied specialists** sent to support the Guildmaster. They are pioneers in their fields — a master smith, a master wizard, an experienced chief, a skilled operational assistant — all high-ranking in their own right, but supporting the Guildmaster for this expedition.

NPCs address the player as **"{Class} Guildmaster"** — e.g., "Warrior Guildmaster", "Ranger Guildmaster", "Mage Guildmaster". This is the player's title in-world.

**Naming convention (Maoyuu homage):** all NPCs are referred to by title/role, not personal names. Full titles exist in the lore (e.g., "Old Master Blacksmith") but the in-game label shows only the short form ("Blacksmith"). See the NPC roster below.

### Key Lore Beats

- The dungeon was discovered recently. No one has explored beyond the first few floors.
- The town exists solely to support dungeon exploration. There is no other reason for it.
- NPCs are **seasoned guild personnel**, not random merchants. Each is a master in their field.
- The player character is the **Guildmaster** — the designated leader of this branch. The other NPCs technically outrank the player in experience but operate under the Guildmaster's authority by guild protocol.
- The cave entrance is at the edge of town — a dark, natural opening in a rocky hillside.
- As the player pushes deeper, the town may grow and attract new settlers (future feature — not in MVP).
- **Scope note:** the Guild as a larger organization (other branches, leadership hierarchy, politics) is out of scope for this game. The "Guild" framing is primarily **flavor text**. Treat the Guild as "the organization that sent this group here" — we won't visit other branches or see Guild headquarters.

## Design

### Town as a Scene

Town is a separate scene that the player transitions to from the dungeon. It functions as a safe lobby:
- No enemies, no combat
- Walk around freely
- Interact with NPCs by walking up to them
- Layout is static at launch, but designed to support future expansion as the player progresses deeper into the dungeon

### Layout

Compact walkable area. All 4 NPCs are within a few steps of center. The player should be able to reach any NPC in under 2 seconds of walking.

**Conceptual layout (top-down):**

```
        ┌─────────────────────────────┐
        │      [Village Chief]        │
        │                             │
        │  [Blacksmith]   [Guild Maid]│
        │         (center)            │
        │                 [Teleporter]│
        │                             │
        │     [Dungeon Entrance]      │
        └─────────────────────────────┘
```

- **Center:** Open area, player spawns here when entering town
- **NPCs:** Arranged in a loose ring around center, all facing inward
- **Dungeon Entrance:** At the bottom/south edge of town — a natural cave mouth in a rocky hillside, recently discovered, not man-made. Walk into it to enter the dungeon.
- **Total size:** ~2x the viewport. Small enough to see most NPCs on screen at once.

**Reduced NPC count:** with Store and Bank merged under the Guild Maid, the town now has **4 interactive NPCs** (was 5): Guild Maid, Blacksmith, Village Chief, Teleporter. The "Banker" and "Item Shop" roles are retired — their functions moved into the Guild Maid's window.

### NPC List

All NPCs use title-only in-game labels (short form shown in the label column). Full titles documented for lore reference.

| In-game Label | Full Title | Role | Function |
|---------------|-----------|------|----------|
| Guild Maid | Guild Maid Assistant | The Guildmaster's primary operational assistant. A young woman in maid attire (glasses, long skirt, carries a logbook). Runs day-to-day branch operations so the Guildmaster can focus on the dungeon. | Guild window: Store (basic consumables/materials/ammo), Bank (safe storage, slot expansion, gold pocket), Transfer (move items/gold between bank ↔ backpack) |
| Blacksmith | Old Master Blacksmith | Veteran smith sent to support a guildmaster-level adventurer. Knows every recipe, just needs materials. | Craft affixes onto base items, recycle unwanted gear, **backpack expansion** (+5 slots per upgrade), **Workshop tab** for manufacturing high-tier materials |
| Village Chief | Old Village Chief | Experienced settlement leader. Knows the land, handles local relations, issues work contracts for the guild. | Offer quests (kill, boss, floor-clear, depth-push) — procedurally generated, always available |
| Teleporter | Old Master Wizard | Scholarly mage drawn by the dungeon's magical signature. Maintains the teleport network to previously visited floors. | Travel to any previously visited dungeon floor (free, no cost) |

### Interaction Model

- **Walk-up interaction:** Player moves near an NPC (within ~32px radius) → interaction panel appears
- No click-to-talk — proximity triggers the UI
- Panel shows the NPC's services (shop inventory, craft options, etc.)
- Walking away dismisses the panel automatically
- Only one NPC panel can be open at a time

### NPC Personality

NPCs are **functional with brief flavor text**. They are not dialogue-heavy characters — but they are distinct people who chose to be here.
- Each NPC has a short greeting line (1-2 sentences) that plays on interaction
- Greetings reflect their frontier pioneer personalities — why they came, what they need from the player
- Greeting may reference player progress (e.g., Blacksmith comments on your floor depth)
- No dialogue trees, no branching conversations, no quests from NPCs (Guild handles quests)

### Town Access

| Route | How |
|-------|-----|
| Dungeon → Town | Walk to floor 1 exit staircase OR death screen → "Return to Town" |
| Town → Dungeon (floor 1) | Walk to dungeon entrance in town |
| Town → Dungeon (deeper) | Use Level Teleporter NPC → select previously visited floor |

### Scene Flow

```
Game Start
  → Character Select (pick save slot)
  → Town (spawn at center)
  → Player prepares (shop, craft, bank)
  → Player walks to dungeon entrance OR uses Teleporter
  → Dungeon (floor N)
  → ... gameplay ...
  → Death → "Return to Town" → Town
  → OR → Walk to floor 1 exit → Town
  → Repeat
```

### Bank Access

The bank is **town-only**. Players cannot access bank storage from inside the dungeon. This creates tension: bring valuable items into the dungeon (risk losing them on death) or bank them safely (but can't use them).

---

## NPC Specs

### Guild Maid

Runs the Guild window — the merged Store + Bank + Transfer interface. See [guild-window.md](../ui/guild-window.md) for full UI spec.

**Services:**

| Service | Effect | Cost |
|---------|--------|------|
| Store (basic consumables) | Health/Mana potions, Sacrificial Idol, basic ammo | Fixed prices |
| Store (basic materials) | Bottles, shafts, feathers, tanning oil, thread, string, flux powder | Fixed prices |
| Bank (safe storage) | Deposit/withdraw items, safe on death | Free |
| Bank Expansion | +1 slot per upgrade | `50 × N gold` (N = expansion number) |
| Bank gold pocket | Separate gold balance, safe on death | Free |
| Transfer tab | Move items and gold between bank ↔ backpack | Free |

**Store inventory (fixed, always in stock):**

| Item | Cost | Category |
|------|------|----------|
| Small Health Potion | 50 gold | Consumable |
| Small Mana Potion | 50 gold | Consumable |
| Sacrificial Idol | 200 gold | Consumable (free "Save Both" on death) |
| Glass Bottle | 5 gold | Material (basic) |
| Arrow Shaft | 3 gold | Material (basic) |
| Feather | 2 gold | Material (basic) |
| Tanning Oil | 10 gold | Material (basic) |
| Thread | 2 gold | Material (basic) |
| String | 3 gold | Material (basic) |
| Flux Powder | 8 gold | Material (basic) |
| Iron Arrows | 1 gold | Ammo (basic) |
| Iron Bolts | 1 gold | Ammo (basic) |

*Prices are tunable during playtesting. Relative ratios (consumables cheap, mats cheaper, ammo cheapest per-unit) are locked.*

### Blacksmith (Old Master Blacksmith)

Adds affixes to base items, recycles gear, expands the backpack, and manufactures high-tier crafting materials. See [items.md](../inventory/items.md) for affix crafting rules and [backpack.md](../inventory/backpack.md) for expansion costs.

**Services (Forge tab):**

| Service | Effect | Cost |
|---------|--------|------|
| Add Affix | Pick exact prefix/suffix, apply to item | Materials + gold (scales with affix tier) |
| Recycle Gear | Break down equipment into materials | Free (materials returned to player) |
| Backpack Expansion | +5 backpack slots per upgrade | `200 × N² gold + crafting materials` (see [backpack.md](../inventory/backpack.md)) |

**Services (Workshop tab):**

- Manufacture **high-tier crafting materials** from mid-tier drops + rare ingredients. Recipes are always available (no unlock gate); player just needs the inputs.
- Workshop is a second tab in the Blacksmith window, alongside the Forge tab.

The Blacksmith's affix list is gated by the player's deepest floor.

### Village Chief (Old Village Chief)

Offers radiant quests — procedurally generated, always available. Completing quests rewards gold and materials.

**Quest Types:**

| Type | Example | Reward |
|------|---------|--------|
| Kill quest | "Slay 20 enemies on floor 15" | Gold + common materials |
| Boss quest | "Defeat the Zone 3 boss" | Gold + rare materials |
| Clear floor | "Clear all enemies on floor 22" | Gold + bonus XP |
| Depth push | "Reach floor 31 for the first time" | Gold + rare materials |

**Rules:**
- 3 quests available at a time, refreshed on completion
- Quests scale to player's current deepest floor
- No time limits — complete at your own pace
- No penalty for abandoning a quest

### Teleporter (Old Master Wizard)

Instantly teleport to any previously visited dungeon floor. **Free, no cost.**

**Rules:**
- Shows a list of all floors the player has visited (up to deepest floor reached)
- Teleports directly to the floor entrance safe spot
- Floor layout is pulled from cache if available (max 10 cached), otherwise regenerated with a new seed
- Cannot teleport to floors deeper than the player's deepest visited floor

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Visual layout (map vs menu) | Compact walkable map. All NPCs within a few steps. |
| Bank access from dungeon? | No — town-only. Encourages return trips and risk/reward decisions. |
| NPC dialogue/personality? | Minimal — functional with brief flavor text. No dialogue trees. |
| Blacksmith risky upgrades? | Replaced by deterministic affix system. See [items.md](../inventory/items.md). |
| Social elements? | None. Single-player game. |
| Town growth? | Static at launch (all NPCs from start). Future feature: town expands as player reaches deeper floors, attracting new settlers. Not in MVP scope. |
| Town well? | Decorative only. The well asset (`assets/tiles/town/well.png`) is a visual landmark — no gameplay interaction, no HP restore, no wishing mechanic. |
| Item Shop class filtering? | The Item Shop filters displayed items by relevance to the player's class (affinity-matched items shown first). Any item can be purchased and equipped — there are no class restrictions. Scrolls show all-class spells plus the player's class spells. |

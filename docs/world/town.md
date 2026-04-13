# Town Hub

## Summary

A compact, walkable safe zone — a frontier settlement founded beside a newly discovered infinite dungeon. The player is the first resident adventurer, and the NPCs are early settlers who came to support the expedition. No combat in town. All NPCs available from game start. Inspired by Diablo 1's Tristram — purely functional, minimal walking.

## Current State

**Spec status: LOCKED (layout and scene flow).** Individual NPC specs (SPEC-01b-f) are separate tickets.

Not yet implemented. The prototype only has the dungeon scene.

## Lore

The town is a **frontier settlement** — recently founded as an "adventurers' town" beside a newly discovered cave mouth that leads into an infinite dungeon. Nobody knows how deep it goes, what made it, or why it exists. The cave is natural, not man-made — something ancient and possibly alive.

The player is the **first resident adventurer**, assigned to delve into the dungeon, establish a foothold, and study what lies below. Every NPC in town chose to be here. They left established lives to set up shop on the frontier, drawn by opportunity, curiosity, or duty. The buildings are freshly constructed, the roads are dirt, and the settlement smells of sawdust and ambition.

**Key lore beats:**
- The dungeon was discovered recently. No one has explored beyond the first few floors.
- The town exists solely to support dungeon exploration. There is no other reason for it.
- NPCs are pioneers, not established merchants. They are staking their livelihoods on the player's success.
- The cave entrance is at the edge of town — a dark, natural opening in a rocky hillside.
- As the player pushes deeper, the town may grow and attract new settlers (future feature — not in MVP).

## Design

### Town as a Scene

Town is a separate scene that the player transitions to from the dungeon. It functions as a safe lobby:
- No enemies, no combat
- Walk around freely
- Interact with NPCs by walking up to them
- Layout is static at launch, but designed to support future expansion as the player progresses deeper into the dungeon

### Layout

Compact walkable area. All 5 NPCs are within a few steps of center. The player should be able to reach any NPC in under 2 seconds of walking.

**Conceptual layout (top-down):**

```
        ┌─────────────────────────────┐
        │         [Banker]            │
        │                             │
        │  [Blacksmith]   [Item Shop] │
        │         (center)            │
        │  [Guild]   [Teleporter]     │
        │                             │
        │     [Dungeon Entrance]      │
        └─────────────────────────────┘
```

- **Center:** Open area, player spawns here when entering town
- **NPCs:** Arranged in a loose ring around center, all facing inward
- **Dungeon Entrance:** At the bottom/south edge of town — a natural cave mouth in a rocky hillside, recently discovered, not man-made. Walk into it to enter the dungeon.
- **Total size:** ~2x the viewport. Small enough to see most NPCs on screen at once.

### NPC List

| NPC | Role | Function | Spec Ticket |
|-----|------|----------|-------------|
| Item Shop | Eager frontier merchant, came to supply the expedition | Buy consumables (Sacrificial Idol, potions, spell scrolls) | SPEC-01b |
| Blacksmith | Rugged smith who hauled a forge to the frontier for monster materials | Craft affixes onto base items, recycle unwanted gear for materials | SPEC-01c |
| Adventure Guild | Veteran explorer who organized the entire expedition | View quests, achievements, or challenges | SPEC-01d |
| Level Teleporter | Scholarly mage drawn by the dungeon's magical signature | Travel to previously visited dungeon floors | SPEC-01e |
| Banker | Practical security expert — frontier towns need vaults | Access bank storage (safe, permanent, town-only) | SPEC-01f |

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

### Item Shop (SPEC-01b)

Sells consumables and backpack expansions. Inventory is always available — no stock limits.

**Inventory:**

| Item | Cost | Effect |
|------|------|--------|
| Health Potion | 50 gold | Restore 30% max HP instantly |
| Mana Potion | 50 gold | Restore 30% max mana instantly |
| Sacrificial Idol | 200 gold | Negates backpack item loss on death (consumed on death) |
| Spell Scroll (varies) | varies | One-use spell cast. Repeated use teaches the spell permanently (Mage). |
| Backpack Expansion | `300 * N^2` | +5 backpack slots (N = expansion number) |

Scroll availability scales with player's deepest floor reached (deeper = more powerful scrolls available).

### Blacksmith (SPEC-01c)

Adds affixes to base items and recycles unwanted gear. See [items.md](../inventory/items.md) for full crafting rules.

**Services:**

| Service | Effect | Cost |
|---------|--------|------|
| Add Affix | Pick exact prefix or suffix, apply to item | Materials + gold (scales with affix tier) |
| Recycle Gear | Break down equipment into materials | Free (materials returned to player) |

The Blacksmith's available affix list is gated by the player's deepest floor (determines max affix tier accessible).

### Adventure Guild (SPEC-01d)

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

### Level Teleporter (SPEC-01e)

Instantly teleport to any previously visited dungeon floor. **Free, no cost.**

**Rules:**
- Shows a list of all floors the player has visited (up to deepest floor reached)
- Teleports directly to the floor entrance safe spot
- Floor layout is pulled from cache if available (max 10 cached), otherwise regenerated with a new seed
- Cannot teleport to floors deeper than the player's deepest visited floor

### Banker (SPEC-01f)

Access bank storage and purchase bank expansions. See [bank.md](../inventory/bank.md) for full bank rules.

**Services:**

| Service | Effect | Cost |
|---------|--------|------|
| Deposit/Withdraw | Move items between backpack and bank | Free |
| Bank Expansion | +10 bank slots | `500 * N^2` (N = expansion number) |

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

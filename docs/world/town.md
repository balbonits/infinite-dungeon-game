# Town Hub

## Summary

A compact, static, walkable safe zone where players interact with NPCs, manage inventory, and prepare for dungeon runs. No combat in town. All NPCs available from game start. Inspired by Diablo 1's Tristram — purely functional, minimal walking.

## Current State

**Spec status: LOCKED (layout and scene flow).** Individual NPC specs (SPEC-01b-f) are separate tickets.

Not yet implemented. The prototype only has the dungeon scene.

## Design

### Town as a Scene

Town is a separate scene that the player transitions to from the dungeon. It functions as a safe lobby:
- No enemies, no combat
- Walk around freely
- Interact with NPCs by walking up to them
- Static layout — town does not change or grow over time

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
- **Dungeon Entrance:** At the bottom/south edge of town — walk into it to enter the dungeon
- **Total size:** ~2x the viewport. Small enough to see most NPCs on screen at once.

### NPC List

| NPC | Function | Spec Ticket |
|-----|----------|-------------|
| Item Shop | Buy consumables (Sacrificial Idol, potions, spell scrolls) | SPEC-01b |
| Blacksmith | Craft affixes onto base items, recycle unwanted gear for materials | SPEC-01c |
| Adventure Guild | View quests, achievements, or challenges | SPEC-01d |
| Level Teleporter | Travel to previously visited dungeon floors | SPEC-01e |
| Banker | Access bank storage (safe, permanent, town-only) | SPEC-01f |

### Interaction Model

- **Walk-up interaction:** Player moves near an NPC (within ~32px radius) → interaction panel appears
- No click-to-talk — proximity triggers the UI
- Panel shows the NPC's services (shop inventory, craft options, etc.)
- Walking away dismisses the panel automatically
- Only one NPC panel can be open at a time

### NPC Personality

NPCs are **functional with brief flavor text**. They are not dialogue-heavy characters.
- Each NPC has a short greeting line (1-2 sentences) that plays on interaction
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
| Town growth? | No. Static layout. All NPCs from start. |

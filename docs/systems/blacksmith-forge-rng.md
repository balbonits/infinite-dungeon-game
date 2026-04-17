# Blacksmith Forge RNG System (FORGE-01)

## Summary

The endgame progression path for equipment. The player brings a high-tier base item and a large material investment to the Blacksmith; the Forge consumes both and outputs a named "unique" item with a hand-designed affix package. RNG decides *which* unique you get from the eligible pool; the stats themselves are fixed per-unique. Replaces the traditional "unique drops from bosses" model — uniques are crafted, not looted, because the player is the **first** dungeon explorer in this frontier settlement and the Blacksmith is learning to forge as he goes.

## Current State

**Spec status: DRAFT.** Tracked as FORGE-01 in [dev-tracker.md](../dev-tracker.md). Wires into the existing `BlacksmithWindow` (Craft + Recycle tabs) as a 3rd tab. Per the ITEM-01 catalog decision, uniques do NOT live in [item-catalog.md](../inventory/item-catalog.md) — they get a dedicated `UniqueItemDatabase` registry, authored here.

Depends on:
- [item-catalog.md](../inventory/item-catalog.md) — 259 base items, 5-tier ladder (LOCKED).
- [items.md](../inventory/items.md) — `ItemDef`, affix model (LOCKED).
- [depth-gear-tiers.md](depth-gear-tiers.md) — BaseQuality ladder + affix slot counts (LOCKED).
- [combat-equipment-integration.md](combat-equipment-integration.md) — how unique's stats flow into combat (sibling spec).
- [classes.md](classes.md) — for class-themed uniques.

## Design

### 1. The player fantasy

The forge is the moment where a Mega Chopper stops being "the Tier 5 axe I found on floor 120" and becomes "**Skullcleaver**, the named relic of my run." The player remembers where they got the base, what they sacrificed to craft it, which unique it rolled into. Uniques are **memorable** — named, flavored, irreplaceable. The catalog is the periodic table; uniques are the molecules.

The Blacksmith's voice: gruff, forge-honest, surprised. "Never made one like that before. Might not work. Show me what you've got." He's the first smith on this frontier, and every unique is partly an experiment.

### 2. The forge flow

1. Player opens Blacksmith Window → Forge tab (3rd tab, after Craft and Recycle).
2. UI prompts: "Choose a base item from your backpack."
3. Player selects a base item. UI shows:
   - The base's tier (determines the unique pool).
   - The required material cost (computed from tier — see §5).
   - Whether the player has enough materials + gold.
   - **Preview of the eligible unique pool** — just names + tier icons, not stats. ("You may forge one of 7 Tier 3 uniques.")
4. Player confirms. Cost is deducted. Base is consumed.
5. Forge rolls one unique uniformly from the eligible pool.
6. Output unique goes directly into backpack (or equips if slot is empty? — no, always backpack, let the player choose).
7. Blacksmith delivers a one-line reaction based on which unique rolled.

**Failure cases:**
- Insufficient materials / gold → button disabled, tooltip shows the gap.
- Backpack full → refuse, "Clear a slot first" toast.
- No base selected → button disabled.

### 3. Decisions locked by the user brief

- **Input:** 1 base item + large material cost. Base is **consumed**.
- **Output:** a named forged item with a hand-designed affix package.
- **Uniques live in their own registry** (per ITEM-01 deferral): `UniqueItemDatabase`, authored in code from this spec's §10 seed content.
- Gated by floor depth AND base item tier. High-tier base required for high-tier unique.
- Blacksmith NPC. Forge tab joins existing Craft + Recycle tabs.

### 4. Decisions made in this spec

| Question | Decision | Reasoning |
|---|---|---|
| One-shot or re-rollable? | **One-shot.** Base is consumed into the unique. | Encourages commitment. A re-rollable forge turns the system into a slot machine — the player burns materials until they get what they want. One-shot makes the base choice meaningful and every unique feel earned. Re-rolling invites addiction-design dynamics the game shouldn't have. |
| Storage: seed / template / procedural? | **Named templates with fixed affix lists.** | Uniques are hand-designed content. The RNG is in *which* unique you get from the pool, not in any stat roll. This keeps uniques memorable ("everybody knows Skullcleaver hits for X") and makes balance possible. A seeded-procedural unique is just a rare affixed item, which defeats the point. |
| Roll pool gating | **Per-base-tier pool.** Tier *N* base rolls a Tier *N* unique. | Simple to explain, predictable for the player ("Tier 3 in, Tier 3 out"), lets the design team concentrate authorship effort per tier. No cross-tier bleed. |
| Pool size | **5–10 uniques per tier, growing with depth.** | Tier 1: 3. Tier 2: 4. Tier 3: 5. Tier 4: 6. Tier 5: 10. Low-tier pools are small because the player won't spend many cycles there; endgame (T5) gets the most variety because that's where the player will be farming dozens of runs. |
| Affix stacking on uniques | **REPLACE.** Unique's fixed affix list is the whole package. Base's affixes are discarded. Cannot add affixes to a unique at the Blacksmith's Craft tab. | Uniques distinct from affixed-normals: you're choosing between "Mega Sword with 6 perfect affixes I picked" vs "Skullcleaver with 4 hand-designed thematic affixes." Different build identities. A unique that *also* accepts affixes would collapse into "strictly better affixed item." |
| Gold cost? | **Yes, scales with tier.** | Materials are the primary cost; gold is a secondary sink. Matches how Craft tab works — materials + gold per affix. |
| Floor-depth gating beyond base tier? | **None.** Base tier IS the depth gate — you can only drop Tier 5 bases on floor 51+ per [item-generation.md](item-generation.md) and [depth-gear-tiers.md](depth-gear-tiers.md). A player carrying a T5 base has already proven they reached the depth. No additional gate needed. |
| Class-affinity on uniques | **Each unique has a fixed `ClassAffinity`.** Matches its thematic class voice. | Same rules as normal items: +25% stat bonus when wielder's class matches. Some uniques are neutral (e.g. universal accessories). |
| Can the forge fail / downgrade? | **No.** Every successful forge produces a unique. | Failure creates frustration without creating depth. The RNG is already in *which* unique rolls. A second layer of "but sometimes you get nothing" is punitive. |

### 5. Cost formula

**Material cost (primary cost, tier-dominant):**

```
primary_material_cost(tier) = 20 × tier²     // Top-Shelf tier material
secondary_material_cost(tier) = 10 × tier²   // Signature species material, player choice
```

`primary_material` is the tier-matched Top-Shelf material (Tier 1 → Iron Ore, Tier 5 → Dragonite Ore). `secondary_material` is **any single signature material** the player has (Bone Dust, Goblin Tooth, etc.) — the forge accepts the thematic tilt the player brings. High-INT players who've been farming Dark Mages bring Arcane Residue; Warrior-leaning players bring Orc Tusks. The signature material doesn't change the roll outcome (pool is per-tier), but it shows up in the unique's flavor text as the "ingredient used."

| Tier | Primary material | Primary cost | Secondary sig cost | Gold cost |
|---|---|---|---|---|
| 1 | Iron Ore | 20 | 10 | 500 |
| 2 | Steel Ingot | 80 | 40 | 2,000 |
| 3 | Mithril Ore | 180 | 90 | 8,000 |
| 4 | Orichalcum Ore | 320 | 160 | 25,000 |
| 5 | Dragonite Ore | 500 | 250 | 80,000 |

Gold cost: `500 × tier^2.5` rounded to a readable number.

**Design check:** at Tier 5, the player needs 500 Dragonite Ore. Per [monster-drops.md](monster-drops.md), a floor-100+ monster drops ~25% material rate biased toward the floor's tier. At a generous 1 ore per kill, that's 500 kills. Running a full floor-100 run nets maybe 50 Dragonite Ore. So a Tier 5 forge is ~10 full deep runs of material grind. That's the intended "ultimate endgame goal" feel from [depth-gear-tiers.md](depth-gear-tiers.md).

Tier 1 uniques are cheap on purpose — they're the tutorial for the forge system, available by floor 10. A player can try the forge early, learn the mechanic, and carry a Tier 1 unique as a fond memento for the rest of their career.

### 6. Data model sketches

```csharp
/// <summary>
/// A hand-designed unique item template. Stored in UniqueItemDatabase, not ItemDatabase.
/// Forge rolls pick a UniqueItemDef uniformly from a per-tier pool;
/// the output is a concrete ItemDef/CraftableItem instance with the fixed affixes pre-applied.
/// </summary>
public record UniqueItemDef
{
    public string Id { get; init; } = "";            // e.g. "unique_mainhand_t5_skullcleaver"
    public string DisplayName { get; init; } = "";   // e.g. "Skullcleaver"
    public EquipSlot Slot { get; init; }             // which base slots can forge into this
    public WeaponArchetype? RequiredArchetype { get; init; }  // Main Hand only — must match base's archetype
    public int Tier { get; init; }                   // 1..5
    public PlayerClass? ClassAffinity { get; init; } // +25% bonus if matched
    public string FlavorText { get; init; } = "";    // shown in tooltip

    // Fixed stat package (replaces base's affixes entirely)
    public int BaseDamage { get; init; }             // Main Hand / Off Hand only
    public int BaseDefense { get; init; }            // Armor slots only
    public int BonusStr { get; init; }
    public int BonusDex { get; init; }
    public int BonusSta { get; init; }
    public int BonusInt { get; init; }
    public int BonusHp { get; init; }
    public float CritChance { get; init; }           // baked-in combat focuses (see COMBAT-01)
    public float HasteMult { get; init; }
    public float DodgeBonus { get; init; }
    public float BlockChance { get; init; }

    // Narrative hooks
    public string[] ForgeReactionLines { get; init; } = new string[0];
    // Blacksmith picks one line randomly when this unique is forged.
}
```

```csharp
public static class UniqueItemDatabase
{
    // Per-tier pools keyed by (slot, tier). For Main Hand, filtered further by archetype.
    private static readonly Dictionary<(EquipSlot, int), List<UniqueItemDef>> _pools = ...;

    public static IReadOnlyList<UniqueItemDef> GetPool(
        EquipSlot slot, int tier, WeaponArchetype? archetype = null)
    {
        if (!_pools.TryGetValue((slot, tier), out var list)) return Array.Empty<UniqueItemDef>();
        if (archetype == null) return list;
        return list.Where(u => u.RequiredArchetype == null
                            || u.RequiredArchetype == archetype).ToList();
    }
}
```

```csharp
public static class ForgeRng
{
    public static ForgeResult TryForge(
        CraftableItem baseItem,
        string signatureMaterialId,
        Inventory backpack,
        Random rng)
    {
        // 1. Validate costs (primary material, signature material, gold)
        // 2. Look up per-tier pool, filtered by archetype for Main Hand
        // 3. Return failure if pool is empty (shouldn't happen in content-complete state)
        // 4. Pick uniform random UniqueItemDef from pool
        // 5. Deduct materials + gold; remove base from backpack
        // 6. Construct the unique as a CraftableItem with the fixed package
        // 7. Add unique to backpack
        // 8. Return ForgeResult { Success, ForgedUnique, BlacksmithReactionLine }
    }
}

public record ForgeResult(
    bool Success,
    UniqueItemDef? ForgedUnique,
    string? FailureReason,
    string? BlacksmithReactionLine);
```

### 7. Interaction with Craft and Recycle tabs

- **Craft tab:** disabled for uniques. A unique's affix list is fixed; the Craft tab's `CanApplyAffix` check should return false for any item with the `IsUnique` flag. (Add `bool IsUnique { get; init; }` to `ItemDef`, default false.)
- **Recycle tab:** uniques CAN be recycled (player's choice). Recycle yield scales with tier like normal items but adds a flat bonus: `recycle_gold(unique) = normal_recycle(item) + 200 × tier`. Recycling a unique is a permanent loss — it will not re-roll into the same unique. The player is trading a decision for materials.

### 8. Save / load

- `SavedEquipment` and `CraftableItem` serialization gain an optional `UniqueId` field pointing at the `UniqueItemDef.Id`. On restore, the item's stats are re-populated from `UniqueItemDatabase` — **stats are NEVER serialized**. If a unique is ever rebalanced in a patch, the player's existing copy inherits the new values. This is a deliberate choice: the unique's identity is stable (the name, the fantasy) while the numbers remain tunable.
- Uniques that are removed from the database in a future patch: fallback to the original base item (via `UniqueItemDef.FallbackBaseId`, optional field, defaults to a tier-matched generic). No save-file corruption; the item simply reverts.

### 9. UI — Forge tab layout

```
┌──────────────────────────────────────────────┐
│  [Craft]  [Recycle]  [Forge]                 │
├──────────────────────────────────────────────┤
│  Select a base item:                         │
│  > Mega Sword                 T5, Masterwork │
│    Top-Shelf Longer           T5, Elite      │
│    Apprentice's Vestments     T2, Normal     │
├──────────────────────────────────────────────┤
│  Selected: Mega Sword                        │
│  Base tier: 5                                │
│                                              │
│  Cost:                                       │
│    500 Dragonite Ore     (have 320) ✗        │
│    250 [pick signature]  (pick one)          │
│    80,000 gold           (have 120,000) ✓    │
│                                              │
│  May forge into one of:                      │
│    • Skullcleaver                            │
│    • Sunflare Edge                           │
│    • Unwritten Name (7 others…)              │
│                                              │
│         [ Not enough Dragonite Ore ]         │
└──────────────────────────────────────────────┘
```

Pool preview shows names only — not stats. The player learns which unique does what by forging, not by browsing. This protects the "first time forging X is memorable" feeling.

Signature material selector is a sub-picker (dropdown or sub-list) — any sig material the player owns is selectable. Some signature materials are *rarer* than others (Arcane Residue drops only from Dark Mages), which adds flavor without changing pool outcome.

### 10. Seed content — 28 uniques across 5 tiers

Hand-designed starter set so FORGE-01 ships with a playable pool at every tier. Naming voices mirror [item-catalog.md](../inventory/item-catalog.md): Warrior uniques are short and brash, Ranger uniques are dry and mechanical, Mage uniques are verbose and scholarly, neutral uniques are clean-fantasy.

Numbers shown are **placeholders** per the catalog convention — final balancing lives in `UniqueItemDatabase.cs` implementation. The design locked here is the names, the slots, the class affinities, the thematic flavor, and the rough stat personality ("crit-focused," "haste-stacker," "tanky-but-slow").

#### Tier 1 (3 uniques) — "First Forge" pool

| ID | Name | Slot | Archetype | Affinity | Personality |
|---|---|---|---|---|---|
| `unique_mh_t1_first_bite` | First Bite | Main Hand | Sword | Warrior | Low-tier crit sword; trophy of the first deep run |
| `unique_ring_t1_lucky_band` | Lucky Band | Ring | — | null | Small bonus to all four combat focuses (jack-of-all-trades sampler) |
| `unique_neck_t1_greenhorns_chain` | Greenhorn's Chain | Neck | — | null | +HP + XP bonus; a keepsake for new adventurers |

#### Tier 2 (4 uniques)

| ID | Name | Slot | Archetype | Affinity | Personality |
|---|---|---|---|---|---|
| `unique_mh_t2_chopblock` | Chopblock | Main Hand | Axe | Warrior | High flat damage, -20% attack speed; committed swings |
| `unique_mh_t2_whispershot` | Whispershot | Main Hand | Shortbow | Ranger | DEX focus + crit; stealthy fantasy |
| `unique_mh_t2_apprentice_folly` | The Apprentice's Folly of Unexpected Potency | Main Hand | Wand | Mage | High spell damage, -20% mana pool; glass-cannon starter |
| `unique_body_t2_steady_vest` | Steady Vest | Body | — | Ranger | Dodge boost; unflashy but reliable |

#### Tier 3 (5 uniques)

| ID | Name | Slot | Archetype | Affinity | Personality |
|---|---|---|---|---|---|
| `unique_mh_t3_skullcleaver` | Skullcleaver | Main Hand | Hammer | Warrior | Huge flat damage, low crit, slow; the iconic brute |
| `unique_mh_t3_long_whisper` | Long Whisper | Main Hand | Longbow | Ranger | Range boost + DEX; the sniper archetype |
| `unique_mh_t3_adept_crackling` | The Adept's Staff of Crackling Insight | Main Hand | Staff | Mage | Chain-damage spell feel; INT-heavy |
| `unique_oh_t3_stonehold` | Stonehold | Off Hand | (Shield) | Warrior | Max block chance boost |
| `unique_ring_t3_precisionist` | The Precisionist | Ring | — | null | Pure crit stacker; stack multiple for crit-build |

#### Tier 4 (6 uniques)

| ID | Name | Slot | Archetype | Affinity | Personality |
|---|---|---|---|---|---|
| `unique_mh_t4_super_skullcleaver` | Super Skullcleaver | Main Hand | Hammer | Warrior | Heir to Tier 3 Skullcleaver — bigger, slower, meaner |
| `unique_mh_t4_mean_crank` | Mean Crank | Main Hand | Crossbow | Ranger | High damage crossbow; slow cooldown, guaranteed crit at full mana |
| `unique_mh_t4_masters_imperious` | The Master's Imperious Conduit of Burning Thought | Main Hand | Staff | Mage | Spell damage + mana regen |
| `unique_body_t4_super_armor` | Super Super Armor | Body | — | Warrior | (The Warrior doesn't know he said "Super" twice) Max defense + HP |
| `unique_body_t4_fancy_fancy_vest` | Fancy Fancy Vest | Body | — | Ranger | Dodge + speed; Ranger's flagship mid-late body |
| `unique_neck_t4_orichalcum_sunburst` | Orichalcum Sunburst | Neck | — | null | Balanced offense; +damage + crit |

#### Tier 5 (10 uniques) — the endgame

| ID | Name | Slot | Archetype | Affinity | Personality |
|---|---|---|---|---|---|
| `unique_mh_t5_worldbreaker` | Worldbreaker | Main Hand | Hammer | Warrior | Peak flat damage, AoE on crit |
| `unique_mh_t5_mega_mega` | Mega Mega Sword | Main Hand | Sword | Warrior | (The Warrior's proudest naming moment.) Balanced monster sword |
| `unique_mh_t5_dragonite_chopper` | The Dragonite Chopper Probably | Main Hand | Axe | Warrior | (Warrior unsure if the metal is actually dragonite) High damage, lifesteal on kill |
| `unique_mh_t5_last_word` | Last Word | Main Hand | Longbow | Ranger | Extreme range + crit; long-distance execution |
| `unique_mh_t5_top_shelf_crank` | Top-Shelf Crank Prototype | Main Hand | Crossbow | Ranger | Experimental; massive damage, occasional misfire (small self-damage chance) |
| `unique_mh_t5_quiet_shortie` | Quiet Shortie | Main Hand | Shortbow | Ranger | Stealth-focused; haste-stacker |
| `unique_mh_t5_archmages_absolute` | The Archmage's Absolute Paramount Staff of Supreme Singular Theurgism | Main Hand | Staff | Mage | Peak spell damage, reduced mana costs; the Mage endgame weapon |
| `unique_mh_t5_wand_of_unmaking` | The Wand of Polite Unmaking | Main Hand | Wand | Mage | High crit on spells; single-target burst |
| `unique_ring_t5_dragonite_signet` | Dragonite Signet of Ultimate Precision | Ring | — | null | Caps crit fast when stacked; the crit-build keystone |
| `unique_neck_t5_first_explorer` | Chain of the First Explorer | Neck | — | null | Lore piece — references the player's pioneer role (see [frontier_lore.md](../../.claude/agent-memory/design-lead/project_frontier_lore.md)); balanced stat spread |

**Total: 28 uniques.** Two are slot-specific (Ring and Neck) and class-neutral. Main Hand uniques are gated by archetype — a Warrior with a sword base cannot forge Skullcleaver (which requires Hammer). This lets the design team give each weapon archetype distinct unique identities instead of funneling everything through one "best sword."

User can extend the pool later (adding uniques to `UniqueItemDatabase.cs` is a content-only change, no system work needed).

### 11. Blacksmith reaction lines (flavor)

Each unique ships with 1–3 reaction lines. The Blacksmith delivers one randomly when the forge succeeds. Sample lines seeded for three uniques — the rest are authorship work per unique:

**Skullcleaver:**
- "That one's got a weight to it. Feels wrong. Feels right. Take it."
- "Never seen metal pour like that. Hope you know what you're holding."

**Whispershot:**
- "Didn't make a sound coming out of the quench. Not a good sign, not a bad one."

**The Archmage's Absolute Paramount Staff of Supreme Singular Theurgism:**
- "…you name it. I'm not saying all that."
- "Whatever that is, it's yours now. I'm going to go lie down."

Lines reinforce the Blacksmith's "pioneer smith learning as he goes" characterization from [frontier_lore.md](../../.claude/agent-memory/design-lead/project_frontier_lore.md).

### 12. Interaction with COMBAT-01 combat focuses

Uniques can ship with pre-baked `CritChance` / `HasteMult` / `DodgeBonus` / `BlockChance` values via the `UniqueItemDef` fields. These contribute to `EquipmentCombatStats` the same way ring focuses do — additive into the per-tier-level aggregate, same caps apply. A unique with `CritChance = 0.15f` is equivalent to ~2.5 Tier 3 crit rings baked into one item.

This is how uniques break the "ring stacking is the only combat-focus vector" mold. A Precision-focused build still wants rings, but a Skullcleaver-wielder gets a chunk of crit built into the weapon too.

## Acceptance Criteria

- [ ] `UniqueItemDef` record exists, with all fields listed in §6.
- [ ] `UniqueItemDatabase` static class exists with the 28 seed uniques from §10.
- [ ] `ItemDef` gains a `bool IsUnique` field (default false). Craft tab's `CanApplyAffix` returns false when `IsUnique`.
- [ ] Blacksmith Window has a 3rd "Forge" tab wired to `ForgeRng.TryForge`.
- [ ] Forge flow validates materials + gold + backpack space; refuses cleanly with toast feedback.
- [ ] Base item is consumed on successful forge; unique is added to backpack.
- [ ] Per-tier pool cost matches §5 table (20/80/180/320/500 primary material, 10/40/90/160/250 secondary, 500/2k/8k/25k/80k gold).
- [ ] Main Hand uniques filter by `RequiredArchetype` — can't forge Skullcleaver from a Sword base.
- [ ] Recycled unique yields bonus `200 × tier` gold on top of normal recycle value.
- [ ] Save/load: unique is serialized as `UniqueId`; stats re-populate from database on restore.
- [ ] At least 3 uniques have full reaction-line content authored (rest can follow).
- [ ] Unit tests: cost validation, pool filtering (slot + tier + archetype), forge consumes inputs, `IsUnique` blocks affix crafting.
- [ ] Integration smoke test: forge a Tier 1 unique start-to-finish through the UI.

## Implementation Notes

- **Database authorship:** `UniqueItemDatabase.cs` is a content file, hand-edited by the design lead. Treat it like `ItemDatabase.cs` — entries are lines of data, not code.
- **Pool sampling:** uniform random over the filtered pool. Do NOT weight by rarity. Every unique in a tier pool has equal roll chance. "Rarer" uniques get their feeling of rarity from being in the Tier 5 pool (10 entries → 10% per forge → ~10 Tier 5 forges to see all of them).
- **Archetype filter:** when the base is a Main Hand, filter pool by `base.Archetype`. Off-hand, armor, accessory uniques don't filter (the slot match is enough).
- **Balance check on seed content:** once COMBAT-01 is live, Balance team (design lead) should spot-check that seed uniques are roughly competitive with a 6-affix equivalent normal of the same tier. Uniques should be *different*, not *strictly better*. A fully-affixed Transcendent Mega Sword and Skullcleaver should both be viable endgame choices.
- **Frontier lore hook:** the Blacksmith's "learning as he goes" characterization is important for why uniques are *crafted* (not dropped). That framing should show up in Blacksmith dialogue everywhere, not just reaction lines. Flag to flow lead for [flows/npc.md](../flows/npc.md) or similar.
- **Not in scope:** boss-dropped uniques, seeded/deterministic uniques, re-rolling, upgrading a unique into a higher-tier unique, set bonuses (multiple uniques cooperating). All potential future work; out of FORGE-01.

## Open Questions

None.

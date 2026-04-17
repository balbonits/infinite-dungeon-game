# Combat–Equipment Integration (COMBAT-01)

## Summary

Defines how equipped items actually change what happens in a fight. Today `EquipmentSet.GetTotalBonuses()` computes the bonus block but nothing consumes it — the player can wear Mega Armor and hit for the same damage as a naked Warrior. This spec closes the gap: equipment stats merge into the existing StatBlock derivation, new ring focuses (Crit / Haste / Evasion / Bulwark) feed combat formulas, and a cache-on-equip strategy keeps per-frame cost flat.

## Current State

**Spec status: DRAFT.** Tracked as COMBAT-01 in [dev-tracker.md](../dev-tracker.md). Blocks SYS-11 from feeling real in-game.

Depends on (all locked unless noted):
- [equipment.md](equipment.md) — 19-slot layout, class affinity, ammo rules.
- [stats.md](stats.md) — diminishing-returns curve and derived formulas.
- [combat.md](combat.md) — `Player.ExecuteAttack` flow, P2 stat integration plan.
- [items.md](../inventory/items.md) — `ItemDef` fields (`BonusStr`, `BonusDex`, `BonusSta`, `BonusInt`, `BonusHp`, `BonusDamage`).
- [item-catalog.md](../inventory/item-catalog.md) — ring catalog with 4 stat focuses + 4 combat focuses (Crit / Haste / Evasion / Bulwark).
- [combat-equipment-integration.md → COMBAT-02](weapon-archetype-dispatch.md) — sibling spec, same touch-point on `Player.GetEffectivePrimary()`.

## Design

### 1. Overlay model — equipment is a StatBlock overlay, not a separate column

Equipment bonuses **add on top of** allocated `StatBlock` values before the diminishing-returns curve is applied. The allocated stat is the player's permanent build commitment; equipment is a temporary overlay that moves with the gear.

```
effective_str = StatBlock.Str + EquipmentBonuses.Str         // raw sum
derived = raw * (100 / (raw + 100))                         // single DR pass
```

Everything downstream — `MeleeFlatBonus`, `AttackSpeedMultiplier`, `BonusMaxHp`, `SpellDamageMultiplier` — uses the combined raw value, not two separate curves. One stat. One curve.

**Why:** Two parallel DR curves would make each +1 STR from a ring worth wildly different amounts depending on whether the player had 10 raw STR or 100. A player reading the stats tooltip should be able to add their "base 40" + "equipment +8" and predict their effective 48. It also means "10× Dragonite Ring of Strength" (catalog-intended stacking) keeps returning diminishing — but known-diminishing — value, rather than circumventing DR by stacking.

### 2. Class affinity is a per-item multiplier, not a final-sum multiplier

The existing `EquipmentSet.Accumulate` helper applies the 1.25× multiplier **per item** before summing:

```csharp
float mult = (item.ClassAffinity == playerClass) ? 1.25f : 1.0f;
bonuses.Str += item.BonusStr * mult;
```

This means a Warrior in Mega Armor (affinity match) and a Ranger Ring (no match) gets:
```
final_str_bonus = (armor.BonusStr * 1.25) + (ring.BonusStr * 1.0)
```

Document this as canonical. Do NOT change it to a post-sum multiplier. Mixing class-affinity and off-class gear is a legitimate build vector; a post-sum multiplier would either over-reward off-class contributions or under-reward on-class ones.

### 3. Recompute strategy — cache on equip, not per attack

The `EquipmentBonuses` aggregation is O(19) items × per-frame = 1140+ reads/sec at 60 fps for a value that only changes when the player opens the equipment UI. Instead:

**Cache** an `EquipmentCombatStats` struct on `EquipmentSet`, **invalidated** on any mutation. Recomputed lazily on next read.

```csharp
public class EquipmentSet
{
    private EquipmentCombatStats? _cachedStats;

    public EquipmentCombatStats GetCombatStats(PlayerClass playerClass)
    {
        if (_cachedStats.HasValue) return _cachedStats.Value;
        _cachedStats = Recompute(playerClass);
        return _cachedStats.Value;
    }

    // Every mutator MUST call this.
    private void InvalidateCache() => _cachedStats = null;
}
```

**Invalidation hook points (all existing methods):**
- `TryEquip` — after successful swap.
- `Unequip` — after successful removal.
- `ForceEquip` — after overwrite.
- `RestoreState` — after save load.
- `DestroyRandomEquipped` — after death roll.

**Also invalidate if `PlayerClass` changes** (future-proofing — currently class is fixed per run, but if reclass is ever added, cache must rebuild).

Cache is NOT serialized — it's a pure function of equipment state and class, both of which are persisted. On load, the cache starts null and rebuilds on first read.

### 4. `EquipmentCombatStats` — the full combat-relevant aggregate

Extend today's `EquipmentBonuses` into a richer struct that captures everything `Player.ExecuteAttack` and `GameState.MaxHp` need:

```csharp
public readonly record struct EquipmentCombatStats
{
    // Core stat overlays (fed into StatBlock DR curve)
    public float Str       { get; init; }
    public float Dex       { get; init; }
    public float Sta       { get; init; }
    public float Int       { get; init; }

    // Direct HP contribution (flat, already multiplied by affinity)
    public float BonusHp   { get; init; }

    // Weapon/gear flat damage (added to baseDamage before STR/INT multiplier)
    public float BonusDamage { get; init; }

    // Combat-ring aggregates (0..1 floats; already capped per formula below)
    public float CritChance   { get; init; }  // 0..0.60
    public float HasteMult    { get; init; }  // multiplicative, 1.00..~1.60
    public float DodgeBonus   { get; init; }  // added to DEX dodge, pre-cap
    public float BlockChance  { get; init; }  // 0..0.60
}
```

Values are `float`: the 1.25× affinity multiplier produces fractional intermediates, and these are summed before any integer conversion at the callsite. Callers floor/round at the boundary where an `int` is needed (e.g. `BonusMaxHp`, `finalDamage`).

### 5. `MaxHp` recompute — unified formula

Today `GameState.MaxHp` recompute lives in three places and uses only `Stats.BonusMaxHp`:

- `StatAllocDialog.cs:121`
- `PauseMenu.cs:863`
- `DebugConsole.cs:130`
- Plus the one-line `GameState.cs:218` recompute path.

All four sites must become:

```csharp
var es = GameState.Instance.Equipment.GetCombatStats(SelectedClass);
// Fold equipment STA into StatBlock for derivation
int effectiveSta = Stats.Sta + (int)es.Sta;
int staDerivedHp = (int)(StatBlock.GetEffective(effectiveSta) * 5.0f);

MaxHp = Constants.PlayerStats.GetMaxHp(Level)    // level-based floor
      + staDerivedHp                             // replaces Stats.BonusMaxHp
      + (int)es.BonusHp;                         // direct +HP from gear (e.g. neck defense)
```

Same pattern applies for `MaxMana`:

```csharp
int effectiveInt = Stats.Int + (int)es.Int;
int intDerivedMana = (int)(StatBlock.GetEffective(effectiveInt) * 4.0f);

MaxMana = Constants.PlayerStats.GetClassBaseMana(SelectedClass)
        + intDerivedMana;
```

Consolidate the recompute into one helper on `GameState` (`RecomputeDerivedStats()`) so all four callsites become a single call. This is implementation hygiene; it's required for correctness because "forget to update one of the four sites" is the exact bug we're trying to prevent.

Trigger `RecomputeDerivedStats()` on:
- `StatsChanged` signal (already emitted on equip/unequip — wire the handler).
- Level-up (already handled).
- Save restore.

### 6. Melee damage formula — equipment `BonusDamage` is weapon base damage

The existing `ExecuteAttack` path:

```csharp
int baseDamage = Constants.PlayerStats.GetDamage(GameState.Instance.Level);
float statBonus = attack.IsProjectile
    ? stats.SpellDamageMultiplier
    : 1.0f + stats.MeleePercentBoost / 100f;
float flatBonus = attack.IsProjectile ? 0 : stats.MeleeFlatBonus;
int finalDamage = (int)((baseDamage + flatBonus) * attack.DamageMultiplier * statBonus);
```

Equipment's `BonusDamage` folds into `baseDamage` as "weapon base damage":

```csharp
float weaponBase = Constants.PlayerStats.GetDamage(Level) + es.BonusDamage;
float flatBonus  = attack.IsProjectile ? 0 : stats.MeleeFlatBonus;
float finalDamage = (weaponBase + flatBonus) * attack.DamageMultiplier * statBonus;
```

`BonusDamage` is pre-multiplied by attack-type multipliers AND STR/INT scaling — that's the point. A +20 damage weapon in a 100-STR Warrior's hands hits harder than in a 10-STR Warrior's hands, exactly as [stats.md](stats.md) promises.

This also makes equipment items consistent with the "Weapon base damage scaling" spec in [item-generation.md](item-generation.md): a rolled Mega Sword with `BonusDamage = 180` is treated as a 180-damage weapon, not a +180 flat bonus.

### 7. Ring combat focuses — formulas

The catalog ships 4 combat-ring focuses (Precision / Haste / Evasion / Bulwark), 5 tiers each, with ring stacking across the 10 ring slots being the primary build vector. Each ring contributes a **per-tier contribution** to the combat stat. Values are additive across stacked rings, then clamped.

| Focus | Per-tier contribution | Stacked formula | Cap | Stack point in combat |
|---|---|---|---|---|
| Crit (Precision) | 2% per tier-level | `sum(ring.Tier) * 0.02` | 60% | Final damage multiplier |
| Haste | 3% per tier-level | `sum(ring.Tier) * 0.03` | +60% attack speed | Attack cooldown |
| Dodge (Evasion) | 1.5% per tier-level | `sum(ring.Tier) * 0.015` | combines with DEX, global 60% | Incoming hit resolution |
| Block (Bulwark) | 2% per tier-level | `sum(ring.Tier) * 0.02` | 60% | Incoming damage negation |

"Per tier-level" means a Tier 3 ring contributes 3× the per-tier value; a Tier 5 ring contributes 5×. Stacking ten Tier 5 Precision rings yields `10 * 5 * 2% = 100% raw`, capped to 60%. That cap is deliberate — the catalog advertises 10-ring stacking as a "legitimate goal," but uncapped stacking would let the player achieve guaranteed crits and break enemy design.

**Where each focus applies in `ExecuteAttack`:**

```csharp
// 1. HASTE — modifies cooldown AFTER DEX attack-speed
_attackTimer = attack.Cooldown
             / stats.AttackSpeedMultiplier
             / (1.0f + Min(es.HasteMult, 0.60f));

// 2. CRIT — post-damage multiplier, rolled per-attack
float critRoll = _rng.NextSingle();
float critMultiplier = (critRoll < Min(es.CritChance, 0.60f)) ? 1.5f : 1.0f;
finalDamage = (int)(finalDamage * critMultiplier);

// 3. (DODGE/BLOCK live in the enemy→player hit path, not ExecuteAttack — see §8)
```

**Why these numbers:** the per-tier rate was chosen so a mid-game player (say 4 Tier 3 rings of Precision + 6 empty slots) lands at `4 * 3 * 2% = 24%` crit — a meaningful but non-dominating tilt. A late-game dedicated stacker (10 Tier 5) caps out. A casual player (2 Tier 2 rings) sits at 8% — a light tickle. The slope is gentle enough to reward investment without punishing neglect.

Crit multiplier is fixed at 1.5× to match [combat.md §P2 crit](combat.md) notation. Future affixes can add crit damage bonus; that's out of scope here.

### 8. Dodge and Block — incoming-damage path

Dodge and Block resolve in the **enemy→player** hit path, not outgoing. The enemy hit area (see [combat.md §Enemy Damage to Player](combat.md)) calls `GameState.TakeDamage()`; wrap that entrypoint with a dodge/block resolution:

```csharp
public void TakeDamage(int incoming)
{
    var es = Equipment.GetCombatStats(SelectedClass);

    // DODGE — combined DEX + ring contribution, global 60% cap
    float dodge = Min(Stats.DodgeChance + es.DodgeBonus, 0.60f);
    if (_rng.NextSingle() < dodge)
    {
        // Full negation
        FloatingText.Dodge(player, position);  // new text type
        return;
    }

    // BLOCK — partial negation
    float block = Min(es.BlockChance, 0.60f);
    if (_rng.NextSingle() < block)
    {
        incoming = (int)(incoming * 0.50f);    // halve damage
        FloatingText.Block(player, position);
    }

    // ... existing Hp subtract + flash ...
}
```

**Dodge** fully negates; **Block** halves. Two clearly different fantasies:
- Dodge is the Ranger fantasy — "I wasn't there." Binary. Visually, the hit misses.
- Block is the Warrior/Shield fantasy — "I ate half of it." Partial. Visually, the hit connects but reads as absorbed.

Dodge takes precedence (checked first) so block doesn't waste its roll on a hit that dodge would have avoided.

**Floor text:** `FloatingText.Dodge` and `FloatingText.Block` are new variants (yellow "MISS" and teal "BLOCK" respectively). Reuse the existing floating-text system ([targeting.md](targeting.md)-adjacent — the floating-text infrastructure is in place; just two new color/label variants needed).

### 9. Data flow diagram

```
┌───────────────────────────┐
│  Equip/Unequip/Force/...  │
└──────────┬────────────────┘
           │ mutator
           ▼
┌───────────────────────────┐
│ EquipmentSet (invalidate  │
│ _cachedStats = null)      │
└──────────┬────────────────┘
           │ StatsChanged signal
           ▼
┌───────────────────────────┐
│ GameState.RecomputeDerived│ ← reads EquipmentSet.GetCombatStats()
│ Stats()                   │   (lazy rebuild on first read)
│ • MaxHp                   │
│ • MaxMana                 │
└───────────────────────────┘

Per-attack:
┌───────────────────────────┐
│ Player.ExecuteAttack      │ ← reads EquipmentSet.GetCombatStats()
│ • weapon base + BonusDmg  │   (cache hit — O(1))
│ • STR/INT scaling         │
│ • Haste cooldown          │
│ • Crit roll               │
└───────────────────────────┘

Per-incoming-hit:
┌───────────────────────────┐
│ GameState.TakeDamage      │ ← reads EquipmentSet.GetCombatStats()
│ • Dodge roll              │
│ • Block roll              │
└───────────────────────────┘
```

## Acceptance Criteria

- [ ] `EquipmentSet` caches `EquipmentCombatStats`; cache invalidates on all mutators (`TryEquip`, `Unequip`, `ForceEquip`, `RestoreState`, `DestroyRandomEquipped`).
- [ ] `EquipmentCombatStats` exposes Str/Dex/Sta/Int/BonusHp/BonusDamage/CritChance/HasteMult/DodgeBonus/BlockChance.
- [ ] Allocated stats + equipment stats merge BEFORE the DR curve is applied (single effective value per stat).
- [ ] `MaxHp` and `MaxMana` recompute uses a single helper (`GameState.RecomputeDerivedStats`) invoked from all four current callsites.
- [ ] `Player.ExecuteAttack` folds `es.BonusDamage` into weapon base damage before STR/INT scaling.
- [ ] Haste divides the post-DEX cooldown, capped at +60% attack speed.
- [ ] Crit is rolled per-attack against `Min(es.CritChance, 0.60f)`; on crit, damage ×1.5.
- [ ] Dodge resolves first in `TakeDamage`, fully negating; Block resolves second, halving.
- [ ] Floating text `Dodge` (yellow "MISS") and `Block` (teal "BLOCK") variants exist.
- [ ] Unit tests cover: cache invalidation on each mutator; affinity multiplier per-item; combined DR curve math; crit/haste/dodge/block cap behavior.
- [ ] Equipping a Tier 5 Mega Sword visibly changes damage output in-game (manual smoke test — the "SYS-11 feels real" criterion).

## Implementation Notes

- **Scope boundary:** this spec touches `EquipmentSet.cs`, `GameState.cs`, `Player.cs`, `FloatingText.cs`, and the `StatAllocDialog` / `PauseMenu` / `DebugConsole` recompute callsites. It does NOT modify `AttackConfig`, `ClassAttacks`, or `ItemDef`.
- **Ring-focus data source:** `ItemDef` for combat rings currently ships with `BonusStr = 0` etc. (per ITEM-01 note "will be activated by COMBAT-01"). The crit/haste/dodge/block contribution is derived from `ItemDef.Tier` + a category/focus marker. Implementing teams should add a `RingFocus` enum to `ItemDef` (`None`, `Str`, `Dex`, `Sta`, `Int`, `Crit`, `Haste`, `Dodge`, `Block`) rather than inferring from the ID string. IDs are for wire format; enums are for logic.
- **RNG source:** use a shared `GameState.CombatRng` (new field, seeded per-run) for crit / dodge / block rolls. Deterministic-replay concerns are out of scope, but using a named RNG makes future work tractable.
- **Tests to seed:** `EquipmentSetCacheTests` (invalidation), `StatMergeTests` (overlay math), `CritRollTests` (cap + per-tier contribution), `TakeDamageMitigationTests` (dodge first, block second).
- **Save/load:** no schema change. Cache is transient; bonuses are recomputed from saved equipment + class on restore.
- **Don't bake ring focuses into `AffixDef`.** The 4 combat focuses are *base-item identity*, not affixes. They belong on the `ItemDef` directly.

## Open Questions

None.

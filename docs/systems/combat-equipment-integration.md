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

    // Combat-ring aggregates (raw %, NOT pre-capped — callers apply SoftCap + overflow per §7/§8)
    public float CritRaw   { get; init; }  // raw %, e.g. 24 for 24% raw
    public float HasteRaw  { get; init; }  // raw %, attack-speed side
    public float DodgeRaw  { get; init; }  // raw %, ring-contributed only (DEX-derived dodge is on StatBlock)
    public float BlockRaw  { get; init; }  // raw %
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

### 7. Ring combat focuses — formulas (split chance-vs-power with soft-cap conversion)

The catalog ships 4 combat-ring focuses (Precision / Haste / Evasion / Bulwark), 5 tiers each, with ring stacking across the 10 ring slots being the primary build vector. Each ring contributes a **per-tier contribution** to the combat stat. Values are additive across stacked rings, then passed through a **soft-cap curve** identical in shape to the stats.md diminishing-returns formula. The raw sum doesn't just clamp — the portion above the soft cap **converts** into a secondary power stat, so continued stacking stays meaningful at arbitrary depth.

**Soft-cap curve (all four focuses):**

```
raw       = sum(ring.Tier * per_tier_contribution)   // in %, as a number like 24 or 100
effective = raw * (60 / (raw + 60))                  // asymptotes at 60%, never reaches it
overflow  = raw - effective                          // always ≥ 0, grows without bound
```

This is the **same shape** as `stats.md`'s universal DR curve (`raw * (K / (raw + K))`) with K=60 instead of K=100. Using the same curve shape everywhere is a consistency win — players who already understand stat DR immediately understand combat-focus DR, and build-guide authors can reuse the same intuition.

**Per-focus contribution + overflow conversion:**

| Focus | Per-tier raw% | Stacked raw formula | Soft-cap asymptote (chance side) | Overflow conversion (power side) | Hard cap |
|---|---|---|---|---|---|
| Crit (Precision) | 2% per tier-level | `sum(ring.Tier) * 2%` | 60% crit chance | overflow × 2 → crit-damage bonus (adds to 1.5× base multiplier) | none on crit-damage |
| Haste | 3% per tier-level | `sum(ring.Tier) * 3%` | +60% attack speed | overflow × 0.5 → Flurry chance (free extra swing on attack) | **Flurry hard-capped at 40%** |
| Dodge (Evasion) | 1.5% per tier-level | `sum(DEX_dodge + ring.Tier * 1.5%)` | 60% dodge chance | overflow × 1 → Phase duration in ms (i-frame window after successful dodge) | **Phase hard-capped at 500 ms** |
| Block (Bulwark) | 2% per tier-level | `sum(ring.Tier) * 2%` | 60% block chance | overflow × 0.5 → block reduction % above 50% baseline | **Block reduction hard-capped at 80%** (i.e. overflow reduction gain capped at +30%) |

**Why Flurry is the one hard cap:** Haste's overflow becomes a second swing. A double-attack that fires on 60%+ of swings would trivialize enemy telegraph timing — the window players have to react to an enemy wind-up is designed around "one player swing per tempo," and a multi-swing loop at high frequency effectively removes that window. 40% is the frequency where Flurry still *feels* like a bonus proc rather than a rhythm change. The other three overflows compound damage or survival on a single action, which doesn't break enemy design the same way.

#### Worked example — 10× Tier 5 Precision (the "does infinite work?" proof)

A max-commitment crit stacker: ten Tier 5 Precision rings in all ten ring slots.

```
raw crit chance        = 10 * 5 * 2% = 100%
effective crit chance  = 100 * (60 / (100 + 60)) = 100 * 0.375 = 37.5%
overflow raw           = 100 - 37.5 = 62.5
crit-damage bonus      = 62.5 * 2 = 125%
effective crit multiplier = 1.5 (base) + 1.25 (overflow bonus) = 2.75×
```

**Per-swing expected damage multiplier:**

```
E[multiplier] = (1 - 0.375) * 1.0 + 0.375 * 2.75
             = 0.625 + 1.031
             = 1.656×
```

A naked-ring player sits at 1.0×. A 10× T5 Precision stacker averages 1.66× damage output. The "infinite part" works: raw% keeps climbing as the player adds more tiers or better rings, effective chance keeps climbing toward its asymptote, and overflow crit-damage has **no ceiling** — at 200 raw%, effective chance is 46.2% and overflow is 153.8, giving a 4.58× crit multiplier. The curve rewards commitment at any depth.

#### Mid-game sanity check — the original rationale still holds

The old spec's mid-game example was "4 Tier 3 Precision rings = 24% crit." Under the soft-cap curve:

```
raw       = 4 * 3 * 2% = 24%
effective = 24 * (60 / (24 + 60)) = 24 * 0.714 = 17.1%
overflow  = 24 - 17.1 = 6.9   → crit-damage bonus = +13.8%
```

Mid-game is pre-overflow in feel: a ~17% crit chance with a ~1.64× crit multiplier is functionally "a meaningful crit happens about 1 in 6 swings for a bit of extra punch." The player experience at mid-game is essentially unchanged — the DR curve only bites hard at high raw, which is the exact range the soft-cap is designed for.

#### Where each focus applies in `ExecuteAttack`

```csharp
// 1. HASTE — effective chance modifies cooldown; overflow rolls Flurry
float hasteEffective = SoftCap(es.HasteRaw);           // 0..60
float hasteOverflow  = es.HasteRaw - hasteEffective;
float flurryChance   = Min(hasteOverflow * 0.005f, 0.40f);  // 0..0.40

_attackTimer = attack.Cooldown
             / stats.AttackSpeedMultiplier
             / (1.0f + hasteEffective / 100f);

// 2. CRIT — effective chance rolls crit; overflow inflates crit damage
float critEffective   = SoftCap(es.CritRaw);
float critOverflow    = es.CritRaw - critEffective;
float critMultiplier  = 1.5f + (critOverflow * 0.02f);  // +2% per raw-overflow, no cap

bool didCrit = _rng.NextSingle() < (critEffective / 100f);
if (didCrit) finalDamage = (int)(finalDamage * critMultiplier);

// 3. FLURRY — free extra swing proc after the main attack resolves
if (_rng.NextSingle() < flurryChance)
{
    // Re-enter the attack pipeline once, no cooldown consumed
    ExecuteAttack(attack, isFlurry: true);
}

// 4. (DODGE/BLOCK live in the enemy→player hit path, not ExecuteAttack — see §8)

// Helper:
static float SoftCap(float raw) => raw * (60f / (raw + 60f));
```

**Why these numbers (unchanged):** per-tier rates are the same as the old flat-cap model. A mid-game player (4× T3 Precision) still sits at 17.1% effective crit — the curve bends, not breaks. A late-game stacker (10× T5) doesn't brick at 60% anymore; they keep getting returns in the form of crit damage. A casual player (2× T2) sits at `2 * 2 * 2% = 8% raw → 7.1% effective`, which feels identical to the old spec at that scale. The curve is gentle where the catalog's mid-game players live, and firm where dedicated stackers live.

**Tooltip implications (UI, not spec):** each combat ring focus now exposes **two numbers** to the player: the effective chance (soft-capped) and the overflow-conversion (crit damage / Flurry / Phase ms / block %). A Tier 5 Precision ring's tooltip will read something like "+10% Crit Chance (soft-capped at 60%; excess converts to crit damage)" — both the per-ring contribution and the global conversion must be visible. This is a UI story, not a spec blocker — tooltip copy and layout can be iterated without touching the formulas.

### 8. Dodge and Block — incoming-damage path

Dodge and Block resolve in the **enemy→player** hit path, not outgoing. The enemy hit area (see [combat.md §Enemy Damage to Player](combat.md)) calls `GameState.TakeDamage()`; wrap that entrypoint with soft-capped dodge/block resolution that mirrors the §7 overflow model.

**Phase state (new):** a successful dodge grants a short **i-frame window** — for its duration, the next incoming hit is auto-consumed (no Hp subtract, no subsequent dodge/block roll). Phase duration comes from dodge overflow:

```csharp
public class GameState
{
    // ... existing fields ...
    private float _phaseExpiresAt;  // in-game time (seconds); 0 = no active phase
    public bool IsPhased => TimeNow < _phaseExpiresAt;
}
```

Phase consumes on **one next hit** OR expires at its duration, whichever comes first.

```csharp
public void TakeDamage(int incoming)
{
    var es = Equipment.GetCombatStats(SelectedClass);

    // PHASE — i-frame window from a prior successful dodge
    if (IsPhased)
    {
        _phaseExpiresAt = 0f;                // consume on this hit
        FloatingText.Phase(player, position); // "PHASED" variant
        return;
    }

    // DODGE — soft-capped; overflow becomes Phase duration (ms)
    // Combined raw: DEX-derived dodge% + ring-contributed dodge%
    float dodgeRaw      = Stats.DodgeChance * 100f + es.DodgeRaw;  // in %
    float dodgeEffective = SoftCap(dodgeRaw);                       // 0..60
    float dodgeOverflow  = dodgeRaw - dodgeEffective;

    if (_rng.NextSingle() < dodgeEffective / 100f)
    {
        float phaseMs = Min(dodgeOverflow, 500f);                   // hard cap 500ms
        _phaseExpiresAt = TimeNow + phaseMs / 1000f;
        FloatingText.Dodge(player, position);                       // "MISS"
        return;
    }

    // BLOCK — soft-capped chance; overflow raises reduction % above 50% baseline
    float blockRaw       = es.BlockRaw;
    float blockEffective = SoftCap(blockRaw);                       // 0..60
    float blockOverflow  = blockRaw - blockEffective;

    if (_rng.NextSingle() < blockEffective / 100f)
    {
        // Base 50% reduction; overflow adds up to +30% (hard ceiling = 80% total)
        float reduction = 0.50f + Min(blockOverflow * 0.005f, 0.30f);
        incoming = (int)(incoming * (1.0f - reduction));
        FloatingText.Block(player, position);                       // "BLOCK"
    }

    // ... existing Hp subtract + flash ...
}

static float SoftCap(float raw) => raw * (60f / (raw + 60f));
```

**Three clearly different fantasies:**
- **Dodge** — the Ranger fantasy. "I wasn't there." Full negation. Overflow extends into a **Phase** window so the very next incoming hit is also ignored. Late-game evasion feels like "I am repeatedly not where you swung."
- **Block** — the Warrior/Shield fantasy. "I ate it." Partial. Baseline halves; overflow raises reduction toward 80%. Late-game block feels like "your hits barely move the HP bar."
- **Phase** — the carry-over from dodge. Brief and deterministic ("I slip the next one too"), never stacks, never granted without a prior successful dodge.

**Resolution order (unchanged): Phase → Dodge → Block.** Phase is checked first because if a prior dodge set it, the current hit is already spoken for; dodge is checked before block so block doesn't waste its roll on a hit that dodge would have avoided.

**Floating text variants:** `FloatingText.Dodge` ("MISS", yellow), `FloatingText.Block` ("BLOCK", teal), `FloatingText.Phase` ("PHASED", cyan). Reuse the existing floating-text system — three new color/label variants, no new infrastructure.

**Block reduction hard-cap rationale (80%):** unlike Flurry's timing argument, Block's cap is about Hp-bar legibility at high depth. A 100% reduction would make the shielded Warrior literally invincible to certain attacks, which breaks the "meaningful death" pillar. 80% keeps every hit's numbers on the HUD readable and every enemy attack at least slightly threatening.

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
│ • Phase check (prior i-frame) │
│ • Dodge roll (soft-cap + Phase overflow on success) │
│ • Block roll (soft-cap + reduction overflow)         │
└───────────────────────────┘
```

`GameState._phaseExpiresAt` is transient (in-game time, not serialized) — it naturally resets on save/load since Phase is a ~500 ms window and save/load never fires mid-combat.

### 10. Why split caps and not flat caps

In an infinite-depth roguelike with no run-length cap, a flat 60% cap on crit/haste/dodge/block turns every combat-focus ring worthless the moment the cap is reached. At floor 150, a player with 10 Tier 5 Precision rings would be in the *exact same damage regime* as a player with 10 Tier 3 Precision rings — both hit the 60% wall and further investment does nothing. That outcome contradicts the "infinite progression, no level cap" pillar in `overview.md`.

The split-cap model (soft-cap for the chance side, overflow conversion into a paired power stat) keeps every additional ring meaningful at any depth. It mirrors the genre precedent Diablo 3 established with crit-chance + crit-damage as linked sliders — once crit-chance approaches its soft ceiling, build attention shifts to crit-damage, and the two multiply. We're doing the same thing but generalized across all four combat focuses, so each focus has a well-defined "where my next ring goes" story. The soft-cap curve shape is borrowed directly from `stats.md` so players only have to learn one DR intuition for the whole game.

## Acceptance Criteria

- [ ] `EquipmentSet` caches `EquipmentCombatStats`; cache invalidates on all mutators (`TryEquip`, `Unequip`, `ForceEquip`, `RestoreState`, `DestroyRandomEquipped`).
- [ ] `EquipmentCombatStats` exposes Str/Dex/Sta/Int/BonusHp/BonusDamage/CritRaw/HasteRaw/DodgeRaw/BlockRaw.
- [ ] Allocated stats + equipment stats merge BEFORE the DR curve is applied (single effective value per stat).
- [ ] `MaxHp` and `MaxMana` recompute uses a single helper (`GameState.RecomputeDerivedStats`) invoked from all four current callsites.
- [ ] `Player.ExecuteAttack` folds `es.BonusDamage` into weapon base damage before STR/INT scaling.
- [ ] Soft-cap curve `effective = raw * (60 / (raw + 60))` applies to all four combat focuses (Crit, Haste, Dodge, Block); asymptotes at 60%, never reaches it.
- [ ] Overflow conversions produce the spec'd values at three sample points: raw = 0% (effective 0, overflow 0), raw = cap-reaching 60% (effective 30, overflow 30), and raw = 300% (5× past cap — effective 50, overflow 250).
- [ ] Crit: crit damage multiplier = `1.5 + overflow * 0.02`, unbounded (no hard cap).
- [ ] Haste: Flurry chance = `min(overflow * 0.005, 0.40)` — **Flurry hard-capped at 40%**; a Flurry proc re-enters the attack pipeline once with no cooldown consumed.
- [ ] Dodge: on success, Phase duration = `min(overflow, 500)` ms — **Phase hard-capped at 500 ms**; Phase auto-consumes the next incoming hit OR expires at its duration, whichever comes first.
- [ ] Block: reduction % = `0.50 + min(overflow * 0.005, 0.30)` — **Block reduction hard-capped at 80%** total.
- [ ] Resolution order in `TakeDamage`: Phase (if active) consumes the hit first; else Dodge rolls; else Block rolls.
- [ ] Floating text variants exist: `Dodge` (yellow "MISS"), `Block` (teal "BLOCK"), `Phase` (cyan "PHASED").
- [ ] Unit tests cover: cache invalidation on each mutator; affinity multiplier per-item; combined DR curve math; soft-cap + overflow math for all four focuses at 0%, at-cap, and 5× past cap; Flurry 40% ceiling holds; Phase consume-or-expire semantics; Block 80% reduction ceiling holds.
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

# Weapon Archetype Attack Dispatch (COMBAT-02)

## Summary

The equipped Main Hand weapon's archetype — not the wielder's class — determines which attack mechanic fires. A Warrior holding a staff casts bolts. A Mage holding a crossbow shoots bolts (if she has a quiver). Class affinity still pays the +25% multiplier; it does not change the attack animation, projectile, or scaling stat. This spec formalizes the dispatch logic that the catalog already promises ([item-catalog.md §Main Hand cross-class weapon rule](../inventory/item-catalog.md#main-hand-weapons-40-items)) but that `Player.GetEffectivePrimary()` only partly implements.

## Current State

**Spec status: DRAFT.** Tracked as COMBAT-02 in [dev-tracker.md](../dev-tracker.md).

Currently `Player.GetEffectivePrimary()` handles only one branch of the rule (`Ranger + no quiver → bow-bash`); every other class uses `ClassAttacks.GetPrimary(class)` regardless of what's in their Main Hand. A Warrior equipping the Archmage's Transcendent Staff still swings a sword-slash. That's a catalog contradiction waiting to ship.

Depends on:
- [equipment.md](equipment.md) — slot layout, class affinity, ammo rule (LOCKED).
- [item-catalog.md](../inventory/item-catalog.md) — weapon archetypes enumerated per class (LOCKED).
- [combat.md](combat.md) — `AttackConfig` data model, `ExecuteAttack` flow (LOCKED).
- [combat-equipment-integration.md](combat-equipment-integration.md) — sibling spec; shares the `GetEffectivePrimary()` touch-point.

## Design

### 1. Dispatch point — dynamic, on every attack

`Player.GetEffectivePrimary()` already exists and is called inside `HandleAttack` every physics frame (guarded by `_attackTimer`). Extend it to read Main Hand first:

```csharp
private AttackConfig GetEffectivePrimary()
{
    var gs = GameState.Instance;
    var mainHand = gs.Equipment.MainHand;

    // No weapon — class default (existing path)
    if (mainHand == null)
        return ClassAttacks.GetPrimary(gs.SelectedClass);

    // Archetype dispatch
    return ResolveAttackForArchetype(mainHand, gs.Equipment.HasQuiver());
}
```

**Why dynamic (not cached on equip):** the lookup is a single dictionary/switch — O(1). The cost of a stale cache bug (player swaps weapon in the pause menu but the attack is still the old one) is larger than the cost of the dispatch call. Resolution is stateless — pure function of (MainHand ItemDef, HasQuiver). No invalidation to manage.

This is a deliberate difference from the COMBAT-01 bonuses cache: bonuses are expensive to recompute, attack dispatch is cheap.

### 2. `WeaponArchetype` — new enum on `ItemDef`

Introduce a `WeaponArchetype` enum so dispatch never has to parse ID strings:

```csharp
public enum WeaponArchetype
{
    None,          // Non-weapons, or weapons without defined archetype (shouldn't happen)
    Sword,         // Warrior T1..T5
    Axe,           // Warrior T1..T5
    Hammer,        // Warrior T1..T5
    Shortbow,      // Ranger T1..T5
    Longbow,       // Ranger T1..T5
    Crossbow,      // Ranger T1..T5
    Staff,         // Mage T1..T5
    Wand,          // Mage T1..T5
}

// In ItemDef:
public WeaponArchetype Archetype { get; init; } = WeaponArchetype.None;
```

The catalog's 40 Main Hand items map 1-to-1 onto this enum (5 tiers × 3 Warrior archetypes + 5 × 3 Ranger + 5 × 2 Mage = 40). `ItemDatabase` initialization sets the field per-item.

### 3. Archetype → `AttackConfig` mapping table

| Archetype | Resolves to | Ranged? | Requires quiver? | Fallback if quiver absent |
|---|---|---|---|---|
| `Sword` | `WarriorSlash` | No | — | — |
| `Axe` | `WarriorSlash` (variant future work) | No | — | — |
| `Hammer` | `WarriorSlash` (variant future work) | No | — | — |
| `Shortbow` | `RangerArrowShot` | Yes | Yes | `RangerBowBash` |
| `Longbow` | `RangerArrowShot` | Yes | Yes | `RangerBowBash` |
| `Crossbow` | `RangerArrowShot` | Yes | Yes | `RangerBowBash` |
| `Staff` | `MageMagicBolt` | Yes | No | — |
| `Wand` | `MageMagicBolt` | Yes | No | — |
| `None` (no weapon) | `ClassAttacks.GetPrimary(class)` | — | — | — |

Axe / Hammer and Longbow / Crossbow currently resolve to the same `AttackConfig` as their archetype peers. Per-archetype variants (Hammer has higher flat damage, slower cooldown, maybe a tiny stun chance; Crossbow fires slower but hits harder; etc.) are explicit **future work** — flagged as Open Question §1 below but not required for this spec. The mapping table gives us room to diverge later without restructuring dispatch.

```csharp
private static AttackConfig ResolveAttackForArchetype(ItemDef weapon, bool hasQuiver)
{
    return weapon.Archetype switch
    {
        WeaponArchetype.Sword    or
        WeaponArchetype.Axe      or
        WeaponArchetype.Hammer   => ClassAttacks.WarriorSlash,

        WeaponArchetype.Shortbow or
        WeaponArchetype.Longbow  or
        WeaponArchetype.Crossbow => hasQuiver
                                      ? ClassAttacks.RangerArrowShot
                                      : ClassAttacks.RangerBowBash,

        WeaponArchetype.Staff    or
        WeaponArchetype.Wand     => ClassAttacks.MageMagicBolt,

        _ => ClassAttacks.GetPrimary(GameState.Instance.SelectedClass),
    };
}
```

### 4. Stat scaling — archetype dictates the scaling stat

Per [items.md cross-class weapon rule](../inventory/items.md), the weapon archetype dictates the attack mechanic. This spec extends that: **the archetype also dictates which stat scales the attack's damage.**

| Archetype family | Scaling stat | Rationale |
|---|---|---|
| Sword / Axe / Hammer | STR | Melee = muscle. Warrior's on-class. |
| Shortbow / Longbow / Crossbow (with quiver) | DEX | Ranged physical = coordination. Ranger's on-class. |
| Shortbow / Longbow / Crossbow (no quiver, bow-bash) | STR | Blunt melee impact = muscle. |
| Staff / Wand | INT | Spell-weapon = mental focus. Mage's on-class. |
| No weapon | Class default (Warrior STR, Ranger DEX, Mage INT) | Consistent with `ClassAttacks.GetPrimary`. |

This is already how `ExecuteAttack` behaves today — it reads `attack.IsProjectile` and applies `SpellDamageMultiplier` (INT) for projectile or `MeleePercentBoost` (STR) for melee. **That branching happens to line up with the new rule for every archetype except one edge case: bows (projectile, but should scale on DEX, not INT).**

### 5. The DEX-scaling edge case — minimum change

Today `ExecuteAttack`:

```csharp
float statBonus = attack.IsProjectile
    ? stats.SpellDamageMultiplier          // INT-scaled
    : 1.0f + stats.MeleePercentBoost / 100f;
```

A Mage's magic bolt (INT-scaled) and a Ranger's arrow (should be DEX-scaled) both go through the `IsProjectile` branch. Today, `RangerArrowShot` damage silently uses the Mage formula — wrong by spec, but invisible because Rangers don't invest in INT anyway.

Introduce a third branch driven by a new `AttackConfig.ScalingStat` enum:

```csharp
public enum AttackScalingStat { Strength, Dexterity, Intelligence }

public record AttackConfig
{
    // ... existing fields ...
    public AttackScalingStat ScalingStat { get; init; } = AttackScalingStat.Strength;
}
```

Default `Strength` keeps `WarriorSlash` and `RangerBowBash` correct without field writes. Set explicitly on:
- `RangerArrowShot.ScalingStat = Dexterity`
- `MageMagicBolt.ScalingStat = Intelligence`
- `MageStaffMelee.ScalingStat = Intelligence` (Mage's staff-melee fallback — already INT-scaled fantasy)

In `ExecuteAttack`:

```csharp
(float flatBonus, float percentBonus) = attack.ScalingStat switch
{
    AttackScalingStat.Strength     => (stats.MeleeFlatBonus,  stats.MeleePercentBoost / 100f),
    AttackScalingStat.Dexterity    => (0f,                    stats.AttackSpeedMultiplier - 1.0f),  // see note
    AttackScalingStat.Intelligence => (0f,                    stats.SpellDamageMultiplier - 1.0f),
    _ => (0, 0),
};

float statBonus = 1.0f + percentBonus;
```

**DEX damage-scaling note:** today [stats.md](stats.md) gives DEX attack-speed + dodge, not damage. We need a DEX damage multiplier for ranged-physical attacks. Three options:
- **Option A (chosen):** reuse `effective_dex * 0.6%` as a ranged-physical damage bonus. Tight value chosen so a 50-DEX Ranger gets +20% ranged damage — less than a Mage's +40% spell damage at 50 INT (INT is more one-dimensional), but meaningful. Adds a new derived stat `RangedDamageMultiplier` to `StatBlock`.
- Option B: give Ranger arrows STR scaling like melee. Rejected — breaks "bow = DEX fantasy" and makes a Ranger who dumped DEX hit the same as a Warrior.
- Option C: defer. Rejected — the catalog already promises this; shipping the archetype rule without the DEX scaling means Rangers do no more damage with a Top-Shelf Longer than with a Shortie. Which is the exact "SYS-11 feels real" failure COMBAT-01 is trying to solve.

Going with **A**. Add `RangedDamageMultiplier` to `StatBlock`:

```csharp
// In StatBlock:
// ranged_damage_bonus = effective_dex * 0.6%
public float RangedDamageMultiplier => 1.0f + GetEffective(Dex) * 0.006f;
```

Update [stats.md](stats.md) to document this. (This is an edit the design-lead owns; listed in §Implementation Notes.)

### 6. Class affinity interaction — unchanged

The +25% affinity multiplier from [equipment.md](equipment.md) applies to the item's `BonusDamage` (via `EquipmentSet.Accumulate`). It does not buff the attack multiplier, the scaling stat, or anything about the archetype dispatch. A Warrior holding an Archmage's Staff (Mage affinity) gets:

- Attack: `MageMagicBolt` (archetype win)
- Scaling: INT (archetype win — Warrior has low INT, will hit like wet paper)
- Weapon base damage: staff's `BonusDamage` × 1.0× (no affinity bonus for the Warrior)
- Mage equipping the same staff: same attack + scaling, weapon base × 1.25×

This matches the catalog philosophy: class affinity is a reward for on-class play; off-class gives up the affinity multiplier but doesn't break the archetype contract.

### 7. Ranger bow-bash fallback — generalization

Today `RangerBowBash` triggers only for `PlayerClass.Ranger` with no quiver. This spec generalizes it: **any class wielding a bow/crossbow without a quiver performs the bash.** A Warrior holding a Longer with no quiver bashes. A Mage holding a Crank with no quiver bashes. The bash uses the `RangerBowBash` `AttackConfig` as-is (it's `ScalingStat.Strength` by default, which is fair for a blunt melee swing regardless of wielder class).

This removes the `SelectedClass` check from `GetEffectivePrimary` — the class field is no longer consulted in the archetype dispatch path. It's consulted only when the Main Hand is empty (fallback to class default).

### 8. Off-Hand interaction — out of scope

Off-Hand items (shields, grimoires, defensive melee) do not alter attack dispatch in this spec. They contribute stats via `EquipmentSet` but not attack mechanics. Dual-wielding weapons (two `MainHand`-compatible items) is not supported — catalog has one Main Hand slot, and off-hand slots accept only Shield / Spellbook / DefensiveMelee categories.

## Acceptance Criteria

- [ ] `ItemDef` gains a `WeaponArchetype Archetype` field. All 40 Main Hand catalog items set it correctly.
- [ ] `AttackConfig` gains a `ScalingStat` field (default `Strength`). `RangerArrowShot`, `MageMagicBolt`, `MageStaffMelee` set it explicitly.
- [ ] `StatBlock.RangedDamageMultiplier` exists and uses `effective_dex * 0.6%`.
- [ ] [stats.md](stats.md) is updated to document `RangedDamageMultiplier`.
- [ ] `Player.GetEffectivePrimary()` dispatches from `Equipment.MainHand.Archetype`, falling back to class default only when Main Hand is empty.
- [ ] Warrior + staff equipped fires `MageMagicBolt` (verified by integration test).
- [ ] Mage + crossbow + quiver equipped fires `RangerArrowShot` (verified by integration test).
- [ ] Mage + crossbow + no quiver equipped performs `RangerBowBash` (verified by integration test).
- [ ] No weapon equipped: Warrior swings `WarriorSlash`, Ranger fires `RangerArrowShot` (with quiver), Mage uses `MageStaffMelee`. (Current behavior preserved.)
- [ ] `ExecuteAttack` scaling branches on `AttackConfig.ScalingStat`, not `IsProjectile`.
- [ ] Dispatch is called per-attack (dynamic); no equip-time cache.
- [ ] Unit tests cover: every archetype → correct `AttackConfig`; bow/crossbow quiver branching; empty Main Hand → class default; scaling-stat branching.

## Implementation Notes

- **Order with COMBAT-01:** these two specs touch adjacent code but don't conflict. COMBAT-01 owns `EquipmentSet` cache + `ExecuteAttack` damage formula. COMBAT-02 owns `GetEffectivePrimary` + archetype enum + scaling-stat enum. Land COMBAT-02 first (smaller footprint, no cache invalidation risk), then COMBAT-01 on top.
- **`ClassAttacks.GetMeleeFallback` stays as-is.** Fallback is for "enemy in melee range while primary is ranged" (future Mage staff-melee behavior), not for archetype dispatch. Orthogonal.
- **Stats-doc edit required:** [stats.md](stats.md) currently lists DEX derived values as `attack_speed_bonus` and `dodge_chance` only. Add `ranged_damage_bonus = effective_dex * 0.6%` to the DEX section and the summary table. That edit is authored by the design lead as part of landing COMBAT-02, not deferred.
- **Tests:** seed `WeaponArchetypeDispatchTests` covering the full 9-archetype × {quiver / no-quiver} matrix. Prefer integration tests that construct a `Player` with a real `EquipmentSet` over unit tests on `GetEffectivePrimary` directly — the real bug surface is the interaction with live `GameState`.
- **No AttackConfig proliferation:** don't create `WarriorAxeSlash`, `WarriorHammerSlash` variants yet. One `WarriorSlash` serves all three archetypes per the table. Per-archetype tuning is future work (Open Question §1).

## Open Questions

1. **Per-archetype variant tuning.** The table collapses Sword/Axe/Hammer → one `WarriorSlash` and Shortbow/Longbow/Crossbow → one `RangerArrowShot`. Eventually Axe should hit harder and slower than Sword; Crossbow should crit more than Shortbow; Longbow should have the longest range. Design lead to author a follow-up (COMBAT-03?) defining the per-archetype tuning once COMBAT-01 and COMBAT-02 are both live and we can balance against real equipment. Scope creep to add it here.

# Entity Mechanics Framework

## Summary

A unified framework where all entities (player, enemies, NPCs) share the same mechanics. Entities only differ in configuration values (stats, assets, hitboxes) — never in code paths. Other systems (items, skills, effects) interact with entities through this framework.

## Design Principles

1. **One data model** — `EntityData` is used for ALL entity types. No separate PlayerState/MonsterData.
2. **No type branching** — Systems never check `if (entity.Type == Player)`. Behavior differences come from stat values.
3. **Static systems** — Each system is a static class. Methods take `EntityData` as their first parameter.
4. **Pure logic** — No Godot dependencies. Fully testable with xUnit.
5. **Composition over inheritance** — Entities are data bags, systems are logic.

## System Architecture

| System | File | Responsibility |
|--------|------|----------------|
| **EntityData** | `entities/EntityData.cs` | Unified data model for all entities |
| **EntityFactory** | `entities/EntityFactory.cs` | Create pre-configured entities (player, enemy, NPC) |
| **VitalSystem** | `systems/VitalSystem.cs` | HP/MP management, death, revive, regeneration |
| **StatSystem** | `systems/StatSystem.cs` | STR/DEX/INT/VIT, diminishing returns, derived stats |
| **CombatSystem** | `systems/CombatSystem.cs` | Damage calculation, crits, defense mitigation |
| **EffectSystem** | `systems/EffectSystem.cs` | Status effects (poison, regen, buffs, debuffs) |
| **ProgressionSystem** | `systems/ProgressionSystem.cs` | XP, leveling, stat/skill point allocation |
| **SkillSystem** | `systems/SkillSystem.cs` | Skill execution, cooldowns, mana costs |

> **Note:** These files do not currently exist. All entity framework code was deleted in the Session 8 fresh start. The file paths and designs below are the target for reimplementation.

**Planned (not yet implemented):**

| System | Responsibility |
|--------|----------------|
| **InventorySystem** | Item management, equip/unequip, consume |
| **LootSystem** | Drop tables, rewards, gold |
| **MovementSystem** | Position, velocity, isometric transform (Godot-dependent) |

## Data Model

```
EntityData
├── Identity: Id, Name, Type, SpriteSheet, SpriteLayers
├── Vitals: HP, MaxHP, MP, MaxMP, IsDead
├── Stats: STR, DEX, INT, VIT, Level
├── Combat: BaseDamage, BaseDefense, AttackSpeed, AttackRange, HitboxRadius
├── Movement: MoveSpeed
├── Effects: List<ActiveEffect>
├── Player-only: XP, Gold, StatPoints, SkillPoints, Equipment, Inventory
├── Enemy-only: XPReward, GoldReward, Tier
└── Derived (cached): TotalDamage, TotalDefense, XPToNextLevel
```

## System Interactions

```
Items/Skills/Effects
        │
        ▼
┌─────────────┐     ┌─────────────┐     ┌──────────────┐
│ SkillSystem │────▶│ VitalSystem │◀────│ EffectSystem │
└─────────────┘     └──────┬──────┘     └──────────────┘
                           │
                    ┌──────▼──────┐
                    │ CombatSystem│
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  StatSystem │
                    └─────────────┘
                           │
                    ┌──────▼──────┐
                    │ Progression │
                    └─────────────┘
```

**Flow example — Player attacks Enemy:**
1. `CombatSystem.DealDamage(player, enemy)` — calculates damage from player's TotalDamage
2. Rolls crit (15% chance, 1.5x)
3. `StatSystem.GetDefenseReduction(enemy)` — applies enemy's defense
4. `VitalSystem.TakeDamage(enemy, finalDamage)` — reduces HP, checks death
5. If enemy dies → `ProgressionSystem.AwardXP(player, enemy.XPReward)`

**Same flow — Enemy attacks Player:**
1. `CombatSystem.DealDamage(enemy, player)` — **exact same function**, just swapped parameters

## Key Formulas

| Formula | Implementation |
|---------|---------------|
| Diminishing returns | `raw * (100 / (raw + 100))` |
| Max HP | `100 + Level * 8 + VIT * 3 + equipment` |
| Max MP | `50 + INT * 3 + equipment` |
| XP to next level | `Level² × 45` |
| Defense reduction | `GetEffective(defense) / 100` (0-1 range, never reaches 1) |
| Crit | 15% chance, 1.5x multiplier |
| Minimum damage | Always 1 (even with max defense) |

## Effect Types

| Effect | Behavior |
|--------|----------|
| Poison | Deals `magnitude` damage every `tickInterval` seconds |
| Regen | Heals `magnitude` HP every `tickInterval` seconds |
| StatBuff / StatDebuff | Passive — modify stats while active |
| Stun / Slow / Haste | Passive — affect movement/action speed while active |
| DamageBoost / DefenseBoost | Passive — modify combat while active |

Effects do not stack — reapplying the same type refreshes duration.

## File Locations

```
scripts/game/
├── GameCore.cs              ← Legacy (deprecated, kept for old test compat)
├── entities/
│   ├── EntityData.cs
│   ├── EntityFactory.cs
│   └── EntityType.cs
├── systems/
│   ├── VitalSystem.cs
│   ├── StatSystem.cs
│   ├── CombatSystem.cs
│   ├── EffectSystem.cs
│   ├── ProgressionSystem.cs
│   └── SkillSystem.cs
└── effects/
    ├── EffectData.cs
    ├── ActiveEffect.cs
    └── EffectType.cs
```

## Testing

All systems are pure C# with no Godot dependencies — fully testable with xUnit. Tests cover:
- Happy path, boundary conditions, overflow/underflow
- State invariants (HP never negative, dead can't attack)
- Symmetry (same combat code for all entity types)
- Multi-step scenarios (buff → attack → expire → attack)
- Edge cases (poison kill, multi-level-up, equipment stat clamp)

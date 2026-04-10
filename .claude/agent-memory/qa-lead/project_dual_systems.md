---
name: Dual System Architecture (GameCore vs Entity Framework)
description: The codebase has two parallel game systems -- legacy GameCore.cs monolith and new entity framework. Tests exist for both. Critical migration debt.
type: project
---

The codebase contains two complete, parallel implementations of game logic:

1. **Legacy monolith** (`GameCore.cs`): Contains `PlayerState`, `MonsterData`, `GameState` (static singleton), `GameSystems` -- all combat, inventory, leveling, shop, etc.
2. **New entity framework** (`scripts/game/systems/`, `scripts/game/entities/`): `EntityData`, `EntityFactory`, `VitalSystem`, `StatSystem`, `CombatSystem`, `EffectSystem`, `ProgressionSystem`, `SkillSystem`

**Why:** Entity framework was designed to replace the monolith per docs/architecture/entity-framework.md. However, the NpcPanel UI code still references `GameState` and `GameSystems` directly.

**How to apply:** When reviewing any new code, ensure it targets the entity framework -- not the legacy monolith. Flag any new code that imports or references `GameState`/`GameSystems`/`PlayerState`/`MonsterData` as technical debt.

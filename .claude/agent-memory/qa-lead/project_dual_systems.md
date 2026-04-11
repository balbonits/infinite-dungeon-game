---
name: Dual System Architecture (GameCore vs Entity Framework) — RESOLVED
description: Historical context only. The dual system issue was resolved by the Session 8 fresh start which deleted all code. No legacy monolith or entity framework code exists in the codebase.
type: project
---

**Status: RESOLVED — Historical context only.**

The Session 8 fresh start deleted all code from the repository (see dev-journal Session 8). The dual system problem described below no longer exists. This memory is retained for historical context only.

---

Previously, the codebase contained two complete, parallel implementations of game logic:

1. **Legacy monolith** (`GameCore.cs`): Contained `PlayerState`, `MonsterData`, `GameState` (static singleton), `GameSystems` -- all combat, inventory, leveling, shop, etc.
2. **New entity framework** (`scripts/game/systems/`, `scripts/game/entities/`): `EntityData`, `EntityFactory`, `VitalSystem`, `StatSystem`, `CombatSystem`, `EffectSystem`, `ProgressionSystem`, `SkillSystem`

This technical debt was eliminated entirely when all code was deleted. The rebuild will implement a single unified architecture from the locked specs.

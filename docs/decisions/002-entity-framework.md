# ADR-002: Unified Entity Framework

**Status:** Accepted
**Date:** 2026-04-09

**Context:** The game has three kinds of entities -- player, enemies, and NPCs -- that share overlapping mechanics: health, stats, damage dealing, damage taking, status effects, and movement. Early prototypes used separate data classes (PlayerState, MonsterData) with duplicated logic. As systems grew (combat, effects, progression), the duplication became a maintenance and testing burden.

**Decision:** A unified entity framework where all entities share one data model (EntityData) and all game logic lives in static system classes. Entities differ only in configuration values (stats, assets, hitboxes), never in code paths.

Architecture:
- **EntityData** -- single data class for all entity types (player, enemy, NPC)
- **EntityFactory** -- creates pre-configured entities with correct defaults per type
- **Static systems** -- VitalSystem, StatSystem, CombatSystem, EffectSystem, ProgressionSystem, SkillSystem
- **No type branching** -- systems never check entity type; behavior differences come from stat values
- **Pure C#** -- no Godot dependencies in the framework, fully testable with xUnit

**Consequences:**
- `CombatSystem.DealDamage(attacker, target)` works identically whether the attacker is a player or an enemy -- the same function, just swapped parameters
- All entity logic can be unit tested without launching Godot, enabling fast test cycles
- Adding a new entity type (e.g., a summon, a trap) requires only a new EntityFactory preset, not new classes or systems
- The Godot scene layer (CharacterBody2D nodes, sprites, collision) is separate from the data layer -- scenes read from and write to EntityData but don't contain game logic
- Systems that need Godot (movement, rendering) will be added as separate Godot-dependent layers on top of the pure C# framework

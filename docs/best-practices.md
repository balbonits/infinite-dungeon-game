# Best Practices

Development guidelines for this project. These apply to both human and AI contributors.

## Development Principles

- **KISS** — Keep It Simple. Use the simplest approach that satisfies the spec. Don't over-engineer.
- **DRY** — Don't Repeat Yourself. Extract shared logic when repetition appears, but don't prematurely abstract.
- **No scope creep** — Only implement what the current spec describes. Don't add features, "nice to haves," or placeholder code for unplanned work. If it's not in the doc, it's not in the code.
- **Spec-driven** — Every system is fully documented in `docs/` before implementation begins. Docs are the source of truth.
- **Test-driven** — Tests are written before implementation. Manual test cases first, then automated tests (GdUnit4), then the code.
- **AI-coded** — All code is written by AI assistants. The user directs, reviews, and makes design decisions.
- **Free assets only** — No paid assets. Use Polygon2D placeholders, then free/open-source asset packs.

## Scope Discipline

This project is micromanaged by design. Every task has a defined scope, and AI must not exceed it.

- Do exactly what is asked — nothing more, nothing less
- Do not add features, refactors, or "improvements" beyond the request
- Do not invent requirements or make assumptions about intent
- If something is ambiguous, stop and ask instead of guessing
- If you spot something out of scope that seems wrong, flag it — don't fix it
- Scope violations (hallucinations, assumptions, extras) are the #1 AI development failure mode

## Docs First

- Documentation includes: node hierarchy, property values, method pseudocode, signal connections, test cases
- Changes to game mechanics should be reflected in docs before (or alongside) code changes
- The docs are the source of truth — if the code disagrees with the docs, one of them needs updating

## Project Structure

- Follow the directory layout defined in `docs/architecture/project-structure.md`
- Each game entity (player, enemy, etc.) has its scene (.tscn) and script (.cs) together in `scenes/`
- Autoload singletons live in `scripts/autoloads/`
- Binary assets (PNGs, audio) in `assets/`, Godot resources (.tres) in `resources/`
- Documentation in `docs/`, organized by topic

## Small, Testable Changes

- One feature or system per commit
- Include manual test steps in commit messages or PR descriptions
- Example: "Kill 3 enemies → verify XP increases → verify level up at threshold → verify HP heals"
- Keep scope small — it's easier to review and revert

## Naming Conventions (C#)

- **Private fields:** `_camelCase` — `_moveSpeed`, `_attackTimer`, `_hitCooldownTimer`
- **Public properties and methods:** `PascalCase` — `MoveSpeed`, `HandleMovement()`, `TakeDamage()`
- **Constants:** `PascalCase` — `MoveSpeed`, `AttackCooldown`, `EnemySoftCap`
- **Node names in scenes:** `PascalCase` — `CollisionShape2D`, `AttackRange`, `HitCooldownTimer`
- **Signals:** `PascalCase`, past tense for events — `EnemyDefeated`, `StatsChanged`, `PlayerDied`
- **Groups:** `snake_case` (Godot convention) — `"player"`, `"enemies"`
- **Files:** `PascalCase` — `GameState.cs`, `DeathScreen.cs`, `Dungeon.cs`
- **No abbreviations** unless universally obvious — `player` not `plr`, `level` not `lvl`, `position` not `pos`

## Code Organization

- Each `.cs` script handles one responsibility (player movement, enemy AI, HUD display)
- Constants and static fields at the top of the class, `[Export]` properties next, private fields, then lifecycle methods (`_Ready`, `_PhysicsProcess`), then helper methods
- Autoloads (`GameState`, `EventBus`) centralize shared state and cross-system communication
- Use signals for decoupled communication between systems — avoid direct node references across scene boundaries
- Obtain node references in `_Ready()` via `GetNode<T>("NodePath")` — avoid `GetNode()` calls in per-frame methods

## Performance

- Avoid per-frame allocations — don't create new objects in `_PhysicsProcess()` unless necessary
- Use `QueueFree()` for cleanup; instantiate new scenes only when needed
- Keep collision shapes simple — circles for prototype, refine later
- Soft caps on active enemies (14) to prevent frame drops
- Prefer `_PhysicsProcess()` for gameplay logic (consistent tick rate)
- Use `_Process()` only for visual-only updates that don't affect physics

## Accessibility

- Keyboard navigation must work for all menus and dialogs
- Color alone should not be the only indicator — pair with text or shape differences
- Death screen must be navigable via keyboard (R key) and mouse (button click)
- Future: screen reader support for menu systems

## Save Safety

- Never lose player data silently — confirm before any destructive action
- Auto-save at key moments (level-ups, floor transitions, inventory changes)
- `FileAccess` with `user://` for persistent storage
- Base64 export/import via `Marshalls` (or `System.Convert`) as backup mechanism
- Validate imported save data before applying

## Branching & Commits

- Feature branches off `main`
- Descriptive commit messages: what changed and why
- Keep `main` in a working state — don't merge broken code
- One feature or bugfix per branch

## Testing

- Manual playtesting in Godot editor (F5) after every change
- Automated tests via GdUnit4 framework for state logic and formulas
- Document test steps for each feature in `docs/testing/manual-tests.md`
- Run the full test suite before merging to `main`
- See `docs/testing/test-strategy.md` for the complete testing approach

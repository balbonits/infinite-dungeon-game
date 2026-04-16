# Claude Code Instructions

All project guidelines and AI context live in [AGENTS.md](AGENTS.md).
Read that file for all rules, conventions, and game design references.

For detailed game design documentation, see the [docs/](docs/) folder.
For development progress tracking, see [docs/dev-tracker.md](docs/dev-tracker.md).

## The User

**The user is the product owner / client — not a developer.** They direct game vision, make design decisions, and approve outcomes. They do not write, review, or debug code. All implementation is handled by AI.

- Ask about the **game** ("how should this feel to the player?"), not about **code** ("which data structure should we use?")
- Make all technical decisions autonomously — the user approves what the game *does*, not how it's built
- When the user says "do X", do it — don't explain the plan unless asked
- Present options as player experience tradeoffs, not architecture tradeoffs
- The AI is the entire dev team. The user is the client.

## Current Mode

**Active development.** All 26 specs are locked. Full game loop playable: splash → class select → town → NPCs → dungeon → combat → death → respawn. Tabbed PauseMenu (Diablo 2-style, 8 tabs) with keyboard-only navigation. See `docs/dev-tracker.md` for feature status.

**Stack:** Godot 4.6 + C# (.NET 8+). See [docs/architecture/setup-guide.md](docs/architecture/setup-guide.md) for environment setup.

## Paradigm

This repo is a real-world experiment in **AI+Human natural-language programming**. The user directs in English; AI writes all code, tests, docs, art, commits. **Specs in `docs/` are the source of truth** — if code and specs disagree, one needs updating. See [`docs/development-paradigm.md`](docs/development-paradigm.md).

## Testing

Three layers, all runnable from the Makefile:

| Layer | Framework | Command | Purpose |
|-------|-----------|---------|---------|
| Unit | xUnit | `make test-unit` | Pure C# logic, no Godot runtime |
| Integration | xUnit | `make test-integration` | Cross-system logic flows |
| E2E (scene) | GdUnit4 | `make test-gdunit` | Scene loading, asset validation |
| UI (in-game) | GoDotTest | `make test-ui` | Keyboard-driven full-game tests via `--run-tests` in live Godot runtime |

In-game UI tests live in `scripts/testing/tests/*.cs` (extend `GameTestBase`). Drive the game via `InputHelper` (keyboard simulation using `GodotTestDriver`) and verify state via `UiHelper` (focus, windows, pause state). Tests assert specs from `docs/flows/*.md`.

`ScreenTransition.Play()` has a critical invariant: **overlay must be opaque before new worlds are swapped in**. Always put `Close()`/hide work INSIDE the midpoint callback, never before `Play()` starts. See `docs/flows/screen-transition.md`.

## AI Teams

Specialized team leads are defined in `.claude/agents/`. Route work to the right team automatically, or @mention a lead directly. See [docs/conventions/teams.md](docs/conventions/teams.md).

Active: `@design-lead`, `@qa-lead`, `@devops-lead`, `@art-lead`

## Critical Rules

1. **Stay in scope.** Do exactly what is asked. Nothing more. Do not add features, refactors, or extras beyond the current task.
2. **Do not assume.** If something is unclear, ask. Do not guess or hallucinate requirements.
3. **Read the spec first.** Check the relevant doc in `docs/` before writing or modifying any code.
4. **Tests before code.** Write or reference test cases before writing implementation.
5. **Docs are the source of truth.** If code and docs disagree, one needs updating.
6. **Specs before code.** Every system must be fully specified in `docs/` before implementation begins. No exceptions.

## Post-Task Protocol

After any code change, before committing:

1. **Test:** Run `make test` — all tests must pass
2. **Docs:** Update the relevant spec in `docs/` if game behavior changed
3. **Journal:** Add what changed to `docs/dev-journal.md` under today's session
4. **Changelog:** Add a summary line to `CHANGELOG.md`
5. **Counts:** Update test counts in docs if tests were added/removed
6. **Commit:** Use conventional format: `type(scope): description`
   - Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`
   - Examples: `feat(combat): add elemental damage system`, `docs(journal): session 6d entry`

This protocol is non-negotiable. Do not commit without completing these steps.

## Documentation Maintenance

- **Never hardcode volatile numbers** in AGENTS.md or CLAUDE.md (test counts, file counts, step counts). Reference commands instead: "Run `make test` for current count."
- **Journal first, then commit.** The dev journal entry must exist before the git commit.
- **CHANGELOG.md must stay current.** Every commit that changes behavior gets a changelog entry.
- **New systems need docs.** If you create a new system (new .cs files with game logic), create a corresponding spec in `docs/systems/` or `docs/world/`.

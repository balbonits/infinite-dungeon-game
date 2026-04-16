# Claude Code Instructions

**Context chain:** `docs/` (source of truth) → [AGENTS.md](AGENTS.md) (AI-readable index) → **CLAUDE.md** (you are here — fast navigation) → AI output.

This file is a jump-index into AGENTS.md. Never answer from this file alone — always follow the chain back to the relevant spec in `docs/`. The specs are the truth; AGENTS.md organizes them for AI consumption; this file helps Claude find the right section fast.

## Jump-to Index (→ AGENTS.md sections)

| Need to... | Section |
|------------|---------|
| Understand the paradigm (AI+Human, specs-driven) | [Paradigm](AGENTS.md#paradigm--read-this-first) |
| Know who the user is / how to communicate | [Who You're Working For](AGENTS.md#who-youre-working-for) |
| Route work to the right AI team | [AI Team Structure](AGENTS.md#ai-team-structure) |
| Stay in scope / avoid scope creep | [Scope Discipline](AGENTS.md#1-scope-discipline-read-this-first) |
| Follow the workflow cycle | [AI Workflow Protocol](AGENTS.md#2-ai-workflow-protocol) |
| After making a change (before commit) | [Post-Task Protocol](AGENTS.md#2a-post-task-protocol) |
| Update docs correctly | [Documentation Maintenance](AGENTS.md#2b-documentation-maintenance) |
| Find conventions (naming, C#, Godot) | [C# Conventions](AGENTS.md#4-c-conventions) · [Naming](AGENTS.md#6-naming-conventions) |
| Check tech stack / NuGet deps | [Tech Stack](AGENTS.md#5-tech-stack) |
| Find the project layout | [Project Structure](AGENTS.md#7-project-structure) |
| Pick the right test framework | [Tech Stack → Testing layers](AGENTS.md#5-tech-stack) |

## Jump-to Index (→ docs/ — source of truth)

| Working on... | Spec location |
|---------------|---------------|
| Game design, systems | [docs/systems/](docs/systems/) |
| Flow (splash, town, NPC, death, etc.) | [docs/flows/](docs/flows/) |
| World/lore | [docs/world/](docs/world/) |
| UI (pause menu, HUD, death screen) | [docs/ui/](docs/ui/) |
| Architecture (autoloads, signals) | [docs/architecture/](docs/architecture/) |
| Conventions (AI workflow, teams, code) | [docs/conventions/](docs/conventions/) |
| Dev history | [docs/dev-journal.md](docs/dev-journal.md) · [docs/dev-tracker.md](docs/dev-tracker.md) |
| Paradigm (why this repo exists) | [docs/development-paradigm.md](docs/development-paradigm.md) |

## Quick Commands

```bash
make test           # xUnit unit + integration
make test-ui        # GoDotTest in-game keyboard-nav tests
make test-gdunit    # GdUnit4 scene/asset tests
make build          # Build C# project
make run            # Launch Godot
```

## Hard Rules Reminder (full detail in AGENTS.md)

1. **Stay in scope.** Do exactly what's asked.
2. **Read the spec first** in `docs/`.
3. **Specs are source of truth.** If code and docs disagree, one needs updating.
4. **Tests before code.** Reference or write test cases first.
5. **Post-task protocol is non-negotiable.** Test → Docs → Journal → Changelog → Counts → Commit.
6. **Never hardcode volatile numbers** (test counts, file counts) in AI-context files.

---

**Precedence:** `docs/` > `AGENTS.md` > `CLAUDE.md`. If any two disagree, the higher-precedence source wins. Add new rules to `AGENTS.md` (or the relevant `docs/` file) — not here.

**Adding a new AI tool's context file?** Each tool has its own convention (Claude Code: `CLAUDE.md`; Gemini CLI: `GEMINI.md`; Copilot: `.github/copilot-instructions.md`; Cursor: `.cursor/rules/*.mdc`; etc.). Verify the filename in the tool's official docs first — do not guess. Template and full guidance in [docs/conventions/ai-context-files.md](docs/conventions/ai-context-files.md).

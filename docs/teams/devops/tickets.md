# DevOps Team — Ticket Board

**Lead:** `@devops-lead` | **Model:** Sonnet | **Domain:** CI, tooling, Makefile, project config

> **Session 8 fresh start:** All previous code, scenes, and tests were deleted. The project is a clean slate with only project config files remaining. DevOps tickets below reflect post-reset state.

## Active Queue

| ID | Title | Status | Details |
|----|-------|--------|---------|
| CFG-03 | Re-enable CI when code exists | To Do | CI pipeline needs test targets to be meaningful |

## Blocked Queue

| ID | Title | Status | Blocked By |
|----|-------|--------|------------|
| SETUP-04 | Update project.godot for .NET | Blocked | SETUP-02 |
| SETUP-05 | Update Makefile for C# | Blocked | SETUP-02 |
| SETUP-06 | Update CI and pre-commit hook | Blocked | SETUP-05 |
| SETUP-07 | Write C# sanity tests | Blocked | SETUP-04 |
| P6-06 | Test coverage tooling | Blocked | SETUP-07 |

## Done

| ID | Title |
|----|-------|
| SETUP-01 | Install dev environment |
| SETUP-02 | Create DungeonGame.csproj |
| SETUP-03 | Remove GUT and GDScript tests |
| CFG-01 | Fix project.godot stale references |
| CFG-02 | Update pre-commit hook for C# |

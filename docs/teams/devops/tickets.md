# DevOps Team — Ticket Board

**Lead:** `@devops-lead` | **Model:** Sonnet | **Domain:** CI, tooling, Makefile, project config

## Active Queue

| ID | Title | Status | Details |
|----|-------|--------|---------|
| SETUP-01 | Install dev environment | Done | .NET 10, Godot .NET 4.6.2, VS Code extensions |
| SETUP-02 | Create .csproj and .sln | To Do | Let Godot generate, add NuGet refs |
| SETUP-03 | Remove GUT and GDScript tests | To Do | Delete `addons/gut/`, `tests/test_project_setup.gd` |

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

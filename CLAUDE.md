# Claude Code Instructions

**[AGENTS.md](AGENTS.md) is the canonical reference. Read the relevant section before acting.** This file is a fast navigation index to help you jump to what matters — not a substitute for reading AGENTS.md.

## Jump-to Index

| Need to... | Section in AGENTS.md |
|------------|---------------------|
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

## Quick Commands

```bash
make test           # xUnit unit + integration
make test-ui        # GoDotTest in-game keyboard-nav tests
make test-gdunit    # GdUnit4 scene/asset tests
make build          # Build C# project
make run            # Launch Godot
```

## Hard Rules (from AGENTS.md — always apply)

1. **Stay in scope.** Do exactly what's asked.
2. **Read the spec first** in `docs/`.
3. **Specs are source of truth.** If code and docs disagree, one needs updating.
4. **Tests before code.** Reference or write test cases first.
5. **Post-task protocol is non-negotiable.** Test → Docs → Journal → Changelog → Counts → Commit.
6. **Never hardcode volatile numbers** (test counts, file counts) in AI-context files.

## Paradigm (TL;DR)

Every line of code, test, doc, and piece of art in this repo is AI-written. The human directs in natural language and does not write code. Specs in `docs/` drive implementation. Full detail: [`docs/development-paradigm.md`](docs/development-paradigm.md).

---

**If AGENTS.md and this file ever conflict, AGENTS.md wins.** If you need to add a new rule, add it to AGENTS.md (not here) and update the index above.

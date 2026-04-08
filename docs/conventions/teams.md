# AI Team Structure & Conventions

How the AI dev teams are organized, how they work, and how to interact with them.

## Studio Structure

```
PRODUCT OWNER (you)
 │
 ├── Design Lead ── game specs, formulas, balance, player experience
 ├── Engine Lead ── Godot scenes, physics, rendering, tiles (Phase 2)
 ├── Systems Lead ── C# logic, autoloads, state, signals (Phase 2)
 ├── UI Lead ── HUD, menus, panels, input handling (Phase 2)
 ├── World Lead ── dungeon generation, floors, town (Phase 2)
 ├── QA Lead ── testing, spec review, bug verification
 └── DevOps Lead ── CI, tooling, project config
```

## Team Roster

### Active Now (Docs Phase)

| Team | Lead Agent | Model | Access | Tickets |
|------|-----------|-------|--------|---------|
| Design | `@design-lead` | Opus | Read + write docs | SPEC-* |
| QA | `@qa-lead` | Sonnet | Read-only | TEST-*, spec reviews |
| DevOps | `@devops-lead` | Sonnet | Full | SETUP-*, INFRA-* |

### Available When Coding Starts

| Team | Lead Agent | Model | Access | Tickets |
|------|-----------|-------|--------|---------|
| Engine | `@engine-lead` | Sonnet | Full + worktree | P1-03, P1-04, P1-05 (scenes) |
| Systems | `@systems-lead` | Sonnet | Full + worktree | P1-01, P1-02, P1-07 (logic) |
| UI | `@ui-lead` | Sonnet | Full + worktree | P1-08, P1-09 (UI scenes) |
| World | `@world-lead` | Opus | Full + worktree | P4-* (proc gen) |

## How To Talk To Teams

### Automatic Routing

Just describe what you want. The AI routes to the right team:

- "How should rested XP work?" → Design Lead
- "Set up the Makefile" → DevOps Lead
- "Review the combat spec for gaps" → QA Lead

### Direct @mention

For specific requests, mention the team lead:

- `@design-lead write the town hub spec`
- `@qa-lead check if the leveling formulas make sense at high levels`
- `@devops-lead update CI for the new test framework`

### What You Don't Need To Do

- Pick which team handles a task (automatic)
- Review code (teams handle correctness)
- Know technical details (teams translate your vision into implementation)
- Manage inter-team handoffs (the system handles it)

## Team Rules

### Ownership

Each team owns specific files and directories:

| Team | Owns | Does NOT Touch |
|------|------|---------------|
| Design | `docs/systems/`, `docs/world/`, `docs/inventory/`, `docs/ui/`, `docs/objects/` | Any `.cs`, `.tscn`, `.tres` file |
| Engine | `scenes/`, `resources/` | `docs/`, `scripts/autoloads/` |
| Systems | `scripts/` (except UI) | `scenes/`, `docs/` |
| UI | `scripts/ui/`, `scenes/ui/` | `scripts/autoloads/`, `docs/` |
| World | `scripts/dungeon/`, `scripts/world/` | UI, player, enemy scripts |
| QA | `tests/` | Production code, docs |
| DevOps | `Makefile`, `.github/`, `.githooks/`, `.editorconfig`, `.gitignore`, `*.csproj` | Game code, docs |

### Handoffs

When a team needs something from another team's domain:

1. **Design → Implementation teams:** Design writes the spec, marks it "locked." Implementation team picks up the corresponding ticket.
2. **Implementation → QA:** After building, the implementation team flags QA to write/run tests.
3. **Any team → DevOps:** If tooling or config needs changing, create a ticket for DevOps.
4. **Conflicts:** If two teams disagree on approach, escalate to the product owner.

### Memory

Each team lead has persistent memory (`memory: project`). They remember:

- Project patterns and conventions they've learned
- Decisions made in previous sessions
- Mistakes to avoid

Ask a team lead to "save what you learned" after important sessions.

## Ticket Templates By Team

### Design Team Tickets (SPEC-*)

```markdown
#### SPEC-XX: {Title}

- **Type:** spec
- **Team:** Design
- **Description:** {What game mechanic to define — in player terms}
- **Doc:** {path to spec doc}
- **Decisions Needed:**
  - [ ] {Question about game feel / player experience}
- **Acceptance Criteria:**
  - [ ] {Doc section written with concrete values}
  - [ ] {No open questions remain}
- **Status:** To Do
- **Deps:** {other SPEC tickets}
```

### Implementation Team Tickets (P1-*, P2-*, etc.)

```markdown
#### PX-XX: {Title}

- **Type:** impl
- **Team:** {Engine | Systems | UI | World}
- **Description:** {What to build}
- **Create:** {File paths}
- **Key Details:** {Class, methods, values, signals}
- **Acceptance Criteria:**
  - [ ] {Behavior with exact values}
  - [ ] {Tests that must pass}
- **Spec:** {Link to design doc}
- **Status:** To Do
- **Deps:** {other tickets}
```

### QA Tickets (TEST-*)

```markdown
#### TEST-XX: {Title}

- **Type:** test
- **Team:** QA
- **Description:** {What to test}
- **Create:** {Test file path}
- **Framework:** {xUnit | GdUnit4}
- **Test Cases:**
  - [ ] {Input → expected output}
- **Status:** To Do
- **Deps:** {impl ticket}
```

### DevOps Tickets (SETUP-*, INFRA-*)

```markdown
#### SETUP-XX: {Title}

- **Type:** infra
- **Team:** DevOps
- **Description:** {What to configure}
- **Modify:** {File paths}
- **Acceptance Criteria:**
  - [ ] {Command works}
- **Status:** To Do
```

## Scaling

This team structure is designed to be replaceable:

- **Now:** Each "team" is an AI agent with a persona
- **Later:** Any team lead can be replaced by a human developer
- **The tickets and specs work the same way** regardless of who (or what) picks them up

The key is that every ticket is **self-contained** — whoever picks it up has everything they need without asking other teams for context.

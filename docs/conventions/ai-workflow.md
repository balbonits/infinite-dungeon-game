# AI Workflow Protocol

## Summary

This document defines how AI assistants must work on this project. It codifies lessons from [spec-driven development](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development), [Claude Code best practices](https://code.claude.com/docs/en/best-practices), and [real-world AI development failures](https://towardsdatascience.com/the-black-box-problem-why-ai-generated-code-stops-being-maintainable/). Every rule exists because its absence caused bugs, wasted effort, or scope explosions in AI-assisted projects.

## Current State

This protocol is active. All AI-generated code in this project must follow this workflow.

## Design

### Dev Ticket Cycle

Every ticket follows this git-based workflow. Each ticket gets its own branch. Do not skip steps.

```
create branch → research → plan → verify plan (research bot cross-check)
→ update plan → write tests (if possible/needed) → code → verify
→ run tests (if any) → run/verify build → update docs
→ commit (checkpoint if partial) → push (squash checkpoints) → delete branch
```

**Step-by-step:**

**1. Create Branch**
- Branch from `main`: `git checkout -b TICKET-ID` (e.g., `P1-04d`)
- One branch per ticket. No multi-ticket branches.

**2. Research**
- Read the relevant spec doc(s) in `docs/`
- Read the existing code if modifying
- Spin up researcher agent to cross-reference best practices, code patterns, edge cases
- Check if a RES-* ticket exists for this area — if so, complete it first

**3. Plan**
- Enter plan mode
- State what you will do, list files to touch, confirm plan matches spec
- For tasks touching multiple files, describe the order of changes

**4. Verify Plan (Research Bot Cross-Check)**
- Researcher agent reviews the plan against industry patterns and known pitfalls
- Flag anything that contradicts best practices or has known edge cases
- Update plan based on findings

**5. Write Tests (if possible/needed)**
- Reference test cases from `docs/testing/manual-tests.md` and `docs/testing/automated-tests.md`
- If a test case doesn't exist, write one BEFORE implementation
- The test defines "done"

**6. Code**
- Write the minimum code that passes tests and satisfies the spec
- Follow [C# conventions](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html) and project naming rules (see AGENTS.md §6)
- One responsibility per script, one purpose per function
- Use static typing everywhere (C# enforces this)
- Prefer composition (child nodes) over monolithic scripts

**7. Verify**
- Run tests if possible. If manual, describe EXACT verification steps.
- Check: does the change modify ONLY what the ticket asked for?

**8. Run Tests / Verify Build**
- `dotnet test` for automated tests
- `dotnet build` to verify clean compilation
- Manual playtest if the ticket affects gameplay
- **Capture visual evidence** for E2E validation (see below)

**9. Update Docs**
- Update any spec docs affected by implementation decisions
- Update dev-tracker ticket status

**10. Commit & Push**
- Commit as checkpoints during partial work (small, descriptive messages)
- On completion: squash all checkpoint commits into one clean commit
- Push to remote, create PR if needed
- Delete the feature branch after merge

**11. Stop**
- Do not continue to the next ticket
- Do not "clean up" nearby code
- Do not add "while I'm here" improvements
- Wait for user direction

### Debug Tools

A suite of toggleable debug overlays for development and testing. All tools render on a dedicated CanvasLayer above the game but below menus.

**Tools:**

| Tool | What It Shows |
|------|--------------|
| **Performance overlay** | FPS, entity count, current floor, player position |
| **Input visualizer** | Active keys/buttons, D-pad state, bumper state, face button presses — real-time display of what inputs the game is receiving |
| **Collision shapes** | Renders all CollisionShape2D outlines (player, enemies, walls, attack range, hit areas) |
| **Game state inspector** | Live values: HP/MaxHP, Mana, XP, Level, Floor, attack cooldown, active buffs |
| **Entity inspector** | Tap/hover an enemy to see its HP, tier, speed, target status, distance to player |

**Controls:**
- **F3** = master toggle (all debug tools on/off)
- Individual tools can be toggled in a debug submenu (F3 then cycle)

**Visibility flag:**
- Debug tools have a global `debug_visible` flag
- When `debug_visible = false`, ALL debug overlays are hidden — even if F3 was toggled on
- This is used when capturing screenshots/recordings for visual evidence — disable debug overlays so captures show the clean game
- Setting persisted in a dev config file (not the player save)

**Implementation:**
- **Use existing packages/addons first** — check Godot Asset Library and NuGet for debug overlay tools before building custom. Only build what isn't available.
- Separate CanvasLayer (layer 90 — above game, below UI layer 100)
- Each tool is a Control node that can be individually shown/hidden
- Debug tools are **compiled out of release builds** (wrapped in `#if DEBUG` or equivalent)
- No performance impact when disabled
- See RES-28 for package research

### Visual Test Evidence (Screenshots & Recordings)

Every ticket that affects gameplay or UI must include visual evidence for E2E validation. This serves as a living reference of what the game looks like at each stage.

**When to capture:**
- New visual feature (movement, combat, HUD, death screen, etc.)
- Changed behavior that's visually verifiable
- Bug fix that affects what the player sees

**What to capture:**

| Type | Format | When |
|------|--------|------|
| Screenshot | PNG | Static UI, HUD layout, menu screens, tile rendering |
| Screen recording | MP4/GIF | Movement, combat, animations, transitions, game feel |

**Where to store:**
```
docs/evidence/
├── P1-04d/              ← one folder per ticket
│   ├── movement-8dir.mp4
│   ├── wall-collision.png
│   └── notes.md         ← brief description of what each file shows
├── P1-07b/
│   ├── attack-basic.mp4
│   └── slash-effect.png
└── ...
```

**Rules:**
- Evidence folder named by ticket ID
- Include a `notes.md` with a one-liner per file describing what it validates
- For new features: capture the first working version (baseline)
- For changes to existing features: capture before AND after (or just after if no prior baseline)
- Keep files small — trim recordings to the relevant 5-15 seconds
- Evidence is committed with the ticket's branch and included in the squashed commit

### Three-Tier Boundary System

Adapted from [Addy Osmani's spec-writing guide](https://addyosmani.com/blog/good-spec/).

**Always Do (no approval needed):**
- Read spec docs before code changes
- Use static typing in all C# (enforced by the compiler)
- Follow naming conventions (_camelCase private fields, PascalCase public members/methods/nodes, past-tense signals)
- Run tests after implementing changes
- Keep scripts under ~300 lines
- Use `GetNode<T>()` in `_Ready()` for node references
- Follow "call down, signal up" pattern

**Ask First (need user approval):**
- Adding new files, scenes, or scripts not described in the spec
- Changing autoload singletons (GameState, EventBus)
- Modifying project.godot settings
- Changing collision layers or masks
- Adding new dependencies, plugins, or addons
- Refactoring or restructuring code beyond the current task
- Changing the spec docs themselves

**Never Do (hard stops, no exceptions):**
- Add features not described in the spec
- Make assumptions about what the user wants — ask instead
- Skip writing or referencing tests
- Change code outside the current task's scope
- Add TODO comments, "future-proofing" code, or placeholder implementations
- Suppress errors or warnings to make things "work"
- Delete or modify files in `archive/`
- Add paid assets or proprietary dependencies

### Task Decomposition

When a task is large, break it into subtasks that follow these rules:

- **Atomic** — each subtask produces one reviewable, testable change
- **Independent** — subtasks should not depend on unfinished other subtasks
- **Spec-aligned** — every subtask maps to something in the spec docs
- **Ordered** — list dependencies explicitly; don't assume order is obvious

One subtask = one commit. This enables easy rollback if something goes wrong.

### Context Management

Based on [Claude Code documentation](https://code.claude.com/docs/en/best-practices):

- Start fresh for each unrelated task (don't carry over context from prior work)
- Keep prompts focused on the single task at hand
- Reference files by path rather than pasting large blocks
- If a task requires reading many files, delegate to a subagent to preserve main context
- When context gets large, summarize what matters and discard the rest

### Anti-Patterns to Avoid

From research on AI development failures:

| Anti-Pattern | Why It Fails | Instead |
|-------------|-------------|---------|
| Kitchen sink session | Context fills with irrelevant info, AI loses focus | Start fresh per task |
| Correcting repeatedly | Failed approaches pollute context | After 2 corrections, start over with a better prompt |
| Over-specified instructions | AI ignores buried rules | Keep rules concise; only include what changes behavior |
| Trust without verification | Plausible code hides edge-case bugs | Always provide tests or verification criteria |
| Monolithic generation | Coupled code becomes unmaintainable | Enforce component boundaries, one responsibility per node |
| Vague prompts | AI fills gaps with assumptions | Scope the task: which files, what behavior, what "done" looks like |
| Premature optimization | Adds complexity without measured need | Make it work first, optimize only when profiling shows a problem |

### Quality Gates

Before any code is considered "done":

1. **Spec compliance** — does the code do exactly what the spec describes? Nothing more?
2. **Tests pass** — are there test cases, and do they pass?
3. **Scope check** — does the diff contain ONLY changes related to the task?
4. **Style check** — does the code follow C# conventions and project naming rules?
5. **Boundary check** — are dependencies explicit and one-directional? No hidden coupling?

## Automation

The following tooling ensures AI assistants can develop, test, and validate code entirely from the terminal:

- **Pre-commit hooks** (`.githooks/pre-commit`) — runs `dotnet format --verify-no-changes` on staged `.cs` files. Configured via `git config core.hooksPath .githooks` (or `make setup`).
- **C# formatting** (`dotnet format`) — `dotnet format` for formatting and style enforcement. Run with `make lint` / `make format`.
- **GdUnit4 test framework** (`addons/gdUnit4/`) — headless test execution via `make test`. Tests live in `tests/` with `Test` suffix.
- **GitHub Actions CI** (`.github/workflows/ci.yml`) — lint + test on every push and PR to `main`.
- **Makefile** — `make help` lists all targets: `setup`, `test`, `lint`, `format`, `check`, `run`, `tiles`, `clean`.

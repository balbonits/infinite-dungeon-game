# AI Workflow Protocol

## Summary

This document defines how AI assistants must work on this project. It codifies lessons from [spec-driven development](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development), [Claude Code best practices](https://code.claude.com/docs/en/best-practices), and [real-world AI development failures](https://towardsdatascience.com/the-black-box-problem-why-ai-generated-code-stops-being-maintainable/). Every rule exists because its absence caused bugs, wasted effort, or scope explosions in AI-assisted projects.

## Current State

This protocol is active. All AI-generated code in this project must follow this workflow.

## Design

### The Cycle: Read → Plan → Test → Implement → Verify → Stop

Every task follows this sequence. Do not skip steps. Do not reorder.

**1. Read**
- Read the relevant spec doc in `docs/` before touching anything
- If the task references a game system, read the system doc AND the object doc
- If you're modifying code, read the existing code first
- If you're unsure which doc applies, ask

**2. Plan**
- State what you will do in 1-3 sentences
- List the files you will touch
- Confirm the plan matches the spec — if it doesn't, stop and ask
- For tasks touching multiple files, describe the order of changes
- Keep plans minimal — the spec already has the details

**3. Test First**
- Reference the relevant test cases from `docs/testing/manual-tests.md`
- If automated tests exist for the system, reference those from `docs/testing/automated-tests.md`
- If a test case doesn't exist for the requested change, write one BEFORE writing implementation code
- The test defines "done" — nothing else does

**4. Implement**
- Write the minimum code that passes the tests and satisfies the spec
- Follow [GDScript conventions](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_styleguide.html) and the project's naming rules (see AGENTS.md §6)
- One responsibility per script, one purpose per function
- Use static typing everywhere
- Prefer composition (child nodes) over monolithic scripts

**5. Verify**
- Run the tests if possible
- If you cannot run them (e.g., manual tests), describe the EXACT steps to verify
- List what should be true after the change
- Check: does the change modify ONLY what the task asked for? If not, revert the extras.

**6. Stop**
- Do not continue to the next task
- Do not "clean up" nearby code
- Do not add "while I'm here" improvements
- Wait for user direction

### Three-Tier Boundary System

Adapted from [Addy Osmani's spec-writing guide](https://addyosmani.com/blog/good-spec/).

**Always Do (no approval needed):**
- Read spec docs before code changes
- Use static typing in all GDScript
- Follow naming conventions (snake_case vars, PascalCase nodes, past-tense signals)
- Run tests after implementing changes
- Keep scripts under ~300 lines
- Use `@onready` for node references
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
4. **Style check** — does the code follow GDScript conventions and project naming rules?
5. **Boundary check** — are dependencies explicit and one-directional? No hidden coupling?

## Automation

The following tooling ensures AI assistants can develop, test, and validate code entirely from the terminal:

- **Pre-commit hooks** (`.githooks/pre-commit`) — runs `gdlint` and `gdformat --check` on staged `.gd` files. Configured via `git config core.hooksPath .githooks` (or `make setup`).
- **GDScript linting** (`gdtoolkit`) — `gdlint` for static analysis, `gdformat` for formatting. Installed via `pip3 install gdtoolkit`. Run with `make lint` / `make format`.
- **GUT test framework** (`addons/gut/`) — headless test execution via `make test`. Tests live in `tests/` with `test_` prefix.
- **GitHub Actions CI** (`.github/workflows/ci.yml`) — lint + test on every push and PR to `main`.
- **Makefile** — `make help` lists all targets: `setup`, `test`, `lint`, `format`, `check`, `run`, `tiles`, `clean`.

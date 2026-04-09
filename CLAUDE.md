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

**Implementation active.** All 26 specs are locked. Code is being written. Follow the dev ticket cycle in [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md).

**Stack:** Godot 4 + C# (.NET 8+). See [docs/architecture/setup-guide.md](docs/architecture/setup-guide.md) for environment setup.

## AI Teams

Specialized team leads are defined in `.claude/agents/`. Route work to the right team automatically, or @mention a lead directly. See [docs/conventions/teams.md](docs/conventions/teams.md).

Active now: `@design-lead`, `@qa-lead`, `@devops-lead`

## Critical Rules

1. **Stay in scope.** Do exactly what is asked. Nothing more. Do not add features, refactors, or extras beyond the current task.
2. **Do not assume.** If something is unclear, ask. Do not guess or hallucinate requirements.
3. **Read the spec first.** Check the relevant doc in `docs/` before writing or modifying any code.
4. **Tests before code.** Write or reference test cases before writing implementation.
5. **Docs are the source of truth.** If code and docs disagree, one needs updating.
6. **Specs before code.** Every system must be fully specified in `docs/` before implementation begins. No exceptions.

# Claude Code Instructions

All project guidelines and AI context live in [AGENTS.md](AGENTS.md).
Read that file for all rules, conventions, and game design references.

For detailed game design documentation, see the [docs/](docs/) folder.
For development progress tracking, see [docs/dev-tracker.md](docs/dev-tracker.md).

## Current Mode

**Docs and specs only.** No code until the user explicitly says otherwise. All work should be writing, completing, or refining documentation in `docs/`. Do not create C# files, scenes, or any game code.

**Language migration in progress:** The project has switched from GDScript to C# (.NET 8+). All docs are being updated to reflect the new stack. See [docs/architecture/setup-guide.md](docs/architecture/setup-guide.md) for environment setup.

## Critical Rules

1. **Stay in scope.** Do exactly what is asked. Nothing more. Do not add features, refactors, or extras beyond the current task.
2. **Do not assume.** If something is unclear, ask. Do not guess or hallucinate requirements.
3. **Read the spec first.** Check the relevant doc in `docs/` before writing or modifying any code.
4. **Tests before code.** Write or reference test cases before writing implementation.
5. **Docs are the source of truth.** If code and docs disagree, one needs updating.
6. **Specs before code.** Every system must be fully specified in `docs/` before implementation begins. No exceptions.

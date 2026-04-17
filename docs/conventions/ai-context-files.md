# Convention: AI Context Files

## Summary

This doc formalizes the pattern for any AI-tool-specific context file in this repo. The goal is to keep all AI assistants "in line" — working from the same source of truth — while still giving each tool a fast, focused entry point tuned to how it's used.

## The Context Chain

```
docs/ (source of truth)
   ↓
AGENTS.md (AI-readable index into docs/)
   ↓
<TOOL>.md (fast jump-index into AGENTS.md and docs/)
   ↓
AI output
```

**Precedence:** `docs/` > `AGENTS.md` > `<TOOL>.md`. Higher wins any conflict.

## Which Filenames Are Real

Each AI coding tool auto-loads a specific filename. We can only add tool-specific files for real conventions — verified from official docs. As of 2026:

| Tool | Filename(s) | Notes |
|------|-------------|-------|
| **Claude Code** | `CLAUDE.md` + `.claude/` memory dir | Project root. Auto-loaded into every session. |
| **Gemini CLI** | `GEMINI.md` | Project root + global at `~/.gemini/GEMINI.md`. Supports hierarchical loading (subdirectories). |
| **GitHub Copilot** | `.github/copilot-instructions.md` | Repository-wide. Also supports `.github/instructions/*.instructions.md` with glob patterns. Copilot code review reads only first 4,000 characters. |
| **Cursor** | `.cursor/rules/*.mdc` (current) | Legacy `.cursorrules` is deprecated but still supported. |
| **Aider** | `CONVENTIONS.md` + `.aider.conf.yml` | `CONVENTIONS.md` is loaded via `/read` or `--read`. Config file searched in home/repo/cwd. |
| **OpenAI Codex CLI** | `AGENTS.md` | Reads AGENTS.md natively. |

### About `AGENTS.md`

`AGENTS.md` is the closest thing to a cross-tool standard, stewarded by the Linux Foundation's Agentic AI Foundation. It's read natively by **OpenAI Codex CLI, GitHub Copilot, Cursor, Windsurf, Amp, and Devin**. **Claude Code and Gemini CLI do not read `AGENTS.md` natively** — they use their own files (`CLAUDE.md`, `GEMINI.md`). That's why this repo keeps `AGENTS.md` as the canonical source and mirrors a thin `CLAUDE.md` pointer into it.

If you're adding support for a new tool, **look up the tool's official docs first** and confirm the exact filename before creating a file. Do not guess.

## Rules

### 1. `AGENTS.md` is canonical.

All substantive guidance — paradigm, team structure, rules, conventions, workflow, tech stack, post-task protocol — lives in `AGENTS.md`. It is the **tool-agnostic** reference. Any AI tool can read it and work correctly on this repo.

### 2. `AGENTS.md` indexes `docs/`, it doesn't duplicate it.

Game design, systems, flows, world lore — all of that lives in `docs/`. `AGENTS.md` references it and synthesizes AI-workflow rules around it. When `AGENTS.md` describes behavior, it links to the relevant `docs/` spec.

### 3. Tool-specific `.md` files are navigation indices, not rulebooks.

`CLAUDE.md`, `GEMINI.md`, etc. exist because each tool auto-loads a specific filename. Their job is to help that tool navigate the canonical sources quickly — not to replicate content.

A tool-specific file should contain:

- **Header**: Explicit statement of the context chain and precedence.
- **Jump-to index**: A table mapping "I need to do X" to specific anchors in AGENTS.md and paths in `docs/`.
- **Quick commands**: Frequently used shell commands (`make test`, `make build`, etc.).
- **Hard rules reminder**: 5-8 lines of the most critical rules, linking to AGENTS.md for detail.
- **Precedence footer**: Restates that AGENTS.md wins, with instructions to add new rules there.

A tool-specific file should NOT contain:

- Any rule that isn't also in AGENTS.md.
- Duplicated spec content from `docs/`.
- Stale information that requires parallel updates to stay in sync.
- Tool-specific quirks that aren't essential (those go in tool config, not context files).

### 4. New rules go UP the chain, not down.

When you add a rule:

- **Game design rule** → `docs/systems/` or relevant `docs/` folder.
- **AI workflow rule** → `AGENTS.md`.
- **Tool-specific rule** → Only if the tool's quirks literally require it. Prefer AGENTS.md.

Never add a rule to `CLAUDE.md` / `<TOOL>.md` as a shortcut. Rules in tool-specific files become stale invisibly.

### 5. When a tool-specific file conflicts with AGENTS.md, fix the tool file.

If an AI tool reads its context file and gets different guidance than what `AGENTS.md` says, that's a bug in the tool file. Update it to match AGENTS.md (usually by removing the conflicting content and pointing to AGENTS.md instead).

### 6. Verify before adding.

Before creating a new tool-specific file:

1. Confirm the exact filename in the tool's official documentation.
2. Confirm the tool doesn't already read `AGENTS.md` natively (if it does, no new file needed).
3. Only then copy the template below and commit.

Do not guess filenames.

## Template for a New Tool-Specific File

Use this template when adding a new tool's context file:

```markdown
# <Tool> Instructions

**Context chain:** `docs/` (source of truth) → [AGENTS.md](AGENTS.md) (AI-readable index) → **<Tool>.md** (you are here — fast navigation) → AI output.

This file is a jump-index into AGENTS.md. Never answer from this file alone — always follow the chain back to the relevant spec in `docs/`.

## Jump-to Index (→ AGENTS.md sections)

| Need to... | Section |
|------------|---------|
| Understand the paradigm | [Paradigm](AGENTS.md#paradigm--read-this-first) |
| Follow the workflow | [AI Workflow Protocol](AGENTS.md#2-ai-workflow-protocol) |
| After making a change | [Post-Task Protocol](AGENTS.md#2a-post-task-protocol) |
| ... (add more rows as needed) | ... |

## Jump-to Index (→ docs/ — source of truth)

| Working on... | Spec location |
|---------------|---------------|
| Game systems | [docs/systems/](docs/systems/) |
| User flows | [docs/flows/](docs/flows/) |
| World/lore | [docs/world/](docs/world/) |
| ... | ... |

## Quick Commands

<Tool-relevant quick commands here>

## Hard Rules Reminder (full detail in AGENTS.md)

1. Stay in scope.
2. Read the spec first.
3. Specs are source of truth.
4. Tests before code.
5. Post-task protocol is non-negotiable.

---

**Precedence:** `docs/` > `AGENTS.md` > `<Tool>.md`. Higher wins any conflict. New rules go in AGENTS.md — not here.
```

## Current Tool-Specific Files In This Repo

| File | Tool | Purpose |
|------|------|---------|
| [CLAUDE.md](../../CLAUDE.md) | Claude Code | Jump-index for Claude Code CLI sessions |

When a new tool is onboarded, add it here after verifying the filename.

## Why This Pattern Works

- **Single source of truth.** `docs/` holds game specs. `AGENTS.md` holds AI workflow rules. No parallel rulebooks drift.
- **Fast context for each tool.** Each tool's entry file is tuned to how that tool loads context.
- **Consistency across tools.** Every AI assistant follows the same chain back to the same specs.
- **Easy to onboard new tools.** Verify filename → copy template → commit.
- **Failure-tolerant.** If a tool-specific file drifts, AGENTS.md and `docs/` are still correct.

## Sources

Verified against official documentation in 2026:

- [Claude Code docs](https://code.claude.com/docs)
- [Gemini CLI — GEMINI.md docs](https://geminicli.com/docs/cli/gemini-md/)
- [GitHub Copilot — Adding repository custom instructions](https://docs.github.com/copilot/customizing-copilot/adding-custom-instructions-for-github-copilot)
- [Cursor — Rules for AI](https://docs.cursor.com/context/rules)
- [Aider — Specifying coding conventions](https://aider.chat/docs/usage/conventions.html)
- [OpenAI Codex CLI Reference](https://developers.openai.com/codex/cli/reference)

## See Also

- [AGENTS.md](../../AGENTS.md) — the canonical reference
- [CLAUDE.md](../../CLAUDE.md) — current implementation of this pattern
- [docs/conventions/ai-workflow.md](ai-workflow.md) — the full AI workflow protocol
- [docs/development-paradigm.md](../development-paradigm.md) — why this repo is AI-built

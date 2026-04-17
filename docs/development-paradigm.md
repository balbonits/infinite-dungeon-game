# Development Paradigm: AI+Human Natural-Language Programming

## What This Project Is

This is a game, and it's also a real-world experiment in a new software development paradigm: **AI+Human natural-language programming**.

**The short version:**

- Every line of code in this repository is AI-written.
- Every pixel, sprite, icon, and tile is AI-generated.
- Every test is AI-written.
- Every spec and design doc is AI-written (from human-directed conversation).
- The human product owner has never written, edited, or debugged code in this repo.

The human directs in natural language. The AI executes — writing code, designing systems, generating art, writing tests, committing to git, and updating documentation. The AI is the entire engineering, QA, art, and DevOps team. The human is the client, the director, and the decision-maker.

## Why

We're exploring what happens when you take "vibe coding" — the casual, chat-driven interaction with AI — and push it to its limit: can you build a non-trivial, shippable game entirely through directed conversation? Not by writing code yourself and having AI autocomplete, but by specifying the game in English and letting the AI figure out the implementation.

Three reasons:

1. **It's the near-future of software.** Tools like Claude Code, Cursor, and others have reached a capability threshold where a skilled director can guide an agent through complex, multi-week projects. We want to know where this actually breaks.

2. **Game development has hard constraints.** Games require code correctness, visual design, game feel, performance, and a functioning end-to-end experience. A broken game is obvious in a way that a half-broken SaaS app is not. It's a fair stress test for the paradigm.

3. **Specs become source of truth.** When humans can't read the code fluently, the spec documents become the contract. If `docs/systems/combat.md` says a crit does 2x damage, the code must match — and the tests verify it. This flips the traditional relationship where code is truth and docs are a nice-to-have.

## How It Works

### The Human (Product Owner / Client)

- Directs in natural language: *"make the pause menu tabbed like Diablo 2"*, *"the keyboard nav is broken in the shop window — fix it"*
- Makes design decisions: *"Elemental, Aether, and Attunement — those are the three magic schools"*
- Approves or redirects outcomes: *"this feels off, try again"*
- Does not write, review, or debug code
- Does not open `.cs` files. Does not run commands beyond occasional shell utilities.

### The AI (The Entire Dev Team)

Multiple specialized AI agents, coordinated by a primary Claude Code session:

- **Design lead** — writes specs when asked to design systems
- **QA lead** — writes tests and reviews specs for gaps
- **DevOps lead** — configures CI, Makefile, project scaffolding
- **Art lead** — generates sprites, tiles, and animations via PixelLab
- **General-purpose agents** — for exploration, research, parallel work

The primary session:
- Reads specs before touching code
- Writes or references tests before writing implementation
- Updates docs when behavior changes
- Commits to git with conventional format
- Pushes to GitHub

### The Spec-First Loop

```
Human:  "Make the Ranger class feel more tactical and whimsical in tone."
           ↓
AI:     Reads docs/world/class-lore.md and docs/systems/skills.md
        Proposes design changes in conversation
           ↓
Human:  "Yes, and call that ability 'Tip Toes' — Rangers name things whimsically."
           ↓
AI:     Updates docs/world/class-lore.md and docs/systems/skills.md
        Writes tests in scripts/testing/tests/*.cs
        Implements changes in scripts/**/*.cs
        Runs: make build && make test
        Commits: "feat(ranger): rewrite ability names with whimsical tone"
        Pushes to main
           ↓
Human:  Opens the game, tests it, says "good" or "no, do it differently"
```

## The Rules We Follow

From `docs/conventions/ai-workflow.md`:

1. **Read the spec first.** No code until the relevant doc is read.
2. **Tests before or with code.** Integration tests verify spec behavior.
3. **Specs are source of truth.** If code and docs disagree, one must be updated.
4. **Stay in scope.** Do exactly what's asked. No extras, no refactors, no "while we're here."
5. **Don't assume.** If unclear, ask in conversation before coding.
6. **Docs maintained in commit.** Every behavior change updates the corresponding spec.

## What This Produces

### Code Quality

C# code, Godot 4, ~90 scripts. Structured with: autoloads, pure-logic separation (testable with xUnit, no Godot runtime required), dedicated UI framework (`GameWindow` base class, `GameTabPanel`), and constants centralization.

### Tests

- **Unit tests** (xUnit, ~374): pure C# logic — skills, inventory, stats, save/load data, achievements. No Godot dependency. Run in milliseconds.
- **Integration tests** (xUnit, ~11): cross-system flows. Still pure C#.
- **E2E tests** (GdUnit4, ~34): scene loading, asset validation, system verification.
- **In-game UI tests** (GoDotTest + GodotTestDriver): keyboard navigation, window lifecycle, pause state, focus management. Runs the game headless, drives it with simulated keyboard input, verifies the UI behaves per spec.

### Docs

~50 markdown files: systems, world lore, flows, conventions, reference. The design docs are primary — they define behavior. The dev journal logs every session's work. The changelog tracks every behavior change.

### Art

All sprites generated via PixelLab (AI art tool) — characters, enemies, tiles, projectiles, UI icons. The AI art-lead agent writes the prompts, manages the asset pipeline, and names files consistently.

## Two-AI Code Review (Claude ↔ Copilot)

A novel part of this workflow: the primary dev (Claude Code) publishes PRs and a second AI (GitHub Copilot's PR reviewer bot) reviews them asynchronously. The human directs but doesn't mediate the technical review.

The loop:

1. Claude pushes a commit to a PR. The branch ruleset's `copilot_code_review` rule auto-triggers a Copilot review (no manual step).
2. Copilot posts inline review comments — findings on specific lines — within 30-90s.
3. Claude verifies each finding by tracing the codebase and checking primary-source docs (Godot official docs, NuGet package READMEs, GitHub docs). No silent trust. See [`docs/conventions/work-discipline.md`](conventions/work-discipline.md) "External AI Feedback" for the verification protocol.
4. Claude replies threaded under each Copilot comment — either *"verified the fix — commit \<sha\>"* or *"verified this claim is incorrect because X, keeping as-is"* — creating an audit-trail dialogue.
5. Claude force-pushes the fix. Copilot re-reviews. Repeat until clean.
6. The human reads the final PR state (or not) and approves the merge.

Why this matters: **no single AI is the source of truth.** Claude's implementation is checked by Copilot's review; Copilot's review is checked by Claude's verification against primary sources. Errors from either side get caught before landing on main.

Failure modes to watch for:
- **Amplification** — one AI's confident-wrong output becoming another AI's confident-wrong fix. Mitigated by the "primary-source verification" rule (Rule 3 of External AI Feedback).
- **Sycophancy** — one AI rubber-stamping the other's suggestions to be agreeable. Mitigated by documenting rejected claims explicitly in the PR thread and journal.

The PR threads are public and permanent. Anyone can read the full dialogue: [github.com/balbonits/infinite-dungeon-game/pulls?q=is%3Aclosed](https://github.com/balbonits/infinite-dungeon-game/pulls?q=is%3Aclosed). Session 20 in the dev journal walks through a concrete example.

## What We've Learned So Far

### Works Well

- **Specs-first is real.** When the spec exists and is clear, the AI produces good code. When the spec is vague, the code is vague.
- **Decomposition via agents.** Delegating research, art, and QA to specialized agents scales the work in parallel.
- **Automated tests are non-negotiable.** Without them, the AI can't verify its own work and behavior drifts silently.
- **Conventional commits + changelogs.** Machine-readable history makes every change auditable.
- **Two-AI review catches what one misses.** Claude's verification-against-primary-sources has caught one Copilot hedged/incorrect claim (see Session 20); Copilot has caught multiple Claude architectural bugs. Neither alone would have caught both.

### Failure Modes (and guards)

- **Scope creep.** AI loves to "improve" adjacent code. Mitigated by the "stay in scope" rule and frequent human course-correction.
- **Hallucinated APIs.** AI invents method names that don't exist. Mitigated by reading actual NuGet package docs and checking by grep before use.
- **Confident wrongness.** AI claims something works without testing. Mitigated by hard rules: run `make build`, run `make test`, run the game in a browser/executable before claiming done.
- **Memory loss across sessions.** Claude Code compresses old context. Mitigated by the dev journal, changelog, and spec docs — the AI can re-read them fresh each session.

## Reading the Repo

If you're a developer looking at this code:

- Start with `docs/overview.md` (what the game is).
- Read `docs/dev-journal.md` for the narrative history.
- Read `docs/conventions/ai-workflow.md` for the process.
- Browse `docs/systems/*` for the game design specs (these drive implementation).
- Look at `scripts/testing/tests/*.cs` for examples of how tests verify specs.
- Every commit message is written by AI. The human wrote zero commit messages.

If you're a researcher interested in AI-driven development:

- Every session's work is logged in `docs/dev-journal.md`.
- Every human-AI interaction is preserved in Claude Code session files (not committed to git).
- The `CHANGELOG.md` is an auditable record of what changed when.
- Look for failure patterns: session entries that describe bugs, misunderstandings, course corrections.

## A Note on Authorship and Attribution

This repository was directed by a human who is not a software engineer. Every line of code, every test, every document, every sprite — all AI-generated — was nonetheless shaped by hundreds of small decisions: which directions to pursue, which designs to reject, what feels right, what doesn't. The AI is powerful but not autonomous. Without continuous human direction, it drifts.

The human is Claude-Code's **director**. The AI is its **hands**. Neither works alone.

We publish this repo publicly because we think the paradigm is worth examining — its strengths, its failure modes, what it produces, and what it can't do (yet).

---

**Last updated:** 2026-04-17.
**Primary AI:** Claude Opus (various versions across sessions).
**Tools:** Claude Code, PixelLab, Godot 4 + C#.

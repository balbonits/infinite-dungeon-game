# Copilot PR Review Instructions

## Project at a Glance

This is a **spec-driven** Godot 4 + C# game — **"A Dungeon in the Middle of Nowhere"**. Every behavior is specified in `docs/` before being implemented. The code is AI-written, the specs are AI-co-authored with a human product owner, and PRs go through a two-AI review loop (Claude as author, Copilot as reviewer).

**Canonical context to consult when reviewing:**
- [`AGENTS.md`](../AGENTS.md) — working conventions, tech stack, PR workflow, naming rules
- [`docs/development-paradigm.md`](../docs/development-paradigm.md) — the AI+Human natural-language programming paradigm this repo is an experiment in
- [`docs/conventions/work-discipline.md`](../docs/conventions/work-discipline.md) — verification discipline (including how Claude verifies your feedback before applying it)
- [`docs/systems/*.md`](../docs/systems/) — game system specs (behavior, formulas, balance)
- [`docs/flows/*.md`](../docs/flows/) — user-facing flow specs (splash screen, death, combat, etc.)

## What to Flag (high signal)

1. **Code that diverges from an existing spec in `docs/`.** If a function's behavior contradicts its corresponding spec, that's the most valuable finding you can surface. Cross-reference `docs/systems/` and `docs/flows/`.
2. **Regressions** — behavior that worked in prior commits on `main` but was broken by this PR.
3. **Resource leaks, thread-safety issues, un-disposed Godot nodes**, signal-connection mismatches.
4. **Missing test coverage** for new critical paths, especially UI flows and save/load data.
5. **Security-sensitive code** — save file parsing, input injection in tests.

## What NOT to Flag (false positives from prior reviews)

- **"Magic numbers" that come from specs.** A cinematic timing of `6.3s` isn't a magic number — it's specified in `docs/flows/death.md` as 5 phases summing to 6.3s. Check the spec before flagging literals.
- **Single-commit PRs with force-push history.** This repo uses squash-merge + single-commit branches intentionally (see `AGENTS.md` §2b). Don't flag "this branch has been force-pushed" as a concern.
- **The `Copilot` (capital C) vs `copilot-pull-request-reviewer[bot]` login distinction.** These are the correct GitHub API logins for inline comments vs top-level reviews respectively, verified via live API query. Don't flag code handling them separately as a bug.
- **`WindowStack` owning the pause lifecycle.** This is intentional per `docs/dev-journal.md` Session 20 — the per-window `_wePaused` approach was tried and rejected because it broke chained-modal scenarios. Don't suggest reverting.
- **Deliberately incomplete files marked "Coming Soon" or spec-deferred.** E.g., the Equipment tab in `PauseMenu.cs` is intentionally a placeholder until `SYS-11` lands.
- **Style disagreements with Godot conventions.** This repo follows the Godot C# style guide (partial classes, PascalCase, `[Signal] ... EventHandler`). Don't suggest style that contradicts `AGENTS.md` §4 (C# Conventions).

## Review Cadence

This repo configures automatic Copilot review on every push to a PR targeting `main` (branch ruleset `copilot_code_review` rule). Reviews are welcome on:
- **Quick-fix PRs** (1-2 commits, 1-3 files): each push
- **Major-feature draft PRs** (ongoing work): each meaningful-milestone push

If a PR is still in draft, it's intentionally work-in-progress. Note incomplete-ness as context, not as bugs.

### Docs-only PRs — one review pass, then done

When a PR touches **only** prose/spec files — `docs/**`, repository `*.md` files, or `.github/copilot-instructions.md` — and does **not** modify code or automation/config files such as `.cs`, `.tscn`, `.csproj`, `Makefile`, workflow `.yml`, `.github/rulesets/**`, or any other behavior-affecting config under `.github/`:

- **Single review pass total.** Don't request follow-up reviews on prose iteration. Claude's workflow (`docs/conventions/ai-workflow.md` §10b-postscript) treats a docs PR's first review as the final review.
- **Flag only substantive findings:** factual errors, internal contradictions, broken cross-doc references, ambiguity that would block downstream implementation. Skip prose nits, wording preferences, and reorganization suggestions.
- **No "consider rewording X to Y" comments unless the original is genuinely ambiguous or contradictory.** Wordsmithing wastes review cycles on text that isn't load-bearing.
- **Cross-doc consistency is the highest-value check.** If a spec change in this PR contradicts another spec in `docs/`, flag the conflict explicitly. That's the single most useful thing reviews can surface for docs.

User direction (2026-04-17): *"can we just have one pass of copilot review for the specs/docs, then we ignore the rest. it doesn't make sense to spend hours on words that's not code."*

### SPEC-prefixed PRs — documentation only, never code

PRs and tickets whose ID starts with `SPEC-` (e.g. `SPEC-PC-ART-01`, `SPEC-SPECIES-01`, `SPEC-MAGICULE-DENSITY-01`, `SPEC-RECONCILE-BRACKETS-01`, etc.) are **work definition only**. They produce or modify documentation in `docs/` and **never modify code, scenes, csproj, or build config**. This is by design.

**For Copilot reviewing a SPEC-* PR:**

- Treat it identically to "Docs-only PRs" above (single review pass, substantive findings only).
- **Do NOT flag** missing C# implementation, missing scene wiring, missing tests, missing Godot signals, or "this spec doesn't ship code." That's the entire point — implementation lives in a separate downstream ticket (e.g. SPEC-LOOT-01 → LOOT-01 impl ticket; SPEC-PC-ART-01 → ART-PC-REDRAW impl ticket).
- **Do NOT suggest** that the PR also include the corresponding code changes. The two-step pattern (spec lands first → impl ticket lands later) is intentional and protects against re-implementing while specs are still in flux.
- **DO flag** spec-vs-spec contradictions, missing acceptance criteria, ambiguous values an implementer would need to guess at, broken cross-references to other docs, and named-IP leakage in art-pipeline prompts (per `docs/assets/prompt-templates.md` §11 IP Protection).

User direction (2026-04-18): *"inform/instruct copilot that 'SPEC' PRs are just documentation and work definition. we won't be coding with these tickets."*

The downstream implementation ticket (named without the `SPEC-` prefix — e.g. `LOOT-01`, `COMBAT-01`, `ISO-01`, `ART-PC-REDRAW`) is where code review actually applies.

### Tracking files — do NOT review

Two files are tracking/bookkeeping only and should be skipped entirely during review:

- **`docs/dev-tracker.md`** — the running ticket registry. Rows get added, statuses change, notes get rewritten as work lands. No content here is "load-bearing design" — it's a dashboard.
- **`docs/dev-journal.md`** — chronological work log. Entries are retrospective narrative, not a spec. Deliberately long-form for future retrospectives, not a review target.

**For Copilot reviewing a PR that touches these files:**

- **Skip them.** Do not flag changes, typos, wordsmithing, formatting, internal inconsistencies, or "this row should link to X" concerns in either file. Treat them as scratchpads the humans maintain.
- This includes common findings like: "this row is out of zone order," "the journal entry from Phase X contradicts the phrasing in Phase Y," "the status in tracker row N differs from the roadmap." These are non-issues — both files are eventually-consistent by design.
- Still-in-scope for review: `docs/spec-roadmap.md` (the prioritized next-work list is load-bearing), every `docs/systems/**`, `docs/world/**`, `docs/flows/**`, `docs/inventory/**`, `docs/conventions/**`, etc.

User direction (2026-04-18): *"let Copilot know not to review `dev-tracker` & `dev-journal`. it's not something to review."*

## If Something Doesn't Look Right

We take your findings seriously — Claude verifies each one against the codebase and primary-source docs (Godot docs, NuGet READMEs) before acting, per the work-discipline convention. Rejected claims get threaded replies with the verification evidence so future reviews have that context.

If multiple reviews in a row miss project-specific context (flagging correct spec-driven behavior as wrong), this repo has a documented kill-switch (`AGENTS.md` §2b) to disable automatic Copilot review. We'd rather have no review than a confidently-wrong one.

---

**For the canonical rulebook**, always defer to `AGENTS.md` and the linked `docs/` files above. This file is a short lens pointed at them — not a replacement.

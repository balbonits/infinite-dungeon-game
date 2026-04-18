# AI Workflow Protocol

## Summary

This document defines how AI assistants must work on this project. It codifies lessons from [spec-driven development](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development), [Claude Code best practices](https://code.claude.com/docs/en/best-practices), and [real-world AI development failures](https://towardsdatascience.com/the-black-box-problem-why-ai-generated-code-stops-being-maintainable/). Every rule exists because its absence caused bugs, wasted effort, or scope explosions in AI-assisted projects.

## Current State

This protocol is active. All AI-generated code in this project must follow this workflow.

## User Role — Hands-Off on Implementation, Active on Discussions & Specs

**Per the user's 2026-04-17 directive:** *"i want to be hands-off on any implementation, i'm only active for discussions & specs."*

**AI handles autonomously (no approval gate, just brief or update):**

- All implementation: code changes, refactors, bug fixes, test writing, build/lint/CI fixes, dependency updates, infrastructure work.
- All routine PR mechanics: branch creation, commits, pushes, Copilot review cycles, in-scope finding fixes, merges (after the §10b brief).
- All housekeeping: dev-tracker / dev-journal / changelog updates, docs sync, file moves, ticket creation/closure.
- Out-of-scope findings: file as a new ticket, name it in the brief, keep moving.

**Bring the user in (active participation required):**

- **Spec design** — any new spec doc, or an existing spec needing a design decision (e.g., ITEM-02b quiver channel, FORGE-01 gating). Use the multiple-choice format the user prefers (lettered options with `[rec]` annotations). Don't ship spec writes without their call on the open questions.
- **Cross-system trade-offs** — anything that changes contracts the user explicitly approved earlier (combat formulas, save format, NPC roster, class identity).
- **Scope decisions** — when a finding could be its own ticket vs bundled into the current PR, default to "its own ticket" but surface the trade if non-obvious.
- **Substantive in-scope review findings** — never wave these off; either fix or surface a clear reason not to.

**Don't bring the user in for:**

- "Ok to merge?" after the §10b brief — the brief satisfies the consent requirement (see §10b).
- "Should I run the tests?" — yes, always, before commit.
- "Which test framework?" — pick per `docs/conventions/` (xUnit for pure-logic units, GoDotTest for in-game keyboard nav, GdUnit4 for scene/asset tests).
- "What name for this branch?" — follow the existing convention (`fix/audit-NN-short-tag`, `feat/system-name`, `docs/topic`).
- Any choice the existing docs already specify.

The mental model: the user is the **product owner and chief designer**, not a code reviewer. Their attention is for discussions and specs; their absence is the default state during implementation.

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

**10a-prelude. One branch at a time, parallelize files within it.**

Solo dev (you, the AI) gets zero parallelism benefit from multiple in-flight branches and pays the full concurrency cost: rebase conflicts on shared files (especially `docs/dev-tracker.md`), split attention across async Copilot rounds, and a multi-PR brief the user has to parse instead of one PR's-worth of context.

**Within one branch, freely edit multiple files in parallel** — a feature that touches code + spec + tests is one branch, not three.

**Across branches, work strictly serial:** pick one branch, take it from creation through Copilot-clean review through merge, **with 100% focus and intent**, then start the next. Context is lost between runs; mid-PR state does not survive a session boundary, so half-finished branches are dangerous. Finish atomically.

**Exceptions:** if a previously-pushed PR is genuinely waiting on something external (Copilot round, user review, agent revising in background), it is acceptable to draft the *next* branch's work locally — but DO NOT push it, request its review, or open the PR until the prior one is merged. Local commits or stash to preserve in-progress work are fine.

User feedback (2026-04-17): "don't work on multiple PR's, go at it one by one"; "work on parallel files, not parallel branches"; "complete a branch/PR with 100% focus & intent. you'll be losing context on the next run."

**10a-postscript. Do not auto-`/compact` or `/clear` between PRs. The user owns that decision.**

Never invoke `/compact` or `/clear` on your own initiative, and do not prompt the user with "free moment for `/compact`?" between PRs. The user will run them when they want to. If the user has already explicitly provided the next ticket in the same session, continue on the same context without suggesting a context reset; otherwise, follow §11 and wait for user direction.

Original guidance (2026-04-17, now superseded) treated post-PR compact/clear as a non-negotiable step. That added friction without clear benefit — the user reverted it the same day: "scrap the `/compact`/`/clear` process/order. we'll clear/compact only when we want to."

**10a-pre-push. 30-second self-review before every push to cut Copilot rounds.**

Each avoidable Copilot round costs ~5–15 minutes of wall time (review wait + fix + push + re-request + re-poll). On AUDIT-02 (PR #16), 4 rounds shipped because R1–R3 each caught a real bug a 30-second self-review would have caught:

- **R1** — Toast lived under `Main`'s scene tree; calling `ReloadCurrentScene()` immediately after `Toast.Show` tore it down before render. (Scene-lifetime miss.)
- **R2** — `?? true` defaulted "missing SaveManager autoload" to "successful save," silently bypassing the failure toast. (Null-default semantics miss.)
- **R3** — adding a 3-second timer between failure and reload meant the world resumed during the delay because `Close()` unpauses and `Paused = false` was pre-branch. (Pause-state-across-delays miss.)

Before every push, walk each modified file with these four checks:

1. **Scene lifetime** — for any `ReloadCurrentScene` / `QueueFree` / `ChangeScene` call, does any node you just touched survive it? If not, defer or move it to a persistent layer.
2. **Null-default semantics** — for every `?? value` and `?.method()` chain, ask: when the LHS is null, is `value` (or the no-op) the *correct* outcome the user expects, or just the first thing that compiles?
3. **Pause state across delays** — for any `await`, `SceneTreeTimer`, or callback-after-delay, ask: what's the tree's pause state during the delay? Is that intended?
4. **Symmetric callsites** — if you fix a pattern at one callsite, grep for the same pattern; fix all matches in the same PR (don't ship a half-fix Copilot will flag as inconsistency).

User feedback (2026-04-17): *"we have to make the process faster, more efficient. an audit ticket shouldn't take 2-3 hours due to code reviews. make sure to learn & improve by every commit & push. record and reference, keep learning & cutting excess, be more efficient."*

**10a. Pair every PR push with a Copilot review request and a polling cron — atomic step.**

Every `git push` to a PR branch is incomplete until all of these happened in the same response:

1. `make pr-copilot-request PR=N` — the ruleset's auto-review trigger is unreliable (especially on force-push; confirmed PR #5, Session 21). Do the manual request as the source of truth.
2. `make pr-copilot-wait PR=N` started in background — do NOT skip and "wait for the notify-claude workflow." The notify workflow has gating issues; the explicit poller is what closes the loop.
3. **Verify the poller actually entered its loop, not just that the shell launched.** PR URL returned + background task ID returned only confirm the shell process started. After kicking off the background poller, immediately read its output file (`/private/tmp/claude-501/.../tasks/<task-id>.output`) and confirm the first `Waiting for new Copilot review on PR #N (current count: X)...` line is present. If it's missing, the script crashed silently before its main loop — and silent-crashed background processes do NOT fire completion notifications, so you'd wait forever for a signal that never comes. Restart and investigate before scheduling the wakeup.
4. **Cap the wait at 20 minutes total.** The Make target's built-in 10-min loop is too short. Either run two consecutive 10-min waits, or schedule a wakeup at 20 min to kill the poller if no review has landed.
5. **On 20-min timeout:** kill the poller, surface the failure to the user, and decide — re-request the review (Copilot may have dropped it) or proceed without (with explicit user OK). Do not let a stalled poller linger.

The mental shortcut is "you didn't push if you didn't poll." This is one reflex, not three steps performed at different prompts.

**10b. Pre-merge briefing is mandatory — but the brief is the heads-up, not a request for permission.**

Before running `gh pr merge` on any PR — even under auto mode, even on a "ship it" instruction — post a brief. The brief must include:

1. **What's shipping** — one-sentence summary per major ticket/system (not the raw commit list).
2. **Open review findings** — every substantive Copilot/human comment, classified (bug vs nitpick vs out-of-scope) with a one-line explanation. **If anything is substantive AND in scope, fix it before merging.** A follow-up ticket IS a valid resolution for out-of-scope findings (pre-existing behavior, unrelated subsystem, etc.) — file the ticket, name it in the brief, then merge.
3. **CI state** — explicit note if CI is red, and the reason (known-broken workflow vs real test failure). Do not paper over red CI silently. Known-broken infrastructure (e.g., AUDIT-15 Coverage Gate) with a tracking ticket is OK to ship over.
4. **Deferred follow-ups** — anything being punted, stated by ticket ID.

**Then merge.** Don't ask "ok to merge?" after the brief — the brief itself satisfies the informed-consent requirement; if everything in it is clean (substantive findings actioned, real test failures absent, deferrals ticketed), proceed. The user reads the brief and reacts only if something is off. This is the same model as a stand-up status update, not an approval gate.

**Block on explicit user OK only when:**
- A substantive in-scope finding was waved off without fix
- CI red is from a real test failure (not pre-existing infra)
- The PR introduces shared-state risk the brief surfaced as a concern
- The user has previously expressed reservation about this specific change

Auto mode does NOT override "do not ship known-bad code." But it DOES mean: brief, then merge. Don't sit on a merge waiting for an OK that the rules don't actually require.

**10b-postscript. Docs-only PRs: one Copilot pass, then ship.**

For PRs that touch **only** prose/spec files — `docs/**`, repository `*.md` files, or `.github/copilot-instructions.md` — and do **not** modify code or automation/config files such as `.cs`, `.tscn`, `.csproj`, `Makefile`, workflow `.yml`, `.github/rulesets/**`, or any other behavior-affecting config under `.github/`, take **one** Copilot review pass. Address its substantive findings (factual errors, internal contradictions, broken cross-doc references) in a single fix commit if needed, then merge. Do NOT request a second Copilot round; do NOT iterate prose multiple times.

Code PRs keep the multi-round protocol above — each round can catch a real bug. Docs PRs don't have the same payoff; a second round on a spec usually surfaces nits, not safety issues.

The companion config `.github/copilot-instructions.md` instructs Copilot to be brief on docs-only PRs (skip prose nits, flag only substantive issues) so the single review is high-signal.

User direction (2026-04-17): *"can we just have one pass of copilot review for the specs/docs, then we ignore the rest. it doesn't make sense to spend hours on words that's not code."*

**Why these exist:** PR #8 merged on 2026-04-17 with seven substantive Copilot bugs unaddressed (floor-100 boundary, thematic biases, Load-Game stranding, focus no-op, button-zone navigation, GameState.Reset save-slot clobber, silent-no-op test). The user had to flag both the silent merge AND the bad code. PR #9 was the triage. The rule is "brief always, fix in-scope substantives always" — not "wait for explicit user merge approval on every PR." The user's 2026-04-17 follow-up: *"i don't think y'all need to wait for my call to merge. last time, you merged when even copilot flagged it with issues. so, i reacted."*

**10c. Don't ask the user for manual GitHub-UI dispatch / approval.**

When a workflow shows `action_required`, a stalled approval gate, or any "Approve and run" prompt, do not surface it as "you'll need to approve...". Either route around it (background poller via `gh api`, alternate trigger, scheduled wakeup) or fix the underlying config (workflow `permissions:` block, branch protection, fork-PR policy) — propose a PR for the fix, don't ask the user to flip the switch in the GitHub UI. The user's expectation is that automation actually automates.

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

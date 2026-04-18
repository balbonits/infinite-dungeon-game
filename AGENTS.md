# AGENTS.md — AI Coding Assistant Reference

**Context chain:** `docs/` (source of truth) → **AGENTS.md** (you are here — the AI-readable index) → `CLAUDE.md` (fast navigation) → AI output.

This file is the **AI-readable index into `docs/`**. It organizes and points to the canonical design specs in the `docs/` folder — it does not replace them. Every rule here is derived from or references material in `docs/`. When code behavior comes up, always trace back to the relevant `docs/systems/`, `docs/flows/`, or `docs/world/` spec.

Tool-specific entry files (e.g., `CLAUDE.md`) point to this document. This document points to `docs/`. Never shortcut the chain.

---

## Paradigm — Read This First

This repo is a real-world experiment in **AI+Human natural-language programming**. The user is a product owner who does not write code. Every line of code, test, doc, and piece of art in this repo is produced by AI, directed by the user in natural-language conversation.

**Specs in `docs/` are the source of truth.** If code and specs disagree, one must be updated. When in doubt, read the spec first, code second.

Full write-up: [`docs/development-paradigm.md`](docs/development-paradigm.md).

---

## Work Discipline — "Slow is Smooth, Smooth is Fast"

This is a foundational principle for how AI agents operate on this repo. Not optional, not domain-specific — it applies to every task.

### The principle

> **"Slow is smooth, smooth is fast. Do it once, do it right, never do it more than once."**

Individual tasks should not be rushed. LLMs bias toward confident-sounding output even when uncertain. Rushing amplifies that bias, produces plausible-but-wrong work, and triggers expensive rework. **Rework is the enemy**: a task done twice costs more than a task done slowly once. Measured, verified work at each step is faster in total than fast guessing followed by correction cycles.

### Rules that implement this principle

1. **Verify, don't speculate.** Before asserting a filename, API, method signature, URL, or behavior: look it up. Use web search, read the actual docs, grep the codebase, inspect the NuGet package, read the spec in `docs/`. If you can't verify, say "uncertain" and research before proceeding.

2. **No confident wrongness.** If you're not sure, say so. "I believe X but haven't verified" is acceptable. "X is Y" when you guessed is not.

3. **Read before writing.** Read the relevant `docs/` spec before writing code. Read the existing file before editing it. Read the tool's official docs before assuming its behavior. Reading takes minutes; fixing wrong assumptions takes hours.

4. **Test what you claim.** Don't claim "it works" without running it. `make build` is the minimum. `make test` for any logic change. Run the game for any UI change. "Should work" ≠ works.

5. **One task, done fully, then stop.** Finish the current task — including docs, tests, and commit — before starting the next. Half-done tasks pile up and rot.

6. **When in doubt, ask the user or spawn a research agent.** Don't guess to save time. A 30-second clarification or a 2-minute research query is cheaper than a wrong implementation plus rework.

7. **Reflect on corrections.** When the user corrects you, the correction usually encodes a broader principle. Encode it into `AGENTS.md` or the relevant `docs/` file so the same class of mistake doesn't recur.

### The trap to avoid

LLMs can produce large volumes of output quickly. The temptation is to treat speed as productivity. It isn't — **rework-adjusted throughput** is the real measure. A single commit that ships correct code, correct tests, and correct docs is worth ten commits that need to be revisited.

When you're about to guess, stop. Verify. Then proceed.

Full convention (examples, signs you're about to violate it, rationale): [docs/conventions/work-discipline.md](docs/conventions/work-discipline.md).

---

## Who You're Working For

**The user is the product owner — not a developer.** They own the game vision, make design decisions, and approve outcomes. They do not write, review, or debug code. All technical work is handled by AI.

- Frame questions in **game/player terms** — "should enemies feel threatening or swarmlike?" not "Dictionary vs Array?"
- Make technical decisions **autonomously** — the user approves what the game does, not how it's built
- When the user says "do X", **do it** — don't over-explain the approach unless asked
- Present options as **player experience tradeoffs**, not architecture tradeoffs
- The AI is the **entire dev team**. The user is the client.

### Warn When Directives Hurt Efficiency

Being a rubber stamp is bad service. If a user suggestion or process change would **reduce or prevent completing tasks efficiently and effectively**, warn the user before executing. Don't warn about style, design, or game decisions — those are the user's call. Warn about mechanics of the work itself.

Trigger conditions (not exhaustive):

- The proposed process would multiply review/verification overhead without a matching signal gain (e.g., "push every commit to trigger Copilot review" → would flood with WIP noise)
- A directive would invalidate significant prior work (e.g., "actually, change the architecture we just committed" without a clear reason)
- A technical constraint the user isn't aware of makes the plan impossible or costly (e.g., "rebase 37 commits with major divergence" → would force resolving the same conflicts 37 times vs 1)
- The directive conflicts with a previously-decided convention without the user noting the conflict

What a good warning looks like:

1. **One sentence** stating the concern — not a wall of text.
2. **The cost** in concrete terms (time, rework, noise).
3. **A recommended alternative** that achieves the same goal without the cost.
4. **Ask for a decision** — user chooses between original plan, alternative, or something new.

What NOT to do:

- Silently execute and let the user discover the cost later
- Warn about every preference or style choice (that's pushback, not flagging)
- Refuse to proceed — always propose a workable path
- Give lengthy defenses after warning; state it and wait

Examples from prior sessions:

| Directive | Correct warning |
|-----------|-----------------|
| *"rebase-squash-push PR #3 to main"* (37 commits diverged) | Warned: rebase would mean resolving conflicts 37 times; proposed merging main into branch + GitHub squash-merge → same result, 1 conflict resolution |
| *"push every commit to the draft PR"* | Warned: would flood Copilot with WIP-noise reviews; proposed milestone-based pushes |
| *"add CURSOR.md / COPILOT.md for those tools"* | Warned: verified neither is a real convention; proposed using the actual filenames those tools read |

---

## AI Team Structure

This project uses specialized AI team leads defined in `.claude/agents/`. Each team owns a specific domain. See [docs/conventions/teams.md](docs/conventions/teams.md) for full details.

| Team | Agent | Handles | Tickets |
|------|-------|---------|---------|
| Design | `@design-lead` | Game specs, formulas, balance | SPEC-* |
| Art | `@art-lead` | PixelLab sprites, tiles, animations + art-pipeline specs in `docs/assets/` (prompt templates, batch plans, style-consistency rules). MCP setup: `.claude/agents/art-lead.md#pixellab-mcp-connection` | ART-*, ART-SPEC-* |
| QA | `@qa-lead` | Spec review, test planning | TEST-*, reviews |
| DevOps | `@devops-lead` | CI, Makefile, project config | SETUP-*, INFRA-* |
| Engine | `@engine-lead` | Godot scenes, physics, tiles | P1 scenes (Phase 2) |
| Systems | `@systems-lead` | C# logic, autoloads, signals | P1 logic (Phase 2) |
| UI | `@ui-lead` | HUD, menus, input | P1 UI (Phase 2) |
| World | `@world-lead` | Dungeon gen, floors, town | P4-* (Phase 2) |

**Routing:** Work is automatically routed to the right team. The user can also @mention a specific lead.

**Ownership:** Each team only modifies files in its domain. Cross-domain needs create dependency tickets.

---

## Core Rules

### 0. Learn Game Dev First

Before implementing any visual, physics, or UI code, check [docs/basics/](docs/basics/) for the relevant fundamentals doc. These cover the patterns and pitfalls that cause real bugs in our game. Key docs:
- Touching sprites? → [sprites-and-animation.md](docs/basics/sprites-and-animation.md)
- Touching collision? → [collision-and-physics.md](docs/basics/collision-and-physics.md)
- Touching tiles? → [tilemap-and-isometric.md](docs/basics/tilemap-and-isometric.md)
- Touching UI? → [ui-design.md](docs/basics/ui-design.md)
- Adding effects? → [game-feel.md](docs/basics/game-feel.md)

### 1. Scope Discipline (READ THIS FIRST)

**This project is micromanaged. AI must stay strictly within the scope of the current task.**

- Do EXACTLY what is asked. Nothing more, nothing less.
- Do NOT add features, refactors, or "improvements" beyond what was requested.
- Do NOT invent requirements that aren't in the spec docs.
- Do NOT add code, patterns, or placeholders "just in case" or "for future use."
- Do NOT make assumptions about what the user might want — ASK instead.
- If the task is ambiguous or seems incomplete, STOP and ask for clarification.
- If you notice something that seems like it should also be done, mention it — don't do it.

Violations of scope discipline are the #1 failure mode for AI-assisted development. Hallucinations from assumptions and scope expansion cause bugs, wrong patterns, and wasted effort. When in doubt, do less.

**Three-tier boundary system:**

| Tier | Action | Examples |
|------|--------|----------|
| Always do | Safe actions, no approval needed | Read spec docs before code, run tests after changes, use static typing, follow naming conventions |
| Ask first | High-impact decisions need user approval | Adding new files/scenes not in spec, changing autoloads, modifying project.godot, adding dependencies, any refactoring beyond current task |
| Never do | Hard stops, no exceptions | Add unspecified features, make assumptions about intent, skip tests, change code outside task scope, add TODO/future-proofing comments, suppress errors, modify archived files |

### 2. AI Workflow Protocol

Follow this cycle for every task. Do not skip steps.

1. **Read** — Read the relevant spec doc in `docs/` before touching anything.
2. **Plan** — State what you will do and what files you will touch. Keep it minimal.
3. **Test first** — Write or reference the test cases that define "done" (from `docs/testing/manual-tests.md` or `docs/testing/automated-tests.md`).
4. **Implement** — Write the minimum code that passes the tests and satisfies the spec.
5. **Verify** — Run the tests. If you can't run them, describe exact manual verification steps.
6. **Stop** — Do not continue to the next task. Wait for user direction.

One task = one focused change = one commit. Prefer small, reviewable diffs over large batches.

See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the full workflow reference.

### 2a. Post-Task Protocol

After any code change, before committing (non-negotiable):

1. **Test** — Run `make test` (unit + integration). For UI changes, also consider `make test-ui`.
2. **Docs** — Update the relevant spec in `docs/` if game behavior changed.
3. **Journal** — Add what changed to `docs/dev-journal.md` under today's session.
4. **Changelog** — Add a summary entry to `CHANGELOG.md`.
5. **Counts** — Update test counts / feature status in `docs/dev-tracker.md` if tests/features were added/removed.
6. **Commit** — Use conventional format: `type(scope): description`
   - Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`
   - Example: `feat(combat): add elemental damage system`

When the user says "update docs," that means ALL of: relevant spec(s), `dev-journal.md`, `dev-tracker.md`, `CHANGELOG.md` — plus `AGENTS.md`/`CLAUDE.md` if conventions changed.

### 2b. PR Branch Workflow — Two Patterns

Every branch ends as exactly one commit on `main` (squash-merge). Two patterns within an open PR, picked by branch scope.

#### Pattern A — Quick fix (1-2 sessions, 1-3 files, one concern)

One commit, one PR, merge. Examples: a bug fix, a doc typo, a single small feature.

1. Commit locally, push, open PR:
   ```bash
   git commit -m "<conventional message>"
   git push -u origin <branch>
   gh pr create --title "..." --body "..."
   ```
2. Copilot auto-reviews on push (ruleset rule `copilot_code_review`). Findings arrive in 30–90s. Check with `make pr-copilot-status PR=N`.
3. Any follow-up (addressing a Copilot finding, a review comment, a doc tweak): **amend + force-push**.
   ```bash
   git add -A
   git commit --amend --no-edit    # or --edit to rewrite the message
   git push --force-with-lease     # never plain --force
   ```
4. When clean: `gh pr merge N --squash`.

#### Pattern B — Major branch (multi-session feature, refactor, system rebuild)

Open a **draft PR from day 1** so Copilot reviews each milestone push throughout development. The goal: catch architectural issues early, not at merge time.

1. Create the branch and an initial commit (even an empty scaffold works):
   ```bash
   git checkout -b feat/<name> main
   git commit --allow-empty -m "scaffold: <branch-purpose>"
   git push -u origin feat/<name>
   gh pr create --draft --title "..." --body "..."
   ```
2. During development, keep commits local and frequent — but **push only at meaningful milestones**, not every checkpoint. A milestone is a vertical slice that produces a coherent Copilot-reviewable artifact:
   - Specs locked (new/updated `docs/` files)
   - Pure-logic layer done (C# logic + unit tests pass)
   - Scene/UI wired up (game builds and behavior renders)
   - Integration tests pass (GoDotTest suite added)
   - Final pass (all tests green, docs updated)
3. **Between milestones: amend locally.** Don't push WIP — Copilot would review half-finished code and generate noise.
   ```bash
   # local checkpoint during the milestone
   git add -A && git commit -m "wip: <what>"
   # later, folding into the milestone commit
   git reset --soft $(git merge-base main HEAD)
   git commit -m "<milestone message>"
   ```
4. **At each milestone: force-push.** The branch stays at exactly one commit throughout the PR's life.
   ```bash
   git push --force-with-lease
   # wait for Copilot:
   make pr-copilot-wait PR=N
   make pr-copilot-status PR=N
   # verify findings per docs/conventions/work-discipline.md
   # fix or dismiss, amend, force-push again for next milestone
   ```
5. When the branch is feature-complete and Copilot is clean: flip the PR from draft → ready-for-review:
   ```bash
   gh pr ready N
   gh pr merge N --squash
   ```

Why "push only at milestones":
- **Review cost.** Each push triggers a Copilot review (30–90s + my verification time per finding). Pushing every WIP commit would generate 10-20 reviews per feature branch, most of which flag "this function is incomplete" noise.
- **Signal per review.** A milestone push shows Copilot a coherent vertical slice — better analysis than a mid-refactor snapshot.
- **Branch stays tidy.** One commit throughout the PR means the final squash-merge commit message is the one you wrote, not an autogenerated concat.

#### Common rules (both patterns)

- Use `--force-with-lease` instead of `--force` — protects against overwriting remote changes you haven't seen.
- Never force-push on `main`. Only on feature/fix branches.
- After any force-push, `make pr-copilot-wait PR=N` blocks until Copilot's new review lands; then `make pr-copilot-status PR=N` shows findings.
- Each Copilot finding goes through [External AI Feedback discipline](conventions/work-discipline.md) — verify before acting.

#### Copilot Kill-Switch — Disable If Not Adding Value

Copilot's code review is an assistant, not a colleague. If it starts reviewing without understanding the project, disable it immediately. Two AIs talking past each other is worse than one AI working in isolation.

**Signals that Copilot doesn't have the project context** (any one of these, disable):

1. **Flags correct code as wrong because it doesn't know the spec.** Example: "6.3s cinematic duration is a magic number" when `docs/flows/death.md` specifies exactly 6.3s.
2. **Suggests architectural changes that contradict our documented conventions.** Example: "you should use plain merge commits, not squash-force-push" when `AGENTS.md` §2b establishes the pattern.
3. **Repeatedly makes false-positive claims that require us to explain our design.** More verification overhead than fixing value = negative signal.
4. **Doesn't reference spec files in findings that obviously touch spec behavior.** Spec-driven development is core — a reviewer unaware of it reviews the wrong artifact.
5. **Hit rate drops below ~60% valid findings per review** over a sustained window (3+ reviews). Below that threshold, verification time exceeds fix time — the discipline is paying for the AI, not the AI paying for itself.

**How to disable** (single API call, reversible):

```bash
# Remove the copilot_code_review rule from the main ruleset
python3 -c "
import json, subprocess
r = subprocess.run(['gh','api','/repos/balbonits/infinite-dungeon-game/rulesets/15183156'],
                   capture_output=True, text=True)
d = json.loads(r.stdout)
d['rules'] = [x for x in d['rules'] if x['type'] != 'copilot_code_review']
with open('.github/rulesets/main-protection.json','w') as f:
    json.dump(d, f, indent=2)
"
gh api --method PUT /repos/balbonits/infinite-dungeon-game/rulesets/15183156 \
  --input .github/rulesets/main-protection.json
```

Commit the updated JSON with a message documenting why. Re-enable later (same API, flipped) if Copilot adds context (via `.github/copilot-instructions.md` or a future GitHub update).

**Not a kill signal:**
- An occasional false-positive on a general coding issue (no reviewer is perfect)
- A hedged suggestion that turns out narrowly wrong but points at a real defensive improvement (we applied Copilot's defensive OR-match in PR #4 even though the underlying claim was wrong — net positive)
- Disagreement on style preferences — Copilot flags both sides of a style debate; not a context problem

#### Copilot on Docs-Only PRs — Filter by Signal Type

Spec/doc PRs (only `.md` and other prose files changed) get different handling than code PRs. Copilot reviews prose from a code-reviewer prior and its signal-to-noise ratio drops: most findings are stylistic suggestions ("this could be clearer", "consider an example"), not defects.

**Rule:** on docs-only PRs, filter Copilot comments by signal type before engaging.

**Act on:**
- Cross-reference inconsistencies ("bank.md says 25 slots, items.md says 50")
- Broken links / wrong file paths
- Numbers that contradict other specs
- Lore contradictions between files
- Missing resolved-questions entries when a decision is stated

**Ignore:**
- Prose-style suggestions ("consider rephrasing", "this paragraph is long")
- Subjective "add more detail" without a specific gap named
- Formatting preferences (header style, list style, table width)
- Requests to add examples where the spec is already unambiguous

Don't toggle the ruleset per PR — that's manual overhead. Just apply this filter when triaging doc-PR findings. Same verification discipline as code reviews: each finding must be fact-backed (point to a specific contradiction), not just a style preference.

### 2c. Documentation Maintenance

- **Never hardcode volatile numbers** in AGENTS.md or CLAUDE.md (test counts, file counts, step counts). Reference commands instead: "Run `make test` for current count."
- **Journal first, then commit.** The dev journal entry must exist before the git commit.
- **CHANGELOG.md must stay current.** Every commit that changes behavior gets a changelog entry.
- **New systems need docs.** If you create a new system (new .cs files with game logic), create a corresponding spec in `docs/systems/` or `docs/world/`.
- **AGENTS.md is the canonical reference.** Tool-specific files must delegate to it, not duplicate it. Each AI tool has its own context filename (e.g., Claude Code reads `CLAUDE.md`; Gemini CLI reads `GEMINI.md`; GitHub Copilot reads `.github/copilot-instructions.md`). Before adding a new tool file, **verify the filename in the tool's official docs first** — do not guess. Then follow the pattern in [docs/conventions/ai-context-files.md](docs/conventions/ai-context-files.md).

### 3. Development Principles

- **KISS** — Keep It Simple. Use the simplest approach that satisfies the spec.
- **DRY** — Don't Repeat Yourself. Extract shared logic when repetition appears; don't prematurely abstract.
- **No scope creep** — Only implement what the current spec describes. Nothing extra.
- **Spec-driven** — Every system is fully documented in `docs/` before code is written. Read the relevant doc before modifying any code.
- **Test-driven** — Tests are written before implementation. Manual test cases first, then automated tests (GdUnit4 + xUnit), then code.
- **AI-coded** — All code is written by AI assistants. The user directs and reviews.
- **Free assets only** — No paid assets. Polygon2D placeholders, then free/open-source packs.

### 4. C# Conventions

Follow the [official Godot C# style guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html) and standard [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Script ordering** (top to bottom):
1. `using` directives
2. `namespace` declaration
3. `[Signal]` delegates
4. Enums
5. Constants
6. `[Export]` properties
7. Public properties
8. Private fields (`_camelCase`)
9. Lifecycle overrides (`_Ready`, `_Process`, `_PhysicsProcess`)
10. Public methods
11. Private methods

**All node scripts must use `partial`:**
```csharp
public partial class Player : CharacterBody2D { }
```

**Godot lifecycle methods** (override with `public override void`):
```csharp
public override void _Ready() { }
public override void _Process(double delta) { }
public override void _PhysicsProcess(double delta) { }
public override void _Input(InputEvent @event) { }
```

**Signals** use `[Signal]` attribute with `EventHandler` suffix:
```csharp
[Signal] public delegate void StatsChangedEventHandler();
[Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);

// Emit:
EmitSignal(SignalName.StatsChanged);
// Connect (preferred — auto-disconnects on node free):
source.Connect(SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));
```
Use `Connect()` over `+=` for signals — `Connect()` auto-disconnects when nodes are freed.

**Exports:**
```csharp
[Export] public float Speed { get; set; } = 190.0f;
[Export(PropertyHint.Range, "0,100,1")] public int Armor { get; set; }
[ExportGroup("Movement")]
[Export] public float JumpVelocity { get; set; } = 4.5f;
```

**Node references:**
```csharp
// Preferred — type-safe:
private Sprite2D _sprite = null!;
public override void _Ready() { _sprite = GetNode<Sprite2D>("Sprite"); }

// Or with unique names (% in scene tree):
[Export] public Sprite2D Sprite { get; set; } = null!;
```

**Scene architecture:**
- **"Call down, signal up"** — parents call methods on children; children emit signals to parents. Never reach up the tree.
- **One responsibility per node** — if a script exceeds ~300 lines, split behavior into child nodes.
- **Composition over inheritance** — build entities by combining specialized child scenes, not deep inheritance chains.
- **Static typing is enforced** — C# provides compile-time type checking. Use explicit types everywhere.
- **Nullable enabled** — use `null!` for fields initialized in `_Ready()`. Enable `<Nullable>enable</Nullable>` in `.csproj`.
- **Autoloads sparingly** — only for truly global state (GameState) and cross-system signals (EventBus). Access via static `Instance` property.

### 5. Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Godot 4.x (.NET edition) | Separate download from standard Godot |
| Language | C# / .NET 8+ | Strong typing, PascalCase, partial classes |
| Renderer | GL Compatibility | Broadest hardware support |
| Testing | GoDotTest + GdUnit4 + xUnit | GoDotTest (in-game UI/keyboard tests) + GdUnit4 (scene/asset) + xUnit (pure logic) |
| Serialization (saves) | System.Text.Json | Source-generated, human-readable, AOT-friendly |
| Serialization (cache) | MessagePack-CSharp v3 | Binary, ~10x faster, source generator support |
| Object pooling | Microsoft.Extensions.ObjectPool | Pool enemies, effects, projectiles — avoid GC |
| Async generation | System.Threading.Channels | Background floor generation pipeline |
| Entity System | Static C# classes | EntityData + 6 systems (Vital, Stat, Combat, Effect, Progression, Skill) |
| Physics | Built-in 2D | CharacterBody2D + Area2D |
| Perspective | Isometric 2D | 2:1 diamond tiles — floors 64×32, wall blocks 64×64 (ISS standard) |
| UI | Control nodes | GameWindow base, TabBar, ScrollList, ContentSection, WindowStack |
| Persistence | FileAccess + JSON/MessagePack | user:// directory |
| Platform | Desktop native | macOS primary, Windows/Linux supported |

**Current testing dependencies:**
```xml
<!-- DungeonGame.csproj -->
<PackageReference Include="Chickensoft.GodotTestDriver" Version="3.1.66" />
<PackageReference Include="Chickensoft.GoDotTest" Version="2.0.28" />

<!-- tests/e2e/DungeonGame.Tests.E2E.csproj -->
<PackageReference Include="gdUnit4.api" Version="5.0.0" />
<PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />

<!-- tests/unit + tests/integration -->
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

**Testing layers:**
- **xUnit** (`tests/unit/`, `tests/integration/`) — pure C# logic, no Godot runtime. Fast.
- **GdUnit4** (`tests/e2e/`) — scene loading, asset validation, system verification. Runs with `make test-gdunit`.
- **GoDotTest** (`scripts/testing/tests/*.cs`) — in-game UI tests driven by simulated keyboard input. Runs via `godot --headless --run-tests --quit-on-finish` (see `Main.cs`). Run with `make test-ui` or `make test-ui-suite SUITE=<Name>`. Built on `GodotTestDriver` for input simulation + `InputHelper`/`UiHelper` helpers in `scripts/testing/`.

**Known limitation:** C# web export is not supported as of Godot 4.6. Desktop-only.

See [docs/architecture/tech-stack.md](docs/architecture/tech-stack.md) for details.

### 6. Naming Conventions

- **Public properties/methods:** `PascalCase` — `MoveSpeed`, `HandleMovement()`, `MaxHp`
- **Private fields:** `_camelCase` — `_attackTimer`, `_isDead`, `_moveSpeed`
- **Constants:** `PascalCase` — `MoveSpeed`, `AttackCooldown`, `EnemySoftCap`
- **Node names:** `PascalCase` — `CollisionShape2D`, `AttackRange`, `HitCooldownTimer`
- **Signals:** `PascalCase` + `EventHandler` — `EnemyDefeatedEventHandler`, `StatsChangedEventHandler`
- **Namespaces:** `PascalCase` by directory — `DungeonGame.Autoloads`, `DungeonGame.Scenes.Player`
- **Groups:** `snake_case` — `"player"`, `"enemies"`
- **C# files:** `PascalCase.cs` — `GameState.cs`, `DeathScreen.cs`, `EventBus.cs`
- **Scene/resource files:** `PascalCase` — `Player.tscn`, `DungeonTileset.tres`
- **No abbreviations** — `Player` not `Plr`, `Level` not `Lvl`

### 7. Project Structure

> **Note:** All code, scenes, and tests were deleted in the Session 8 fresh start. The tree below shows what actually exists now.

```text
dungeon-web-game/
├── DungeonGame.csproj             — Main project (minimal, no NuGet refs yet)
├── project.godot                  — Godot 4 project config (.NET edition)
├── Makefile                       — AI-drivable automation (make help)
├── .editorconfig                  — Editor formatting rules (C# + Godot)
├── .gitignore / .gdignore         — Git + editor ignores (includes bin/, obj/)
├── .githooks/pre-commit           — C# formatting check on commit
├── AGENTS.md / CLAUDE.md          — AI assistant guidelines
├── README.md / CHANGELOG.md       — Project docs
├── docs/                          — Game design documentation (80+ files)
│   ├── overview.md / dev-tracker.md / dev-journal.md
│   ├── architecture/              — Tech stack, setup guide, project structure, scene tree, autoloads, signals
│   ├── conventions/               — Code patterns, agile process, AI workflow, teams
│   ├── objects/                   — Player, enemies, tilemap, effects specs
│   ├── assets/                    — Tile, sprite, UI theme specs
│   ├── systems/                   — Stats, classes, combat, leveling, death, save, movement, spawning, camera
│   ├── world/                     — Dungeon, town, monsters
│   ├── inventory/                 — Backpack, bank, items
│   ├── ui/                        — Controls, HUD, death screen
│   └── testing/                   — Test strategy, manual tests, automated tests
├── assets/                        — Tiles, sprites, fonts, icons (819+ files)
│   └── ATTRIBUTION.md             — Asset license tracking
└── icon.svg
```

See [docs/architecture/project-structure.md](docs/architecture/project-structure.md) for the full breakdown.

### 8. How to Propose Changes

- Read the relevant design doc in `docs/` before touching code
- Show the modified function or section with clear context
- Explain **why** each change was made
- Include manual test steps (from `docs/testing/manual-tests.md` or new ones)
- One feature or system per response — keep scope small
- If the change affects game mechanics, update the design doc too

### 9. Visual Style

- **Placeholder shapes:** Polygon2D diamonds for characters/enemies (no sprites yet)
- **Colors:**
  - Player: `#8ed6ff` (light blue)
  - Enemy tiers: `#6bff89` (green), `#ffde66` (yellow), `#ff6f6f` (red)
  - Accent/sword: `#f5c86b` (gold)
  - UI panel: `rgba(22, 27, 40, 0.75)` with `rgba(245, 200, 107, 0.3)` border
- **Tiles:** [Screaming Brain Studios](https://opengameart.org/users/screaming-brain-studios) (CC0) — sole source for all isometric textures/tiles. ISS pack: floors 64×32, wall blocks 64×64, magenta key
- See [docs/assets/ui-theme.md](docs/assets/ui-theme.md) for the full color palette

### 10. Development Automation

All development can be driven from the terminal. Run `make help` for available targets.

**Daily workflow:**
```bash
make build          # dotnet build
make run            # Launch the game (godot --path .)
make import         # Run Godot import (after adding new assets/scenes)
make clean          # Remove build artifacts
make doctor         # Check dev environment health
```

> **Note:** Most make targets (`test`, `lint`, `check`) require code/tests to exist. Run `make help` to see available targets. CI is currently disabled (moved to `.github/workflows-disabled/`).

**Tools required:**
| Tool | Install | Purpose |
|------|---------|---------|
| .NET 9 SDK | `brew install dotnet` | Build, test, format C# |
| Godot 4.x (.NET) | Download from godotengine.org (.NET build) | Engine, scene test runner |
| VS Code + C# Dev Kit | `ms-dotnettools.csharp` extension | IDE, IntelliSense, debugging |

See [docs/architecture/setup-guide.md](docs/architecture/setup-guide.md) for full install instructions.

**Pre-commit hook:** `.githooks/pre-commit` runs `dotnet format --verify-no-changes` on staged `.cs` files.

See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the full automation reference.

---

## Game Design Quick Reference

Detailed design docs live in `docs/`. Here's a summary with links.

### Character & Stats

- **4 stats:** STR (physical power), DEX (agility), STA (health), INT (magic) — [docs/systems/stats.md](docs/systems/stats.md)
- **3 classes:** Warrior (STR/STA), Ranger (DEX), Mage (INT) — [docs/systems/classes.md](docs/systems/classes.md)
- **Persistent character** — no rerolls, one character forever

### Skills & Abilities

Hierarchical skill trees per class with unique category names, hybrid leveling (use-based + point-based), infinite scaling — [docs/systems/skills.md](docs/systems/skills.md)

Skills/Abilities redesign (separating passive masteries from active combat actions) — [docs/systems/SKILLS_AND_ABILITIES_SYSTEMS.md](docs/systems/SKILLS_AND_ABILITIES_SYSTEMS.md)

### Color System

Unified cool→warm gradient for all game elements (enemies, items, zones), level-relative coloring — [docs/systems/color-system.md](docs/systems/color-system.md)

### Combat

Auto-targeting, cooldown-based, scales with level and stats — [docs/systems/combat.md](docs/systems/combat.md)

### Leveling

Redesigned XP curve (linear-polynomial hybrid), rested XP bonus, floor-scaling enemy XP, no level cap — [docs/systems/leveling.md](docs/systems/leveling.md)

### Player Engagement

Feedback loops, session pacing, juice/feel, retention hooks — [docs/systems/player-engagement.md](docs/systems/player-engagement.md)

### Death Penalties

Scale by deepest floor achieved. Gold buyout mitigates EXP and backpack loss. Sacrificial Idol negates backpack loss — [docs/systems/death.md](docs/systems/death.md)

### Class Lore

Class backstories, magic philosophy per class, and how lore shapes each skill tree — [docs/world/class-lore.md](docs/world/class-lore.md)

### Dungeon

Infinite descent, procedural floors, 10-floor cache, safe spots at entrances/exits — [docs/world/dungeon.md](docs/world/dungeon.md)

### Town Hub

Safe scene with NPCs: Item Shop, Blacksmith, Adventure Guild, Level Teleporter, Banker — [docs/world/town.md](docs/world/town.md)

### Inventory

- **Backpack:** 25 start slots, at risk on death — [docs/inventory/backpack.md](docs/inventory/backpack.md)
- **Bank:** 15 start slots, safe storage, town-only — [docs/inventory/bank.md](docs/inventory/bank.md)
- **Items/Loot:** deferred — [docs/inventory/items.md](docs/inventory/items.md)

### UI

- **Controls:** keyboard + future gamepad — [docs/ui/controls.md](docs/ui/controls.md)
- **HUD:** HP, XP, level, floor overlay — [docs/ui/hud.md](docs/ui/hud.md)
- **Death screen:** multi-step flow with destination choice, mitigations, confirmation — [docs/ui/death-screen.md](docs/ui/death-screen.md)

---

## Documentation Map

```text
docs/
├── overview.md                — Project vision and design philosophy
├── dev-tracker.md             — Master ticket list and dependency graph
├── dev-journal.md             — Running session log
├── architecture/
│   ├── tech-stack.md          — Godot 4 stack details
│   ├── project-structure.md   — File organization and naming
│   ├── scene-tree.md          — Complete node hierarchy for every scene
│   ├── autoloads.md           — GameState + EventBus singleton design
│   ├── signals.md             — Signal flow between all systems
│   ├── setup-guide.md         — .NET SDK, Godot .NET, VS Code setup
│   └── analytics.md           — Opt-in telemetry, bug reporting, feedback (offline-first)
├── conventions/
│   ├── code.md                — Code patterns, naming, quality standards
│   ├── agile.md               — Dev process, tickets, scope discipline
│   ├── ai-workflow.md         — Dev ticket cycle protocol
│   └── teams.md               — AI team structure and ownership
├── reference/
│   ├── godot-basics.md        — Godot concepts for web devs
│   ├── game-dev-concepts.md   — Game dev fundamentals (C#)
│   ├── game-development.md    — Research journal (accumulated learnings)
│   ├── subagent-research.md   — AI agent design research
│   └── godot4-engine-reference.md — Built-in engine systems, what to use vs custom
├── objects/
│   ├── player.md              — Player node, script, movement, attack
│   ├── enemies.md             — Enemy tiers, AI, damage
│   ├── tilemap.md             — TileSet, floor generation
│   └── effects.md             — Slash, camera shake, visual fx
├── assets/
│   ├── tile-specs.md          — Tile dimensions, colors, formats
│   ├── sprite-specs.md        — Character/enemy shape specs
│   └── ui-theme.md            — Color palette, fonts, panel styles
├── systems/
│   ├── stats.md               — STR/DEX/STA/INT
│   ├── classes.md             — Warrior/Ranger/Mage
│   ├── skills.md              — Skills & Abilities (passive masteries + active combat actions, all class trees)
│   ├── point-economy.md       — SP/AP rates, sources, budget
│   ├── synergy-bonuses.md     — Mastery threshold bonuses (Lv.5/10/25/50/100)
│   ├── ability-affinity.md    — Cosmetic use-based milestones
│   ├── SKILLS_AND_ABILITIES_SYSTEMS.md — ARCHIVED working document
│   ├── color-system.md        — Unified color gradient (cool→warm, level-relative)
│   ├── combat.md              — Auto-targeting, cooldowns, damage
│   ├── leveling.md            — XP curve, rested XP, floor-scaling enemy XP
│   ├── player-engagement.md   — Feedback loops, session pacing, juice/feel, retention
│   ├── death.md               — Penalties, gold buyout, Sacrificial Idol
│   ├── save.md                — FileAccess, JSON, user://, Base64 export
│   ├── movement.md            — Isometric movement + transform matrix
│   ├── spawning.md            — Enemy spawn system + timers
│   └── camera.md              — Camera follow, zoom, shake
├── world/
│   ├── dungeon.md             — Infinite descent, floor generation, caching
│   ├── town.md                — Town hub, NPC list, interaction
│   ├── monsters.md            — Enemy types, danger tiers, spawning
│   └── class-lore.md          — Class backstories, magic philosophy, skill tree rationale
├── inventory/
│   ├── backpack.md            — Risky carry storage (25 slots)
│   ├── bank.md                — Safe town storage (15 slots)
│   └── items.md               — Item system (deferred)
├── ui/
│   ├── controls.md            — Input methods (keyboard, gamepad)
│   ├── hud.md                 — HUD overlay with Control nodes
│   └── death-screen.md        — Death UI flow
└── testing/
    ├── test-strategy.md       — Testing approach (manual + GdUnit4 + xUnit)
    ├── manual-tests.md        — 33 manual test cases
    └── automated-tests.md     — GdUnit4 + xUnit automated tests
```

---

## Current State

**Phase: Fresh start.** All 26 specs are locked. All code, scenes, and tests were deleted in Session 8 (commit `1f917e2`). Rebuilding from scratch with visual-first development.

**Stack:** Godot 4.6 + C# (.NET 8+). No code exists yet. NuGet packages listed in docs are not yet in .csproj.

**Dynamic state** (do not hardcode numbers here — they go stale):
- No code, scenes, or tests exist yet
- Recent work: run `git log --oneline -10`
- Ticket status: see [docs/dev-tracker.md](docs/dev-tracker.md)
- Session history: see [docs/dev-journal.md](docs/dev-journal.md)

**Current mode:** Visual-first rebuild. One tile, one sprite, one scene at a time. Verify each step visually before proceeding. Follow the dev ticket cycle in [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md).

## Priorities

1. **Visual foundation** — Render tiles, place sprites, movement, camera (VIS-01 through VIS-06)
2. **Playable prototype** — Combat, HUD, death, game loop (PROTO-01 through PROTO-06)
3. **Complete systems** — Classes, skills, death flow, quests (SYS-01 through SYS-10)

---

When helping:

- **Stay in scope.** Do exactly what was asked. Nothing more.
- **Do not assume.** If something is unclear, ask. Don't guess and run with it.
- **Read the spec first.** Check the relevant doc in `docs/` before proposing or writing anything.
- **One task at a time.** Keep responses focused on the single thing that was requested.
- **Flag, don't fix.** If you notice something out of scope that seems wrong, mention it — don't silently fix it.
- **Tests before code.** Write or reference the test cases before writing the implementation.
- **Verify your work.** Run tests or describe exact manual verification steps. Passing tests are the definition of "done."
- **Prefer "X over Y"** framing over "don't do X" when explaining tradeoffs — positive guidance is clearer than prohibitions.
- See [docs/conventions/code.md](docs/conventions/code.md) for code patterns, naming, and quality standards
- See [docs/conventions/agile.md](docs/conventions/agile.md) for dev process, scope discipline, and ticket workflow
- See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the detailed AI workflow protocol

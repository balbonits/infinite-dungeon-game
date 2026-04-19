# FE-First Development

**Status:** LOCKED — PO direction 2026-04-18.

## The rule

During active **code phases**, front-end (visible) work comes before back-end (invisible) work:

- Asset generation, UI polish, sprite redraws, animation work, flow visuals → **top of queue**.
- Save-system bugs, AI internals, test-coverage, performance memoization, deep audit fixes → **defer behind FE**.

## When this rule does NOT apply

**Spec-writing / doc-authoring phases** are not governed by FE-first. During the spec roadmap (Phases A-J on `docs/spec-roadmap.md`), priority is driven by the roadmap's dependency chain, not by FE/BE. A BE-systems spec (e.g. SPEC-MAGICULE-DENSITY-01, SPEC-CRAFTING-QUALITY-LADDER-01) ranks above an FE-visual spec if the roadmap puts it there.

The trigger for "FE-first applies" is **the PR contains any non-doc, behavior-affecting change** — not just `docs/`. `scripts/`, `scenes/`, and `assets/` are the common cases, but this also includes code-adjacent project files like `shaders/`, `addons/`, `tests/`, `project.godot`, `.csproj`, workflow/build config, and similar. The list is non-exhaustive; if the change could alter what ships to the player, treat it as a code phase.

## Why

Visible progress drives motivation and playtester feedback. A polished vertical slice of the player experience — art, UI, flow — is worth shipping ahead of audit-grade back-end work at this phase of the project. The game needs to look and feel right before the invisible systems are rounded out.

> *PO, 2026-04-18: "we're doing front-to-back development (FE-first) ... IF we're coding. if not, are we're still writing specs [sic]."*

## How to apply

### Triage

When looking at a backlog, re-rank tickets by whether the fix lands in a frame the player sees:

- **FE-visible:** NPC sprite redraws, tile pipeline redraws, HUD layout, menu restructures, flow-screen polish (splash, death, town), camera shake, hitstop, animation work. **These go first.**
- **BE-invisible:** save-slot bugs, import-state validators, test-coverage additions, performance memoization, obsolete-method cleanup in pure-logic files. **These go after FE.**
- **Hybrid:** dead code removal is technically BE cleanup, but scene-tree nodes still registered in `main.tscn` are visible clutter in the editor → belongs in the FE queue.
- **BE exception:** a BE bug that actively breaks a visible FE surface (e.g., a save-corrupt bug that wipes a visible progression) jumps the queue. FE-first means visible-first, not visible-only.

### Dispatch

When choosing what to dispatch next:

- Prefer "one NPC sprite → show PO" over "five unit tests → nothing visible." The FE path produces review-able output; the BE path produces green CI checks only.
- When batching fixes for a PR, prefer to split FE fixes into their own PR (so the PO can see them fast) rather than bundling with BE work that's harder to review.

### Ordering within FE

Inside the FE queue, follow existing memory rules:

- **One image first** — for any art batch, generate one reference image, get PO theme approval, then batch the rest.
- **Art pipeline is two-stage** — Claude drafts low-fi concept first, PixelLab pixelates. No 1:1 licensed-ref replication.
- **IP protection** — cartoonish style, no named-IP in prompts, per ART-SPEC-01 §11.

## Corollary for audit findings

The audit doc itself is not user-facing, so its BE fixes (corrupt-save guards, import-state validators) are secondary. Except: findings about visibly stale UI (dead scene nodes, broken menu items, orphaned sprites) are FE-visible and belong in the FE queue alongside asset work.

## See also

- [docs/conventions/work-discipline.md](work-discipline.md) — "Slow is smooth" and verification before speculation.
- [docs/conventions/ai-workflow.md](ai-workflow.md) — PR workflow and tag-team protocol.
- [docs/assets/prompt-templates.md](../assets/prompt-templates.md) — ART-SPEC-01 pipeline conventions.
- [docs/spec-roadmap.md](../spec-roadmap.md) — spec priority during spec-writing phases.

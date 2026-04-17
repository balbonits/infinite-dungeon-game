# ADR-006: Keep GitHub Issues Enabled on the Public Repo

**Status:** Accepted
**Date:** 2026-04-17
**Decided by:** Product owner (approved explicitly — "up to you. i'll trust your judgement on this one" → "add that to our docs, so you can blame me if i ever question why it was left there")

## Context

The repo is public. During Session 20's permissions audit, the question came up: should we disable GitHub Issues (`has_issues: false`) to prevent strangers from filing noise?

Arguments on the table:

**For disabling Issues:**
- Solo dev; no team needs them internally
- Ticket tracking already happens in `docs/dev-tracker.md`
- Public repo → strangers could open noise issues (spam, irrelevant reports)
- Issue moderation is overhead the product owner doesn't want
- Less surface area = less maintenance

**For keeping Issues enabled:**
- The repo is published **specifically** as a paradigm showcase (see [`docs/development-paradigm.md`](../development-paradigm.md) and the README's front-page section). The explicit goal is that researchers and developers interested in AI-driven development examine it.
- Closing Issues sends a contradictory signal: *"here is our open experiment, but don't engage with it."*
- Genuine external input (thoughtful questions, observations about the paradigm, real bug reports) is part of the intended value of publishing.
- Current noise level is **zero**. Pre-emptive lockdown is premature optimization.
- GitHub's built-in spam detection catches obvious garbage from brand-new accounts.
- The decision is **reversible in one API call** if real noise ever appears: `gh api --method PATCH /repos/:o/:r -f has_issues=false`.

## Decision

**Keep GitHub Issues enabled.** The paradigm's purpose — inviting external examination — argues against closing the feedback channel. Decide on evidence, not speculation. If spam becomes a real problem, disable then.

## What Remains Protected

Leaving Issues on does NOT weaken any of the protections already in place:

- Only `balbonits` has write access (no other collaborators)
- Branch ruleset on `main` (ID 15183156) requires PR + squash-only merge + linear history
- `copilot_code_review` rule auto-requests Copilot review on every PR push
- External contributors could open PRs from forks, but cannot merge — only the repo owner can

Issues are a read-write surface for the public, but they're metadata: filing an issue doesn't modify code, docs, or game behavior.

## Trigger to Revisit

Revisit this decision if any of the following happen:

- Sustained spam on Issues that GitHub's auto-filter doesn't catch
- Low-signal bug reports creating time-consuming triage
- The repo transitions from "public paradigm showcase" to "actively shipped product" (different goals, different tradeoffs)

At that point, the fix is: `gh api --method PATCH /repos/balbonits/infinite-dungeon-game -f has_issues=false`.

## Audit Trail

- Product owner explicitly deferred to AI judgment for this decision.
- AI recommendation: keep enabled, for the reasons above.
- Product owner approved and asked it be documented. This ADR is that documentation.

If the product owner later questions why Issues are enabled, this record shows: the decision was considered, made consciously, and approved at the time.

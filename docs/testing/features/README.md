# Flow Features (Gherkin)

Human-readable descriptions of the user flows the game must support. Each `.feature` file describes a flow in Gherkin (`Given / When / Then`) so a non-code reader (product owner, QA, future-me) can understand what the automated tests enforce without opening a `.cs` file.

## Relationship to code

Each feature file maps 1:1 to a `[Test]` method in `scripts/testing/tests/*.cs`. The test name is noted at the top of the feature. If a feature and its test disagree, that's a bug in one of them — the spec is the source of truth per [docs/development-paradigm.md](../../development-paradigm.md), so resolve the mismatch by updating whichever side (feature doc, test, or implementation) is wrong against the canonical spec in `docs/`.

These files do NOT run automatically via a Reqnroll/SpecFlow runner. They are documentation kept adjacent to the code that implements them. If the team grows past one contributor and PO-readable living docs are worth their own runner, we can add Reqnroll later — the Gherkin here will port directly.

## Writing conventions

- **Feature** — one user-facing flow (e.g. "Starting a new game when save slots are full").
- **Scenario** — one concrete path through that flow. Prefer multiple focused scenarios over one branchy scenario with `But`.
- **Given** — preconditions seeded via `SaveManager` / `GameState` (not clicked through the UI, since setup isn't what's under test).
- **When** — the user-facing action under test, expressed through driver verbs (`click New Game`).
- **Then** — observable outcome (screen appears, dialog closes, GameState flag flips, screenshot matches baseline).
- **And** — continuation of the previous clause.

## Current inventory

- [`splash.feature`](splash.feature) — splash → new game, continue, tutorial, settings, slots-full dialog.

(Expand as new flows land.)

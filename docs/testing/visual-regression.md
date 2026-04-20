# Visual Regression + UX Testing

UX-flow tests assert that user interactions produce the correct **observable outcome** — not just that no exception fires. A silent no-op is a real bug; an error-only check misses it.

This doc covers the four test layers the game uses. Each layer catches a different class of regression.

## Layer 1 — GodotTestDriver drivers (flow assertions)

Page-object wrappers over each screen (`SplashScreenDriver`, `SlotsFullDialogDriver`, `LoadGameScreenDriver`, `ClassSelectDriver`). Tests call driver verbs (`Flow.Splash.ClickNewGame()`) instead of raw `Ui.FindButton + EmitSignal`.

- **Location:** `scripts/testing/drivers/`
- **Composition root:** `GameFlowDriver` — one field per screen, lazy `Func<>` producers so scene swaps don't invalidate.
- **Library:** Chickensoft.GodotTestDriver 3.1.66 (already in `DungeonGame.csproj`).
- **Catches:** click wiring, focus routing, screen-transition lifecycle.
- **Misses:** rendered output, layout, contrast, accessibility.

## Layer 2 — Screenshot approval (visual regression)

Captures the viewport at each flow step and compares to a stored baseline.

- **Helpers:** `scripts/testing/ScreenshotHelper.cs` (capture + diff), `GameTestBase.VerifyScreenshot(stepName, tolerancePercent)`.
- **Library:** `Codeuctivity.ImageSharpCompare 4.0.204` (pixel-diff with tolerance, diff-mask image output).
- **Layout:**
  - Baselines: `tests/e2e/screenshots/baselines/<Suite>/<Test>/NN_<step>.verified.png` (committed)
  - Received (mismatches only): `tests/e2e/screenshots/received/<Suite>/<Test>/NN_<step>.received.png` (gitignored)
  - Diff masks: same directory as received, `.diff.png`
- **First run** seeds the baseline (Expect pass, logged as BaselineSeeded).
- **Subsequent runs** Expect the pixel diff ≤ `tolerancePercent` (default 1.0%).
- **To accept a visual change:** delete the `.verified.png` baseline, re-run — the next run seeds a new baseline. Review the `.received.png` before promoting.
- **Catches:** layout regressions, wrong sprites, color drift, off-screen modals, tile rendering bugs.
- **Misses:** interaction logic, perceptual changes below tolerance (e.g., font kerning, anti-aliasing diffs).

## Layer 3 — Accessibility linter

Static pass over a Control subtree asserting UX heuristics.

- **Helper:** `scripts/testing/AccessibilityLinter.cs`, `GameTestBase.ExpectNoAccessibilityViolations(root, label)`.
- **Checks:**
  - Touch-target size ≥ 44×44 px per Apple HIG / Material.
  - Text contrast ratio ≥ 4.5:1 per WCAG 2.x AA.
  - Modal windows (`GameWindow`) have a visible Close/Cancel/Back button (no keyboard-only traps).
- **Severity:**
  - **Error** — hard fail (Expect fails the test).
  - **Warning** — logged but doesn't fail.
- **Catches:** tiny buttons, low-contrast labels, modal traps, ships-with-no-escape dialogs.
- **Misses:** dynamic contrast (rendered gradients), screen-reader compatibility, keyboard-nav chain completeness (future extension).

## Layer 4 — Gherkin feature docs

Human-readable flow documentation paired with the C# tests.

- **Location:** `docs/testing/features/*.feature`
- **Format:** standard Gherkin (`Feature / Scenario / Given / When / Then / And`).
- **Relationship to code:** each scenario names the `[Test]` method that enforces it. Not currently wired to a Gherkin runner (Reqnroll) — the feature files are documentation. Can be promoted later if team grows.
- **Catches:** spec / code drift that a reader would notice but the C# test wouldn't flag.
- **Misses:** everything runtime — these are docs, not executable.

## Typical flow-test shape

```csharp
[Test]
public async Task MyFlow_DoesTheRightThing()
{
    StartTest(nameof(MyFlow_DoesTheRightThing));      // for screenshot numbering

    await SeedSomeStateDirectly();                     // setup — not the flow under test
    bool atSplash = await ResetToFreshSplash();
    if (!atSplash) { Expect(false, "could not reach splash"); return; }

    await VerifyScreenshot("01-starting-state");       // visual baseline
    ExpectNoAccessibilityViolations(Flow.Splash.Root!, "splash");  // a11y lint

    Flow.Splash.ClickNewGame();                        // the actual flow step

    await WaitUntil(() => Flow.SomeNextScreen.IsShown, // observable outcome
                    timeout: 2f, what: "next screen appears");

    await VerifyScreenshot("02-after-click");
}
```

## Workflow: updating a baseline

When a UI change is intentional and you want to update the baseline:

1. Run `make test-ui` — the test fails with a `.diff.png` under `tests/e2e/screenshots/received/`.
2. Open the `.diff.png` — pink/red pixels show what changed.
3. If the change is intended: delete `tests/e2e/screenshots/baselines/<path>.verified.png`.
4. Re-run `make test-ui` — it seeds a new baseline.
5. Commit the new `.verified.png` to lock it in.

If the change is NOT intended, that's your regression — fix it and re-run.

## CI integration

The `tests/e2e/screenshots/baselines/` directory is committed.

**Current CI (GitHub Actions UI Tests job):** runs Godot with `--headless`. `ScreenshotHelper.IsHeadless` (checks `DisplayServer.GetName() == "headless"`) returns true, so `VerifyScreenshot` returns `VerifyStatus.Skipped` and the step logs without failing. Visual baselines are **not currently enforced in CI** — only locally when developers run `make test-ui` windowed.

A follow-up change will move CI to `xvfb-run -a -s "-screen 0 1280x720x24" godot ...` to provide a virtual X display and let verification proceed. Until that lands, treat baseline regressions as a local-first safety net — run the windowed UI suite before merging changes to splash / class-select / dialog surfaces.

When baselines become enforced in CI, pixel mismatches above `tolerancePercent` will fail the job. Platform diffs (macOS vs Linux GL rendering) can drift subpixels — tune `tolerancePercent` per test if needed, or seed baselines on the same platform CI runs on.

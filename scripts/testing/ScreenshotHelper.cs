#if DEBUG
using System;
using System.IO;
using System.Threading.Tasks;
using Codeuctivity.ImageSharpCompare;
using Godot;
using SixLabors.ImageSharp;

namespace DungeonGame.Testing;

/// <summary>
/// Captures the current viewport to PNG for flow-test visual evidence.
/// Existing code can only assert state I cannot see ("button is visible");
/// screenshots let a human reviewer (or a later me) see the actual frame
/// each flow step produces and catch mis-renders that state checks miss —
/// e.g., a correctly-sized TextureRect that's still rendering the wrong
/// region, or a dialog that opens off-screen.
///
/// Saves under <c>tests/e2e/screenshots/&lt;suite&gt;/&lt;test&gt;/NN_&lt;step&gt;.png</c>.
/// The directory is already gitignored so artifacts don't bloat the repo.
///
/// Usage (from GameTestBase subclasses):
/// <code>
/// await Screenshot("splash-initial");
/// Flow.Splash.ClickNewGame();
/// await Screenshot("after-new-game-click");
/// </code>
/// </summary>
public static class ScreenshotHelper
{
    private const string RootDir = "tests/e2e/screenshots";

    /// <summary>
    /// True when the game is running via <c>--headless</c>. Viewport capture
    /// does not work in that mode — GetTexture().GetImage() returns empty.
    /// Visual-regression tests must run windowed (e.g., <c>make test-ui-windowed</c>).
    /// </summary>
    public static bool IsHeadless => DisplayServer.GetName() == "headless";

    /// <summary>
    /// Capture the main viewport to a PNG file. Returns the absolute path
    /// of the saved file, or null on failure. Waits for 2 process frames
    /// before grabbing so any just-fired draw commands land in the capture.
    /// In headless mode this is a no-op returning null.
    /// </summary>
    public static async Task<string?> Capture(
        Node host,
        string suiteName,
        string testName,
        int stepNumber,
        string stepName)
    {
        if (IsHeadless)
        {
            GD.Print($"[Screenshot] skipped (headless): {suiteName}/{testName}/{stepName}");
            return null;
        }

        // Wait a couple process frames so the capture includes the most
        // recent tweens / focus / visible toggles.
        for (int i = 0; i < 2; i++)
            await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);

        var viewport = host.GetViewport();
        if (viewport is null)
        {
            GD.PushWarning($"[Screenshot] viewport null for {suiteName}/{testName}/{stepName}");
            return null;
        }

        var tex = viewport.GetTexture();
        var img = tex?.GetImage();
        if (img is null || img.IsEmpty())
        {
            GD.PushWarning($"[Screenshot] viewport image empty for {suiteName}/{testName}/{stepName}");
            return null;
        }

        var repoRoot = GetRepoRoot();
        var dir = Path.Combine(repoRoot, RootDir, Sanitize(suiteName), Sanitize(testName));
        Directory.CreateDirectory(dir);

        var filename = $"{stepNumber:D2}_{Sanitize(stepName)}.png";
        var path = Path.Combine(dir, filename);

        var err = img.SavePng(path);
        if (err != Error.Ok)
        {
            GD.PushError($"[Screenshot] SavePng failed: {err} at {path}");
            return null;
        }

        GD.Print($"[Screenshot] {suiteName}/{testName}/{filename} saved");
        return path;
    }

    /// <summary>
    /// Resolve the repo root by walking up from the executable location
    /// until we find a directory containing project.godot. Falls back to
    /// the current working directory.
    /// </summary>
    private static string GetRepoRoot()
    {
        var cwd = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(cwd);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return cwd;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '-');
    }

    // ── Approval workflow (Verify-ImageSharp-style) ────────────────────────

    /// <summary>
    /// Result of a verification against a stored baseline.
    /// </summary>
    public enum VerifyStatus
    {
        /// <summary>No baseline existed; this run's capture was promoted to the baseline.</summary>
        BaselineSeeded,
        /// <summary>Capture matched the baseline within tolerance.</summary>
        Match,
        /// <summary>Capture differed from the baseline beyond tolerance; diff image written.</summary>
        Mismatch,
        /// <summary>Capture failed for environmental reasons (no viewport, empty image).</summary>
        Failed,
        /// <summary>
        /// Headless run — viewport capture unavailable; verification neither
        /// passed nor failed. Distinct from <see cref="BaselineSeeded"/> so the
        /// test reporter can't conflate "didn't check" with "wrote baseline".
        /// </summary>
        Skipped,
    }

    public readonly record struct VerifyReport(
        VerifyStatus Status,
        string? ReceivedPath,
        string? VerifiedPath,
        string? DiffPath,
        double PixelDifferencePercent);

    private const string BaselinesDir = "tests/e2e/screenshots/baselines";
    private const string ReceivedDir = "tests/e2e/screenshots/received";

    /// <summary>
    /// Sentinel returned by <see cref="VerifyAgainstBaseline"/> when headless
    /// mode prevents viewport capture — the check is skipped, not passed.
    /// </summary>
    public static readonly VerifyReport HeadlessSkipped =
        new(VerifyStatus.Skipped, null, null, null, 0);

    /// <summary>
    /// Capture the viewport and compare against a baseline PNG under
    /// <c>tests/e2e/screenshots/baselines/&lt;suite&gt;/&lt;test&gt;/NN_&lt;step&gt;.verified.png</c>.
    /// First run seeds the baseline; subsequent runs assert diff ≤ tolerancePercent.
    /// On mismatch, writes <c>.received.png</c> + <c>.diff.png</c> for review.
    /// </summary>
    public static async Task<VerifyReport> VerifyAgainstBaseline(
        Node host,
        string suiteName,
        string testName,
        int stepNumber,
        string stepName,
        double tolerancePercent = 1.0)
    {
        if (IsHeadless)
        {
            GD.Print($"[VerifyScreenshot] skipped (headless): {suiteName}/{testName}/{stepName}");
            return HeadlessSkipped;
        }

        // Wait a couple process frames so the capture reflects the latest draw.
        for (int i = 0; i < 2; i++)
            await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);

        var viewport = host.GetViewport();
        var img = viewport?.GetTexture()?.GetImage();
        if (img is null || img.IsEmpty())
        {
            GD.PushWarning($"[VerifyScreenshot] empty capture for {suiteName}/{testName}/{stepName}");
            return new VerifyReport(VerifyStatus.Failed, null, null, null, 0);
        }

        var repoRoot = GetRepoRoot();
        var relFile = $"{stepNumber:D2}_{Sanitize(stepName)}";
        var baselineDir = Path.Combine(repoRoot, BaselinesDir, Sanitize(suiteName), Sanitize(testName));
        var receivedDir = Path.Combine(repoRoot, ReceivedDir, Sanitize(suiteName), Sanitize(testName));
        Directory.CreateDirectory(baselineDir);
        Directory.CreateDirectory(receivedDir);

        var verifiedPath = Path.Combine(baselineDir, $"{relFile}.verified.png");
        var receivedPath = Path.Combine(receivedDir, $"{relFile}.received.png");
        var diffPath = Path.Combine(receivedDir, $"{relFile}.diff.png");

        // First run — seed baseline. Callers see BaselineSeeded and decide
        // whether to hard-fail the test (CI) or accept (local first-run).
        if (!File.Exists(verifiedPath))
        {
            var seedErr = img.SavePng(verifiedPath);
            if (seedErr != Error.Ok)
                return new VerifyReport(VerifyStatus.Failed, null, null, null, 0);
            GD.Print($"[VerifyScreenshot] SEEDED baseline {relFile}.verified.png");
            return new VerifyReport(VerifyStatus.BaselineSeeded, null, verifiedPath, null, 0);
        }

        // Baseline exists — save received + diff. Check the SavePng error
        // (Copilot PR #33 round-4): a silent failure here would leave the
        // subsequent CalcDiff call to either throw or compare a stale/missing
        // file, both of which report useless diagnostics to the reviewer.
        var receivedErr = img.SavePng(receivedPath);
        if (receivedErr != Error.Ok)
        {
            GD.PushError($"[VerifyScreenshot] failed to save received PNG for {relFile}: {receivedErr}");
            return new VerifyReport(VerifyStatus.Failed, null, verifiedPath, null, 0);
        }
        try
        {
            var diff = ImageSharpCompare.CalcDiff(receivedPath, verifiedPath);
            double diffPct = diff.MeanError;
            if (diffPct <= tolerancePercent)
            {
                // Clean — delete received to keep the tree tidy.
                File.Delete(receivedPath);
                return new VerifyReport(VerifyStatus.Match, null, verifiedPath, null, diffPct);
            }

            using var diffImg = ImageSharpCompare.CalcDiffMaskImage(receivedPath, verifiedPath);
            diffImg.SaveAsPng(diffPath);
            GD.PushError($"[VerifyScreenshot] MISMATCH {relFile} — {diffPct:F2}% diff; see {diffPath}");
            return new VerifyReport(VerifyStatus.Mismatch, receivedPath, verifiedPath, diffPath, diffPct);
        }
        catch (Exception e)
        {
            GD.PushError($"[VerifyScreenshot] compare exception: {e.Message}");
            return new VerifyReport(VerifyStatus.Failed, receivedPath, verifiedPath, null, 0);
        }
    }
}
#endif

#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the class selection screen (see docs/flows/class-select.md).
/// Verifies:
///   - Screen appears with 3 class cards (Warrior, Ranger, Mage)
///   - Left/Right cycles cards within zone 0 and auto-selects
///   - Down moves from zone 0 (cards) → zone 1 (Confirm) → zone 2 (Back)
///   - Pressing S/Enter on Confirm triggers the transition (fade + LoadTown)
///   - GameState.Instance.SelectedClass matches the card that was confirmed
///
/// Each test isolates by navigating from splash → New Game → ClassSelect so
/// it does not rely on test-ordering.
/// </summary>
public class ClassSelectTests : GameTestBase
{
    private readonly Node _sceneRoot;

    public ClassSelectTests(Node testScene) : base(testScene)
    {
        _sceneRoot = testScene;
    }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ ClassSelectTests ═══");

    [Setup]
    public async Task OpenClassSelect()
    {
        // Always start from a known-fresh splash. A prior test may have
        // confirmed into Town or been left mid-ClassSelect; ResetToFreshSplash
        // reloads the scene so this setup has one deterministic entry point.
        if (!await ResetToFreshSplash())
        {
            Expect(false, "[setup] could not reset to splash");
            return;
        }

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null)
        {
            Expect(false, "[setup] New Game button not found on splash");
            return;
        }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(), timeout: 3f,
            what: "[setup] ClassSelect appears after New Game");

        // Let ClassSelect finish _Ready (card layout, initial zone).
        await Input.WaitFrames(5);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec: "3 class cards displayed horizontally (Warrior, Ranger, Mage)"
    /// Plus the Confirm and Back buttons exist.
    /// </summary>
    [Test]
    public async Task ClassSelect_ShowsThreeClassCards()
    {
        Expect(Ui.HasNodeOfType<ClassSelect>(), "ClassSelect node present in tree");

        // The class names appear as Label text inside each card panel.
        Expect(FindTextInTree(Strings.Classes.Warrior), "Warrior label visible in ClassSelect");
        Expect(FindTextInTree(Strings.Classes.Ranger), "Ranger label visible in ClassSelect");
        Expect(FindTextInTree(Strings.Classes.Mage), "Mage label visible in ClassSelect");

        Expect(Ui.FindButton("Confirm") is not null, "Confirm button exists");
        // Actual text is "Back to Main Menu" — use Contains so the test
        // survives copy-changes without churning the spec.
        Expect(Ui.FindButton(b => b.Text.Contains("Back")) is not null, "Back button exists");

        await Input.WaitFrames(2);
    }

    /// <summary>
    /// Spec: Zone 0 (Cards) — Left/Right navigates cards and auto-selects
    /// (calls OnCardClicked, which sets _confirmButton.Disabled = false).
    /// </summary>
    [Test]
    public async Task ClassSelect_LeftRightCyclesCardsAndEnablesConfirm()
    {
        // Initial state per spec: zone 0, focus index -1, Confirm disabled.
        var confirm = Ui.FindButton("Confirm");
        Expect(confirm is not null, "Confirm button present");
        if (confirm is null) return;

        Expect(confirm.Disabled, "Confirm is initially disabled (no card selected)");

        // Press Right — lands on first card and auto-selects it.
        await Input.NavRight();
        await Input.WaitFrames(5);
        Expect(!confirm.Disabled, "Confirm enabled after Right (auto-select)");

        // Press Right again — still zone 0, cycles to next card.
        await Input.NavRight();
        await Input.WaitFrames(5);
        Expect(!confirm.Disabled, "Confirm still enabled after second Right");

        // Press Left — cycles back. Confirm should remain enabled.
        await Input.NavLeft();
        await Input.WaitFrames(5);
        Expect(!confirm.Disabled, "Confirm remains enabled after Left (a card is always selected)");
    }

    /// <summary>
    /// Spec: Down moves zone 0 → zone 1. In zone 1, _confirmButton.GrabFocus()
    /// is called, so the focused control should be the Confirm button.
    /// </summary>
    [Test]
    public async Task ClassSelect_DownMovesFocusToConfirmZone()
    {
        // Enter zone 0 with a card selected first (Right to auto-select).
        await Input.NavRight();
        await Input.WaitFrames(5);

        // Down → zone 1 (Confirm)
        await Input.NavDown();
        await Input.WaitFrames(5);

        Expect(Ui.FocusedButtonText == "Confirm",
            $"Confirm button has focus after Down (got: {Ui.FocusedButtonText ?? "null"})");
    }

    /// <summary>
    /// Spec: Down again from zone 1 → zone 2 (Back button).
    /// </summary>
    [Test]
    public async Task ClassSelect_DownTwiceMovesToBackZone()
    {
        await Input.NavRight();          // zone 0 — auto-select
        await Input.WaitFrames(5);
        await Input.NavDown();           // zone 0 → 1 (Confirm)
        await Input.WaitFrames(5);
        await Input.NavDown();           // zone 1 → 2 (Back)
        await Input.WaitFrames(5);

        Expect(Ui.FocusedButtonText == "Back",
            $"Back button has focus after two Downs (got: {Ui.FocusedButtonText ?? "null"})");
    }

    /// <summary>
    /// Spec: pressing S (action_cross) on Confirm (zone 1) fires OnConfirmPressed.
    /// That sets GameState.Instance.SelectedClass = Warrior, fades out, and LoadTown()s.
    /// We verify:
    ///   (a) GameState.Instance.SelectedClass == Warrior (synchronous in OnConfirmPressed)
    ///   (b) ClassSelect is eventually hidden/removed
    ///   (c) Town scene eventually loads (after ~2.5s transition)
    /// </summary>
    [Test]
    public async Task ClassSelect_ConfirmWarriorSetsStateAndLoadsTown()
    {
        // _focusIndex starts at -1; first Right sets index 0 (Warrior).
        await Input.NavRight();
        await Input.WaitFrames(5);

        // Zone 0 → Zone 1 (Confirm)
        await Input.NavDown();
        await Input.WaitFrames(5);
        Expect(Ui.FocusedButtonText == "Confirm", "Confirm focused before press");

        // Press S (action_cross) to confirm.
        await Input.Confirm();
        await Input.WaitFrames(5);

        // GameState.SelectedClass is set synchronously in OnConfirmPressed.
        Expect(GameState.Instance is not null, "GameState.Instance exists");
        Expect(GameState.Instance.SelectedClass == PlayerClass.Warrior,
            $"SelectedClass == Warrior (got: {GameState.Instance?.SelectedClass})");

        // Total transition is ~2.5s (0.4s tween + 2.1s screen transition).
        await WaitUntil(
            () => !Ui.HasNodeOfType<ClassSelect>() ||
                  Ui.FindNodeOfType<ClassSelect>()?.Visible == false,
            timeout: 5f,
            what: "ClassSelect hidden/removed after confirm");

        await WaitUntil(
            () => Ui.HasNodeOfType<Scenes.Town>(),
            timeout: 6f,
            what: "Town scene loaded after confirm");
    }

    /// <summary>
    /// Regression: class-select portraits must render as region-cropped
    /// AtlasTextures (south-facing walk frame 0), not as raw full-sheet
    /// Texture2D loads. The LPC character full_sheet.png is a multi-row
    /// animation atlas ~832x1344; loading it directly caused every card to
    /// tile the entire sprite grid (visible bug 2026-04-19). Assert each
    /// card's portrait TextureRect uses a ≤128x128 effective texture region.
    /// </summary>
    [Test]
    public async Task ClassSelect_PortraitsAreCroppedAtlasTextures()
    {
        var classSelect = Ui.FindNodeOfType<ClassSelect>();
        Expect(classSelect is not null, "ClassSelect node present in tree");
        if (classSelect is null) return;

        var portraits = new System.Collections.Generic.List<TextureRect>();
        CollectPortraitRects(classSelect, portraits);

        Expect(portraits.Count >= 3,
            $"At least 3 portrait TextureRects found under ClassSelect (got: {portraits.Count})");

        foreach (var tr in portraits)
        {
            var tex = tr.Texture;
            Expect(tex is not null, "Portrait TextureRect has a texture");
            if (tex is null) continue;

            int w = tex.GetWidth();
            int h = tex.GetHeight();
            Expect(w <= 128 && h <= 128,
                $"Portrait texture is region-cropped ({w}x{h} ≤ 128x128). " +
                $"If this fails, the card is loading the full LPC sheet instead of LoadPortraitFrame.");

            Expect(tex is AtlasTexture,
                $"Portrait texture is AtlasTexture (got: {tex.GetType().Name})");
        }

        await Input.WaitFrames(2);
    }

    private static void CollectPortraitRects(Node root, System.Collections.Generic.List<TextureRect> output)
    {
        // Class-select cards include a portrait TextureRect whose size is
        // driven by CustomMinimumSize = (92, 92). Skill icons live in a nested
        // HBoxContainer and have smaller min sizes (~32 typically). Filter to
        // the portrait-sized rects only.
        if (root is TextureRect tr && tr.Texture is not null &&
            tr.CustomMinimumSize.X >= 64 && tr.CustomMinimumSize.Y >= 64)
        {
            output.Add(tr);
        }
        foreach (var child in root.GetChildren())
            CollectPortraitRects(child, output);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Recursively searches the scene tree (from the viewport root) for any
    /// visible Label / Button / RichTextLabel matching the given text.
    /// </summary>
    private bool FindTextInTree(string text)
    {
        return SearchText(_sceneRoot.GetTree().Root, text);
    }

    private static bool SearchText(Node root, string text)
    {
        switch (root)
        {
            case Label l when l.Text == text && l.Visible: return true;
            case Button b when b.Text == text && b.Visible: return true;
            case RichTextLabel r when r.Text == text && r.Visible: return true;
        }
        foreach (var child in root.GetChildren())
            if (SearchText(child, text)) return true;
        return false;
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("ClassSelectTests");
}
#endif

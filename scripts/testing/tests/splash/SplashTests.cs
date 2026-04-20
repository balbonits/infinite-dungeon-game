#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the splash screen. Verifies keyboard navigation works with
/// Godot's built-in focus system and the game transitions to class select.
/// </summary>
public class SplashTests : GameTestBase
{
    public SplashTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ SplashTests ═══");

    [Setup]
    public async Task WaitForSplashScreen()
    {
        // Canonical cross-suite isolation (TEST-09): ResetToFreshSplash wipes
        // the sandbox, reloads the scene, and awaits splash reappearance. The
        // prior "just wait for splash" variant trusted that the preceding
        // suite left the scene on splash — SlotsFullTests breaks that
        // assumption (it ends with a SplashScreen that has seeded saves, and
        // a following [Setup] that just polls for splash can find splash is
        // already present but in a stale state, so the subsequent test body
        // presses Enter and lands on LoadGame instead of ClassSelect).
        if (!await ResetToFreshSplash())
            Expect(false, "[setup] ResetToFreshSplash did not land on splash");
    }

    [Test]
    public async Task Splash_HasNewGameButton()
    {
        var newGameBtn = Ui.FindButton("New Game");
        Expect(newGameBtn is not null, "New Game button exists");
        Expect(newGameBtn?.FocusMode == Control.FocusModeEnum.All, "New Game button is focusable");
    }

    [Test]
    public async Task Splash_KeyboardFocusesButton()
    {
        // SplashScreen auto-focuses first button after a short delay
        await WaitUntil(() => Ui.HasFocus, timeout: 2f, what: "auto-focus on splash button");

        var focused = Ui.FocusedControl;
        Expect(focused is Button, $"A button has focus (got: {focused?.GetType().Name ?? "null"})");
    }

    [Test]
    public async Task Splash_ArrowKeysNavigateButtons()
    {
        await WaitUntil(() => Ui.HasFocus, timeout: 2f);

        var firstFocused = Ui.FocusedButtonText;
        await Input.NavDown();
        await Input.WaitFrames(5);

        var secondFocused = Ui.FocusedButtonText;
        Expect(secondFocused != firstFocused,
            $"Arrow down changed focus: {firstFocused} → {secondFocused}");
    }

    [Test]
    public async Task Splash_EnterOnNewGameOpensClassSelect()
    {
        // Grab focus on New Game explicitly (test isolation)
        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }

        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        Expect(Ui.FocusedButtonText == "New Game", "New Game focused");

        await Input.PressEnter();
        await Input.WaitSeconds(0.5f);

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 2f, what: "ClassSelect to appear");
    }

    // Regression for the "New Game button doesn't work after Continue" bug:
    // clicking Continue opened LoadGameScreen; pressing Back re-showed splash
    // but left keyboard focus on a now-freed control, so Enter/S on New Game
    // silently did nothing. Fix: Main.ShowLoadGameScreen's Back handler calls
    // splash.FocusFirstButton() after restoring visibility.
    [Test]
    public async Task Splash_AfterContinueBack_NewGameStillWorks()
    {
        // Force a save to exist so Continue is enabled and clickable.
        DungeonGame.Autoloads.GameState.Instance.SelectedClass = PlayerClass.Warrior;
        DungeonGame.Autoloads.GameState.Instance.CurrentSaveSlot = 0;
        DungeonGame.Autoloads.SaveManager.Instance?.SaveToSlot(0);

        // Reset so the splash re-reads save state and enables Continue. Must
        // preserve the save just written — ResetToFreshSplash wipes the
        // sandbox by default, so opt out of the wipe here.
        bool atSplash = await ResetToFreshSplash(wipeSaves: false);
        if (!atSplash) { Expect(false, "Could not return to splash after save"); return; }

        var continueBtn = Ui.FindButton("Continue");
        if (continueBtn is null || continueBtn.Disabled)
        {
            Expect(false, "Continue is not enabled — save did not register");
            return;
        }

        continueBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();
        await WaitUntil(() => Ui.HasNodeOfType<LoadGameScreen>(),
            timeout: 2f, what: "LoadGameScreen to appear");

        // Press Escape to Back out of LoadGameScreen.
        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);

        // Splash should be visible again AND a button should be focused.
        await WaitUntil(() => Ui.HasFocus, timeout: 2f,
            what: "a splash button is focused after Back");

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        // If Back left focus orphaned, this would never trigger ClassSelect.
        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 2f, what: "ClassSelect appears after Continue→Back→New Game");
    }

    /// <summary>
    /// Regression for the "New Game doesn't work after deleting a save slot"
    /// bug reported 2026-04-19: user flow was splash → Continue → LoadGameScreen →
    /// delete slot → Back → New Game → silently did nothing. The original
    /// in-place RebuildSlots path had a timer lambda holding `this` across a
    /// possible user Back press, which could free the screen mid-transition and
    /// leave splash focus orphaned. The fix: deleting a slot now emits
    /// SlotDeleted, and Main recreates a fresh LoadGameScreen. This test
    /// asserts the recreate-on-delete lifecycle does NOT break splash's
    /// post-Back focus/input state.
    /// </summary>
    [Test]
    public async Task Splash_AfterDeleteSlotBack_NewGameStillWorks()
    {
        // Populate slot 0 so Continue is enabled AND we have a slot to delete.
        DungeonGame.Autoloads.GameState.Instance.SelectedClass = PlayerClass.Warrior;
        DungeonGame.Autoloads.GameState.Instance.CurrentSaveSlot = 0;
        DungeonGame.Autoloads.SaveManager.Instance?.SaveToSlot(0);

        // Preserve the seeded save across the reload — the default reset
        // wipes the sandbox, which would destroy the slot we just created
        // and leave Continue disabled for the test flow.
        bool atSplash = await ResetToFreshSplash(wipeSaves: false);
        if (!atSplash) { Expect(false, "Could not return to splash after save"); return; }

        var continueBtn = Ui.FindButton("Continue");
        if (continueBtn is null || continueBtn.Disabled)
        {
            Expect(false, "Continue is not enabled — save did not register");
            return;
        }
        continueBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();
        bool loaded = await WaitUntil(() => Ui.HasNodeOfType<LoadGameScreen>(),
            timeout: 2f, what: "LoadGameScreen appears after Continue");
        if (!loaded) return;
        await Input.WaitFrames(5);

        // Find the slot 0 delete button (red "X") and fire its Pressed signal
        // directly. GrabFocus + PressEnter on the X is unreliable under the
        // anchor-based positioning the button uses (tests showed focus
        // didn't grant in time); firing the signal exercises the same code
        // path (OpenDeleteDialog) without the focus race.
        var deleteBtn = Ui.FindButton("X");
        if (deleteBtn is null)
        {
            Expect(false, "Delete (X) button not found on populated slot");
            return;
        }
        deleteBtn.EmitSignal(BaseButton.SignalName.Pressed);
        await Input.WaitFrames(5);

        // DeleteConfirmDialog should open.
        bool dialogOpen = await WaitUntil(() => Ui.FindButton("Delete") is not null,
            timeout: 2f, what: "DeleteConfirmDialog appears");
        if (!dialogOpen) return;

        // Fire the Delete confirm signal directly (same reason).
        var confirmBtn = Ui.FindButton("Delete");
        if (confirmBtn is null) { Expect(false, "Delete confirm button missing"); return; }
        confirmBtn.EmitSignal(BaseButton.SignalName.Pressed);
        await Input.WaitFrames(5);

        // Wait for the SlotDeleted flow to tear down and recreate LoadGameScreen.
        // Main.CallDeferred(ShowLoadGameScreen) fires on the next idle frame;
        // wait on the observable condition (fresh screen mounted + _ready set)
        // instead of a fixed sleep so the test doesn't rely on wall-clock timing.
        await WaitUntil(
            () => Ui.HasNodeOfType<LoadGameScreen>() &&
                  Ui.FindButton("Load") is not null,
            timeout: 2f, what: "fresh LoadGameScreen mounted after delete");

        // Press Escape to go back to splash.
        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);

        // Splash should be visible again AND a button should be focused.
        bool focused = await WaitUntil(() => Ui.HasFocus, timeout: 2f,
            what: "a splash button is focused after delete+Back");
        if (!focused) return;

        // Now try New Game.
        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing on splash"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        // THE assertion: ClassSelect must appear. If this fails, the New Game
        // button click silently did nothing — the bug the user reported.
        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 2f, what: "ClassSelect appears after delete → Back → New Game");
    }

    /// <summary>
    /// Regression: LoadGameScreen populated slot cards must render portrait
    /// TextureRects with region-cropped AtlasTextures. Loading the full LPC
    /// sheet directly produced the tiled-grid bug (2026-04-19) where each
    /// card rendered the entire ~832x1344 animation atlas instead of a
    /// single 64x64 south-facing frame. Mirrors
    /// ClassSelect_PortraitsAreCroppedAtlasTextures.
    /// </summary>
    [Test]
    public async Task LoadGame_PopulatedCardsUseCroppedPortraits()
    {
        // Fabricate a save so Continue is enabled.
        DungeonGame.Autoloads.GameState.Instance.SelectedClass = PlayerClass.Warrior;
        DungeonGame.Autoloads.GameState.Instance.CurrentSaveSlot = 0;
        DungeonGame.Autoloads.SaveManager.Instance?.SaveToSlot(0);

        // Preserve the seeded save across the reload — default wipe would
        // destroy the slot we just created and fail the Continue-enabled check.
        bool atSplash = await ResetToFreshSplash(wipeSaves: false);
        if (!atSplash) { Expect(false, "Could not return to splash after save"); return; }

        var continueBtn = Ui.FindButton("Continue");
        if (continueBtn is null || continueBtn.Disabled)
        {
            Expect(false, "Continue is not enabled — save did not register");
            return;
        }
        continueBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        bool loaded = await WaitUntil(() => Ui.HasNodeOfType<LoadGameScreen>(),
            timeout: 2f, what: "LoadGameScreen to appear");
        if (!loaded) return;

        // Let the slot cards finish mounting after _Ready.
        await Input.WaitFrames(5);

        var loadScreen = Ui.FindNodeOfType<LoadGameScreen>();
        Expect(loadScreen is not null, "LoadGameScreen present");
        if (loadScreen is null) return;

        var portraits = new System.Collections.Generic.List<TextureRect>();
        CollectPortraitRects(loadScreen, portraits);

        Expect(portraits.Count >= 1,
            $"At least one portrait TextureRect found under LoadGameScreen (got: {portraits.Count})");

        foreach (var tr in portraits)
        {
            var tex = tr.Texture;
            Expect(tex is not null, "Portrait TextureRect has a texture");
            if (tex is null) continue;

            int w = tex.GetWidth();
            int h = tex.GetHeight();
            Expect(w <= 128 && h <= 128,
                $"Load-game portrait is region-cropped ({w}x{h} ≤ 128x128). " +
                "If this fails, CharacterCard is loading the full LPC sheet instead of LoadPortraitFrame.");

            Expect(tex is AtlasTexture,
                $"Load-game portrait is AtlasTexture (got: {tex.GetType().Name})");
        }

        // Back out so the next test starts from a clean splash.
        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);
    }

    private static void CollectPortraitRects(Node root, System.Collections.Generic.List<TextureRect> output)
    {
        if (root is TextureRect tr && tr.Texture is not null &&
            tr.CustomMinimumSize.X >= 64 && tr.CustomMinimumSize.Y >= 64)
        {
            output.Add(tr);
        }
        foreach (var child in root.GetChildren())
            CollectPortraitRects(child, output);
    }

    // SlotsFullDialog subflow extracted to splash/slots_full/SlotsFullTests.cs
    // per Chickensoft GameDemo convention (subfolder for subflow that grew
    // its own scenarios). See also docs/testing/features/splash.feature.

    /// <summary>
    /// Flow: clicking Tutorial on splash opens TutorialPanel. A basic
    /// "button → panel appears" flow test — catches wiring regressions
    /// where Tutorial.Pressed was disconnected or TutorialPanel.Open stopped
    /// pushing to the tree.
    /// </summary>
    [Test]
    public async Task Splash_ClickingTutorial_OpensTutorialPanel()
    {
        bool atSplash = await ResetToFreshSplash();
        if (!atSplash) { Expect(false, "Could not reach splash"); return; }

        var tutorialBtn = Ui.FindButton("Tutorial");
        if (tutorialBtn is null) { Expect(false, "Tutorial button missing"); return; }
        tutorialBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<TutorialPanel>(),
            timeout: 2f, what: "TutorialPanel appears after Tutorial click");

        // Escape should close the panel.
        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);

        Expect(!Ui.HasNodeOfType<TutorialPanel>() ||
               Ui.FindNodeOfType<TutorialPanel>()?.IsOpen == false,
            "TutorialPanel closed after Escape");
    }

    /// <summary>
    /// Flow: Settings button on splash opens SettingsPanel.
    /// </summary>
    [Test]
    public async Task Splash_ClickingSettings_OpensSettingsPanel()
    {
        bool atSplash = await ResetToFreshSplash();
        if (!atSplash) { Expect(false, "Could not reach splash"); return; }

        var settingsBtn = Ui.FindButton("Settings");
        if (settingsBtn is null) { Expect(false, "Settings button missing"); return; }
        settingsBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<SettingsPanel>(),
            timeout: 2f, what: "SettingsPanel appears after Settings click");

        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);

        Expect(!Ui.HasNodeOfType<SettingsPanel>() ||
               Ui.FindNodeOfType<SettingsPanel>()?.IsOpen == false,
            "SettingsPanel closed after Escape");
    }

    /// <summary>
    /// Flow: Continue button is Disabled when no save exists. Catches the
    /// ugly but real regression where a bogus save-detection returns true
    /// on a fresh install and Continue lets users click into a NullReference.
    /// </summary>
    [Test]
    public async Task Splash_ContinueDisabledWhenNoSaves()
    {
        // Wipe any saves from prior tests.
        var sm = DungeonGame.Autoloads.SaveManager.Instance;
        for (int i = 0; i < 3; i++) sm?.DeleteSlot(i);

        bool atSplash = await ResetToFreshSplash();
        if (!atSplash) { Expect(false, "Could not reach splash"); return; }

        var continueBtn = Ui.FindButton("Continue");
        Expect(continueBtn is not null, "Continue button exists");
        Expect(continueBtn?.Disabled == true,
            $"Continue is disabled when no saves exist (got Disabled={continueBtn?.Disabled})");
    }

    [CleanupAll]
    public void CleanupAll()
    {
        // Wipe any single-save fixture the LoadGame/DeleteSlot scenarios
        // wrote so downstream suites (DeathTests / NpcTests / TownTests)
        // start clean. Writes are already sandboxed to user://test_saves/
        // so real player saves under user://saves/ are never touched.
        var sm = DungeonGame.Autoloads.SaveManager.Instance;
        for (int i = 0; i < 3; i++)
            sm?.DeleteSlot(i);
        PrintSummary("SplashTests");
    }
}
#endif

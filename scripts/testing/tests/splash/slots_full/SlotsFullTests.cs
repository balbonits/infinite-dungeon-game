#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Sub-flow of the splash screen: the "All save slots are full" dialog.
/// Triggered when the user clicks New Game on splash while every slot
/// (0, 1, 2) already has a save on disk.
///
/// Per Chickensoft GameDemo convention, subflows get their own test file
/// under a subfolder (e.g. <c>test/src/menu/splash/SplashTest.cs</c> —
/// Chickensoft promotes splash out of <c>menu/</c> when it grows its own
/// scenarios). Same pattern here: splash/SplashTests.cs owns the bare
/// splash screen; this file owns the slots-full dialog subflow and its
/// two scenarios (Open-Load-Game vs Cancel).
///
/// Paired Gherkin: <c>docs/testing/features/splash.feature</c> §"3 full slots"
/// scenarios.
/// </summary>
public class SlotsFullTests : GameTestBase
{
    public SlotsFullTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ SlotsFullTests ═══");

    [Setup]
    public async Task ResetSplash()
    {
        // Use the canonical cross-suite isolation path (TEST-09): ResetToFreshSplash
        // reloads the scene and awaits splash reappearance. The earlier "just wait
        // for splash to exist" variant assumed other suites left the game on the
        // splash screen — fragile when test order shifts. Copilot PR #33 round-7
        // finding: bring SlotsFullTests in line with every other suite's setup.
        await ResetToFreshSplash();
    }

    /// <summary>
    /// With all 3 save slots full, clicking New Game MUST open the
    /// SlotsFullDialog with a clear forward path (Open Load Game).
    /// A silent no-op (the original 2026-04-19 bug) fails the "next
    /// screen driver reports visible" shape of this test.
    /// </summary>
    [Test]
    public async Task NewGameWithAllSlotsFull_ShowsSlotsFullDialog()
    {
        StartTest(nameof(NewGameWithAllSlotsFull_ShowsSlotsFullDialog));

        await SeedAllThreeSlotsFull();

        bool atSplash = await ResetToFreshSplash();
        if (!atSplash) { Expect(false, "Could not return to splash after seed"); return; }

        // Visual baseline + a11y lint on the starting state.
        await VerifyScreenshot("01-splash-with-3-slots", tolerancePercent: 2.0);
        var splash = Ui.FindNodeOfType<SplashScreen>();
        if (splash is not null) ExpectNoAccessibilityViolations(splash, "splash screen");

        Flow.Splash.ClickNewGame();

        bool opened = await WaitUntil(
            () => Flow.SlotsFull.IsShown,
            timeout: 2f,
            what: "SlotsFullDialog appears after New Game with 3 full slots");
        if (!opened)
        {
            await Screenshot("02-dialog-did-NOT-appear");
            return;
        }

        await VerifyScreenshot("02-slotsfull-dialog-open", tolerancePercent: 2.0);
        var dlg = Ui.FindNodeOfType<SlotsFullDialog>();
        if (dlg is not null) ExpectNoAccessibilityViolations(dlg, "slots-full dialog");

        Flow.SlotsFull.ClickOpenLoadGame();

        await WaitUntil(
            () => Flow.LoadGame.IsShown,
            timeout: 2f,
            what: "LoadGameScreen appears after Open Load Game");

        // Guard against the GameWindow.ReturnToPauseMenu default-true bug:
        // if the dialog's Close() falls through to ../PauseMenu.Show(),
        // pause menu will be open here even though we just went to LoadGame.
        Expect(!Ui.PauseMenuOpen, "PauseMenu is NOT open after Open Load Game");

        await VerifyScreenshot("03-loadgame-screen", tolerancePercent: 2.0);

        await Input.PressKey(Key.Escape);
        await Input.WaitSeconds(0.3f);
    }

    /// <summary>
    /// Companion scenario: the Cancel branch of the dialog must keep
    /// the user on splash, without opening LoadGameScreen.
    /// </summary>
    [Test]
    public async Task NewGameWithAllSlotsFull_CancelReturnsToSplash()
    {
        await SeedAllThreeSlotsFull();

        bool atSplash = await ResetToFreshSplash();
        if (!atSplash) { Expect(false, "Could not return to splash after seed"); return; }

        Flow.Splash.ClickNewGame();

        bool opened = await WaitUntil(
            () => Flow.SlotsFull.IsShown,
            timeout: 2f,
            what: "SlotsFullDialog appears for Cancel path");
        if (!opened) return;

        Flow.SlotsFull.ClickCancel();
        await Input.WaitFrames(5);

        Expect(!Flow.LoadGame.IsShown,
            "Cancel keeps user on splash — does NOT open LoadGameScreen");
        Expect(Ui.HasNodeOfType<SplashScreen>(),
            "SplashScreen still in tree after Cancel");
        // Cancel must not fall through to PauseMenu either (same bug shape
        // as the Open-Load-Game path — GameWindow.ReturnToPauseMenu=true
        // would have opened pause menu on close).
        Expect(!Ui.PauseMenuOpen, "PauseMenu is NOT open after Cancel");
    }

    /// <summary>
    /// Fill all 3 save slots so AreAllSlotsFull() returns true. Uses
    /// SaveManager directly (the underlying save pipeline) rather than
    /// driving through the UI — seeding is setup, not the flow under test.
    /// Writes go to user://test_saves/ per SaveManager.UseTestSandbox().
    /// </summary>
    private async Task SeedAllThreeSlotsFull()
    {
        var sm = DungeonGame.Autoloads.SaveManager.Instance;
        var state = DungeonGame.Autoloads.GameState.Instance;
        for (int i = 0; i < 3; i++)
        {
            state.SelectedClass = PlayerClass.Warrior;
            state.CurrentSaveSlot = i;
            sm?.SaveToSlot(i);
        }
        Expect(sm?.AreAllSlotsFull() == true, "All 3 save slots populated for test");
        await Input.WaitFrames(1);
    }

    [CleanupAll]
    public void CleanupAll()
    {
        // Wipe test-sandbox saves so downstream suites start clean.
        // (Test sandbox = user://test_saves/; the player's real saves
        // under user://saves/ are untouched.)
        var sm = DungeonGame.Autoloads.SaveManager.Instance;
        for (int i = 0; i < 3; i++)
            sm?.DeleteSlot(i);
        PrintSummary("SlotsFullTests");
    }
}
#endif

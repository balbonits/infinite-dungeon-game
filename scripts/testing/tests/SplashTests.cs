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
        // Wait for the game's splash screen to appear (Main._Ready → ShowSplashScreen)
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(), timeout: 3f, what: "SplashScreen to appear");
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

        // Reset so the splash re-reads save state and enables Continue.
        bool atSplash = await ResetToFreshSplash();
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

        bool atSplash = await ResetToFreshSplash();
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

    [CleanupAll]
    public void CleanupAll() => PrintSummary("SplashTests");
}
#endif

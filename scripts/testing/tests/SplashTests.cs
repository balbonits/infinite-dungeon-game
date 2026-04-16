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

    [CleanupAll]
    public void CleanupAll() => PrintSummary("SplashTests");
}
#endif

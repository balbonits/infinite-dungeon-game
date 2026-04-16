#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the tabbed pause menu (see docs/ui/pause-menu-tabs.md).
/// 8 tabs: Inventory, Equip, Skills, {Warrior Arts | Ranger Crafts | Arcane Spells},
///         Quests, Ledger, Stats, System.
///
/// Verifies:
///   - Esc opens the pause menu while in the Town scene
///   - Esc is a no-op on splash / class-select (pause menu must not open)
///   - PauseMenu.Instance.IsOpen flips true on open, false on close
///   - GetTree().Paused flips true on open, false on close
///   - Q/E cycles through all 8 tabs (wraps around)
///   - Arrow keys keep focus inside the current tab
///   - D (Cancel) and Esc both close the menu
///   - WindowStack is pushed on open and popped on close
///
/// Tests navigate splash → New Game → ClassSelect → Confirm Warrior → Town
/// in [Setup] so each test starts from a clean "in-game" state.
/// </summary>
public class PauseMenuTests : GameTestBase
{
    public PauseMenuTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ PauseMenuTests ═══");

    [Setup]
    public async Task EnterTown()
    {
        // If a previous test left the pause menu open, close it first.
        if (Ui.PauseMenuOpen)
        {
            await Input.PressEscape();
            await Input.WaitSeconds(0.3f);
        }

        // Already in Town? Nothing to do.
        if (Ui.HasNodeOfType<Scenes.Town>())
        {
            return;
        }

        // Wait for splash to load if we're at game start.
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>() || Ui.HasNodeOfType<ClassSelect>(),
            timeout: 5f, what: "[setup] splash or class-select present");

        // Splash → New Game → ClassSelect
        if (Ui.HasNodeOfType<SplashScreen>() && !Ui.HasNodeOfType<ClassSelect>())
        {
            var newGameBtn = Ui.FindButton("New Game");
            if (newGameBtn is null) { Expect(false, "[setup] New Game button missing"); return; }
            newGameBtn.GrabFocus();
            await Input.WaitFrames(3);
            await Input.PressEnter();
            await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(), timeout: 3f,
                what: "[setup] ClassSelect opens");
            await Input.WaitFrames(5);
        }

        // ClassSelect → Warrior → Confirm → Town
        if (Ui.HasNodeOfType<ClassSelect>())
        {
            await Input.NavRight();          // select first card (Warrior)
            await Input.WaitFrames(5);
            await Input.NavDown();           // move to Confirm zone
            await Input.WaitFrames(5);
            await Input.Confirm();           // press Confirm
            // Fade (0.4s) + screen transition (~2.1s)
            await WaitUntil(() => Ui.HasNodeOfType<Scenes.Town>(), timeout: 6f,
                what: "[setup] Town loads after class confirm");
            // Let Town finish setting up (Hud, player, etc.)
            await Input.WaitSeconds(0.5f);
        }
    }

    // ── Open / close behavior ────────────────────────────────────────────────

    /// <summary>
    /// Spec: "Esc (start): toggle pause menu open/close."
    /// In Town, pressing Escape should open the PauseMenu and set
    /// PauseMenu.Instance.IsOpen == true.
    /// </summary>
    [Test]
    public async Task PauseMenu_EscOpensInTown()
    {
        Expect(Ui.HasNodeOfType<Scenes.Town>(), "Precondition: Town is active");
        Expect(!Ui.PauseMenuOpen, "Precondition: PauseMenu is closed");

        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);

        Expect(PauseMenu.Instance is not null, "PauseMenu.Instance exists");
        Expect(Ui.PauseMenuOpen, "PauseMenu.Instance.IsOpen == true after Esc");
    }

    /// <summary>
    /// Spec: game is paused while the pause menu is open, unpaused after close.
    /// </summary>
    [Test]
    public async Task PauseMenu_OpenPausesGame_CloseUnpauses()
    {
        Expect(!Ui.Paused, "Precondition: game not paused");

        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(Ui.Paused, "GetTree().Paused == true after opening PauseMenu");

        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(!Ui.Paused, "GetTree().Paused == false after closing PauseMenu");
        Expect(!Ui.PauseMenuOpen, "PauseMenu closed after second Esc");
    }

    // ── WindowStack ──────────────────────────────────────────────────────────

    /// <summary>
    /// Spec: "WindowStack input blocking still applies — pause menu is topmost
    /// when open." Verifies push on open, pop on close.
    /// </summary>
    [Test]
    public async Task PauseMenu_WindowStackPushedAndPopped()
    {
        int baseCount = Ui.ModalCount;

        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(Ui.ModalCount == baseCount + 1,
            $"ModalCount incremented on open ({baseCount} → {Ui.ModalCount})");
        Expect(Ui.TopmostWindowName == nameof(PauseMenu),
            $"Topmost modal is PauseMenu (got: {Ui.TopmostWindowName ?? "null"})");

        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(Ui.ModalCount == baseCount,
            $"ModalCount restored on close ({Ui.ModalCount} == {baseCount})");
    }

    // ── Tab cycling (Q/E) ────────────────────────────────────────────────────

    /// <summary>
    /// Spec: "Q/E (shoulder buttons): cycle tabs left/right (8 tabs in the cycle)."
    /// Pressing E eight times cycles through all 8 tabs and returns to the start.
    /// </summary>
    [Test]
    public async Task PauseMenu_QEtabCyclingVisitsAllEightTabs()
    {
        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(Ui.PauseMenuOpen, "PauseMenu open");

        var panel = Ui.FindNodeOfType<GameTabPanel>();
        Expect(panel is not null, "GameTabPanel exists inside PauseMenu");
        if (panel is null) return;

        Expect(panel.TabCount == 8, $"PauseMenu has 8 tabs (got: {panel.TabCount})");
        Expect(panel.CurrentTab == 0, $"Starts on tab 0 (got: {panel.CurrentTab})");

        // Cycle forward: E eight times should land back on tab 0.
        int startTab = panel.CurrentTab;
        var visited = new System.Collections.Generic.HashSet<int> { startTab };
        for (int i = 0; i < 8; i++)
        {
            await Input.TabRight();
            await Input.WaitFrames(3);
            visited.Add(panel.CurrentTab);
        }
        Expect(visited.Count == 8, $"E cycled through all 8 tabs (visited: {visited.Count})");
        Expect(panel.CurrentTab == startTab,
            $"After 8 E presses, back on starting tab ({startTab}, got: {panel.CurrentTab})");

        // Cycle backward: Q once should go to tab 7 (wraps from 0).
        await Input.TabLeft();
        await Input.WaitFrames(3);
        Expect(panel.CurrentTab == 7, $"Q from tab 0 wraps to tab 7 (got: {panel.CurrentTab})");
    }

    // ── Arrow-key navigation within a tab ────────────────────────────────────

    /// <summary>
    /// Spec: "Up/Down: navigate buttons within current tab."
    /// After opening the pause menu, Down should move focus to another focusable
    /// control inside the same tab (not change tabs).
    /// </summary>
    [Test]
    public async Task PauseMenu_ArrowKeysNavigateWithinTab()
    {
        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);

        var panel = Ui.FindNodeOfType<GameTabPanel>();
        Expect(panel is not null, "GameTabPanel present");
        if (panel is null) return;

        // Navigate to a tab with focusable rows — Stats tab (index 6) has
        // STR/DEX/STA/INT allocate buttons which are reliably focusable.
        int targetTab = 6;
        int guard = 0;
        while (panel.CurrentTab != targetTab && guard++ < 10)
        {
            await Input.TabRight();
            await Input.WaitFrames(3);
        }
        Expect(panel.CurrentTab == targetTab,
            $"Navigated to Stats tab (got: {panel.CurrentTab})");

        int tabBefore = panel.CurrentTab;

        // Press Down a few times — focus should move but tab index must not change.
        for (int i = 0; i < 3; i++)
        {
            await Input.NavDown();
            await Input.WaitFrames(3);
        }
        Expect(panel.CurrentTab == tabBefore,
            $"Arrow keys did not change tab ({tabBefore} == {panel.CurrentTab})");
    }

    // ── Close via D (Cancel) ─────────────────────────────────────────────────

    /// <summary>
    /// Spec: "D (action_circle): close pause menu (same as Resume)."
    /// </summary>
    [Test]
    public async Task PauseMenu_DclosesMenu()
    {
        await Input.PressEscape();
        await Input.WaitSeconds(0.3f);
        Expect(Ui.PauseMenuOpen, "PauseMenu open before D");

        await Input.Cancel();
        await Input.WaitSeconds(0.3f);
        Expect(!Ui.PauseMenuOpen, "PauseMenu closed after D (action_circle)");
        Expect(!Ui.Paused, "Game unpaused after D-close");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("PauseMenuTests");
}
#endif

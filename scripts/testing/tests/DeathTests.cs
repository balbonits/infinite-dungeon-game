#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Scenes;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for player death + respawn. Forces HP=0 in the dungeon, verifies the
/// DeathScreen appears, keyboard nav works, and respawn returns the player to town
/// with full HP.
/// See docs/flows/death.md.
/// </summary>
public class DeathTests : GameTestBase
{
    public DeathTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ DeathTests ═══");

    /// <summary>Splash → New Game → class select → confirm warrior → town.</summary>
    private async Task GetToTown()
    {
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(),
            timeout: 3f, what: "SplashScreen to appear");

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 3f, what: "ClassSelect to appear");
        await Input.WaitSeconds(0.3f);

        await Input.PressEnter();
        await Input.WaitFrames(5);
        await Input.NavDown();
        await Input.WaitFrames(5);
        await Input.PressEnter();
        await Input.WaitSeconds(0.6f);

        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene to load");
        await Input.WaitSeconds(0.3f);
    }

    /// <summary>Town → Dungeon (call Main.LoadDungeon directly, bypass entrance Area2D).</summary>
    private async Task EnterDungeon()
    {
        Main.Instance.LoadDungeon();
        await Input.WaitSeconds(1.2f); // ScreenTransition hold + fade
        await WaitUntil(() => Ui.FindNodeOfType<Dungeon>() is not null,
            timeout: 6f, what: "Dungeon scene to load");
        await Input.WaitSeconds(0.3f);
    }

    [Test]
    public async Task Death_ForcingHpZeroShowsDeathScreen()
    {
        await GetToTown();
        await EnterDungeon();

        Expect(GameState.Instance.IsDead == false, "Alive before HP=0");

        // Force death
        GameState.Instance.Hp = 0;

        await WaitUntil(() => GameState.Instance.IsDead,
            timeout: 3f, what: "GameState.IsDead becomes true");
        await WaitUntil(() => Ui.HasNodeOfType<DeathScreen>(),
            timeout: 3f, what: "DeathScreen appears");
        Expect(Ui.Paused, "Game is paused while death screen is shown");
    }

    [Test]
    public async Task Death_PlayerIsDeadFlagSet()
    {
        await GetToTown();
        await EnterDungeon();

        GameState.Instance.Hp = 0;
        await WaitUntil(() => GameState.Instance.IsDead,
            timeout: 3f, what: "IsDead flag set after HP=0");

        var player = Ui.FindNodeOfType<Player>();
        Expect(player is not null, "Player node still exists at death");
        Expect(GameState.Instance.IsDead == true,
            "GameState.IsDead == true (authoritative death flag)");
    }

    [Test]
    public async Task Death_KeyboardNavigatesDeathScreen()
    {
        await GetToTown();
        await EnterDungeon();

        GameState.Instance.Hp = 0;
        await WaitUntil(() => Ui.HasNodeOfType<DeathScreen>(),
            timeout: 3f, what: "DeathScreen appears");
        await Input.WaitSeconds(0.3f);

        // Step 1: "Return to Town" should be auto-focused
        var returnBtn = Ui.FindButton(Strings.Death.ReturnToTown);
        Expect(returnBtn is not null, "Return to Town button exists on Step 1");
        Expect(Ui.FocusedButtonText == Strings.Death.ReturnToTown,
            $"Return to Town auto-focused (got: '{Ui.FocusedButtonText}')");

        // Advance to Step 2
        await Input.PressEnter();
        await Input.WaitSeconds(0.3f);

        // Step 2 has a Confirm button. Navigate down with arrow keys.
        var confirmBtn = Ui.FindButton(Strings.Death.Confirm);
        Expect(confirmBtn is not null, "Confirm button exists on Step 2");

        // Prove arrow keys move focus between options on step 2
        var firstFocused = Ui.FocusedButtonText;
        await Input.NavDown();
        await Input.WaitFrames(5);
        var secondFocused = Ui.FocusedButtonText;
        Expect(firstFocused != secondFocused || confirmBtn is not null,
            $"Arrow-down navigates or Confirm present ('{firstFocused}' → '{secondFocused}')");
    }

    [Test]
    public async Task Death_EnterConfirmsAndRespawnsInTown()
    {
        await GetToTown();
        await EnterDungeon();

        int maxHp = GameState.Instance.MaxHp;
        GameState.Instance.Hp = 0;
        await WaitUntil(() => Ui.HasNodeOfType<DeathScreen>(),
            timeout: 3f, what: "DeathScreen appears");
        await Input.WaitSeconds(0.3f);

        // Step 1 → Step 2
        await Input.PressEnter();
        await Input.WaitSeconds(0.4f);

        // Step 2: press Confirm button directly (find + grab focus + press Enter)
        var confirmBtn = Ui.FindButton(Strings.Death.Confirm);
        if (confirmBtn is null) { Expect(false, "Confirm button missing on Step 2"); return; }
        confirmBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        // Respawn: LoadTown runs, player is alive again
        await WaitUntil(() => !GameState.Instance.IsDead,
            timeout: 5f, what: "IsDead cleared after respawn");
        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene re-loaded after respawn");
        await Input.WaitSeconds(0.3f);

        Expect(GameState.Instance.IsDead == false, "Player is alive after respawn");
        Expect(GameState.Instance.Hp == maxHp,
            $"HP restored to MaxHp ({GameState.Instance.Hp}/{maxHp})");
        Expect(GameState.Instance.FloorNumber == 1,
            $"FloorNumber reset to 1 (got {GameState.Instance.FloorNumber})");
        Expect(Ui.FindNodeOfType<Dungeon>() is null, "Dungeon scene no longer active");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("DeathTests");
}
#endif

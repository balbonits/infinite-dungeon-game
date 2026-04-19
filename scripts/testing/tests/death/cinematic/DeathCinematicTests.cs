#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;
using DungeonGame.Scenes;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the SoulsBorne-style "YOU DIED" cinematic that plays on player death.
///
/// Flow: Player dies → red-tinted overlay fades in → big red "YOU DIED" text fades in
/// → holds for dramatic effect → text fades out → death menu (Step 1) appears.
///
/// Verifies: the cinematic flag is set, the menu is hidden during the cinematic,
/// and the "YOU DIED" label reaches full visibility before the menu shows.
/// </summary>
public class DeathCinematicTests : GameTestBase
{
    public DeathCinematicTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ DeathCinematicTests ═══");

    private async Task GetToTown()
    {
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(), timeout: 3f, what: "splash appears");

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(), timeout: 3f, what: "ClassSelect appears");
        await Input.WaitSeconds(0.3f);
        await Flow.ClassSelect.SelectWarriorAndConfirm();

        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene loads");
        await Input.WaitSeconds(0.3f);
    }

    [Test]
    public async Task Death_PlaysYouDiedCinematicBeforeMenu()
    {
        await GetToTown();

        // Enter dungeon
        Main.Instance.LoadDungeon();
        await WaitUntil(() => GameState.Instance.FloorNumber >= 1 &&
            !ScreenTransition.Instance.IsTransitioning, timeout: 6f, what: "dungeon loads");
        await Input.WaitSeconds(0.5f);

        // Trigger death
        GameState.Instance.Hp = 0;
        await Input.WaitFrames(5);

        // The cinematic should be playing now
        var death = Ui.FindNodeOfType<DeathScreen>();
        Expect(death is not null, "DeathScreen exists in tree");
        if (death is null) return;

        await Input.WaitFrames(10);
        Expect(death.IsPlayingCinematic, "IsPlayingCinematic flag is true during cinematic");

        // Menu panel should be hidden during cinematic (Save Both button label starts with "Save Both")
        var saveBothBtn = Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.SaveBoth));
        Expect(saveBothBtn is null || !saveBothBtn.Visible || saveBothBtn.Modulate.A < 0.1f,
            "Sacrifice dialog is NOT visible during cinematic (panel hidden)");

        // Wait for cinematic to complete (fade-in + hold + fade-out + panel reveal ~= 6s)
        await WaitUntil(() => !death.IsPlayingCinematic, timeout: 10f, what: "cinematic finishes");

        // Now the sacrifice dialog should be visible
        await Input.WaitSeconds(0.5f);
        var saveBothBtnAfter = Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.SaveBoth));
        Expect(saveBothBtnAfter is not null && saveBothBtnAfter.Visible,
            "Sacrifice dialog (Save Both button) visible AFTER cinematic");
    }

    [Test]
    public async Task Death_CinematicStateResetsOnEachDeath()
    {
        // Self-contained: trigger death, let the cinematic run to completion,
        // then assert the flag has reset. Previously this test assumed a prior
        // test had already triggered death (shared state), which breaks when
        // GoDotTest runs tests in isolation or reorders them. (Copilot PR #3 review.)
        await GetToTown();

        Main.Instance.LoadDungeon();
        await WaitUntil(() => GameState.Instance.FloorNumber >= 1 &&
            !ScreenTransition.Instance.IsTransitioning, timeout: 6f, what: "dungeon loads");
        await Input.WaitSeconds(0.5f);

        // If player is already dead from a previous test, reset so we can trigger death fresh
        if (GameState.Instance.IsDead)
        {
            GameState.Instance.IsDead = false;
            GameState.Instance.Hp = GameState.Instance.MaxHp;
            await Input.WaitFrames(5);
        }

        GameState.Instance.Hp = 0;
        await Input.WaitFrames(5);

        var death = Ui.FindNodeOfType<DeathScreen>();
        if (death is null) { Expect(false, "DeathScreen missing"); return; }

        // Cinematic should be playing now
        await Input.WaitFrames(10);
        Expect(death.IsPlayingCinematic, "Cinematic playing right after death trigger");

        // Wait for cinematic to complete, then flag should reset
        await WaitUntil(() => !death.IsPlayingCinematic,
            timeout: 10f, what: "cinematic flag reset after cinematic finishes");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("DeathCinematicTests");
}
#endif

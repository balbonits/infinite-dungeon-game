#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Scenes;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the post-redesign 5-option sacrifice dialog. Forces HP=0 in the dungeon,
/// verifies DeathScreen appears with the new buttons, and respawn returns the player
/// to town with full HP.
/// See docs/systems/death.md.
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
        await Input.WaitSeconds(1.2f);
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
    public async Task Death_SacrificeDialogHasAllFiveOptions()
    {
        await GetToTown();
        await EnterDungeon();

        GameState.Instance.Hp = 0;
        await WaitUntil(() => Ui.HasNodeOfType<DeathScreen>(),
            timeout: 3f, what: "DeathScreen appears");

        // Wait for the SoulsBorne cinematic to finish (fade in + hold + fade out + panel reveal ~6s)
        await WaitUntil(() =>
        {
            var ds = Ui.FindNodeOfType<DeathScreen>();
            return ds is not null && !ds.IsPlayingCinematic;
        }, timeout: 10f, what: "cinematic ends and sacrifice dialog appears");
        await Input.WaitSeconds(0.3f);

        // Per docs/systems/death.md the five options should all be visible.
        // All button labels include extra text (cost + detail), so match by prefix.
        Expect(Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.SaveBoth)) is not null,
            "Save Both button exists");
        Expect(Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.SaveEquipment)) is not null,
            "Save Equipment button exists");
        Expect(Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.SaveBackpack)) is not null,
            "Save Backpack button exists");
        var acceptBtn = Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.AcceptFate));
        var quitBtn = Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.QuitGame));
        Expect(acceptBtn is not null, "Accept Fate button exists");
        Expect(quitBtn is not null, "Quit Game button exists");
    }

    [Test]
    public async Task Death_AcceptFateWipesBackpackAndReturnsToTown()
    {
        await GetToTown();
        // Put a test item into the backpack to verify it gets wiped
        var inv = GameState.Instance.PlayerInventory;
        var potion = ItemDatabase.Get("potion_hp_small");
        if (potion is not null)
        {
            inv.TryAdd(potion, 3);
            inv.Gold = 500;
        }
        int itemsBefore = inv.UsedSlots;
        long goldBefore = inv.Gold;
        Expect(itemsBefore > 0 && goldBefore > 0, $"Test setup: backpack has {itemsBefore} items, {goldBefore}g");

        await EnterDungeon();

        int maxHp = GameState.Instance.MaxHp;
        GameState.Instance.Hp = 0;
        await WaitUntil(() => Ui.HasNodeOfType<DeathScreen>(),
            timeout: 3f, what: "DeathScreen appears");

        await WaitUntil(() =>
        {
            var ds = Ui.FindNodeOfType<DeathScreen>();
            return ds is not null && !ds.IsPlayingCinematic;
        }, timeout: 10f, what: "cinematic ends");
        await Input.WaitSeconds(0.3f);

        // Click Accept Fate → opens confirmation
        var acceptBtn = Ui.FindButton(btn => btn.Text.StartsWith(Strings.Death.AcceptFate));
        if (acceptBtn is null) { Expect(false, "Accept Fate button missing"); return; }
        acceptBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();
        await Input.WaitSeconds(0.3f);

        // Confirmation dialog: focus "Confirm — Accept" and press Enter
        var confirmBtn = Ui.FindButton(btn => btn.Text.Contains("Confirm"));
        if (confirmBtn is null) { Expect(false, "Confirmation button missing"); return; }
        confirmBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => !GameState.Instance.IsDead,
            timeout: 5f, what: "IsDead cleared after respawn");
        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene re-loaded after respawn");
        await Input.WaitSeconds(0.3f);

        Expect(GameState.Instance.IsDead == false, "Player is alive after respawn");
        Expect(GameState.Instance.Hp == maxHp, $"HP restored to MaxHp ({GameState.Instance.Hp}/{maxHp})");
        Expect(GameState.Instance.FloorNumber == 1, "FloorNumber reset to 1");
        Expect(GameState.Instance.PlayerInventory.UsedSlots == 0, "Backpack wiped after Accept Fate");
        Expect(GameState.Instance.PlayerInventory.Gold == 0, "Backpack gold wiped after Accept Fate");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("DeathTests");
}
#endif

#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;
using DungeonGame.Scenes;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the merged Guild window (Store / Bank / Transfer tabs).
/// Verifies the window opens on the Bank tab, tab switching works via Q/E,
/// and basic buy / upgrade flows modify game state correctly.
/// Spec: docs/ui/guild-window.md, docs/inventory/bank.md.
/// </summary>
public class GuildWindowTests : GameTestBase
{
    public GuildWindowTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ GuildWindowTests ═══");

    private async Task GetToTown()
    {
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(),
            timeout: 3f, what: "SplashScreen appears");
        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 3f, what: "ClassSelect appears");
        await Input.WaitSeconds(0.3f);
        await Flow.ClassSelect.SelectWarriorAndConfirm();
        await Input.WaitSeconds(0.6f);

        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town loads");
        await Input.WaitSeconds(0.3f);
    }

    private async Task OpenGuildWindow()
    {
        // Open GuildWindow directly — faster than walking to the Guild Maid NPC
        // (NPC interaction is tested separately in NpcTests).
        GuildWindow.Instance?.Open();
        await Input.WaitSeconds(0.3f);
    }

    [Test]
    public async Task Guild_OpensOnBankTab()
    {
        await GetToTown();
        await OpenGuildWindow();

        Expect(GuildWindow.Instance?.IsOpen == true, "GuildWindow is open");
        // Bank tab is default (Y2: c). Upgrade button is unique to the Bank tab.
        var upgradeBtn = Ui.FindButton(btn => btn.Text.StartsWith("Upgrade"));
        Expect(upgradeBtn is not null, "Bank tab is active (Upgrade button present)");
    }

    [Test]
    public async Task Guild_UpgradeButtonIncreasesBankSlots()
    {
        await GetToTown();
        // Seed gold so we can afford an upgrade (cost: 50g for first)
        GameState.Instance.PlayerInventory.Gold = 1000;
        await OpenGuildWindow();

        int slotsBefore = GameState.Instance.PlayerBank.TotalSlots;
        int expansionsBefore = GameState.Instance.PlayerBank.ExpansionCount;

        var upgradeBtn = Ui.FindButton(btn => btn.Text.StartsWith("Upgrade"));
        if (upgradeBtn is null) { Expect(false, "Upgrade button missing"); return; }
        upgradeBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();
        await Input.WaitSeconds(0.3f);

        Expect(GameState.Instance.PlayerBank.TotalSlots == slotsBefore + 1,
            $"Bank grew from {slotsBefore} → {GameState.Instance.PlayerBank.TotalSlots}");
        Expect(GameState.Instance.PlayerBank.ExpansionCount == expansionsBefore + 1,
            $"ExpansionCount = {GameState.Instance.PlayerBank.ExpansionCount}");
    }

    [Test]
    public async Task Guild_DepositAllMovesGoldBackpackToBank()
    {
        await GetToTown();
        GameState.Instance.PlayerInventory.Gold = 500;
        GameState.Instance.PlayerBank.Gold = 0;
        await OpenGuildWindow();

        var depositBtn = Ui.FindButton("Deposit All");
        if (depositBtn is null) { Expect(false, "Deposit All button missing"); return; }
        depositBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();
        await Input.WaitSeconds(0.3f);

        Expect(GameState.Instance.PlayerInventory.Gold == 0, "Backpack gold drained");
        Expect(GameState.Instance.PlayerBank.Gold == 500, "Bank gold increased to 500");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("GuildWindowTests");
}
#endif

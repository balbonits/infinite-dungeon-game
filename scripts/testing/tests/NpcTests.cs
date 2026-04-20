#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Scenes;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for NPC interaction flow: proximity → press S → NpcPanel opens →
/// service button opens the per-NPC window → D/Escape returns to the game.
/// See docs/flows/npc-interaction.md.
/// </summary>
public class NpcTests : GameTestBase
{
    public NpcTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ NpcTests ═══");

    /// <summary>
    /// Shared navigation: splash → New Game → class select → confirm warrior → town.
    /// </summary>
    private async Task GetToTown()
    {
        if (!await ResetToFreshSplash())
        {
            Expect(false, "[setup] could not reset to splash");
            return;
        }

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 3f, what: "ClassSelect to appear");
        await Input.WaitSeconds(0.3f);

        // NavRight first to enter zone 0 and auto-select Warrior — otherwise
        // Confirm fires on _selectedCard=null and Town never loads. See
        // TownTests.NavigateToTown for full analysis.
        await Input.NavRight();            // focus + auto-select Warrior
        await Input.WaitFrames(5);
        await Input.NavDown();             // focus Confirm
        await Input.WaitFrames(5);
        await Input.PressEnter();          // fire Confirm → LoadTown
        await Input.WaitSeconds(0.6f);

        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene to load");
        await Input.WaitSeconds(0.3f);
    }

    /// <summary>Walk the player to a specific NPC so proximity triggers.</summary>
    private async Task WalkToNpc(string npcName)
    {
        var player = Ui.FindNodeOfType<Player>();
        var npc = FindNpcByName(npcName);
        if (player is null || npc is null)
        {
            Expect(false, $"Player or NPC '{npcName}' missing before walk");
            return;
        }

        // Move player to within 12 units of NPC (< 40 Area2D radius).
        // Teleport is sufficient and deterministic for tests; proximity
        // is checked via Area2D body_entered when the physics step runs.
        // Offset is calibrated for the 32x32 square grid (post ADR-007);
        // the prior 20 px value assumed iso-centered coords.
        player.GlobalPosition = npc.GlobalPosition + new Vector2(12, 0);
        await Input.WaitFrames(6);
        // Nudge to force area re-evaluation
        await Input.Move(Vector2.Left, 0.1f);
        await Input.WaitFrames(6);
    }

    private Npc? FindNpcByName(string npcName)
    {
        var root = Ui.FindNodeOfType<Town>()?.GetTree()?.Root;
        if (root is null) return null;
        return SearchNpc(root, npcName);
    }

    private static Npc? SearchNpc(Node root, string name)
    {
        if (root is Npc npc && npc.NpcName == name) return npc;
        foreach (var c in root.GetChildren())
        {
            var found = SearchNpc(c, name);
            if (found is not null) return found;
        }
        return null;
    }

    [Test]
    public async Task Npc_PressSOpensNpcPanel()
    {
        await GetToTown();
        await WalkToNpc(Strings.Npcs.GuildMaid);

        int modalsBefore = Ui.ModalCount;
        Expect(modalsBefore == 0, $"No modals open before interaction (got {modalsBefore})");

        await Input.Confirm(); // action_cross = S
        await Input.WaitSeconds(0.4f); // panel fade-in

        Expect(Ui.HasNodeOfType<NpcPanel>() && NpcPanel.Instance?.IsOpen == true,
            "NpcPanel is open after pressing S near NPC");
        Expect(Ui.ModalCount >= 1, $"ModalCount >= 1 after panel opens (got {Ui.ModalCount})");
    }

    [Test]
    public async Task Npc_PanelHasServiceAndCancelButtons()
    {
        await GetToTown();
        await WalkToNpc(Strings.Npcs.GuildMaid);

        await Input.Confirm();
        await Input.WaitSeconds(0.4f);

        // Guild Maid service button = "Open Guild"
        var serviceBtn = Ui.FindButton(Strings.NpcServices.OpenGuild);
        var cancelBtn = Ui.FindButton(Strings.Ui.Cancel);
        Expect(serviceBtn is not null, "Service button ('Open Guild') exists in NpcPanel");
        Expect(cancelBtn is not null, "Cancel button exists in NpcPanel");
    }

    [Test]
    public async Task Npc_ServiceButtonIsFocusedByDefault()
    {
        await GetToTown();
        await WalkToNpc(Strings.Npcs.GuildMaid);

        await Input.Confirm();
        await Input.WaitSeconds(0.4f);

        // First button (service) should have focus
        var focused = Ui.FocusedButtonText;
        Expect(focused == Strings.NpcServices.OpenGuild,
            $"Service button focused by default (focused: '{focused}')");
    }

    [Test]
    public async Task Npc_PressEnterOpensServiceWindow()
    {
        await GetToTown();
        await WalkToNpc(Strings.Npcs.GuildMaid);

        await Input.Confirm();
        await Input.WaitSeconds(0.4f);

        // Service button is focused — press Enter to open GuildWindow
        await Input.PressEnter();
        await Input.WaitSeconds(0.5f);

        Expect(Ui.HasNodeOfType<GuildWindow>() && GuildWindow.Instance?.IsOpen == true,
            "GuildWindow opens after pressing Enter on service button");
        // NpcPanel should have closed (fade-out) by now
        await WaitUntil(() => NpcPanel.Instance?.IsOpen != true,
            timeout: 1f, what: "NpcPanel closed after service selected");
    }

    [Test]
    public async Task Npc_EscapeClosesServiceWindow()
    {
        await GetToTown();
        await WalkToNpc(Strings.Npcs.GuildMaid);

        await Input.Confirm();
        await Input.WaitSeconds(0.4f);
        await Input.PressEnter(); // open shop
        await Input.WaitSeconds(0.5f);

        Expect(GuildWindow.Instance?.IsOpen == true, "GuildWindow open before cancel");
        int modalsWithShop = Ui.ModalCount;
        Expect(modalsWithShop >= 1, $"ModalCount tracking shop window (got {modalsWithShop})");

        // Press D (action_circle = cancel) — closes GuildWindow
        await Input.Cancel();
        await Input.WaitSeconds(0.5f);

        await WaitUntil(() => GuildWindow.Instance?.IsOpen != true,
            timeout: 2f, what: "GuildWindow closed after D/Cancel");
        Expect(Ui.ModalCount == 0,
            $"ModalCount == 0 after all windows closed (got {Ui.ModalCount})");
        Expect(!Ui.AnyModalOpen, "No modals open after returning to game");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("NpcTests");
}
#endif

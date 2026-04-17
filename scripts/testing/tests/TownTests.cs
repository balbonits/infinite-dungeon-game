#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Scenes;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the Town scene. Verifies the player spawns, is in the correct group,
/// can move, NPCs are present, and the HUD renders (HP orbs + XP bar).
/// See docs/flows/town.md.
/// </summary>
public class TownTests : GameTestBase
{
    public TownTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ TownTests ═══");

    /// <summary>
    /// Shared navigation: splash → New Game → class select → confirm warrior → town.
    /// Each test runs in a fresh test harness, so we must walk through this every time.
    /// </summary>
    [Setup]
    public async Task NavigateToTown()
    {
        // Splash
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(),
            timeout: 3f, what: "SplashScreen to appear");

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        // Class select
        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 3f, what: "ClassSelect to appear");
        await Input.WaitSeconds(0.3f);

        // Select warrior (first card) with Enter, then press Confirm
        await Input.PressEnter();          // selects focused card (warrior)
        await Input.WaitFrames(5);
        await Input.NavDown();             // move focus to Confirm zone
        await Input.WaitFrames(5);
        await Input.PressEnter();          // press Confirm → LoadTown
        await Input.WaitSeconds(0.6f);

        // Wait for town to load (ScreenTransition finishes, Town node is in the tree)
        await WaitUntil(() => Ui.FindNodeOfType<Town>() is not null,
            timeout: 6f, what: "Town scene to load");
        // Extra buffer for spawns + HUD init
        await Input.WaitSeconds(0.3f);
    }

    [Test]
    public async Task Town_LoadsAfterClassSelect()
    {
        var town = Ui.FindNodeOfType<Town>();
        Expect(town is not null, "Town node exists in scene tree");
    }

    [Test]
    public async Task Town_PlayerInPlayerGroup()
    {
        var player = Ui.FindNodeOfType<Player>();
        Expect(player is not null, "Player node exists in the scene");
        Expect(player?.IsInGroup(Constants.Groups.Player) == true,
            $"Player is in group '{Constants.Groups.Player}'");
    }

    [Test]
    public async Task Town_PlayerCanMove()
    {
        var player = Ui.FindNodeOfType<Player>();
        if (player is null) { Expect(false, "Player missing"); return; }

        var startPos = player.GlobalPosition;
        await Input.Move(Vector2.Right, 0.5f);
        await Input.WaitFrames(3);

        var endPos = player.GlobalPosition;
        var delta = endPos.DistanceTo(startPos);
        Expect(delta > 1.0f,
            $"Player moved from {startPos} → {endPos} (Δ = {delta:F1})");
    }

    [Test]
    public async Task Town_AllExpectedNpcsExist()
    {
        string[] expected =
        {
            Strings.Npcs.GuildMaid,
            Strings.Npcs.Blacksmith,
            Strings.Npcs.VillageChief,
            Strings.Npcs.Teleporter,
        };

        // Collect all NPCs currently in the tree (walk from scene-tree root)
        var root = Ui.FindNodeOfType<Town>()?.GetTree()?.Root;
        if (root is null) { Expect(false, "Scene tree root not accessible"); return; }

        var found = new System.Collections.Generic.List<string>();
        void Walk(Node n)
        {
            if (n is Npc npc) found.Add(npc.NpcName);
            foreach (var c in n.GetChildren()) Walk(c);
        }
        Walk(root);

        foreach (var name in expected)
        {
            Expect(found.Contains(name), $"NPC '{name}' present in town");
        }
    }

    [Test]
    public async Task Town_HudIsVisible()
    {
        // HP orbs + XP bar come from the Hud control
        Expect(Ui.HasNodeOfType<Hud>(), "Hud control exists");
        Expect(Ui.HasNodeOfType<OrbDisplay>(), "OrbDisplay (HP orbs) exists");
        Expect(Ui.HasNodeOfType<XpBar>(), "XpBar exists");

        var hud = Ui.FindNodeOfType<Hud>();
        Expect(hud?.Visible == true, "Hud is visible");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("TownTests");
}
#endif

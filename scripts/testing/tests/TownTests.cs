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
        // Always start from a known-fresh splash — prior suites (ClassSelectTests,
        // DeathTests, etc.) may have left the game in Town/Dungeon/Death state.
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

        // Class select
        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(),
            timeout: 3f, what: "ClassSelect to appear");
        await Input.WaitSeconds(0.3f);

        // ClassSelect initial state: _focusIndex=-1, _focusZone=0, _selectedCard=null.
        // The pre-fix sequence fired PressEnter immediately, but zone 0 +
        // focus index < 0 means nothing is selected yet, so OnCardClicked
        // never ran and _selectedCard stayed null. Later NavDown→PressEnter
        // on the Confirm button fired OnConfirmPressed, which returns early
        // when _selectedCard is null → Town never loaded → every downstream
        // test timed out.
        //
        // Fix: NavRight first. ClassSelect._UnhandledInput on ui_right calls
        // MoveFocus(1), which enters zone 0, lands on card 0 (Warrior), and
        // auto-calls OnCardClicked on that card — _selectedCard is set to
        // Warrior. THEN NavDown moves to Confirm, PressEnter fires it.
        await Input.NavRight();            // focus + auto-select first card (Warrior)
        await Input.WaitFrames(5);
        await Input.NavDown();             // zone 0 → zone 1 (Confirm focused)
        await Input.WaitFrames(5);
        await Input.PressEnter();          // fire Confirm → OnConfirmPressed → LoadTown
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
        // 3-NPC roster per NPC-ROSTER-REWIRE-01; the legacy Teleporter NPC
        // was retired (its teleport service is now a Guild Maid menu action).
        string[] expected =
        {
            Strings.Npcs.GuildMaid,
            Strings.Npcs.Blacksmith,
            Strings.Npcs.VillageChief,
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

    // Guards the ADR-007 top-down pivot: Town's tileset must be Square.
    [Test]
    public async Task Town_Tileset_IsSquare32x32()
    {
        var town = Ui.FindNodeOfType<Town>();
        if (town is null) { Expect(false, "Town missing"); return; }
        var layer = town.GetNodeOrNull<TileMapLayer>("TileMapLayer");
        Expect(layer is not null, "Town has a TileMapLayer");
        Expect(layer!.TileSet?.TileShape == TileSet.TileShapeEnum.Square,
            $"Town TileShape is Square (got {layer.TileSet?.TileShape})");
        Expect(layer.TileSet?.TileSize == new Vector2I(32, 32),
            $"Town TileSize is 32x32 (got {layer.TileSet?.TileSize})");
    }

    // Regression guard: the iso building_* Sprite2Ds were deleted with
    // ADR-007. Catches re-introduction of pre-pivot placeholder art.
    [Test]
    public async Task Town_HasNoIsoBuildingSprites()
    {
        var town = Ui.FindNodeOfType<Town>();
        if (town is null) { Expect(false, "Town missing"); return; }

        var banned = new[] { "building_forge", "building_shop", "building_guild" };
        var strayPaths = new System.Collections.Generic.List<string>();
        void Walk(Node n)
        {
            if (n is Sprite2D sprite && sprite.Texture is Texture2D tex)
            {
                string path = tex.ResourcePath ?? "";
                foreach (var name in banned)
                    if (path.Contains(name)) strayPaths.Add(path);
            }
            foreach (var c in n.GetChildren()) Walk(c);
        }
        Walk(town);

        Expect(strayPaths.Count == 0,
            $"No iso building sprites in Town (found: {string.Join(",", strayPaths)})");
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

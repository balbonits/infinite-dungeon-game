namespace DungeonGame.Testing;

/// <summary>
/// Game state verification helpers for AutoPilot.
/// Everything the virtual player can CHECK about the current game state.
/// Each method delegates to AutoPilot.Assert() for logging and pass/fail tracking.
/// </summary>
public class AutoPilotAssertions
{
    private readonly AutoPilot _pilot;

    public AutoPilotAssertions(AutoPilot pilot)
    {
        _pilot = pilot;
    }

    // ── Generic ──────────────────────────────────────────────────────────────

    /// <summary>Custom assertion with description.</summary>
    public void That(bool condition, string description)
    {
        _pilot.Assert(condition, description);
    }

    // ── Player state ─────────────────────────────────────────────────────────

    public void Alive()
    {
        var gs = Autoloads.GameState.Instance;
        _pilot.Assert(gs != null, "GameState exists");
        if (gs == null) return;
        _pilot.Assert(gs.Hp > 0, $"Player alive (HP={gs.Hp})");
        _pilot.Assert(!gs.IsDead, "Player not dead");
    }

    public void Dead()
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.IsDead, "Player is dead");
    }

    public void AtLevel(int expected)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.Level == expected, $"Level == {expected} (actual={gs.Level})");
    }

    public void LevelAtLeast(int min)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.Level >= min, $"Level >= {min} (actual={gs.Level})");
    }

    public void OnFloor(int expected)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.FloorNumber == expected, $"Floor == {expected} (actual={gs.FloorNumber})");
    }

    public void FloorAtLeast(int min)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.FloorNumber >= min, $"Floor >= {min} (actual={gs.FloorNumber})");
    }

    // ── Resources ────────────────────────────────────────────────────────────

    public void HasGoldAtLeast(int min)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.PlayerInventory.Gold >= min,
            $"Gold >= {min} (actual={gs.PlayerInventory.Gold})");
    }

    public void HasXpAtLeast(int min)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.Xp >= min, $"XP >= {min} (actual={gs.Xp})");
    }

    public void HpAtLeast(int min)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.Hp >= min, $"HP >= {min} (actual={gs.Hp})");
    }

    // ── Inventory ────────────────────────────────────────────────────────────

    public void InventoryHas(string itemId)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        bool found = false;
        for (int i = 0; i < gs.PlayerInventory.SlotCount; i++)
        {
            var slot = gs.PlayerInventory.GetSlot(i);
            if (slot?.Item.Id == itemId) { found = true; break; }
        }
        _pilot.Assert(found, $"Inventory contains '{itemId}'");
    }

    public void InventoryCount(int expected)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.PlayerInventory.UsedSlots == expected,
            $"Inventory slots == {expected} (actual={gs.PlayerInventory.UsedSlots})");
    }

    public void InventoryNotEmpty()
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.PlayerInventory.UsedSlots > 0, "Inventory not empty");
    }

    // ── Combat / progression ─────────────────────────────────────────────────

    public void EnemiesExist()
    {
        int count = _pilot.GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        _pilot.Assert(count > 0, $"Enemies present (count={count})");
    }

    public void NoEnemies()
    {
        int count = _pilot.GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        _pilot.Assert(count == 0, "No enemies on screen");
    }

    public void AchievementUnlocked(string id)
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;
        _pilot.Assert(gs.Achievements.IsUnlocked(id), $"Achievement '{id}' unlocked");
    }

    // ── Scene state ──────────────────────────────────────────────────────────

    public void SceneContains(string nodeName)
    {
        var root = _pilot.GetTree().CurrentScene;
        if (root == null)
        {
            _pilot.Assert(false, $"No current scene (looking for '{nodeName}')");
            return;
        }
        var found = root.FindChild(nodeName, recursive: true, owned: false);
        _pilot.Assert(found != null, $"Scene contains node '{nodeName}'");
    }
}

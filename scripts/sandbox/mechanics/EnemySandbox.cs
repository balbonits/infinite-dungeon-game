using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Enemy
/// Shows species configs — stats, collision, sprite scale, AI params.
/// Headless: all species configs load without null refs.
/// Run: make sandbox SCENE=enemy
/// </summary>
public partial class EnemySandbox : SandboxBase
{
    protected override string SandboxTitle => "👾  Enemy Sandbox";

    protected override void _SandboxReady()
    {
        AddSectionLabel("Species");
        foreach (var species in System.Enum.GetValues<EnemySpecies>())
        {
            var s = species;
            AddButton(species.ToString(), () => ShowSpecies(s));
        }
        ShowSpecies(EnemySpecies.Goblin);
    }

    protected override void _Reset() => ShowSpecies(EnemySpecies.Goblin);

    private void ShowSpecies(EnemySpecies species)
    {
        var cfg = SpeciesDatabase.Get((int)species);

        Log($"── {species} ──");
        Log($"  Collision radius: {cfg.CollisionRadius}");
        Log($"  Hit area radius:  {cfg.HitAreaRadius}");
        Log($"  Sprite scale:     {cfg.SpriteScale}");
        Log($"  Sprite offset Y:  {cfg.SpriteOffsetY}");
        Log($"  Label offset Y:   {cfg.LabelOffsetY}");
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        foreach (var species in System.Enum.GetValues<EnemySpecies>())
        {
            var cfg = SpeciesDatabase.Get((int)species);
            Assert(cfg.CollisionRadius > 0, $"{species}: CollisionRadius > 0");
            Assert(cfg.HitAreaRadius > 0, $"{species}: HitAreaRadius > 0");
            Assert(cfg.SpriteScale > 0, $"{species}: SpriteScale > 0");
        }
        FinishHeadless();
    }
}

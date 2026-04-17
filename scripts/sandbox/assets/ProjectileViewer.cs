using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Projectile Viewer
/// Fires each projectile type across the screen in a loop.
/// Headless: verify all projectile textures load.
/// Run: make sandbox SCENE=projectile-viewer
/// </summary>
public partial class ProjectileViewer : SandboxBase
{
    protected override string SandboxTitle => "🏹  Projectile Viewer";

    private static readonly (string Name, string Path, Color Tint)[] Projectiles =
    [
        ("Arrow",        "res://assets/projectiles/arrow_8dir.png",         Colors.White),
        ("Magic Arrow",  "res://assets/projectiles/magic_arrow_8dir.png",  new Color(0.3f, 0.8f, 1f)),
        ("Magic Bolt",   "res://assets/projectiles/magic_bolt_8dir.png",   new Color(0.8f, 0.4f, 1f)),
        ("Fireball",     "res://assets/projectiles/fireball_8dir.png",     Colors.White),
        ("Frost Bolt",   "res://assets/projectiles/frost_bolt_8dir.png",   Colors.White),
        ("Lightning",    "res://assets/projectiles/lightning_8dir.png",    Colors.White),
        ("Stone Spike",  "res://assets/projectiles/stone_spike_8dir.png",  Colors.White),
        ("Energy Blast", "res://assets/projectiles/energy_blast_8dir.png", Colors.White),
        ("Shadow Bolt",  "res://assets/projectiles/shadow_bolt_8dir.png",  Colors.White),
    ];

    private float _speed = 300f;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Speed");
        AddSlider("Speed", 50, 800, _speed, v => _speed = v);
        AddButton("▶  Fire All", FireAll);
        FireAll();
        Log("Projectiles loop continuously — watch for missing textures.");
    }

    protected override void _Reset() => FireAll();

    private void FireAll()
    {
        // Remove old projectiles
        foreach (var child in GetChildren())
            if (child is Node2D n && n.Name.ToString().StartsWith("proj_"))
                n.QueueFree();

        for (int i = 0; i < Projectiles.Length; i++)
        {
            var (name, path, tint) = Projectiles[i];
            Texture2D? tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

            var node = new Sprite2D
            {
                Name = $"proj_{i}",
                Texture = tex,
                Modulate = tint,
                Position = new Vector2(350, 120 + i * 60),
            };
            AddChild(node);

            Log($"{name}: {(tex != null ? "✅" : "❌ missing")}");

            // Tween to animate across screen
            var tween = CreateTween().SetLoops();
            tween.TweenProperty(node, "position:x", 1100f, (1100f - 350f) / _speed)
                 .From(350f);
        }
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        foreach (var (name, path, _) in Projectiles)
        {
            bool exists = ResourceLoader.Exists(path);
            Assert(exists, $"{name}: exists at path");
            if (exists)
            {
                var tex = GD.Load<Texture2D>(path);
                Assert(tex != null, $"{name}: loads as Texture2D");
            }
        }
        FinishHeadless();
    }
}

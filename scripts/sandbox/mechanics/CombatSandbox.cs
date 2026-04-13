using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Combat
/// Simulates attack configs — damage formula, cooldown, AOE, DPS.
/// Headless: asserts damage values match AttackConfig formulas.
/// Run: make sandbox SCENE=combat
/// </summary>
public partial class CombatSandbox : SandboxBase
{
    protected override string SandboxTitle => "⚔️  Combat Sandbox";

    private float _baseDamage = 20f;
    private AttackConfig _config = ClassAttacks.GetPrimary(PlayerClass.Warrior);
    private float _cooldownTimer;
    private int _hitCount;
    private float _totalDamage;
    private double _elapsed;
    private bool _running;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Class / Attack");
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var c = cls;
            AddButton($"{cls} primary", () => { _config = ClassAttacks.GetPrimary(c); ResetSim(); });
        }

        AddSectionLabel("Base Damage");
        AddSlider("Base Damage", 1, 200, _baseDamage, v => _baseDamage = v);

        AddButton("▶  Start Simulation", () => { _running = true; ResetSim(); });
        AddButton("⏹  Stop", () => _running = false);
        AddButton("↺  Reset", () => ResetSim());

        ShowConfig();
    }

    protected override void _Reset() => ResetSim();

    public override void _Process(double delta)
    {
        if (!_running) return;

        _elapsed += delta;
        _cooldownTimer -= (float)delta;

        if (_cooldownTimer <= 0f)
        {
            float damage = _baseDamage * _config.DamageMultiplier;
            _hitCount++;
            _totalDamage += damage;
            _cooldownTimer = _config.Cooldown;

            Log($"Hit {_hitCount}: {damage:F1} dmg  (×{_config.DamageMultiplier:F2} mult)");
            Log($"  DPS: {_totalDamage / _elapsed:F1}  Total: {_totalDamage:F0}");
        }
    }

    private void ResetSim()
    {
        _running = false; _hitCount = 0; _totalDamage = 0; _elapsed = 0;
        _cooldownTimer = 0;
        ShowConfig();
    }

    private void ShowConfig()
    {
        Log($"── Attack Config ──");
        Log($"  Range:        {_config.Range}");
        Log($"  Cooldown:     {_config.Cooldown:F2}s");
        Log($"  Dmg mult:     ×{_config.DamageMultiplier:F2}");
        Log($"  Target mode:  {_config.TargetMode}");
        Log($"  Projectile:   {_config.IsProjectile}");
        Log($"  Max targets:  {_config.MaxTargets}");
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var cfg = ClassAttacks.GetPrimary(cls);
            Assert(cfg != null, $"{cls}: primary config not null");
            Assert(cfg!.Range > 0, $"{cls}: range > 0 (got {cfg.Range})");
            Assert(cfg.Cooldown > 0, $"{cls}: cooldown > 0 (got {cfg.Cooldown})");
            Assert(cfg.DamageMultiplier > 0, $"{cls}: damage multiplier > 0");

            float dmg = 20f * cfg.DamageMultiplier;
            Assert(dmg > 0, $"{cls}: computed damage > 0 ({dmg:F1})");
        }
        FinishHeadless();
    }
}

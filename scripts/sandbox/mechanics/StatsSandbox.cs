using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Stats
/// Sliders for STR/DEX/STA/INT. All derived stats update live.
/// Headless: asserts diminishing returns, class bonus stacking.
/// Run: make sandbox SCENE=stats
/// </summary>
public partial class StatsSandbox : SandboxBase
{
    protected override string SandboxTitle => "📊  Stats Sandbox";

    private StatBlock _stats = new();
    private Label _derived = null!;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Base Stats");
        AddSlider("STR", 0, 200, 0, v => { _stats.Str = (int)v; Refresh(); });
        AddSlider("DEX", 0, 200, 0, v => { _stats.Dex = (int)v; Refresh(); });
        AddSlider("STA", 0, 200, 0, v => { _stats.Sta = (int)v; Refresh(); });
        AddSlider("INT", 0, 200, 0, v => { _stats.Int = (int)v; Refresh(); });

        AddSectionLabel("Class Level Bonus");
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var c = cls;
            AddButton($"+ {cls} level", () => { _stats.ApplyClassLevelBonus(c); Refresh(); });
        }

        _derived = new Label { Position = new Vector2(500, 80) };
        _derived.AddThemeFontSizeOverride("font_size", Ui.UiTheme.FontSizes.Body);
        AddChild(_derived);

        Refresh();
    }

    protected override void _Reset() { _stats = new StatBlock(); Refresh(); }

    private void Refresh()
    {
        var s = _stats;
        string text =
            $"── Derived Stats ──\n" +
            $"STR {s.Str}  effective: {StatBlock.GetEffective(s.Str):F1}\n" +
            $"  Melee flat bonus:    {s.MeleeFlatBonus:F1}\n" +
            $"  Melee % boost:       {s.MeleePercentBoost:F1}%\n\n" +
            $"DEX {s.Dex}  effective: {StatBlock.GetEffective(s.Dex):F1}\n" +
            $"  Attack speed:        ×{s.AttackSpeedMultiplier:F3}\n" +
            $"  Dodge chance:        {s.DodgeChance * 100:F2}%\n\n" +
            $"STA {s.Sta}  effective: {StatBlock.GetEffective(s.Sta):F1}\n" +
            $"  Bonus max HP:        +{s.BonusMaxHp}\n" +
            $"  HP regen/sec:        {s.HpRegen:F2}\n\n" +
            $"INT {s.Int}  effective: {StatBlock.GetEffective(s.Int):F1}\n" +
            $"  Bonus max mana:      +{s.BonusMaxMana}\n" +
            $"  Mana regen/sec:      {s.ManaRegen:F2}\n" +
            $"  Spell damage:        ×{s.SpellDamageMultiplier:F3}\n" +
            $"  Processing eff.:     {s.ProcessingEfficiency * 100:F2}%\n\n" +
            $"Free points: {s.FreePoints}";

        _derived.Text = text;
        Log($"STR={s.Str} DEX={s.Dex} STA={s.Sta} INT={s.Int}  FreePoints={s.FreePoints}");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        Assert(StatBlock.GetEffective(0) == 0f, "effective(0) = 0");
        Assert(Mathf.IsEqualApprox(StatBlock.GetEffective(100), 50f, 0.01f), "effective(100) = 50 (K=100)");

        var sb = new StatBlock { Str = 100 };
        Assert(Mathf.IsEqualApprox(sb.MeleeFlatBonus, 75f, 0.1f), "MeleeFlatBonus at STR=100 ≈ 75");

        var warrior = new StatBlock();
        warrior.ApplyClassLevelBonus(PlayerClass.Warrior);
        warrior.ApplyClassLevelBonus(PlayerClass.Warrior);
        Assert(warrior.Str == 6, "Warrior ×2: STR=6");
        Assert(warrior.FreePoints == 6, "Warrior ×2: FreePoints=6");

        var mage = new StatBlock { Int = 50 };
        Assert(mage.SpellDamageMultiplier > 1f, "Spell damage > 1 with INT>0");

        FinishHeadless();
    }
}

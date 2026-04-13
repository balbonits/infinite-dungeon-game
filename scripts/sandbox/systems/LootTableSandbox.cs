using Godot;
using System.Collections.Generic;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Loot Table
/// Simulate N enemy kills at a given level. Shows drop rate, gold distribution, item frequency.
/// Headless checks: drop rate within expected bounds across levels.
/// Run: make sandbox SCENE=loot-table
/// </summary>
public partial class LootTableSandbox : SandboxBase
{
    protected override string SandboxTitle => "🎲  Loot Table Sandbox";

    private int _enemyLevel = 10;
    private int _killCount = 500;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Parameters");
        AddSlider("Enemy Level", 1, 50, _enemyLevel, v => _enemyLevel = (int)v);
        AddSlider("Kill Count", 10, 1000, _killCount, v => _killCount = (int)v);
        AddButton("▶  Simulate", Simulate);
        Simulate();
    }

    protected override void _Reset() => Simulate();

    private void Simulate()
    {
        int drops = 0;
        int totalGold = 0;
        int minGold = int.MaxValue;
        int maxGold = 0;
        var itemFreq = new Dictionary<string, int>();

        for (int i = 0; i < _killCount; i++)
        {
            int gold = LootTable.GetGoldDrop(_enemyLevel);
            totalGold += gold;
            minGold = Mathf.Min(minGold, gold);
            maxGold = Mathf.Max(maxGold, gold);

            var item = LootTable.RollItemDrop(_enemyLevel);
            if (item != null)
            {
                drops++;
                if (!itemFreq.ContainsKey(item.Name)) itemFreq[item.Name] = 0;
                itemFreq[item.Name]++;
            }
        }

        float dropRate = (float)drops / _killCount * 100f;
        float expectedChance = Mathf.Min(30f, 8f + _enemyLevel * 1f);

        Log($"Level {_enemyLevel}  ×{_killCount} kills");
        Log($"  Expected drop %:  {expectedChance:F1}%");
        Log($"  Actual drop %:    {dropRate:F1}%  ({drops}/{_killCount})");
        Log($"  Gold — avg: {totalGold / _killCount}  min: {minGold}  max: {maxGold}");
        Log("");
        Log("  Item drops:");
        foreach (var (name, count) in itemFreq)
            Log($"    {name}: {count}×");
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        // Test drop rate is within ±8% of expected for each level band
        foreach (int level in new[] { 1, 10, 22, 50 })
        {
            int kills = 2000;
            int drops = 0;
            for (int i = 0; i < kills; i++)
                if (LootTable.RollItemDrop(level) != null) drops++;

            float actual = (float)drops / kills * 100f;
            float expected = Mathf.Min(30f, 8f + level * 1f);
            bool inRange = actual >= expected - 8f && actual <= expected + 8f;
            Assert(inRange, $"Level {level}: drop rate {actual:F1}% within ±8% of {expected:F1}%");
        }

        // Gold always ≥ base (2+level) with no negatives
        for (int level = 1; level <= 20; level++)
        {
            int gold = LootTable.GetGoldDrop(level);
            Assert(gold >= 2 + level, $"Level {level}: gold {gold} ≥ base {2 + level}");
        }

        FinishHeadless();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Quest data model and tracking. Radiant quests from the Adventure Guild.
/// 3 quests available at a time, procedurally generated, scale with deepest floor.
/// Pure logic — no Godot dependency.
/// </summary>
public enum QuestType
{
    Kill,       // "Slay N enemies on floor X"
    ClearFloor, // "Clear all enemies on floor X"
    DepthPush,  // "Reach floor X for the first time"
}

public record QuestDef
{
    public string Id { get; init; } = "";
    public QuestType Type { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public int TargetFloor { get; init; }
    public int TargetCount { get; init; }
    public int GoldReward { get; init; }
    public int XpReward { get; init; }
}

public class QuestState
{
    public string QuestId { get; init; } = "";
    public int Progress { get; set; }
    public bool IsComplete { get; set; }
}

public class QuestTracker
{
    public const int MaxActiveQuests = 3;

    private readonly List<QuestState> _activeQuests = new();
    private readonly List<QuestDef> _questDefs = new();
    private readonly HashSet<string> _completedQuestIds = new();

    public IReadOnlyList<QuestState> ActiveQuests => _activeQuests;
    public IReadOnlyList<QuestDef> QuestDefs => _questDefs;

    /// <summary>
    /// Generate new quests scaled to the player's deepest floor.
    /// </summary>
    public void GenerateQuests(int deepestFloor)
    {
        _activeQuests.Clear();
        _questDefs.Clear();

        var rng = Random.Shared;
        var types = new[] { QuestType.Kill, QuestType.ClearFloor, QuestType.DepthPush };

        for (int i = 0; i < MaxActiveQuests; i++)
        {
            var type = types[rng.Next(types.Length)];
            var def = CreateQuest(type, deepestFloor, i, rng);
            _questDefs.Add(def);
            _activeQuests.Add(new QuestState { QuestId = def.Id });
        }
    }

    private static QuestDef CreateQuest(QuestType type, int deepestFloor, int index, Random rng)
    {
        string id = $"quest_{type}_{deepestFloor}_{index}";
        int targetFloor = Math.Max(1, deepestFloor - rng.Next(5));

        return type switch
        {
            QuestType.Kill => new QuestDef
            {
                Id = id,
                Type = type,
                Title = $"Slay {10 + deepestFloor} enemies",
                Description = $"Defeat {10 + deepestFloor} enemies on floor {targetFloor} or deeper.",
                TargetFloor = targetFloor,
                TargetCount = 10 + deepestFloor,
                GoldReward = 50 + deepestFloor * 10,
                XpReward = 30 + deepestFloor * 8,
            },
            QuestType.ClearFloor => new QuestDef
            {
                Id = id,
                Type = type,
                Title = $"Clear Floor {targetFloor}",
                Description = $"Defeat all enemies on floor {targetFloor}.",
                TargetFloor = targetFloor,
                TargetCount = 1,
                GoldReward = 80 + deepestFloor * 15,
                XpReward = 50 + deepestFloor * 12,
            },
            QuestType.DepthPush => new QuestDef
            {
                Id = id,
                Type = type,
                Title = $"Reach Floor {deepestFloor + 1}",
                Description = $"Descend to floor {deepestFloor + 1} for the first time.",
                TargetFloor = deepestFloor + 1,
                TargetCount = 1,
                GoldReward = 100 + deepestFloor * 20,
                XpReward = 80 + deepestFloor * 15,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    /// <summary>
    /// Record an enemy kill. Updates Kill quest progress.
    /// </summary>
    public QuestDef? RecordEnemyKill(int floorNumber)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            var state = _activeQuests[i];
            if (state.IsComplete) continue;
            var def = _questDefs[i];
            if (def.Type != QuestType.Kill) continue;
            if (floorNumber < def.TargetFloor) continue;

            state.Progress++;
            if (state.Progress >= def.TargetCount)
            {
                state.IsComplete = true;
                _completedQuestIds.Add(def.Id);
                return def;
            }
        }
        return null;
    }

    /// <summary>
    /// Record a floor clear. Updates ClearFloor quest progress.
    /// </summary>
    public QuestDef? RecordFloorClear(int floorNumber)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            var state = _activeQuests[i];
            if (state.IsComplete) continue;
            var def = _questDefs[i];
            if (def.Type != QuestType.ClearFloor || def.TargetFloor != floorNumber) continue;

            state.Progress = 1;
            state.IsComplete = true;
            _completedQuestIds.Add(def.Id);
            return def;
        }
        return null;
    }

    /// <summary>
    /// Record reaching a new floor. Updates DepthPush quest progress.
    /// </summary>
    public QuestDef? RecordFloorReached(int floorNumber)
    {
        for (int i = 0; i < _activeQuests.Count; i++)
        {
            var state = _activeQuests[i];
            if (state.IsComplete) continue;
            var def = _questDefs[i];
            if (def.Type != QuestType.DepthPush || def.TargetFloor != floorNumber) continue;

            state.Progress = 1;
            state.IsComplete = true;
            _completedQuestIds.Add(def.Id);
            return def;
        }
        return null;
    }

    /// <summary>Check if all active quests are complete (triggers refresh).</summary>
    public bool AllComplete => _activeQuests.All(q => q.IsComplete);

    // --- Save/Load ---
    public SavedQuestData CaptureState()
    {
        var saved = new List<SavedQuest>();
        for (int i = 0; i < _activeQuests.Count && i < _questDefs.Count; i++)
        {
            saved.Add(new SavedQuest
            {
                Type = _questDefs[i].Type,
                Title = _questDefs[i].Title,
                Description = _questDefs[i].Description,
                TargetFloor = _questDefs[i].TargetFloor,
                TargetCount = _questDefs[i].TargetCount,
                GoldReward = _questDefs[i].GoldReward,
                XpReward = _questDefs[i].XpReward,
                Progress = _activeQuests[i].Progress,
                IsComplete = _activeQuests[i].IsComplete,
            });
        }
        return new SavedQuestData { Quests = saved.ToArray() };
    }

    public void RestoreState(SavedQuestData data)
    {
        _activeQuests.Clear();
        _questDefs.Clear();

        for (int i = 0; i < data.Quests.Length; i++)
        {
            var sq = data.Quests[i];
            string id = $"quest_restored_{i}";
            _questDefs.Add(new QuestDef
            {
                Id = id,
                Type = sq.Type,
                Title = sq.Title,
                Description = sq.Description,
                TargetFloor = sq.TargetFloor,
                TargetCount = sq.TargetCount,
                GoldReward = sq.GoldReward,
                XpReward = sq.XpReward,
            });
            _activeQuests.Add(new QuestState
            {
                QuestId = id,
                Progress = sq.Progress,
                IsComplete = sq.IsComplete,
            });
        }
    }
}

public record SavedQuestData
{
    public SavedQuest[] Quests { get; init; } = Array.Empty<SavedQuest>();
}

public record SavedQuest
{
    public QuestType Type { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public int TargetFloor { get; init; }
    public int TargetCount { get; init; }
    public int GoldReward { get; init; }
    public int XpReward { get; init; }
    public int Progress { get; init; }
    public bool IsComplete { get; init; }
}

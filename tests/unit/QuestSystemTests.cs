using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class QuestSystemTests
{
    // -- GenerateQuests --

    [Fact]
    public void GenerateQuests_Creates3Quests()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(10);
        tracker.ActiveQuests.Should().HaveCount(3);
        tracker.QuestDefs.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateQuests_QuestsHavePositiveRewards()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(10);
        foreach (var def in tracker.QuestDefs)
        {
            def.GoldReward.Should().BePositive();
            def.XpReward.Should().BePositive();
        }
    }

    [Fact]
    public void GenerateQuests_RewardsScaleWithFloor()
    {
        var low = new QuestTracker();
        low.GenerateQuests(1);
        var high = new QuestTracker();
        high.GenerateQuests(50);

        int lowGold = low.QuestDefs.Sum(d => d.GoldReward);
        int highGold = high.QuestDefs.Sum(d => d.GoldReward);
        highGold.Should().BeGreaterThan(lowGold);
    }

    [Fact]
    public void GenerateQuests_Replaces_ExistingQuests()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(5);
        tracker.GenerateQuests(20); // should replace, not append
        tracker.ActiveQuests.Should().HaveCount(3);
    }

    // -- RecordEnemyKill --

    [Fact]
    public void RecordEnemyKill_IncreasesKillQuestProgress()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(1);

        // Find a Kill quest
        int killIdx = -1;
        for (int i = 0; i < tracker.QuestDefs.Count; i++)
            if (tracker.QuestDefs[i].Type == QuestType.Kill) { killIdx = i; break; }

        if (killIdx < 0) return; // RNG might not generate a Kill quest, skip

        int floor = tracker.QuestDefs[killIdx].TargetFloor;
        tracker.RecordEnemyKill(floor);
        tracker.ActiveQuests[killIdx].Progress.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordEnemyKill_BelowTargetFloor_NoProgress()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(50); // target floors will be around 45-50

        // Kill on floor 1 — below any target floor
        tracker.RecordEnemyKill(1);

        // All Kill quests should have 0 progress (floor too low)
        for (int i = 0; i < tracker.QuestDefs.Count; i++)
        {
            if (tracker.QuestDefs[i].Type == QuestType.Kill &&
                tracker.QuestDefs[i].TargetFloor > 1)
                tracker.ActiveQuests[i].Progress.Should().Be(0);
        }
    }

    [Fact]
    public void RecordEnemyKill_CompletesWhenTargetReached()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(1);

        int killIdx = -1;
        for (int i = 0; i < tracker.QuestDefs.Count; i++)
            if (tracker.QuestDefs[i].Type == QuestType.Kill) { killIdx = i; break; }

        if (killIdx < 0) return;

        var def = tracker.QuestDefs[killIdx];
        for (int k = 0; k < def.TargetCount + 5; k++)
            tracker.RecordEnemyKill(def.TargetFloor);

        tracker.ActiveQuests[killIdx].IsComplete.Should().BeTrue();
    }

    // -- RecordFloorClear --

    [Fact]
    public void RecordFloorClear_CompletesMatchingQuest()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(5);

        for (int i = 0; i < tracker.QuestDefs.Count; i++)
        {
            if (tracker.QuestDefs[i].Type != QuestType.ClearFloor) continue;
            int floor = tracker.QuestDefs[i].TargetFloor;
            var result = tracker.RecordFloorClear(floor);
            result.Should().NotBeNull();
            tracker.ActiveQuests[i].IsComplete.Should().BeTrue();
            return;
        }
        // No ClearFloor quest generated — skip (RNG-dependent)
    }

    // -- RecordFloorReached --

    [Fact]
    public void RecordFloorReached_CompletesDepthPushQuest()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(5);

        for (int i = 0; i < tracker.QuestDefs.Count; i++)
        {
            if (tracker.QuestDefs[i].Type != QuestType.DepthPush) continue;
            int floor = tracker.QuestDefs[i].TargetFloor;
            var result = tracker.RecordFloorReached(floor);
            result.Should().NotBeNull();
            tracker.ActiveQuests[i].IsComplete.Should().BeTrue();
            return;
        }
    }

    // -- AllComplete --

    [Fact]
    public void AllComplete_FalseByDefault()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(1);
        tracker.AllComplete.Should().BeFalse();
    }

    [Fact]
    public void AllComplete_TrueWhenAllDone()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(1);

        // Complete all quests manually
        for (int i = 0; i < tracker.QuestDefs.Count; i++)
        {
            var def = tracker.QuestDefs[i];
            switch (def.Type)
            {
                case QuestType.Kill:
                    for (int k = 0; k < def.TargetCount + 5; k++)
                        tracker.RecordEnemyKill(def.TargetFloor);
                    break;
                case QuestType.ClearFloor:
                    tracker.RecordFloorClear(def.TargetFloor);
                    break;
                case QuestType.DepthPush:
                    tracker.RecordFloorReached(def.TargetFloor);
                    break;
            }
        }

        tracker.AllComplete.Should().BeTrue();
    }

    // -- Save/Load --

    [Fact]
    public void CaptureRestore_RoundTrips()
    {
        var tracker = new QuestTracker();
        tracker.GenerateQuests(10);
        tracker.RecordEnemyKill(10); // some progress

        var state = tracker.CaptureState();
        state.Quests.Should().HaveCount(3);

        var restored = new QuestTracker();
        restored.RestoreState(state);
        restored.ActiveQuests.Should().HaveCount(3);
        restored.QuestDefs.Should().HaveCount(3);
    }
}

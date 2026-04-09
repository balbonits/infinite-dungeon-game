using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DungeonGame.Dungeon;

namespace DungeonGame.Tests;

public class DungeonGeneratorTests
{
    [Fact]
    public void FullPipeline_Connected()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42);

        var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
        var exit = floor.Rooms.First(r => r.Kind == RoomKind.Exit || r.Kind == RoomKind.Boss);
        var reachable = FloodFill(floor, entrance.CenterX, entrance.CenterY);

        Assert.True(reachable.Contains((exit.CenterX, exit.CenterY)),
            "Exit not reachable from entrance");
    }

    [Fact]
    public void FullPipeline_Deterministic()
    {
        var gen = new DungeonGenerator();
        var floor1 = gen.Generate(42, 5);
        var floor2 = gen.Generate(42, 5);

        Assert.Equal(floor1.Rooms.Count, floor2.Rooms.Count);
        for (int x = 0; x < floor1.Width; x++)
            for (int y = 0; y < floor1.Height; y++)
                Assert.Equal(floor1.Tiles[x, y], floor2.Tiles[x, y]);
    }

    [Fact]
    public void BossFloor_HasBossRoom()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42, 10);
        Assert.Contains(floor.Rooms, r => r.Kind == RoomKind.Boss);
    }

    [Fact]
    public void BossFloor_BossIsExitRoom()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42, 10);
        // Boss room should exist, and no separate exit room (boss blocks exit)
        Assert.Contains(floor.Rooms, r => r.Kind == RoomKind.Boss);
        Assert.DoesNotContain(floor.Rooms, r => r.Kind == RoomKind.Exit);
    }

    [Fact]
    public void BossFloor_20_HasBossRoom()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(99, 20);
        Assert.Contains(floor.Rooms, r => r.Kind == RoomKind.Boss);
    }

    [Fact]
    public void NonBossFloor_NoBossRoom()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42, 7);
        Assert.DoesNotContain(floor.Rooms, r => r.Kind == RoomKind.Boss);
    }

    [Fact]
    public void HasEntranceAndExit()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42);
        Assert.Single(floor.Rooms.Where(r => r.Kind == RoomKind.Entrance));
        Assert.Single(floor.Rooms.Where(r => r.Kind == RoomKind.Exit));
    }

    [Fact]
    public void AllRooms_Reachable()
    {
        var gen = new DungeonGenerator();
        for (int seed = 0; seed < 20; seed++)
        {
            var floor = gen.Generate(seed);
            var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
            var reachable = FloodFill(floor, entrance.CenterX, entrance.CenterY);

            foreach (var room in floor.Rooms)
                Assert.True(reachable.Contains((room.CenterX, room.CenterY)),
                    $"Seed {seed}: Room at ({room.CenterX},{room.CenterY}) kind={room.Kind} unreachable");
        }
    }

    // --- Progressive Floor Sizing ---

    [Fact]
    public void FloorSize_Floor1_IsBase()
    {
        var (w, h) = DungeonGenerator.CalculateFloorSize(1);
        Assert.Equal(50, w);
        Assert.Equal(100, h);
    }

    [Fact]
    public void FloorSize_GrowsWithZone()
    {
        var (w1, h1) = DungeonGenerator.CalculateFloorSize(1);
        var (w11, h11) = DungeonGenerator.CalculateFloorSize(11);
        var (w21, h21) = DungeonGenerator.CalculateFloorSize(21);

        Assert.True(w11 > w1, "Zone 2 should be wider than zone 1");
        Assert.True(w21 > w11, "Zone 3 should be wider than zone 2");
        Assert.True(h11 > h1, "Zone 2 should be taller than zone 1");
        Assert.True(h21 > h11, "Zone 3 should be taller than zone 2");
    }

    [Fact]
    public void FloorSize_IntraZoneRamp()
    {
        var (w1, h1) = DungeonGenerator.CalculateFloorSize(1);
        var (w5, h5) = DungeonGenerator.CalculateFloorSize(5);
        var (w10, h10) = DungeonGenerator.CalculateFloorSize(10);

        Assert.True(w5 >= w1, "Floor 5 should be >= floor 1");
        Assert.True(w10 > w5, "Floor 10 should be > floor 5");
    }

    [Fact]
    public void FloorSize_ZoneJump()
    {
        var (w10, _) = DungeonGenerator.CalculateFloorSize(10);
        var (w11, _) = DungeonGenerator.CalculateFloorSize(11);

        // Floor 11 (zone 2 base) should be larger than floor 10 (zone 1 end)
        Assert.True(w11 > w10, "Zone jump: floor 11 should be wider than floor 10");
    }

    [Fact]
    public void FloorSize_Capped()
    {
        var (w, h) = DungeonGenerator.CalculateFloorSize(500);
        Assert.True(w <= 150, $"Width {w} exceeds cap 150");
        Assert.True(h <= 300, $"Height {h} exceeds cap 300");
    }

    [Fact]
    public void FloorSize_GeneratedFloorMatchesCalculation()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42, 25);
        var (expectedW, expectedH) = DungeonGenerator.CalculateFloorSize(25);
        Assert.Equal(expectedW, floor.Width);
        Assert.Equal(expectedH, floor.Height);
    }

    // --- Challenge Room ---

    [Fact]
    public void NonBossFloor_HasChallengeRoom()
    {
        var gen = new DungeonGenerator();
        // Test across multiple seeds — challenge room should appear on most floors
        int found = 0;
        for (int seed = 0; seed < 20; seed++)
        {
            var floor = gen.Generate(seed, 5);
            if (floor.Rooms.Any(r => r.Kind == RoomKind.Challenge))
                found++;
        }
        Assert.True(found >= 15, $"Challenge room only appeared on {found}/20 seeds (expected most)");
    }

    [Fact]
    public void BossFloor_NoChallengeRoom()
    {
        var gen = new DungeonGenerator();
        for (int seed = 0; seed < 10; seed++)
        {
            var floor = gen.Generate(seed, 10);
            Assert.DoesNotContain(floor.Rooms, r => r.Kind == RoomKind.Challenge);
        }
    }

    [Fact]
    public void ChallengeRoom_Reachable()
    {
        var gen = new DungeonGenerator();
        for (int seed = 0; seed < 20; seed++)
        {
            var floor = gen.Generate(seed, 5);
            var challenge = floor.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Challenge);
            if (challenge == null) continue;

            var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
            var reachable = FloodFill(floor, entrance.CenterX, entrance.CenterY);
            Assert.True(reachable.Contains((challenge.CenterX, challenge.CenterY)),
                $"Seed {seed}: Challenge room unreachable");
        }
    }

    private static HashSet<(int, int)> FloodFill(FloorData floor, int startX, int startY)
    {
        var visited = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));
        visited.Add((startX, startY));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                int nx = cx + dx, ny = cy + dy;
                if (floor.IsFloor(nx, ny) && !visited.Contains((nx, ny)))
                {
                    visited.Add((nx, ny));
                    queue.Enqueue((nx, ny));
                }
            }
        }
        return visited;
    }
}

public class FloorCacheTests
{
    [Fact]
    public void CachesResult()
    {
        var cache = new FloorCache(new DungeonGenerator());
        var floor1 = cache.GetFloor(1, 42);
        var floor2 = cache.GetFloor(1, 42);
        Assert.Same(floor1, floor2);
    }

    [Fact]
    public void EvictsAt10()
    {
        var cache = new FloorCache(new DungeonGenerator());
        for (int i = 1; i <= 10; i++)
            cache.GetFloor(i, i * 100);

        Assert.Equal(10, cache.Count);
        Assert.True(cache.Contains(1));

        // Adding 11th should evict floor 1 (LRU)
        cache.GetFloor(11, 1100);
        Assert.Equal(10, cache.Count);
        Assert.False(cache.Contains(1));
    }

    [Fact]
    public void EvictsLRU_NotMRU()
    {
        var cache = new FloorCache(new DungeonGenerator());
        for (int i = 1; i <= 10; i++)
            cache.GetFloor(i, i * 100);

        // Access floor 1 to make it MRU
        cache.GetFloor(1, 100);

        // Adding 11th should evict floor 2 (now LRU), not floor 1
        cache.GetFloor(11, 1100);
        Assert.True(cache.Contains(1));
        Assert.False(cache.Contains(2));
    }

    [Fact]
    public void Clear_EmptiesCache()
    {
        var cache = new FloorCache(new DungeonGenerator());
        cache.GetFloor(1, 42);
        cache.GetFloor(2, 99);
        Assert.Equal(2, cache.Count);

        cache.Clear();
        Assert.Equal(0, cache.Count);
    }
}

public class FloorExplorationTests
{
    [Fact]
    public void Explored_InitializedToAllFalse()
    {
        var floor = new FloorData(20, 30);
        for (int x = 0; x < floor.Width; x++)
            for (int y = 0; y < floor.Height; y++)
                Assert.False(floor.Explored[x, y], $"Tile ({x},{y}) should start unexplored");
    }

    [Fact]
    public void MarkExplored_MarksCenter()
    {
        var floor = new FloorData(20, 20);
        floor.MarkExplored(10, 10, 0);
        Assert.True(floor.IsExplored(10, 10));
    }

    [Fact]
    public void MarkExplored_MarksWithinRadius()
    {
        var floor = new FloorData(20, 20);
        floor.MarkExplored(10, 10, 2);

        // Center and cardinal directions within radius
        Assert.True(floor.IsExplored(10, 10));
        Assert.True(floor.IsExplored(10, 8));  // 2 tiles north
        Assert.True(floor.IsExplored(10, 12)); // 2 tiles south
        Assert.True(floor.IsExplored(12, 10)); // 2 tiles east
        Assert.True(floor.IsExplored(8, 10));  // 2 tiles west

        // Diagonal within radius (distance sqrt(2) < 2)
        Assert.True(floor.IsExplored(11, 11));

        // Outside radius (distance sqrt(8) > 2)
        Assert.False(floor.IsExplored(12, 12));
    }

    [Fact]
    public void MarkExplored_CircularShape()
    {
        var floor = new FloorData(30, 30);
        floor.MarkExplored(15, 15, 3);

        // Check that corners of the bounding square are NOT marked (circular, not square)
        // Distance from (15,15) to (18,18) = sqrt(18) ~= 4.24, outside radius 3
        Assert.False(floor.IsExplored(18, 18));
        Assert.False(floor.IsExplored(12, 12));
        Assert.False(floor.IsExplored(18, 12));
        Assert.False(floor.IsExplored(12, 18));
    }

    [Fact]
    public void MarkExplored_RespectsLowerBounds()
    {
        var floor = new FloorData(10, 10);
        // Mark near origin with large radius — should not throw
        floor.MarkExplored(0, 0, 5);
        Assert.True(floor.IsExplored(0, 0));
        Assert.True(floor.IsExplored(3, 0));
        Assert.True(floor.IsExplored(0, 3));
    }

    [Fact]
    public void MarkExplored_RespectsUpperBounds()
    {
        var floor = new FloorData(10, 10);
        // Mark near edge with large radius — should not throw
        floor.MarkExplored(9, 9, 5);
        Assert.True(floor.IsExplored(9, 9));
        Assert.True(floor.IsExplored(6, 9));
        Assert.True(floor.IsExplored(9, 6));
    }

    [Fact]
    public void IsExplored_OutOfBounds_ReturnsFalse()
    {
        var floor = new FloorData(10, 10);
        floor.MarkExplored(5, 5, 3);

        Assert.False(floor.IsExplored(-1, 5));
        Assert.False(floor.IsExplored(5, -1));
        Assert.False(floor.IsExplored(10, 5));
        Assert.False(floor.IsExplored(5, 10));
    }

    [Fact]
    public void IsExplored_ReturnsCorrectState()
    {
        var floor = new FloorData(20, 20);

        // Before marking — all false
        Assert.False(floor.IsExplored(5, 5));

        // After marking
        floor.MarkExplored(5, 5, 1);
        Assert.True(floor.IsExplored(5, 5));
        Assert.True(floor.IsExplored(5, 4));
        Assert.True(floor.IsExplored(5, 6));
        Assert.True(floor.IsExplored(4, 5));
        Assert.True(floor.IsExplored(6, 5));

        // Far away tile still unexplored
        Assert.False(floor.IsExplored(15, 15));
    }

    [Fact]
    public void MarkExplored_MultipleCallsAccumulate()
    {
        var floor = new FloorData(20, 20);
        floor.MarkExplored(5, 5, 1);
        floor.MarkExplored(15, 15, 1);

        Assert.True(floor.IsExplored(5, 5));
        Assert.True(floor.IsExplored(15, 15));
    }
}

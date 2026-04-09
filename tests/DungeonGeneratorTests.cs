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
        var exit = floor.Rooms.First(r => r.Kind == RoomKind.Exit);
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
    public void FloorDimensions_Default()
    {
        var gen = new DungeonGenerator();
        var floor = gen.Generate(42);
        Assert.Equal(100, floor.Width);
        Assert.Equal(200, floor.Height);
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
                    $"Seed {seed}: Room at ({room.CenterX},{room.CenterY}) unreachable");
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

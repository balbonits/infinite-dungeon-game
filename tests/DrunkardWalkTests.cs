using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DungeonGame.Dungeon;

namespace DungeonGame.Tests;

public class DrunkardWalkTests
{
    private const int Width = 100;
    private const int Height = 200;

    private (FloorData floor, List<(RoomData, RoomData)> pairs) CreateBspFloor(int seed)
    {
        var rng = new Random(seed);
        var bsp = new BspGenerator(Width, Height, rng);
        var floor = bsp.Generate();
        var pairs = bsp.GetSiblingPairs();
        return (floor, pairs);
    }

    [Fact]
    public void AllRooms_Reachable()
    {
        var (floor, pairs) = CreateBspFloor(42);
        var carver = new DrunkardWalkCarver(new Random(42));
        carver.CarveCorridors(floor, pairs);

        // BFS from entrance
        var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
        var reachable = FloodFill(floor, entrance.CenterX, entrance.CenterY);

        foreach (var room in floor.Rooms)
            Assert.True(reachable.Contains((room.CenterX, room.CenterY)),
                $"Room at ({room.CenterX},{room.CenterY}) not reachable from entrance");
    }

    [Fact]
    public void Deterministic()
    {
        var (floor1, pairs1) = CreateBspFloor(42);
        new DrunkardWalkCarver(new Random(100)).CarveCorridors(floor1, pairs1);

        var (floor2, pairs2) = CreateBspFloor(42);
        new DrunkardWalkCarver(new Random(100)).CarveCorridors(floor2, pairs2);

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Assert.Equal(floor1.Tiles[x, y], floor2.Tiles[x, y]);
    }

    [Fact]
    public void PreservesExistingRooms()
    {
        var (floor, pairs) = CreateBspFloor(42);

        // Record room tiles before
        var roomTiles = new HashSet<(int, int)>();
        foreach (var room in floor.Rooms)
            for (int x = room.X; x < room.X + room.Width; x++)
                for (int y = room.Y; y < room.Y + room.Height; y++)
                    roomTiles.Add((x, y));

        new DrunkardWalkCarver(new Random(42)).CarveCorridors(floor, pairs);

        // All room tiles should still be floor
        foreach (var (x, y) in roomTiles)
            Assert.Equal(TileType.Floor, floor.Tiles[x, y]);
    }

    [Fact]
    public void IncreasesFloorCount()
    {
        var (floor, pairs) = CreateBspFloor(42);
        int before = CountFloorTiles(floor);

        new DrunkardWalkCarver(new Random(42)).CarveCorridors(floor, pairs);
        int after = CountFloorTiles(floor);

        Assert.True(after > before, $"Floor tiles did not increase: {before} -> {after}");
    }

    [Fact]
    public void StaysInBounds()
    {
        var (floor, pairs) = CreateBspFloor(42);
        new DrunkardWalkCarver(new Random(42)).CarveCorridors(floor, pairs);

        // Check that tiles array is still the right size
        Assert.Equal(Width, floor.Width);
        Assert.Equal(Height, floor.Height);
    }

    [Fact]
    public void HighLoopChance_MorePaths()
    {
        var (floor1, pairs1) = CreateBspFloor(42);
        new DrunkardWalkCarver(new Random(100), loopChance: 0f).CarveCorridors(floor1, pairs1);
        int tilesNoLoops = CountFloorTiles(floor1);

        var (floor2, pairs2) = CreateBspFloor(42);
        new DrunkardWalkCarver(new Random(100), loopChance: 1f).CarveCorridors(floor2, pairs2);
        int tilesAllLoops = CountFloorTiles(floor2);

        Assert.True(tilesAllLoops >= tilesNoLoops,
            $"Loop corridors should create more floor tiles: {tilesNoLoops} vs {tilesAllLoops}");
    }

    private static int CountFloorTiles(FloorData floor)
    {
        int count = 0;
        for (int x = 0; x < floor.Width; x++)
            for (int y = 0; y < floor.Height; y++)
                if (floor.Tiles[x, y] == TileType.Floor)
                    count++;
        return count;
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

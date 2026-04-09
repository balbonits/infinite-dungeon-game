using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DungeonGame.Dungeon;

namespace DungeonGame.Tests;

public class CellularAutomataTests
{
    private const int Width = 100;
    private const int Height = 200;

    private FloorData CreateFullFloor(int seed)
    {
        var rng = new Random(seed);
        var bsp = new BspGenerator(Width, Height, rng);
        var floor = bsp.Generate();
        var pairs = bsp.GetSiblingPairs();
        new DrunkardWalkCarver(rng).CarveCorridors(floor, pairs);
        return floor;
    }

    [Fact]
    public void PreservesRoomInteriors()
    {
        var floor = CreateFullFloor(42);
        new CellularAutomata().Smooth(floor);

        foreach (var room in floor.Rooms)
            for (int x = room.X; x < room.X + room.Width; x++)
                for (int y = room.Y; y < room.Y + room.Height; y++)
                    Assert.Equal(TileType.Floor, floor.Tiles[x, y]);
    }

    [Fact]
    public void Deterministic()
    {
        var floor1 = CreateFullFloor(42);
        new CellularAutomata().Smooth(floor1);

        var floor2 = CreateFullFloor(42);
        new CellularAutomata().Smooth(floor2);

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Assert.Equal(floor1.Tiles[x, y], floor2.Tiles[x, y]);
    }

    [Fact]
    public void SmoothingChangesGrid()
    {
        var floor = CreateFullFloor(42);
        var before = new TileType[Width, Height];
        Array.Copy(floor.Tiles, before, floor.Tiles.Length);

        new CellularAutomata().Smooth(floor);

        bool anyChanged = false;
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (floor.Tiles[x, y] != before[x, y]) { anyChanged = true; break; }
        Assert.True(anyChanged, "Smoothing should modify at least some tiles");
    }

    [Fact]
    public void ZeroIterations_NoChange()
    {
        var floor = CreateFullFloor(42);
        var before = new TileType[Width, Height];
        Array.Copy(floor.Tiles, before, floor.Tiles.Length);

        new CellularAutomata(iterations: 0).Smooth(floor);

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Assert.Equal(before[x, y], floor.Tiles[x, y]);
    }

    [Fact]
    public void AllRooms_StillReachable()
    {
        var floor = CreateFullFloor(42);
        new CellularAutomata().Smooth(floor);

        var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
        var reachable = FloodFill(floor, entrance.CenterX, entrance.CenterY);

        foreach (var room in floor.Rooms)
            Assert.True(reachable.Contains((room.CenterX, room.CenterY)),
                $"Room at ({room.CenterX},{room.CenterY}) unreachable after smoothing");
    }

    [Fact]
    public void GridDimensions_Unchanged()
    {
        var floor = CreateFullFloor(42);
        new CellularAutomata().Smooth(floor);
        Assert.Equal(Width, floor.Width);
        Assert.Equal(Height, floor.Height);
    }

    private static int CountJaggedTiles(FloorData floor)
    {
        int count = 0;
        for (int x = 0; x < floor.Width; x++)
        {
            for (int y = 0; y < floor.Height; y++)
            {
                if (floor.Tiles[x, y] != TileType.Floor) continue;
                if (floor.IsInsideAnyRoom(x, y)) continue;

                int walls = 0;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (floor.IsWall(x + dx, y + dy)) walls++;
                    }
                if (walls >= 5) count++;
            }
        }
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

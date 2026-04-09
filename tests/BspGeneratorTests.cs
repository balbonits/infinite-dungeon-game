using System;
using System.Linq;
using Xunit;
using DungeonGame.Dungeon;

namespace DungeonGame.Tests;

public class BspGeneratorTests
{
    private const int Width = 100;
    private const int Height = 200;

    [Fact]
    public void SameSeed_SameLayout()
    {
        var floor1 = new BspGenerator(Width, Height, new Random(42)).Generate();
        var floor2 = new BspGenerator(Width, Height, new Random(42)).Generate();

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Assert.Equal(floor1.Tiles[x, y], floor2.Tiles[x, y]);
    }

    [Fact]
    public void DifferentSeed_DifferentLayout()
    {
        var floor1 = new BspGenerator(Width, Height, new Random(42)).Generate();
        var floor2 = new BspGenerator(Width, Height, new Random(999)).Generate();

        bool anyDiff = false;
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (floor1.Tiles[x, y] != floor2.Tiles[x, y]) { anyDiff = true; break; }
        Assert.True(anyDiff);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(777)]
    [InlineData(9999)]
    public void RoomCount_InRange(int seed)
    {
        var floor = new BspGenerator(Width, Height, new Random(seed)).Generate();
        Assert.InRange(floor.Rooms.Count, 2, 16);
    }

    [Fact]
    public void RoomCount_InRange_ManySeeds()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var floor = new BspGenerator(Width, Height, new Random(seed)).Generate();
            Assert.True(floor.Rooms.Count >= 2, $"Seed {seed}: only {floor.Rooms.Count} rooms");
        }
    }

    [Fact]
    public void Rooms_WithinBounds()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();
        foreach (var room in floor.Rooms)
        {
            Assert.True(room.X >= 0, $"Room X={room.X} is negative");
            Assert.True(room.Y >= 0, $"Room Y={room.Y} is negative");
            Assert.True(room.X + room.Width <= Width, $"Room exceeds width: {room.X}+{room.Width} > {Width}");
            Assert.True(room.Y + room.Height <= Height, $"Room exceeds height: {room.Y}+{room.Height} > {Height}");
        }
    }

    [Fact]
    public void Rooms_NoOverlap()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();
        for (int i = 0; i < floor.Rooms.Count; i++)
            for (int j = i + 1; j < floor.Rooms.Count; j++)
                Assert.False(floor.Rooms[i].Intersects(floor.Rooms[j]),
                    $"Rooms {i} and {j} overlap");
    }

    [Fact]
    public void HasEntranceAndExit()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();

        int entrances = floor.Rooms.Count(r => r.Kind == RoomKind.Entrance);
        int exits = floor.Rooms.Count(r => r.Kind == RoomKind.Exit);
        Assert.Equal(1, entrances);
        Assert.Equal(1, exits);
    }

    [Fact]
    public void EntranceAndExit_AreDifferentRooms()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();
        var entrance = floor.Rooms.First(r => r.Kind == RoomKind.Entrance);
        var exit = floor.Rooms.First(r => r.Kind == RoomKind.Exit);
        Assert.NotSame(entrance, exit);
    }

    [Fact]
    public void RoomTiles_AreFloor()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();
        foreach (var room in floor.Rooms)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
                for (int y = room.Y; y < room.Y + room.Height; y++)
                    Assert.Equal(TileType.Floor, floor.Tiles[x, y]);
        }
    }

    [Fact]
    public void GridDimensions_Match()
    {
        var floor = new BspGenerator(Width, Height, new Random(42)).Generate();
        Assert.Equal(Width, floor.Width);
        Assert.Equal(Height, floor.Height);
    }

    [Fact]
    public void GetSiblingPairs_ReturnsNonEmpty()
    {
        var bsp = new BspGenerator(Width, Height, new Random(42));
        bsp.Generate();
        var pairs = bsp.GetSiblingPairs();
        Assert.NotEmpty(pairs);
    }
}

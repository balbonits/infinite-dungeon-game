using System;
using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public enum TileType { Wall = 0, Floor = 1 }

public enum RoomKind { Normal, Entrance, Exit, Boss, Treasure }

public class RoomData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public RoomKind Kind { get; set; } = RoomKind.Normal;

    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    public bool Contains(int px, int py)
        => px >= X && px < X + Width && py >= Y && py < Y + Height;

    public bool Intersects(RoomData other)
        => X < other.X + other.Width && X + Width > other.X
        && Y < other.Y + other.Height && Y + Height > other.Y;
}

public class BspNode
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public BspNode? Left { get; set; }
    public BspNode? Right { get; set; }
    public RoomData? Room { get; set; }

    public bool IsLeaf => Left == null && Right == null;
}

public class FloorData
{
    public TileType[,] Tiles { get; set; }
    public List<RoomData> Rooms { get; set; } = new();
    public int Width { get; }
    public int Height { get; }
    public int Seed { get; set; }

    public FloorData(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new TileType[width, height];
    }

    public bool IsInBounds(int x, int y)
        => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsFloor(int x, int y)
        => IsInBounds(x, y) && Tiles[x, y] == TileType.Floor;

    public bool IsWall(int x, int y)
        => !IsInBounds(x, y) || Tiles[x, y] == TileType.Wall;

    public void SetTile(int x, int y, TileType t)
    {
        if (IsInBounds(x, y))
            Tiles[x, y] = t;
    }

    public bool IsInsideAnyRoom(int x, int y)
    {
        for (int i = 0; i < Rooms.Count; i++)
            if (Rooms[i].Contains(x, y))
                return true;
        return false;
    }
}

using System;
using System.Collections.Generic;
using Godot;

namespace DungeonGame;

/// <summary>
/// Procedural dungeon floor generator.
/// Hybrid algorithm: BSP (macro structure) → Drunkard's Walk (corridors) → Cellular Automata (smoothing).
/// Generates a 2D grid of tile types (Floor, Wall) plus room positions and stair locations.
/// </summary>
public class FloorGenerator
{
    public enum Tile : byte { Wall = 0, Floor = 1, }

    public Tile[,] Grid { get; private set; } = null!;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public List<Rect2I> Rooms { get; } = new();
    public Vector2I EntrancePos { get; private set; }
    public Vector2I ExitPos { get; private set; }

    private readonly Random _rng;

    // BSP constants
    private const int MinRoomSize = 6;
    private const int MaxRoomSize = 14;
    private const int MinPartitionSize = 16;
    private const float LoopCorridorChance = 0.15f;

    public FloorGenerator(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Generate a complete floor layout for the given floor number.
    /// </summary>
    public void Generate(int floorNumber)
    {
        CalculateSize(floorNumber);
        Grid = new Tile[Width, Height];

        // Start all walls
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Grid[x, y] = Tile.Wall;

        // Phase 1: BSP room placement
        var partitions = BspPartition(new Rect2I(1, 1, Width - 2, Height - 2), 0);
        foreach (var partition in partitions)
            PlaceRoom(partition);

        if (Rooms.Count < 2)
        {
            // Fallback: at least 2 rooms
            PlaceRoom(new Rect2I(2, 2, Width / 3, Height / 3));
            PlaceRoom(new Rect2I(Width / 2, Height / 2, Width / 3, Height / 3));
        }

        // Phase 2: Order rooms into IKEA path (nearest-neighbor chain)
        var orderedRooms = OrderRooms();

        // Phase 3: Drunkard's Walk corridors between adjacent rooms in chain
        for (int i = 0; i < orderedRooms.Count - 1; i++)
            CarveCorridor(RoomCenter(orderedRooms[i]), RoomCenter(orderedRooms[i + 1]));

        // Optional loop corridors (15% chance)
        for (int i = 0; i < orderedRooms.Count - 2; i++)
        {
            if (_rng.NextSingle() < LoopCorridorChance)
                CarveCorridor(RoomCenter(orderedRooms[i]), RoomCenter(orderedRooms[i + 2]));
        }

        // Phase 4: Cellular automata smoothing (2 passes)
        for (int pass = 0; pass < 2; pass++)
            SmoothPass();

        // Ensure all rooms are still accessible (re-carve room interiors after smoothing)
        foreach (var room in Rooms)
            CarveRoom(room);

        // Place entrance (first room) and exit (last room)
        EntrancePos = RoomCenter(orderedRooms[0]);
        ExitPos = RoomCenter(orderedRooms[^1]);
    }

    private void CalculateSize(int floorNumber)
    {
        int zone = (floorNumber - 1) / Constants.Zones.FloorsPerZone + 1;
        int intraStep = (floorNumber - 1) % Constants.Zones.FloorsPerZone;
        float zoneScale = 1.0f + (zone - 1) * 0.25f;
        float intraScale = 1.0f + intraStep * 0.02f;
        float sizeScale = zoneScale * intraScale;

        Width = Math.Clamp((int)MathF.Round(50 * sizeScale), 50, 150);
        Height = Math.Clamp((int)MathF.Round(50 * sizeScale), 50, 150);
    }

    // --- BSP ---

    private List<Rect2I> BspPartition(Rect2I area, int depth)
    {
        var leaves = new List<Rect2I>();

        if (depth > 5 || area.Size.X < MinPartitionSize * 2 && area.Size.Y < MinPartitionSize * 2
            || (depth > 2 && _rng.NextSingle() < 0.3f))
        {
            leaves.Add(area);
            return leaves;
        }

        bool splitHorizontal = area.Size.Y > area.Size.X
            ? true
            : area.Size.X > area.Size.Y ? false : _rng.NextSingle() < 0.5f;

        if (splitHorizontal && area.Size.Y >= MinPartitionSize * 2)
        {
            int splitY = area.Position.Y + MinPartitionSize + _rng.Next(area.Size.Y - MinPartitionSize * 2 + 1);
            leaves.AddRange(BspPartition(new Rect2I(area.Position.X, area.Position.Y, area.Size.X, splitY - area.Position.Y), depth + 1));
            leaves.AddRange(BspPartition(new Rect2I(area.Position.X, splitY, area.Size.X, area.End.Y - splitY), depth + 1));
        }
        else if (!splitHorizontal && area.Size.X >= MinPartitionSize * 2)
        {
            int splitX = area.Position.X + MinPartitionSize + _rng.Next(area.Size.X - MinPartitionSize * 2 + 1);
            leaves.AddRange(BspPartition(new Rect2I(area.Position.X, area.Position.Y, splitX - area.Position.X, area.Size.Y), depth + 1));
            leaves.AddRange(BspPartition(new Rect2I(splitX, area.Position.Y, area.End.X - splitX, area.Size.Y), depth + 1));
        }
        else
        {
            leaves.Add(area);
        }

        return leaves;
    }

    private void PlaceRoom(Rect2I partition)
    {
        int roomW = Math.Min(MaxRoomSize, Math.Max(MinRoomSize, partition.Size.X - 4));
        int roomH = Math.Min(MaxRoomSize, Math.Max(MinRoomSize, partition.Size.Y - 4));

        if (roomW < MinRoomSize || roomH < MinRoomSize) return;

        int roomX = partition.Position.X + _rng.Next(Math.Max(1, partition.Size.X - roomW));
        int roomY = partition.Position.Y + _rng.Next(Math.Max(1, partition.Size.Y - roomH));

        var room = new Rect2I(roomX, roomY, roomW, roomH);
        Rooms.Add(room);
        CarveRoom(room);
    }

    private void CarveRoom(Rect2I room)
    {
        for (int x = room.Position.X; x < room.End.X && x < Width - 1; x++)
            for (int y = room.Position.Y; y < room.End.Y && y < Height - 1; y++)
                if (x > 0 && y > 0)
                    Grid[x, y] = Tile.Floor;
    }

    // --- Corridor Generation (Drunkard's Walk between points) ---

    private void CarveCorridor(Vector2I from, Vector2I to)
    {
        int x = from.X, y = from.Y;

        while (x != to.X || y != to.Y)
        {
            // Bias toward target with some randomness
            if (_rng.NextSingle() < 0.6f)
            {
                // Move toward target
                if (Math.Abs(to.X - x) > Math.Abs(to.Y - y))
                    x += x < to.X ? 1 : -1;
                else
                    y += y < to.Y ? 1 : -1;
            }
            else
            {
                // Random walk
                int dir = _rng.Next(4);
                switch (dir)
                {
                    case 0: if (x > 1) x--; break;
                    case 1: if (x < Width - 2) x++; break;
                    case 2: if (y > 1) y--; break;
                    case 3: if (y < Height - 2) y++; break;
                }
            }

            // Carve a 2-wide corridor
            if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
            {
                Grid[x, y] = Tile.Floor;
                if (x + 1 < Width - 1) Grid[x + 1, y] = Tile.Floor;
                if (y + 1 < Height - 1) Grid[x, y + 1] = Tile.Floor;
            }
        }
    }

    // --- Cellular Automata Smoothing ---

    private void SmoothPass()
    {
        var copy = (Tile[,])Grid.Clone();

        for (int x = 1; x < Width - 1; x++)
        {
            for (int y = 1; y < Height - 1; y++)
            {
                int wallCount = 0;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (copy[x + dx, y + dy] == Tile.Wall)
                            wallCount++;

                // 5+ neighbors are walls → become wall (removes isolated floor tiles)
                Grid[x, y] = wallCount >= 5 ? Tile.Wall : Tile.Floor;
            }
        }
    }

    // --- Room Ordering (Nearest-Neighbor Chain) ---

    private List<Rect2I> OrderRooms()
    {
        if (Rooms.Count <= 2) return new List<Rect2I>(Rooms);

        var ordered = new List<Rect2I>();
        var remaining = new List<Rect2I>(Rooms);

        // Start from room closest to top-left
        int startIdx = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < remaining.Count; i++)
        {
            float dist = RoomCenter(remaining[i]).LengthSquared();
            if (dist < minDist) { minDist = dist; startIdx = i; }
        }

        ordered.Add(remaining[startIdx]);
        remaining.RemoveAt(startIdx);

        while (remaining.Count > 0)
        {
            var last = ordered[^1];
            var lastCenter = RoomCenter(last);

            int nearestIdx = 0;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                float dist = lastCenter.DistanceSquaredTo(RoomCenter(remaining[i]));
                if (dist < nearestDist) { nearestDist = dist; nearestIdx = i; }
            }

            ordered.Add(remaining[nearestIdx]);
            remaining.RemoveAt(nearestIdx);
        }

        return ordered;
    }

    private static Vector2I RoomCenter(Rect2I room) =>
        new(room.Position.X + room.Size.X / 2, room.Position.Y + room.Size.Y / 2);
}

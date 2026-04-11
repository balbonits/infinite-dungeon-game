using System;
using System.Collections.Generic;
using Godot;

namespace DungeonGame;

/// <summary>
/// Procedural dungeon floor generator.
/// BSP partitioning for room placement, L-shaped corridors for connectivity.
/// Generates a 2D tile grid plus room rects, entrance/exit positions.
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

    // Room constraints
    private const int MinRoomW = 8;
    private const int MinRoomH = 8;
    private const int MaxRoomW = 18;
    private const int MaxRoomH = 18;
    private const int CorridorWidth = 4;  // tiles wide — needed for isometric movement
    private const int TargetMinRooms = 5;
    private const int TargetMaxRooms = 8;
    private const float LoopCorridorChance = 0.15f;

    public FloorGenerator(int seed)
    {
        _rng = new Random(seed);
    }

    public void Generate(int floorNumber)
    {
        CalculateSize(floorNumber);
        Grid = new Tile[Width, Height];

        // Phase 1: BSP → place rooms
        int minPartition = MaxRoomW + 4; // partition must fit a room + padding
        var partitions = BspPartition(new Rect2I(2, 2, Width - 4, Height - 4), 0, minPartition);

        foreach (var partition in partitions)
            PlaceRoom(partition);

        // Ensure minimum room count
        if (Rooms.Count < TargetMinRooms)
            AddExtraRooms(TargetMinRooms - Rooms.Count);

        // Cap room count
        while (Rooms.Count > TargetMaxRooms)
            Rooms.RemoveAt(Rooms.Count - 1);

        // Carve all rooms into grid, then apply shape variations
        foreach (var room in Rooms)
        {
            CarveRect(room);
            ApplyRoomShape(room);
        }

        // Phase 2: Order rooms into a chain (entrance → ... → exit)
        var chain = OrderRoomsIntoChain();

        // Phase 3: L-shaped corridors between consecutive rooms in chain
        for (int i = 0; i < chain.Count - 1; i++)
            CarveCorridorL(RoomCenter(chain[i]), RoomCenter(chain[i + 1]));

        // Optional shortcut corridors (15% chance per gap of 2)
        for (int i = 0; i < chain.Count - 2; i++)
        {
            if (_rng.NextSingle() < LoopCorridorChance)
                CarveCorridorL(RoomCenter(chain[i]), RoomCenter(chain[i + 2]));
        }

        // Phase 4: Widen corridor-room junctions (carve a small area where corridors meet rooms)
        foreach (var room in Rooms)
        {
            // Widen 2 tiles around each room edge to smooth doorways
            var expanded = new Rect2I(
                room.Position.X - 1, room.Position.Y - 1,
                room.Size.X + 2, room.Size.Y + 2);
            for (int x = expanded.Position.X; x < expanded.End.X; x++)
                for (int y = expanded.Position.Y; y < expanded.End.Y; y++)
                    if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1
                        && Grid[x, y] == Tile.Wall && CountFloorNeighbors(x, y) >= 5)
                        Grid[x, y] = Tile.Floor;
        }

        // Phase 5: Set entrance and exit
        EntrancePos = RoomCenter(chain[0]);
        ExitPos = RoomCenter(chain[^1]);
    }

    // --- Size Calculation (spec formula) ---

    private void CalculateSize(int floorNumber)
    {
        int zone = (floorNumber - 1) / Constants.Zones.FloorsPerZone + 1;
        int intraStep = (floorNumber - 1) % Constants.Zones.FloorsPerZone;
        float zoneScale = 1.0f + (zone - 1) * 0.25f;
        float intraScale = 1.0f + intraStep * 0.02f;
        float scale = zoneScale * intraScale;

        // Spec (dungeon.md): BASE_WIDTH=50, BASE_HEIGHT=100, MAX_WIDTH=150, MAX_HEIGHT=300
        Width = Math.Clamp((int)MathF.Round(50 * scale), 50, 150);
        Height = Math.Clamp((int)MathF.Round(100 * scale), 100, 300);
    }

    // --- BSP Partitioning ---

    private List<Rect2I> BspPartition(Rect2I area, int depth, int minSize)
    {
        var leaves = new List<Rect2I>();

        // Stop splitting if area is small enough or we have enough depth
        bool tooSmallToSplit = area.Size.X < minSize * 2 && area.Size.Y < minSize * 2;
        bool randomStop = depth >= 2 && _rng.NextSingle() < 0.25f;

        if (depth > 4 || tooSmallToSplit || randomStop)
        {
            leaves.Add(area);
            return leaves;
        }

        // Prefer splitting the longer axis
        bool splitH;
        if (area.Size.Y > area.Size.X * 1.3f)
            splitH = true;
        else if (area.Size.X > area.Size.Y * 1.3f)
            splitH = false;
        else
            splitH = _rng.NextSingle() < 0.5f;

        if (splitH && area.Size.Y >= minSize * 2)
        {
            int range = area.Size.Y - minSize * 2;
            int splitY = area.Position.Y + minSize + (range > 0 ? _rng.Next(range + 1) : 0);
            leaves.AddRange(BspPartition(
                new Rect2I(area.Position.X, area.Position.Y, area.Size.X, splitY - area.Position.Y),
                depth + 1, minSize));
            leaves.AddRange(BspPartition(
                new Rect2I(area.Position.X, splitY, area.Size.X, area.End.Y - splitY),
                depth + 1, minSize));
        }
        else if (!splitH && area.Size.X >= minSize * 2)
        {
            int range = area.Size.X - minSize * 2;
            int splitX = area.Position.X + minSize + (range > 0 ? _rng.Next(range + 1) : 0);
            leaves.AddRange(BspPartition(
                new Rect2I(area.Position.X, area.Position.Y, splitX - area.Position.X, area.Size.Y),
                depth + 1, minSize));
            leaves.AddRange(BspPartition(
                new Rect2I(splitX, area.Position.Y, area.End.X - splitX, area.Size.Y),
                depth + 1, minSize));
        }
        else
        {
            leaves.Add(area);
        }

        return leaves;
    }

    // --- Room Placement ---

    private void PlaceRoom(Rect2I partition)
    {
        // Room size: random within constraints, capped by partition
        int maxW = Math.Min(MaxRoomW, partition.Size.X - 2);
        int maxH = Math.Min(MaxRoomH, partition.Size.Y - 2);
        if (maxW < MinRoomW || maxH < MinRoomH) return;

        int roomW = MinRoomW + _rng.Next(maxW - MinRoomW + 1);
        int roomH = MinRoomH + _rng.Next(maxH - MinRoomH + 1);

        // Center the room within the partition with small random offset
        int padX = partition.Size.X - roomW;
        int padY = partition.Size.Y - roomH;
        int roomX = partition.Position.X + (padX > 0 ? _rng.Next(padX) : 0);
        int roomY = partition.Position.Y + (padY > 0 ? _rng.Next(padY) : 0);

        // Clamp to grid bounds (leave 1-tile outer wall)
        roomX = Math.Clamp(roomX, 1, Width - roomW - 1);
        roomY = Math.Clamp(roomY, 1, Height - roomH - 1);

        Rooms.Add(new Rect2I(roomX, roomY, roomW, roomH));
    }

    private void AddExtraRooms(int count)
    {
        for (int i = 0; i < count && i < 20; i++)
        {
            int w = MinRoomW + _rng.Next(MaxRoomW - MinRoomW + 1);
            int h = MinRoomH + _rng.Next(MaxRoomH - MinRoomH + 1);
            int x = 2 + _rng.Next(Math.Max(1, Width - w - 4));
            int y = 2 + _rng.Next(Math.Max(1, Height - h - 4));

            // Check overlap with existing rooms (allow touching but not overlapping)
            var candidate = new Rect2I(x, y, w, h);
            bool overlaps = false;
            foreach (var existing in Rooms)
            {
                var expanded = new Rect2I(existing.Position.X - 2, existing.Position.Y - 2,
                    existing.Size.X + 4, existing.Size.Y + 4);
                if (expanded.Intersects(candidate))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                Rooms.Add(candidate);
        }
    }

    private void CarveRect(Rect2I rect)
    {
        for (int x = rect.Position.X; x < rect.End.X; x++)
            for (int y = rect.Position.Y; y < rect.End.Y; y++)
                if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                    Grid[x, y] = Tile.Floor;
    }

    /// <summary>
    /// Apply a random shape variation to a carved room.
    /// 50% plain rectangle, 20% L-shape, 15% pillared, 15% cross/alcove.
    /// </summary>
    private void ApplyRoomShape(Rect2I room)
    {
        float roll = _rng.NextSingle();

        if (roll < 0.50f)
        {
            // Plain rectangle — already carved, nothing to do
        }
        else if (roll < 0.70f && room.Size.X >= 12 && room.Size.Y >= 12)
        {
            // L-shape: cut out one corner quadrant (only on large rooms so both arms stay wide)
            int cutW = room.Size.X / 3;
            int cutH = room.Size.Y / 3;
            int corner = _rng.Next(4);
            int cx = corner < 2 ? room.Position.X : room.Position.X + room.Size.X - cutW;
            int cy = corner % 2 == 0 ? room.Position.Y : room.Position.Y + room.Size.Y - cutH;
            for (int x = cx; x < cx + cutW; x++)
                for (int y = cy; y < cy + cutH; y++)
                    if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                        Grid[x, y] = Tile.Wall;
        }
        else if (roll < 0.85f)
        {
            // Pillared: place 2x2 wall pillars inside the room (spaced for entity pathfinding)
            int pillarSpacing = 5;
            for (int x = room.Position.X + 3; x < room.End.X - 4; x += pillarSpacing)
            {
                for (int y = room.Position.Y + 3; y < room.End.Y - 4; y += pillarSpacing)
                {
                    if (x + 1 < Width - 1 && y + 1 < Height - 1)
                    {
                        Grid[x, y] = Tile.Wall;
                        Grid[x + 1, y] = Tile.Wall;
                        Grid[x, y + 1] = Tile.Wall;
                        Grid[x + 1, y + 1] = Tile.Wall;
                    }
                }
            }
        }
        else
        {
            // Alcove: carve small extensions on 1-2 sides
            int alcoveDepth = 3;
            int alcoveWidth = Math.Max(4, room.Size.X / 3);
            int side = _rng.Next(4);
            int offset = room.Size.X > alcoveWidth
                ? _rng.Next(room.Size.X - alcoveWidth) : 0;

            switch (side)
            {
                case 0: // north alcove
                    CarveRect(new Rect2I(room.Position.X + offset,
                        room.Position.Y - alcoveDepth, alcoveWidth, alcoveDepth));
                    break;
                case 1: // south alcove
                    CarveRect(new Rect2I(room.Position.X + offset,
                        room.End.Y, alcoveWidth, alcoveDepth));
                    break;
                case 2: // west alcove
                    CarveRect(new Rect2I(room.Position.X - alcoveDepth,
                        room.Position.Y + offset, alcoveDepth, alcoveWidth));
                    break;
                case 3: // east alcove
                    CarveRect(new Rect2I(room.End.X,
                        room.Position.Y + offset, alcoveDepth, alcoveWidth));
                    break;
            }
        }
    }

    // --- L-Shaped Corridors ---

    private void CarveCorridorL(Vector2I from, Vector2I to)
    {
        // L-shaped: go horizontal first, then vertical (or vice versa, 50/50)
        Vector2I bend;
        if (_rng.NextSingle() < 0.5f)
            bend = new Vector2I(to.X, from.Y); // horizontal then vertical
        else
            bend = new Vector2I(from.X, to.Y); // vertical then horizontal

        CarveCorridorStraight(from, bend);
        CarveCorridorStraight(bend, to);

        // Widen the bend point so entities don't get stuck at the corner
        int pad = CorridorWidth;
        for (int dx = -pad; dx <= pad; dx++)
            for (int dy = -pad; dy <= pad; dy++)
            {
                int bx = bend.X + dx, by = bend.Y + dy;
                if (bx > 0 && bx < Width - 1 && by > 0 && by < Height - 1)
                    Grid[bx, by] = Tile.Floor;
            }
    }

    private void CarveCorridorStraight(Vector2I from, Vector2I to)
    {
        int half = CorridorWidth / 2;

        if (from.Y == to.Y)
        {
            // Horizontal corridor
            int minX = Math.Min(from.X, to.X);
            int maxX = Math.Max(from.X, to.X);
            for (int x = minX; x <= maxX; x++)
                for (int dy = -half; dy < half; dy++)
                {
                    int cy = from.Y + dy;
                    if (x > 0 && x < Width - 1 && cy > 0 && cy < Height - 1)
                        Grid[x, cy] = Tile.Floor;
                }
        }
        else if (from.X == to.X)
        {
            // Vertical corridor
            int minY = Math.Min(from.Y, to.Y);
            int maxY = Math.Max(from.Y, to.Y);
            for (int y = minY; y <= maxY; y++)
                for (int dx = -half; dx < half; dx++)
                {
                    int cx = from.X + dx;
                    if (cx > 0 && cx < Width - 1 && y > 0 && y < Height - 1)
                        Grid[cx, y] = Tile.Floor;
                }
        }
        else
        {
            // Diagonal fallback — step along both axes with width
            int x = from.X, y = from.Y;
            while (x != to.X || y != to.Y)
            {
                for (int dx = -half; dx < half; dx++)
                    for (int dy = -half; dy < half; dy++)
                    {
                        int cx = x + dx, cy = y + dy;
                        if (cx > 0 && cx < Width - 1 && cy > 0 && cy < Height - 1)
                            Grid[cx, cy] = Tile.Floor;
                    }

                if (x != to.X) x += x < to.X ? 1 : -1;
                if (y != to.Y) y += y < to.Y ? 1 : -1;
            }
        }
    }

    // --- Room Ordering (nearest-neighbor chain for IKEA path) ---

    private List<Rect2I> OrderRoomsIntoChain()
    {
        if (Rooms.Count <= 2) return new List<Rect2I>(Rooms);

        var ordered = new List<Rect2I>();
        var remaining = new List<Rect2I>(Rooms);

        // Start from room nearest to top-left corner
        int startIdx = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < remaining.Count; i++)
        {
            float dist = RoomCenter(remaining[i]).LengthSquared();
            if (dist < minDist) { minDist = dist; startIdx = i; }
        }

        ordered.Add(remaining[startIdx]);
        remaining.RemoveAt(startIdx);

        // Greedy nearest-neighbor
        while (remaining.Count > 0)
        {
            var lastCenter = RoomCenter(ordered[^1]);
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

    private int CountFloorNeighbors(int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && Grid[nx, ny] == Tile.Floor)
                    count++;
            }
        return count;
    }

    private static Vector2I RoomCenter(Rect2I room) =>
        new(room.Position.X + room.Size.X / 2, room.Position.Y + room.Size.Y / 2);
}

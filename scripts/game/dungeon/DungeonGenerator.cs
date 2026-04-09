using System;
using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public class DungeonGenerator
{
    private const int BaseWidth = 50;
    private const int BaseHeight = 100;
    private const int MinWidth = 50;
    private const int MinHeight = 100;
    private const int MaxWidth = 150;
    private const int MaxHeight = 300;

    public static (int width, int height) CalculateFloorSize(int floorNumber)
    {
        int zone = (floorNumber - 1) / 10 + 1;
        int intraStep = (floorNumber - 1) % 10;

        double zoneScale = 1.0 + (zone - 1) * 0.25;
        double intraScale = 1.0 + intraStep * 0.02;
        double sizeScale = zoneScale * intraScale;

        int width = Math.Clamp((int)Math.Round(BaseWidth * sizeScale), MinWidth, MaxWidth);
        int height = Math.Clamp((int)Math.Round(BaseHeight * sizeScale), MinHeight, MaxHeight);

        return (width, height);
    }

    public FloorData Generate(int seed, int floorNumber = 1)
    {
        var (width, height) = CalculateFloorSize(floorNumber);
        var rng = new Random(seed);

        // Stage 1: BSP room placement
        var bsp = new BspGenerator(width, height, rng);
        var floor = bsp.Generate();
        floor.Seed = seed;

        // Stage 2: Build IKEA chain order (overwrites BSP entrance/exit)
        var chain = BuildRoomChain(floor, rng);

        // Stage 3: Drunkard's walk corridors along chain
        var chainPairs = new List<(RoomData, RoomData)>();
        for (int i = 0; i < chain.Count - 1; i++)
            chainPairs.Add((chain[i], chain[i + 1]));

        var carver = new DrunkardWalkCarver(rng);
        carver.CarveCorridors(floor, chainPairs);

        // Stage 4: Place challenge room shortcut (non-boss floors only)
        if (floorNumber % 10 != 0 && chain.Count >= 4)
            PlaceChallengeRoom(floor, chain, rng, carver);

        // Stage 5: Cellular automata smoothing
        var smoother = new CellularAutomata();
        smoother.Smooth(floor);

        // Stage 6: Assign special room types based on floor number
        AssignRoomTypes(floor, floorNumber, rng);

        return floor;
    }

    private List<RoomData> BuildRoomChain(FloorData floor, Random rng)
    {
        if (floor.Rooms.Count == 0) return new List<RoomData>();

        // Reset all room kinds (BSP assigned entrance/exit; we'll reassign via chain)
        foreach (var room in floor.Rooms)
            room.Kind = RoomKind.Normal;

        // Pick random entrance
        int entranceIdx = rng.Next(floor.Rooms.Count);
        var chain = new List<RoomData>();
        var used = new HashSet<int> { entranceIdx };

        chain.Add(floor.Rooms[entranceIdx]);
        floor.Rooms[entranceIdx].Kind = RoomKind.Entrance;

        // Nearest-neighbor traversal for guided path
        while (used.Count < floor.Rooms.Count)
        {
            var last = chain[^1];
            int bestIdx = -1;
            double bestDist = double.MaxValue;

            for (int i = 0; i < floor.Rooms.Count; i++)
            {
                if (used.Contains(i)) continue;
                double dx = floor.Rooms[i].CenterX - last.CenterX;
                double dy = floor.Rooms[i].CenterY - last.CenterY;
                double dist = dx * dx + dy * dy;
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }

            if (bestIdx < 0) break;
            chain.Add(floor.Rooms[bestIdx]);
            used.Add(bestIdx);
        }

        // Last room in chain is the exit
        if (chain.Count >= 2)
            chain[^1].Kind = RoomKind.Exit;

        return chain;
    }

    private void PlaceChallengeRoom(FloorData floor, List<RoomData> chain, Random rng, DrunkardWalkCarver carver)
    {
        // Connect to an early room in the chain (room index 1 or 2)
        int earlyIdx = Math.Min(2, chain.Count - 3);
        if (earlyIdx < 1) return;
        var earlyRoom = chain[earlyIdx];

        // Shortcut connects to second-to-last room (just before exit)
        var nearExitRoom = chain[^2];

        // Scale room size with floor size
        int roomSize = Math.Clamp(floor.Width / 5, 8, 16);
        int pad = 2;
        RoomData? challengeRoom = null;

        // Collect all valid positions, then pick one at random
        var validPositions = new List<(int x, int y)>();
        int step = Math.Max(roomSize / 2, 4);
        for (int rx = 1; rx <= floor.Width - roomSize - 1; rx += step)
        {
            for (int ry = 1; ry <= floor.Height - roomSize - 1; ry += step)
            {
                var candidate = new RoomData { X = rx, Y = ry, Width = roomSize, Height = roomSize };
                bool overlaps = false;
                foreach (var room in floor.Rooms)
                {
                    var padded = new RoomData
                    {
                        X = room.X - pad, Y = room.Y - pad,
                        Width = room.Width + pad * 2, Height = room.Height + pad * 2
                    };
                    if (candidate.Intersects(padded)) { overlaps = true; break; }
                }
                if (!overlaps)
                    validPositions.Add((rx, ry));
            }
        }

        if (validPositions.Count > 0)
        {
            var (px, py) = validPositions[rng.Next(validPositions.Count)];
            challengeRoom = new RoomData { X = px, Y = py, Width = roomSize, Height = roomSize };
        }

        if (challengeRoom == null) return;

        challengeRoom.Kind = RoomKind.Challenge;

        // Carve the challenge room into the floor
        for (int x = challengeRoom.X; x < challengeRoom.X + challengeRoom.Width; x++)
            for (int y = challengeRoom.Y; y < challengeRoom.Y + challengeRoom.Height; y++)
                floor.SetTile(x, y, TileType.Floor);

        floor.Rooms.Add(challengeRoom);

        // Carve corridor from early room to challenge room
        carver.CarvePath(floor, earlyRoom, challengeRoom);

        // Carve shortcut corridor from challenge room to near-exit room
        carver.CarvePath(floor, challengeRoom, nearExitRoom);
    }

    private void AssignRoomTypes(FloorData floor, int floorNumber, Random rng)
    {
        if (floorNumber % 10 == 0 && floorNumber > 0)
        {
            // Boss floor: boss blocks the exit — set exit room to Boss
            foreach (var room in floor.Rooms)
            {
                if (room.Kind == RoomKind.Exit)
                {
                    room.Kind = RoomKind.Boss;
                    break;
                }
            }
        }
        else
        {
            // ~5% chance for a treasure room
            foreach (var room in floor.Rooms)
            {
                if (room.Kind == RoomKind.Normal && rng.NextDouble() < 0.05)
                {
                    room.Kind = RoomKind.Treasure;
                    break;
                }
            }
        }
    }
}

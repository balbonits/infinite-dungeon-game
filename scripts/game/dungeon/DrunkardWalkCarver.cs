using System;
using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public class DrunkardWalkCarver
{
    private readonly Random _rng;
    private readonly int _maxSteps;
    private readonly float _targetBias;
    private readonly float _loopChance;

    private static readonly (int dx, int dy)[] Dirs = { (0, -1), (0, 1), (-1, 0), (1, 0) };

    /// <summary>Recorded paths for visualization. Each entry is one agent's walk.</summary>
    public List<List<(int x, int y)>> AgentPaths { get; } = new();

    public DrunkardWalkCarver(Random rng, int maxSteps = 2000, float targetBias = 0.7f, float loopChance = 0.15f)
    {
        _rng = rng;
        _maxSteps = maxSteps;
        _targetBias = targetBias;
        _loopChance = loopChance;
    }

    public void CarveCorridors(FloorData floor, List<(RoomData a, RoomData b)> siblingPairs)
    {
        AgentPaths.Clear();

        // Connect all sibling pairs for guaranteed connectivity
        foreach (var (a, b) in siblingPairs)
            CarvePathBetween(floor, a.CenterX, a.CenterY, b.CenterX, b.CenterY);

        // Optional loop corridors between non-sibling rooms
        for (int i = 0; i < floor.Rooms.Count; i++)
        {
            for (int j = i + 1; j < floor.Rooms.Count; j++)
            {
                bool isSibling = false;
                foreach (var (a, b) in siblingPairs)
                {
                    if ((a == floor.Rooms[i] && b == floor.Rooms[j]) ||
                        (a == floor.Rooms[j] && b == floor.Rooms[i]))
                    { isSibling = true; break; }
                }
                if (!isSibling && _rng.NextDouble() < _loopChance)
                    CarvePathBetween(floor, floor.Rooms[i].CenterX, floor.Rooms[i].CenterY,
                                             floor.Rooms[j].CenterX, floor.Rooms[j].CenterY);
            }
        }
    }

    public void CarvePath(FloorData floor, RoomData from, RoomData to)
    {
        CarvePathBetween(floor, from.CenterX, from.CenterY, to.CenterX, to.CenterY);
    }

    private void CarvePathBetween(FloorData floor, int startX, int startY, int targetX, int targetY)
    {
        int x = startX, y = startY;
        var path = new List<(int, int)> { (x, y) };

        for (int step = 0; step < _maxSteps; step++)
        {
            floor.SetTile(x, y, TileType.Floor);

            // Stop if we've reached a floor tile inside the target room's area
            if (x == targetX && y == targetY)
                break;

            // Biased random walk: mostly toward target, sometimes random
            int dx, dy;
            if (_rng.NextDouble() < _targetBias)
            {
                // Move toward target — pick the axis with greater distance
                int distX = targetX - x;
                int distY = targetY - y;
                if (Math.Abs(distX) > Math.Abs(distY) || (Math.Abs(distX) == Math.Abs(distY) && _rng.NextDouble() < 0.5))
                {
                    dx = Math.Sign(distX);
                    dy = 0;
                }
                else
                {
                    dx = 0;
                    dy = Math.Sign(distY);
                }
            }
            else
            {
                // Random direction
                var dir = Dirs[_rng.Next(4)];
                dx = dir.dx;
                dy = dir.dy;
            }

            int nx = x + dx, ny = y + dy;
            if (floor.IsInBounds(nx, ny))
            {
                x = nx;
                y = ny;
                path.Add((x, y));
            }
        }

        // Carve the final tile
        floor.SetTile(x, y, TileType.Floor);
        AgentPaths.Add(path);
    }
}

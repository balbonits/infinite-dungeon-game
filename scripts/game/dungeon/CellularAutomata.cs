using System;
using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public class CellularAutomata
{
    private readonly int _iterations;
    private readonly int _wallThreshold;

    public CellularAutomata(int iterations = 2, int wallThreshold = 5)
    {
        _iterations = iterations;
        _wallThreshold = wallThreshold;
    }

    public void Smooth(FloorData floor)
    {
        for (int iter = 0; iter < _iterations; iter++)
        {
            var copy = new TileType[floor.Width, floor.Height];
            Array.Copy(floor.Tiles, copy, floor.Tiles.Length);

            for (int x = 0; x < floor.Width; x++)
            {
                for (int y = 0; y < floor.Height; y++)
                {
                    // Protect room interiors
                    if (floor.IsInsideAnyRoom(x, y))
                    {
                        copy[x, y] = TileType.Floor;
                        continue;
                    }

                    int walls = CountWallNeighbors(floor, x, y);
                    copy[x, y] = walls >= _wallThreshold ? TileType.Wall : TileType.Floor;
                }
            }

            Array.Copy(copy, floor.Tiles, floor.Tiles.Length);
        }

        // Verify connectivity and repair if broken
        RepairConnectivity(floor);
    }

    private int CountWallNeighbors(FloorData floor, int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                // Out of bounds counts as wall
                if (floor.IsWall(x + dx, y + dy))
                    count++;
            }
        }
        return count;
    }

    private void RepairConnectivity(FloorData floor)
    {
        if (floor.Rooms.Count < 2) return;

        // BFS from entrance room center
        RoomData? entrance = null;
        foreach (var room in floor.Rooms)
            if (room.Kind == RoomKind.Entrance) { entrance = room; break; }
        if (entrance == null) entrance = floor.Rooms[0];

        var visited = new bool[floor.Width, floor.Height];
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((entrance.CenterX, entrance.CenterY));
        visited[entrance.CenterX, entrance.CenterY] = true;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                int nx = cx + dx, ny = cy + dy;
                if (floor.IsInBounds(nx, ny) && !visited[nx, ny] && floor.IsFloor(nx, ny))
                {
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        // Check each room is reachable; if not, carve an L-shaped corridor
        foreach (var room in floor.Rooms)
        {
            if (visited[room.CenterX, room.CenterY]) continue;

            // Find nearest reachable room
            RoomData nearest = null!;
            double bestDist = double.MaxValue;
            foreach (var other in floor.Rooms)
            {
                if (!visited[other.CenterX, other.CenterY]) continue;
                double d = Math.Pow(room.CenterX - other.CenterX, 2) + Math.Pow(room.CenterY - other.CenterY, 2);
                if (d < bestDist) { bestDist = d; nearest = other; }
            }
            if (bestDist == double.MaxValue) continue;

            // Carve L-shaped corridor
            int x = room.CenterX, y = room.CenterY;
            while (x != nearest.CenterX)
            {
                floor.SetTile(x, y, TileType.Floor);
                x += Math.Sign(nearest.CenterX - x);
            }
            while (y != nearest.CenterY)
            {
                floor.SetTile(x, y, TileType.Floor);
                y += Math.Sign(nearest.CenterY - y);
            }
            floor.SetTile(x, y, TileType.Floor);

            // Re-flood from entrance to update visited
            Array.Clear(visited);
            queue.Clear();
            queue.Enqueue((entrance.CenterX, entrance.CenterY));
            visited[entrance.CenterX, entrance.CenterY] = true;
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (floor.IsInBounds(nx, ny) && !visited[nx, ny] && floor.IsFloor(nx, ny))
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }
    }
}

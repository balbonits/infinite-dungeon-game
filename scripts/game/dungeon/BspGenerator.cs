using System;
using System.Collections.Generic;

namespace DungeonGame.Dungeon;

public class BspGenerator
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _minRoomSize;
    private readonly int _padding;
    private readonly int _maxDepth;
    private readonly Random _rng;

    public BspNode? Root { get; private set; }

    public BspGenerator(int width, int height, Random rng, int minRoomSize = 12, int padding = 2, int maxDepth = 3)
    {
        _width = width;
        _height = height;
        _rng = rng;
        _minRoomSize = minRoomSize;
        _padding = padding;
        _maxDepth = maxDepth;
    }

    public FloorData Generate()
    {
        var floor = new FloorData(_width, _height);

        // Initialize all tiles to wall
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                floor.Tiles[x, y] = TileType.Wall;

        // Build BSP tree
        Root = new BspNode { X = 0, Y = 0, Width = _width, Height = _height };
        Split(Root, 0);

        // Place rooms in leaf nodes
        PlaceRooms(Root, floor);

        // Assign entrance (random) and exit (farthest from entrance)
        if (floor.Rooms.Count >= 2)
        {
            int entranceIdx = _rng.Next(floor.Rooms.Count);
            floor.Rooms[entranceIdx].Kind = RoomKind.Entrance;

            int bestIdx = -1;
            double bestDist = -1;
            var entrance = floor.Rooms[entranceIdx];
            for (int i = 0; i < floor.Rooms.Count; i++)
            {
                if (i == entranceIdx) continue;
                double dx = floor.Rooms[i].CenterX - entrance.CenterX;
                double dy = floor.Rooms[i].CenterY - entrance.CenterY;
                double dist = dx * dx + dy * dy;
                if (dist > bestDist) { bestDist = dist; bestIdx = i; }
            }
            floor.Rooms[bestIdx].Kind = RoomKind.Exit;
        }
        else if (floor.Rooms.Count == 1)
        {
            floor.Rooms[0].Kind = RoomKind.Entrance;
        }

        return floor;
    }

    private void Split(BspNode node, int depth)
    {
        // Stop if at max depth
        if (depth >= _maxDepth) return;

        int minSize = _minRoomSize + _padding * 2;

        // Stop if too small to split
        if (node.Width < minSize * 2 && node.Height < minSize * 2)
            return;

        // Choose split direction: prefer longer axis, but allow some randomness
        bool splitHorizontal;
        if (node.Width < minSize * 2)
            splitHorizontal = true;
        else if (node.Height < minSize * 2)
            splitHorizontal = false;
        else
            splitHorizontal = node.Height >= node.Width ? true : _rng.NextDouble() < 0.4;

        if (splitHorizontal)
        {
            if (node.Height < minSize * 2) return;
            int splitMin = (int)(node.Height * 0.4);
            int splitMax = (int)(node.Height * 0.6);
            if (splitMin < minSize) splitMin = minSize;
            if (splitMax > node.Height - minSize) splitMax = node.Height - minSize;
            if (splitMin > splitMax) return;

            int split = _rng.Next(splitMin, splitMax + 1);
            node.Left = new BspNode { X = node.X, Y = node.Y, Width = node.Width, Height = split };
            node.Right = new BspNode { X = node.X, Y = node.Y + split, Width = node.Width, Height = node.Height - split };
        }
        else
        {
            if (node.Width < minSize * 2) return;
            int splitMin = (int)(node.Width * 0.4);
            int splitMax = (int)(node.Width * 0.6);
            if (splitMin < minSize) splitMin = minSize;
            if (splitMax > node.Width - minSize) splitMax = node.Width - minSize;
            if (splitMin > splitMax) return;

            int split = _rng.Next(splitMin, splitMax + 1);
            node.Left = new BspNode { X = node.X, Y = node.Y, Width = split, Height = node.Height };
            node.Right = new BspNode { X = node.X + split, Y = node.Y, Width = node.Width - split, Height = node.Height };
        }

        Split(node.Left, depth + 1);
        Split(node.Right, depth + 1);
    }

    private void PlaceRooms(BspNode node, FloorData floor)
    {
        if (node.IsLeaf)
        {
            int maxW = node.Width - _padding * 2;
            int maxH = node.Height - _padding * 2;
            if (maxW < _minRoomSize || maxH < _minRoomSize) return;

            int roomW = _rng.Next(_minRoomSize, Math.Min(maxW, _minRoomSize * 2) + 1);
            int roomH = _rng.Next(_minRoomSize, Math.Min(maxH, _minRoomSize * 2) + 1);
            int roomX = node.X + _padding + _rng.Next(maxW - roomW + 1);
            int roomY = node.Y + _padding + _rng.Next(maxH - roomH + 1);

            var room = new RoomData { X = roomX, Y = roomY, Width = roomW, Height = roomH };
            node.Room = room;
            floor.Rooms.Add(room);

            // Carve room
            for (int x = roomX; x < roomX + roomW; x++)
                for (int y = roomY; y < roomY + roomH; y++)
                    floor.SetTile(x, y, TileType.Floor);
        }
        else
        {
            if (node.Left != null) PlaceRooms(node.Left, floor);
            if (node.Right != null) PlaceRooms(node.Right, floor);
        }
    }

    /// <summary>
    /// Returns pairs of rooms that are siblings in the BSP tree.
    /// Used by DrunkardWalkCarver to determine which rooms need corridors.
    /// </summary>
    public List<(RoomData, RoomData)> GetSiblingPairs()
    {
        var pairs = new List<(RoomData, RoomData)>();
        if (Root != null)
            CollectSiblingPairs(Root, pairs);
        return pairs;
    }

    private void CollectSiblingPairs(BspNode node, List<(RoomData, RoomData)> pairs)
    {
        if (node.Left == null || node.Right == null) return;

        var leftRoom = GetAnyRoom(node.Left);
        var rightRoom = GetAnyRoom(node.Right);
        if (leftRoom != null && rightRoom != null)
            pairs.Add((leftRoom, rightRoom));

        CollectSiblingPairs(node.Left, pairs);
        CollectSiblingPairs(node.Right, pairs);
    }

    private RoomData? GetAnyRoom(BspNode node)
    {
        if (node.Room != null) return node.Room;
        if (node.Left != null)
        {
            var r = GetAnyRoom(node.Left);
            if (r != null) return r;
        }
        if (node.Right != null)
            return GetAnyRoom(node.Right);
        return null;
    }
}

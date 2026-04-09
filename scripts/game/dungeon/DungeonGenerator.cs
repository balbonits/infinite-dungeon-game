using System;

namespace DungeonGame.Dungeon;

public class DungeonGenerator
{
    private readonly int _width;
    private readonly int _height;

    public DungeonGenerator(int width = 100, int height = 200)
    {
        _width = width;
        _height = height;
    }

    public FloorData Generate(int seed, int floorNumber = 1)
    {
        var rng = new Random(seed);

        // Stage 1: BSP room placement
        var bsp = new BspGenerator(_width, _height, rng);
        var floor = bsp.Generate();
        floor.Seed = seed;

        // Stage 2: Drunkard's walk corridors
        var siblingPairs = bsp.GetSiblingPairs();
        var carver = new DrunkardWalkCarver(rng);
        carver.CarveCorridors(floor, siblingPairs);

        // Stage 3: Cellular automata smoothing
        var smoother = new CellularAutomata();
        smoother.Smooth(floor);

        // Stage 4: Assign special room types based on floor number
        AssignRoomTypes(floor, floorNumber, rng);

        return floor;
    }

    private void AssignRoomTypes(FloorData floor, int floorNumber, Random rng)
    {
        if (floorNumber % 10 == 0 && floorNumber > 0)
        {
            // Boss floor: pick a normal room (not entrance/exit) for the boss
            RoomData? bossRoom = null;
            foreach (var room in floor.Rooms)
            {
                if (room.Kind == RoomKind.Normal)
                {
                    bossRoom = room;
                    break;
                }
            }
            if (bossRoom != null)
                bossRoom.Kind = RoomKind.Boss;
        }
        else
        {
            // ~5% chance for a treasure room
            foreach (var room in floor.Rooms)
            {
                if (room.Kind == RoomKind.Normal && rng.NextDouble() < 0.05)
                {
                    room.Kind = RoomKind.Treasure;
                    break; // Only one treasure room per floor
                }
            }
        }
    }
}

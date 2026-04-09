using System.Collections.Generic;

namespace DungeonGame.Town;

public enum TownTile { Empty, Floor, Wall }

public class TownData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public TownTile[,] Tiles { get; set; }
    public List<NpcData> Npcs { get; set; } = new();
    public int EntranceX { get; set; }
    public int EntranceY { get; set; }
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
}

public static class TownLayout
{
    private const int GridW = 30;
    private const int GridH = 30;

    // NPC positions in a ring around center (15,15)
    // Banker: top center
    // Blacksmith: upper left
    // Item Shop: upper right
    // Adventure Guild: lower left
    // Level Teleporter: lower right
    // Dungeon Entrance: bottom center (special marker, not an NPC)

    public static TownData Build()
    {
        var data = new TownData
        {
            Width = GridW,
            Height = GridH,
            Tiles = new TownTile[GridW, GridH],
        };

        // Fill everything with empty
        for (int x = 0; x < GridW; x++)
            for (int y = 0; y < GridH; y++)
                data.Tiles[x, y] = TownTile.Empty;

        // Border walls
        for (int x = 0; x < GridW; x++)
        {
            data.Tiles[x, 0] = TownTile.Wall;
            data.Tiles[x, GridH - 1] = TownTile.Wall;
        }
        for (int y = 0; y < GridH; y++)
        {
            data.Tiles[0, y] = TownTile.Wall;
            data.Tiles[GridW - 1, y] = TownTile.Wall;
        }

        // Fill interior with floor
        for (int x = 1; x < GridW - 1; x++)
            for (int y = 1; y < GridH - 1; y++)
                data.Tiles[x, y] = TownTile.Floor;

        // Place building footprints as wall blocks (3x3 each) around the NPC positions
        PlaceBuilding(data, 13, 4);   // Banker (top center)
        PlaceBuilding(data, 6, 7);    // Blacksmith (upper left)
        PlaceBuilding(data, 21, 7);   // Item Shop (upper right)
        PlaceBuilding(data, 6, 20);   // Adventure Guild (lower left)
        PlaceBuilding(data, 21, 20);  // Level Teleporter (lower right)

        // Dungeon entrance marker at bottom center (2x2 wall block)
        PlaceBuilding(data, 13, 25);

        // Center spawn point
        data.SpawnX = 15;
        data.SpawnY = 15;

        // Dungeon entrance position (just south of the entrance building)
        data.EntranceX = 14;
        data.EntranceY = 27;

        // NPCs stand in front of (south of) their buildings
        data.Npcs.Add(new NpcData
        {
            Name = "Helena",
            Type = NpcType.Banker,
            Greeting = "Your gold is safe with me. How can I help?",
            TileX = 14,
            TileY = 7,
        });
        data.Npcs.Add(new NpcData
        {
            Name = "Tormund",
            Type = NpcType.Blacksmith,
            Greeting = "Need something hammered into shape?",
            TileX = 7,
            TileY = 10,
        });
        data.Npcs.Add(new NpcData
        {
            Name = "Elara",
            Type = NpcType.ItemShop,
            Greeting = "Welcome! Browse my wares.",
            TileX = 22,
            TileY = 10,
        });
        data.Npcs.Add(new NpcData
        {
            Name = "Captain Bron",
            Type = NpcType.AdventureGuild,
            Greeting = "Looking for work, adventurer?",
            TileX = 7,
            TileY = 23,
        });
        data.Npcs.Add(new NpcData
        {
            Name = "Sage Mira",
            Type = NpcType.LevelTeleporter,
            Greeting = "I can send you to floors you've conquered.",
            TileX = 22,
            TileY = 23,
        });

        return data;
    }

    private static void PlaceBuilding(TownData data, int cx, int cy)
    {
        // 3x3 wall block centered at (cx, cy)
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < data.Width && y >= 0 && y < data.Height)
                    data.Tiles[x, y] = TownTile.Wall;
            }
    }
}

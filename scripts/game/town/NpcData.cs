namespace DungeonGame.Town;

public enum NpcType { ItemShop, Blacksmith, AdventureGuild, LevelTeleporter, Banker }

public class NpcData
{
    public string Name { get; set; }
    public NpcType Type { get; set; }
    public string Greeting { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
}

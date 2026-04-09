using Godot;

/// <summary>
/// Autoload singleton for scene transitions.
/// Registered in project.godot as SceneManager.
/// </summary>
public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // works even when tree is paused
    }

    public void GoToMainMenu()
        => GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");

    public void GoToTown()
    {
        GameState.Location = GameLocation.Town;
        GetTree().ChangeSceneToFile("res://scenes/Town.tscn");
    }

    public void GoToDungeon(int floor)
    {
        GameState.DungeonFloor = floor;
        GameState.Location = GameLocation.Dungeon;
        GetTree().ChangeSceneToFile("res://scenes/Dungeon.tscn");
    }

    public void GoToCharacterCreate()
        => GetTree().ChangeSceneToFile("res://scenes/ui/CharacterCreate.tscn");
}

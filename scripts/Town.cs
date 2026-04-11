using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

public partial class Town : Node2D
{
    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>(Constants.Assets.PlayerScene);

    // NPC data: name, sprite path, tile position, greeting
    private static readonly (string name, string spritePath, Vector2I position, string greeting)[] NpcData =
    {
        (Strings.Npcs.Shopkeeper, "res://assets/characters/npcs/shopkeeper/rotations/south.png", new Vector2I(4, 3), Strings.NpcGreetings.Shopkeeper),
        (Strings.Npcs.Blacksmith, "res://assets/characters/npcs/blacksmith/rotations/south.png", new Vector2I(12, 3), Strings.NpcGreetings.Blacksmith),
        (Strings.Npcs.GuildMaster, "res://assets/characters/npcs/guild_master/rotations/south.png", new Vector2I(4, 7), Strings.NpcGreetings.GuildMaster),
        (Strings.Npcs.Teleporter, "res://assets/characters/npcs/teleporter/rotations/south.png", new Vector2I(12, 7), Strings.NpcGreetings.Teleporter),
        (Strings.Npcs.Banker, "res://assets/characters/npcs/banker/rotations/south.png", new Vector2I(8, 3), Strings.NpcGreetings.Banker),
    };

    private TileMapLayer _tileMap = null!;
    private Node2D _entities = null!;
    private Node2D _player = null!;
    private Label _interactLabel = null!;

    public override void _Ready()
    {
        _tileMap = GetNode<TileMapLayer>("TileMapLayer");
        _entities = GetNode<Node2D>("Entities");

        SetupTileset();
        PaintTown();
        SpawnPlayer();
        SpawnNpcs();
        CreateDungeonEntrance();
        CreateInteractLabel();
    }

    private void SetupTileset()
    {
        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = Constants.Tiles.TileSize;
        tileSet.AddPhysicsLayer();

        // Town floor
        var floorSource = new TileSetAtlasSource();
        floorSource.Texture = GD.Load<Texture2D>(Constants.Assets.TownFloorTexture);
        floorSource.TextureRegionSize = Constants.Tiles.TextureRegionSize;
        floorSource.CreateTile(Constants.Tiles.AtlasCoords);
        tileSet.AddSource(floorSource);

        // Town wall
        var wallSource = new TileSetAtlasSource();
        wallSource.Texture = GD.Load<Texture2D>(Constants.Assets.TownWallTexture);
        wallSource.TextureRegionSize = Constants.Tiles.TextureRegionSize;
        wallSource.CreateTile(Constants.Tiles.AtlasCoords);
        tileSet.AddSource(wallSource);

        TileData wallTileData = wallSource.GetTileData(Constants.Tiles.AtlasCoords, 0);
        wallTileData.AddCollisionPolygon(0);
        wallTileData.SetCollisionPolygonPoints(0, 0, Constants.Tiles.WallCollisionPolygon);

        _tileMap.TileSet = tileSet;
    }

    private void PaintTown()
    {
        for (int col = 0; col < Constants.Town.Width; col++)
        {
            for (int row = 0; row < Constants.Town.Height; row++)
            {
                bool isBorder = col == 0 || col == Constants.Town.Width - 1 ||
                                row == 0 || row == Constants.Town.Height - 1;
                _tileMap.SetCell(new Vector2I(col, row), isBorder ? 1 : 0, Constants.Tiles.AtlasCoords);
            }
        }
    }

    private void SpawnPlayer()
    {
        _player = PlayerScene.Instantiate<CharacterBody2D>();
        // Spawn at town center, not near the dungeon entrance
        _player.GlobalPosition = _tileMap.MapToLocal(new Vector2I(Constants.Town.Width / 2, Constants.Town.Height / 2));
        _entities.AddChild(_player);
    }

    private void SpawnNpcs()
    {
        foreach (var (name, spritePath, position, greeting) in NpcData)
        {
            var npc = new Npc();
            npc.NpcName = name;
            npc.SpritePath = spritePath;
            npc.Greeting = greeting;
            npc.GlobalPosition = _tileMap.MapToLocal(position);
            _entities.AddChild(npc);
        }
    }

    private void CreateDungeonEntrance()
    {
        // Dungeon entrance at the bottom of town
        var entrancePos = new Vector2I(Constants.Town.Width / 2, Constants.Town.Height - 2);

        var entrance = new Node2D();

        // Stairs sprite
        if (ResourceLoader.Exists(Constants.Assets.StairsDownTexture))
        {
            var sprite = new Sprite2D();
            sprite.Texture = GD.Load<Texture2D>(Constants.Assets.StairsDownTexture);
            sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            sprite.Offset = new Vector2(0, -16);
            entrance.AddChild(sprite);
        }

        // Label
        var label = new Label();
        label.Text = Strings.Town.DungeonEntrance;
        label.AddThemeColorOverride("font_color", UiTheme.Colors.Danger);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(-28, -48);
        entrance.AddChild(label);

        // Collision body
        var body = new StaticBody2D();
        body.CollisionLayer = Constants.Layers.Walls;
        var bodyShape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = Constants.Effects.StairsCollisionRadius;
        bodyShape.Shape = circle;
        body.AddChild(bodyShape);
        entrance.AddChild(body);

        // Trigger area
        var area = new Area2D();
        area.CollisionLayer = 0;
        area.CollisionMask = Constants.Layers.Player;
        area.Monitoring = true;
        var areaShape = new CollisionShape2D();
        var triggerCircle = new CircleShape2D();
        triggerCircle.Radius = Constants.Effects.StairsTriggerRadius;
        areaShape.Shape = triggerCircle;
        area.AddChild(areaShape);
        area.Connect(Area2D.SignalName.BodyEntered, new Callable(this, MethodName.OnDungeonEntranceEntered));
        entrance.AddChild(area);

        entrance.GlobalPosition = _tileMap.MapToLocal(entrancePos);
        _entities.AddChild(entrance);
    }

    private void CreateInteractLabel()
    {
        _interactLabel = new Label();
        _interactLabel.Text = "";
        _interactLabel.AddThemeColorOverride("font_color", UiTheme.Colors.Muted);
        _interactLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _interactLabel.AddThemeConstantOverride("outline_size", 3);
        _interactLabel.AddThemeFontSizeOverride("font_size", 12);
        _interactLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _interactLabel.Visible = false;
        AddChild(_interactLabel);
    }

    private void OnDungeonEntranceEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player) || ScreenTransition.Instance.IsTransitioning)
            return;

        ScreenTransition.Instance.Play(
            Strings.Town.DungeonFloor(GameState.Instance.FloorNumber),
            () => Main.Instance.LoadDungeon(),
            Strings.Town.EnteringDungeon
        );
    }
}

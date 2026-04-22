using Godot;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Sprite Viewer
/// Cycle through all 8-direction textures for player classes and enemy types.
/// Headless checks: all expected textures exist and load without null.
/// Run: make sandbox SCENE=sprite-viewer
/// </summary>
public partial class SpriteViewer : SandboxBase
{
    protected override string SandboxTitle => "🎨  Sprite Viewer";

    private record SpriteSubject(string Label, string BasePath);

    private static readonly SpriteSubject[] Subjects =
    [
        new("Warrior",   "res://assets/characters/player/warrior/rotations"),
        new("Ranger",    "res://assets/characters/player/ranger/rotations"),
        new("Mage",      "res://assets/characters/player/mage/rotations"),
        new("Bat",       "res://assets/characters/enemies/bat/rotations"),
        new("Goblin",    "res://assets/characters/enemies/goblin/rotations"),
        new("Orc",       "res://assets/characters/enemies/orc/rotations"),
        new("Skeleton",  "res://assets/characters/enemies/skeleton/rotations"),
        new("Spider",    "res://assets/characters/enemies/spider/rotations"),
        new("Wolf",      "res://assets/characters/enemies/wolf/rotations"),
        new("Dark Mage", "res://assets/characters/enemies/dark_mage/rotations"),
    ];

    private static readonly string[] Directions =
        ["south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east"];

    private int _subjectIndex;
    private int _dirIndex;
    private Sprite2D _sprite = null!;
    private Label _infoLabel = null!;
    private Dictionary<string, Texture2D> _textures = new();

    protected override void _SandboxReady()
    {
        // Display area
        _sprite = new Sprite2D { Position = new Vector2(700, 350), Scale = Vector2.One * 4f };
        AddChild(_sprite);

        _infoLabel = new Label { Position = new Vector2(500, 600) };
        _infoLabel.AddThemeFontSizeOverride("font_size", Ui.UiTheme.FontSizes.Body);
        AddChild(_infoLabel);

        AddSectionLabel("Subject");
        foreach (var (subj, i) in Subjects.Select((s, i) => (s, i)))
        {
            int idx = i;
            AddButton(subj.Label, () => { _subjectIndex = idx; LoadSubject(); });
        }

        AddSectionLabel("Direction");
        foreach (var (dir, i) in Directions.Select((d, i) => (d, i)))
        {
            int idx = i;
            AddButton(dir, () => { _dirIndex = idx; ShowDirection(); });
        }

        AddButton("◀ Prev", () => { _dirIndex = (_dirIndex - 1 + 8) % 8; ShowDirection(); });
        AddButton("▶ Next", () => { _dirIndex = (_dirIndex + 1) % 8; ShowDirection(); });

        LoadSubject();
    }

    protected override void _Reset() { _subjectIndex = 0; _dirIndex = 0; LoadSubject(); }

    private void LoadSubject()
    {
        var subj = Subjects[_subjectIndex];
        _textures = DirectionalSprite.LoadRotations(subj.BasePath);
        Log($"Loaded: {subj.Label} — {_textures.Count}/8 directions found");
        foreach (var dir in Directions)
            Log($"  {dir}: {(_textures.ContainsKey(dir) ? "✅" : "❌ missing")}");
        Log("");
        ShowDirection();
    }

    private void ShowDirection()
    {
        string dir = Directions[_dirIndex];
        if (_textures.TryGetValue(dir, out var tex))
        {
            _sprite.Texture = tex;
            var size = tex.GetSize();
            _infoLabel.Text = $"{Subjects[_subjectIndex].Label}  ·  {dir}  ·  {size.X}×{size.Y}px";
        }
        else
        {
            _sprite.Texture = null;
            _infoLabel.Text = $"{Subjects[_subjectIndex].Label}  ·  {dir}  ·  MISSING";
        }
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        foreach (var subj in Subjects)
        {
            var textures = DirectionalSprite.LoadRotations(subj.BasePath);
            Assert(textures.Count == 8, $"{subj.Label}: all 8 directions loaded (got {textures.Count})");
            foreach (var dir in Directions)
                Assert(textures.ContainsKey(dir), $"{subj.Label}/{dir}: texture exists");
        }
        FinishHeadless();
    }
}

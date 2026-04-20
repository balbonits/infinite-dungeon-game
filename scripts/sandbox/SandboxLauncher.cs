using Godot;
using System.Collections.Generic;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox launcher hub. Lists all available sandboxes grouped by category.
/// Launch via: make sandbox SCENE=launcher
/// Or set as main scene for quick sandbox access.
/// </summary>
public partial class SandboxLauncher : Control
{
    private record SandboxEntry(string Name, string Description, string ScenePath);
    private record SandboxGroup(string Category, Color Color, SandboxEntry[] Entries);

    private static readonly SandboxGroup[] Groups =
    [
        new("🎨  Assets", new Color(1f, 0.6f, 0.2f),
        [
            new("Sprite Viewer",      "8-direction character/enemy sprites",      "res://scenes/sandbox/assets/SpriteViewer.tscn"),
            new("Tile Viewer",        "All dungeon tile variants + tiling check",  "res://scenes/sandbox/assets/TileViewer.tscn"),
            new("Projectile Viewer",  "Projectile types in motion",               "res://scenes/sandbox/assets/ProjectileViewer.tscn"),
        ]),
        new("⚙️  Systems", new Color(0.4f, 0.8f, 1f),
        [
            new("Floor Generator",    "Procedural dungeon floors — room/corridor check",  "res://scenes/sandbox/systems/FloorGenSandbox.tscn"),
            new("Inventory",          "Add/remove/stack/buy/sell items",                  "res://scenes/sandbox/systems/InventorySandbox.tscn"),
            new("Loot Table",         "Drop rate histograms over N kills",                "res://scenes/sandbox/systems/LootTableSandbox.tscn"),
            new("Bank",               "Deposit/withdraw/expand — item survival",          "res://scenes/sandbox/systems/BankSandbox.tscn"),
            new("Death Penalty",      "XP/item loss calculator + idol logic",             "res://scenes/sandbox/systems/DeathPenaltySandbox.tscn"),
            new("Skill Tree",         "Class skill graphs + stat preview",                "res://scenes/sandbox/systems/SkillTreeSandbox.tscn"),
        ]),
        new("⚔️  Mechanics", new Color(1f, 0.4f, 0.4f),
        [
            new("Combat",    "Attack configs, damage, cooldowns, DPS",    "res://scenes/sandbox/mechanics/CombatSandbox.tscn"),
            new("Movement",  "8-way directional sprite + speed check",    "res://scenes/sandbox/mechanics/MovementSandbox.tscn"),
            new("Enemy",     "AI state, species configs, chase behavior", "res://scenes/sandbox/mechanics/EnemySandbox.tscn"),
            new("Stats",     "StatBlock sliders — live derived stat view", "res://scenes/sandbox/mechanics/StatsSandbox.tscn"),
        ]),
    ];

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        // Header
        var header = new PanelContainer();
        header.CustomMinimumSize = new Vector2(0, 64);
        root.AddChild(header);

        var headerInner = new VBoxContainer();
        header.AddChild(headerInner);

        var title = new Label { Text = "🧪  Sandbox Launcher", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Heading);
        headerInner.AddChild(title);

        var subtitle = new Label
        {
            Text = "isolated testing · no game state interference",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Small);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        headerInner.AddChild(subtitle);

        // Scrollable content
        var scroll = new ScrollContainer { FollowFocus = true };
        scroll.SizeFlagsVertical = SizeFlags.Expand;
        root.AddChild(scroll);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 16);
        content.SizeFlagsHorizontal = SizeFlags.Expand;
        scroll.AddChild(content);

        // Groups
        foreach (var group in Groups)
        {
            var groupBox = new VBoxContainer();
            groupBox.AddThemeConstantOverride("separation", 4);
            content.AddChild(groupBox);

            var groupLabel = new Label { Text = group.Category };
            groupLabel.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Body);
            groupLabel.AddThemeColorOverride("font_color", group.Color);
            groupBox.AddChild(groupLabel);

            var separator = new HSeparator();
            groupBox.AddChild(separator);

            var grid = new GridContainer { Columns = 3 };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 8);
            groupBox.AddChild(grid);

            foreach (var entry in group.Entries)
            {
                var card = new PanelContainer();
                card.CustomMinimumSize = new Vector2(180, 80);
                card.SizeFlagsHorizontal = SizeFlags.Expand;
                grid.AddChild(card);

                var cardInner = new VBoxContainer();
                card.AddChild(cardInner);

                var entryName = new Label { Text = entry.Name };
                entryName.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Body);
                cardInner.AddChild(entryName);

                var desc = new Label { Text = entry.Description, AutowrapMode = TextServer.AutowrapMode.Word };
                desc.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Small);
                desc.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
                cardInner.AddChild(desc);

                var launchBtn = new Button { Text = "▶  Launch" };
                var scenePath = entry.ScenePath; // capture for lambda
                launchBtn.Pressed += () => GetTree().ChangeSceneToFile(scenePath);
                cardInner.AddChild(launchBtn);
            }
        }

        // Footer
        var footer = new Label
        {
            Text = "CLI: make sandbox SCENE=<name>   |   headless: make sandbox-headless SCENE=<name>",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        footer.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Small);
        footer.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
        root.AddChild(footer);
    }
}

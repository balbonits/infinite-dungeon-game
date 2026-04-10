using Godot;
using DungeonGame.Town;

namespace DungeonGame.UI;

public partial class NpcPanel : Control
{
    // HUD color scheme
    private static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.75f);
    private static readonly Color BorderColor = new(0.961f, 0.784f, 0.420f, 0.3f);
    private static readonly Color TitleColor = new(0.961f, 0.784f, 0.420f); // #f5c86b
    private static readonly Color BodyColor = new(0.925f, 0.941f, 1.0f);    // #ecf0ff
    private static readonly Color ButtonBg = new(0.12f, 0.14f, 0.20f, 0.9f);
    private static readonly Color ButtonHover = new(0.18f, 0.20f, 0.28f, 0.9f);
    private static readonly Color GoldColor = new(0.961f, 0.784f, 0.420f);
    private static readonly Color DisabledColor = new(0.5f, 0.5f, 0.5f);

    private const float PanelWidth = 280f;
    private const float PanelHeight = 360f;

    private NpcData _currentNpc;
    private Panel _panel;
    private Label _nameLabel;
    private Label _greetingLabel;
    private VBoxContainer _buttonContainer;
    private Label _feedbackLabel;

    public override void _Ready()
    {
        Visible = false;

        // Anchor to right side — responsive: 20px from right edge, 20px from top
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;
        OffsetLeft = -PanelWidth - 20;
        OffsetRight = -20;
        OffsetTop = 20;
        OffsetBottom = 20 + PanelHeight;
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.End;

        // Panel background — fills this Control
        _panel = new Panel();
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.BorderColor = BorderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        // Use a MarginContainer + VBox for responsive internal layout
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        // NPC name
        _nameLabel = new Label();
        _nameLabel.AddThemeColorOverride("font_color", TitleColor);
        _nameLabel.AddThemeFontSizeOverride("font_size", 18);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        var titleFont = ResourceLoader.Load<Font>("res://assets/fonts/extracted/TinyRPG-BrilliantStrength.ttf");
        if (titleFont != null)
            _nameLabel.AddThemeFontOverride("font", titleFont);
        layout.AddChild(_nameLabel);

        // Greeting text
        _greetingLabel = new Label();
        _greetingLabel.AddThemeColorOverride("font_color", BodyColor);
        _greetingLabel.AddThemeFontSizeOverride("font_size", 12);
        _greetingLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        layout.AddChild(_greetingLabel);

        // Button container (expands to fill remaining space)
        _buttonContainer = new VBoxContainer();
        _buttonContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        _buttonContainer.AddThemeConstantOverride("separation", 4);
        layout.AddChild(_buttonContainer);

        // Feedback label (buy/sell results) at bottom
        _feedbackLabel = new Label();
        _feedbackLabel.AddThemeColorOverride("font_color", GoldColor);
        _feedbackLabel.AddThemeFontSizeOverride("font_size", 11);
        _feedbackLabel.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(_feedbackLabel);
    }

    public void ShowNpc(NpcData npc)
    {
        if (_currentNpc == npc && Visible) return;
        _currentNpc = npc;
        Visible = true;

        _nameLabel.Text = $"{npc.Name} - {NpcTypeLabel(npc.Type)}";
        _greetingLabel.Text = npc.Greeting;
        _feedbackLabel.Text = "";

        // Clear old buttons
        foreach (var child in _buttonContainer.GetChildren())
            child.QueueFree();

        // Build service buttons based on NPC type
        switch (npc.Type)
        {
            case NpcType.ItemShop:
                BuildShopButtons();
                break;
            case NpcType.Blacksmith:
                AddPlaceholderButton("Forge Equipment", "Coming Soon");
                AddPlaceholderButton("Recycle Items", "Coming Soon");
                break;
            case NpcType.AdventureGuild:
                AddPlaceholderButton("View Quests", "Coming Soon");
                AddPlaceholderButton("Claim Rewards", "Coming Soon");
                break;
            case NpcType.LevelTeleporter:
                AddPlaceholderButton("Teleport to Floor", "Coming Soon");
                break;
            case NpcType.Banker:
                AddPlaceholderButton("Deposit Gold", "Coming Soon");
                AddPlaceholderButton("Withdraw Gold", "Coming Soon");
                break;
        }
    }

    public new void Hide()
    {
        Visible = false;
        _currentNpc = null;
    }

    private void BuildShopButtons()
    {
        AddBuyButton("Health Potion", 50, "Restores 30 HP", 30, 0);
        AddBuyButton("Mana Potion", 50, "Restores 20 MP", 0, 20);
        AddBuyButton("Sacrificial Idol", 200, "Mysterious idol...", 0, 0);

        // Gold display
        var goldLabel = new Label();
        goldLabel.Text = $"Your Gold: {GameState.Player.Gold}g";
        goldLabel.AddThemeColorOverride("font_color", GoldColor);
        goldLabel.AddThemeFontSizeOverride("font_size", 12);
        goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _buttonContainer.AddChild(goldLabel);
    }

    private void AddBuyButton(string itemName, int cost, string desc, int hpBonus, int mpBonus)
    {
        var btn = new Button();
        btn.Text = $"  {itemName} - {cost}g";
        btn.TooltipText = desc;
        btn.CustomMinimumSize = new Vector2(0, 32);

        var btnStyle = new StyleBoxFlat();
        btnStyle.BgColor = ButtonBg;
        btnStyle.BorderColor = BorderColor;
        btnStyle.SetBorderWidthAll(1);
        btnStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", btnStyle);

        var btnHoverStyle = new StyleBoxFlat();
        btnHoverStyle.BgColor = ButtonHover;
        btnHoverStyle.BorderColor = TitleColor;
        btnHoverStyle.SetBorderWidthAll(1);
        btnHoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", btnHoverStyle);

        btn.AddThemeColorOverride("font_color", BodyColor);
        btn.AddThemeColorOverride("font_hover_color", TitleColor);
        btn.AddThemeFontSizeOverride("font_size", 12);

        // Capture values for the lambda
        string name = itemName;
        int price = cost;
        int hp = hpBonus;
        int mp = mpBonus;

        btn.Pressed += () =>
        {
            var shopItem = GameSystems.CreateItem(
                name, ItemType.Consumable, EquipSlot.None,
                hpBonus: hp, mpBonus: mp,
                value: price, stackable: true, desc: desc
            );
            var (success, reason) = GameSystems.BuyItem(shopItem);
            _feedbackLabel.Text = reason;
            _feedbackLabel.AddThemeColorOverride("font_color",
                success ? GoldColor : new Color(0.9f, 0.3f, 0.3f));

            // Update gold display
            RefreshGoldDisplay();
        };

        _buttonContainer.AddChild(btn);
    }

    private void AddPlaceholderButton(string label, string status)
    {
        var btn = new Button();
        btn.Text = $"  {label}";
        btn.Disabled = true;
        btn.CustomMinimumSize = new Vector2(0, 32);

        var btnStyle = new StyleBoxFlat();
        btnStyle.BgColor = ButtonBg;
        btnStyle.BorderColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        btnStyle.SetBorderWidthAll(1);
        btnStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", btnStyle);
        btn.AddThemeStyleboxOverride("disabled", btnStyle);

        btn.AddThemeColorOverride("font_color", DisabledColor);
        btn.AddThemeColorOverride("font_disabled_color", DisabledColor);
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.TooltipText = status;

        _buttonContainer.AddChild(btn);

        var statusLabel = new Label();
        statusLabel.Text = $"    {status}";
        statusLabel.AddThemeColorOverride("font_color", DisabledColor);
        statusLabel.AddThemeFontSizeOverride("font_size", 10);
        _buttonContainer.AddChild(statusLabel);
    }

    private void RefreshGoldDisplay()
    {
        // Find the gold label in the button container and update it
        foreach (var child in _buttonContainer.GetChildren())
        {
            if (child is Label lbl && lbl.Text.StartsWith("Your Gold:"))
            {
                lbl.Text = $"Your Gold: {GameState.Player.Gold}g";
                break;
            }
        }
    }

    private static string NpcTypeLabel(NpcType type) => type switch
    {
        NpcType.ItemShop => "Item Shop",
        NpcType.Blacksmith => "Blacksmith",
        NpcType.AdventureGuild => "Adventure Guild",
        NpcType.LevelTeleporter => "Teleporter",
        NpcType.Banker => "Banker",
        _ => type.ToString(),
    };
}

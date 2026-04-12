using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// NPC interaction dialog. Centered on screen with semi-transparent overlay.
/// Shows NPC name, greeting, and service button. Dismisses when player walks away.
/// </summary>
public partial class NpcPanel : Control
{
    public static NpcPanel Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private Label _nameLabel = null!;
    private Label _greetingLabel = null!;
    private VBoxContainer _serviceButtons = null!;

    public override void _Ready()
    {
        Instance = this;
        MouseFilter = MouseFilterEnum.Ignore;

        // Semi-transparent background overlay
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.4f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        // Centered container
        _center = new CenterContainer();
        _center.SetAnchorsPreset(LayoutPreset.FullRect);
        _center.Visible = false;
        AddChild(_center);

        // Panel
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(320, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        _nameLabel = new Label();
        UiTheme.StyleLabel(_nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_nameLabel);

        vbox.AddChild(new HSeparator());

        _greetingLabel = new Label();
        UiTheme.StyleLabel(_greetingLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _greetingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _greetingLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_greetingLabel);

        _serviceButtons = new VBoxContainer();
        _serviceButtons.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_serviceButtons);
    }

    public void Show(string npcName, string greeting)
    {
        _nameLabel.Text = npcName;
        _greetingLabel.Text = greeting;

        // Clear old service buttons
        foreach (Node child in _serviceButtons.GetChildren())
            child.QueueFree();

        // Service button
        var serviceBtn = new Button();
        serviceBtn.Text = GetServiceLabel(npcName);
        serviceBtn.CustomMinimumSize = new Vector2(200, 38);
        serviceBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(serviceBtn, UiTheme.FontSizes.Body);
        string capturedName = npcName;
        serviceBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => OnServicePressed(capturedName)));
        _serviceButtons.AddChild(serviceBtn);

        // Dismiss button (secondary/cancel style)
        var dismissBtn = new Button();
        dismissBtn.Text = Strings.Ui.Cancel;
        dismissBtn.CustomMinimumSize = new Vector2(200, 38);
        dismissBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(dismissBtn, UiTheme.FontSizes.Body);
        dismissBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Hide()));
        _serviceButtons.AddChild(dismissBtn);

        _overlay.Visible = true;
        _center.Visible = true;

        // Auto-focus first button for keyboard nav
        UiTheme.FocusFirstButton(_serviceButtons);

        // Fade in
        _center.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_center, "modulate:a", 1.0f, 0.15f);
    }

    public new void Hide()
    {
        if (!_center.Visible)
            return;

        var tween = CreateTween();
        tween.TweenProperty(_center, "modulate:a", 0.0f, 0.1f);
        tween.TweenCallback(Callable.From(() =>
        {
            _overlay.Visible = false;
            _center.Visible = false;
        }));
    }

    private void OnServicePressed(string npcName)
    {
        Hide();
        if (npcName == Strings.Npcs.Shopkeeper)
        {
            var shopItems = new System.Collections.Generic.List<ItemDef>(ItemDatabase.All);
            ShopWindow.Instance?.Open(shopItems);
        }
        else if (npcName == Strings.Npcs.Teleporter)
        {
            TeleportDialog.Instance?.Show();
        }
        else if (npcName == Strings.Npcs.Banker)
        {
            BankWindow.Instance?.Open();
        }
        else if (npcName == Strings.Npcs.Blacksmith)
        {
            BlacksmithWindow.Instance?.Open();
        }
        else if (npcName == Strings.Npcs.GuildMaster)
        {
            QuestPanel.Instance?.Open();
        }
        else
        {
            Toast.Instance?.Info($"{npcName}'s services coming soon.");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_center.Visible)
            return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            _center.Visible = false;
            _overlay.Visible = false;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleInput(@event, _serviceButtons))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private static string GetServiceLabel(string npcName)
    {
        return npcName switch
        {
            Strings.Npcs.Shopkeeper => Strings.NpcServices.OpenShop,
            Strings.Npcs.Blacksmith => Strings.NpcServices.OpenForge,
            Strings.Npcs.GuildMaster => Strings.NpcServices.ViewQuests,
            Strings.Npcs.Teleporter => Strings.NpcServices.Teleport,
            Strings.Npcs.Banker => Strings.NpcServices.OpenBank,
            _ => Strings.NpcServices.Talk,
        };
    }
}

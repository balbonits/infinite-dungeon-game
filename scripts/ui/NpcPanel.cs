using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// NPC interaction dialog. Centered on screen with semi-transparent overlay.
/// Shows NPC name, greeting, and service button. Dismisses when player walks away.
/// Uses GameWindow for lifecycle/input, but overrides Close() to do a tween fade.
/// </summary>
public partial class NpcPanel : GameWindow
{
    public static NpcPanel Instance { get; private set; } = null!;

    private Label _nameLabel = null!;
    private Label _greetingLabel = null!;
    private VBoxContainer _serviceButtons = null!;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        WindowWidth = 320;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        _nameLabel = new Label();
        UiTheme.StyleLabel(_nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_nameLabel);

        content.AddChild(new HSeparator());

        _greetingLabel = new Label();
        UiTheme.StyleLabel(_greetingLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _greetingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _greetingLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_greetingLabel);

        _serviceButtons = new VBoxContainer();
        _serviceButtons.AddThemeConstantOverride("separation", 8);
        content.AddChild(_serviceButtons);
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
        dismissBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => HideWithFade()));
        _serviceButtons.AddChild(dismissBtn);

        // GameWindow.Show() handles overlay, WindowStack, pause
        Show();

        // Auto-focus first button for keyboard nav
        UiTheme.FocusFirstButton(_serviceButtons);

        // Fade in the overlay — targets the overlay (root of the dialog hierarchy),
        // NOT ContentBox (inner VBox). Fading only ContentBox leaves the panel
        // background + border visible during the "fade out", which breaks the
        // illusion. (Copilot PR #3 review.)
        Overlay.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(Overlay, "modulate:a", 1.0f, 0.15f);
    }

    /// <summary>
    /// Animated hide: release button focus, fade out, then let GameWindow close.
    /// </summary>
    private void HideWithFade()
    {
        if (!IsOpen)
            return;

        // Release focus immediately so the next window can grab it
        foreach (Node child in _serviceButtons.GetChildren())
            if (child is Button btn)
                btn.FocusMode = FocusModeEnum.None;

        // Fade the overlay (root), not just ContentBox, so the panel background
        // fades too. (Copilot PR #3 review.)
        var tween = CreateTween();
        tween.TweenProperty(Overlay, "modulate:a", 0.0f, 0.1f);
        tween.TweenCallback(Callable.From(() =>
        {
            Close();
        }));
    }

    /// <summary>
    /// Override to use HideWithFade (animated close) instead of plain Close on cancel.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsOpen) return;
        if (KeyboardNav.BlockIfNotTopmost(this, @event)) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            HideWithFade();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private void OnServicePressed(string npcName)
    {
        HideWithFade();
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

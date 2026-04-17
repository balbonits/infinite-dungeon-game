using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Modal confirmation dialog for deleting a save slot (UI-02, per docs/flows/load-game.md).
/// Shows a short summary of the save being deleted and Cancel / Delete buttons.
/// Use <see cref="Create"/> to build; add to tree, then call <see cref="Open"/>.
/// </summary>
public partial class DeleteConfirmDialog : GameWindow
{
    private System.Action? _onConfirm;
    private SaveData _save = null!;

    public static DeleteConfirmDialog Create(SaveData save, System.Action onConfirm)
    {
        var dialog = new DeleteConfirmDialog();
        dialog._save = save;
        dialog._onConfirm = onConfirm;
        return dialog;
    }

    public override void _Ready()
    {
        WindowWidth = 440f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label { Text = "DELETE CHARACTER?" };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        content.AddChild(new HSeparator());

        var summary = new Label
        {
            Text = $"{_save.SelectedClass} — Level {_save.Level}, Floor {_save.DeepestFloor}",
        };
        UiTheme.StyleLabel(summary, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        summary.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(summary);

        var warn = new Label { Text = "This cannot be undone." };
        UiTheme.StyleLabel(warn, UiTheme.Colors.Danger, UiTheme.FontSizes.Small);
        warn.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(warn);

        content.AddChild(new HSeparator());

        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 16);
        content.AddChild(row);

        var cancel = new Button { Text = "Cancel" };
        cancel.CustomMinimumSize = new Vector2(140, 40);
        cancel.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(cancel, UiTheme.FontSizes.Button);
        cancel.Pressed += () => Close();
        row.AddChild(cancel);

        var delete = new Button { Text = "Delete" };
        delete.CustomMinimumSize = new Vector2(140, 40);
        delete.FocusMode = FocusModeEnum.All;
        UiTheme.StyleDangerButton(delete, UiTheme.FontSizes.Button);
        delete.Pressed += () =>
        {
            _onConfirm?.Invoke();
            Close();
        };
        row.AddChild(delete);
    }

    public void Open() => Show();
}

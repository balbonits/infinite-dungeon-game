using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Placeholder card for an empty slot. Same footprint + focus behavior as a
/// populated <see cref="CharacterCard"/> so LoadGameScreen's row of cards
/// stays visually uniform regardless of which slots are filled.
/// </summary>
public partial class EmptyCard : Card
{
    public static EmptyCard Create(int slotIndex, Action onSelected)
    {
        var card = new EmptyCard();
        card.Populate(slotIndex);
        card.Selected += () => onSelected();
        return card;
    }

    private void Populate(int slotIndex)
    {
        var spacerTop = new Control();
        spacerTop.SizeFlagsVertical = SizeFlags.ExpandFill;
        spacerTop.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(spacerTop);

        var label = new Label { Text = "Empty Slot" };
        UiTheme.StyleLabel(label, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(label);

        var sublabel = new Label { Text = $"Slot {slotIndex + 1}" };
        UiTheme.StyleLabel(sublabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        sublabel.HorizontalAlignment = HorizontalAlignment.Center;
        sublabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(sublabel);

        var spacerBottom = new Control();
        spacerBottom.SizeFlagsVertical = SizeFlags.ExpandFill;
        spacerBottom.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(spacerBottom);
    }
}

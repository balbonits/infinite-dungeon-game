using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Neutral character summary passed to <see cref="CharacterCard"/>. Decouples
/// the card from any specific data source (SaveData, Hall of Fame entry,
/// post-death recap, etc.) — each source builds its own CharacterSummary.
/// </summary>
public readonly record struct CharacterSummary(
    PlayerClass Class,
    int Level,
    int Str, int Dex, int Sta, int Int,
    int Hp, int MaxHp,
    int Mana, int MaxMana,
    long Gold,
    int Floor, int DeepestFloor,
    float XpPct,
    string TimestampLabel);

/// <summary>
/// Card that renders a <see cref="CharacterSummary"/>. Populates content only;
/// framing / focus / activation are inherited from <see cref="Card"/>.
/// </summary>
public partial class CharacterCard : Card
{
    public static CharacterCard Create(CharacterSummary summary, Action onSelected)
    {
        var card = new CharacterCard();
        card.Populate(summary);
        card.Selected += () => onSelected();
        return card;
    }

    public static CharacterSummary FromSaveData(SaveData save)
    {
        int xpToNext = Constants.Leveling.GetXpToLevel(save.Level);
        float xpPct = xpToNext > 0 ? (float)save.Xp / xpToNext * 100f : 0f;
        return new CharacterSummary(
            Class: save.SelectedClass,
            Level: save.Level,
            Str: save.Str, Dex: save.Dex, Sta: save.Sta, Int: save.Int,
            Hp: save.Hp, MaxHp: save.MaxHp,
            Mana: save.Mana, MaxMana: save.MaxMana,
            Gold: save.Gold,
            Floor: save.FloorNumber, DeepestFloor: save.DeepestFloor,
            XpPct: xpPct,
            TimestampLabel: save.SaveDate);
    }

    private void Populate(CharacterSummary s)
    {
        var nameLabel = new Label { Text = s.Class.ToString() };
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(nameLabel);

        int classIdx = (int)s.Class;
        if (classIdx < Constants.Assets.PlayerClassPreviews.Length)
        {
            var portrait = DirectionalSprite.LoadPortraitFrame(Constants.Assets.PlayerClassPreviews[classIdx]);
            if (portrait != null)
            {
                var sprite = new TextureRect();
                sprite.Texture = portrait;
                sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
                sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                sprite.CustomMinimumSize = new Vector2(92, 92);
                sprite.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                sprite.MouseFilter = MouseFilterEnum.Ignore;
                Content.AddChild(sprite);
            }
        }

        var levelLabel = new Label { Text = $"Level {s.Level}" };
        UiTheme.StyleLabel(levelLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Label);
        levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        levelLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(levelLabel);

        Content.AddChild(new HSeparator());

        var statsGrid = new GridContainer();
        statsGrid.Columns = 4;
        statsGrid.AddThemeConstantOverride("h_separation", 6);
        statsGrid.AddThemeConstantOverride("v_separation", 4);
        statsGrid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        statsGrid.MouseFilter = MouseFilterEnum.Ignore;
        AddStatRow(statsGrid, "STR", s.Str);
        AddStatRow(statsGrid, "DEX", s.Dex);
        AddStatRow(statsGrid, "STA", s.Sta);
        AddStatRow(statsGrid, "INT", s.Int);
        Content.AddChild(statsGrid);

        Content.AddChild(new HSeparator());

        var hpMp = new Label { Text = $"HP: {s.Hp}/{s.MaxHp}   MP: {s.Mana}/{s.MaxMana}" };
        UiTheme.StyleLabel(hpMp, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        hpMp.HorizontalAlignment = HorizontalAlignment.Center;
        hpMp.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(hpMp);

        var floorGold = new Label { Text = $"Floor: {s.Floor}   Deepest: {s.DeepestFloor}   Gold: {s.Gold}" };
        UiTheme.StyleLabel(floorGold, UiTheme.Colors.Info, UiTheme.FontSizes.Small);
        floorGold.HorizontalAlignment = HorizontalAlignment.Center;
        floorGold.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(floorGold);

        var xpLabel = new Label { Text = $"XP: {s.XpPct:F0}%" };
        UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        xpLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(xpLabel);

        var dateLabel = new Label { Text = s.TimestampLabel };
        UiTheme.StyleLabel(dateLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        dateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        dateLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(dateLabel);
    }

    private static void AddStatRow(GridContainer grid, string label, int value)
    {
        var lbl = new Label { Text = label };
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        lbl.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(lbl);

        var val = new Label { Text = value.ToString() };
        UiTheme.StyleLabel(val, value > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        val.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(val);
    }
}

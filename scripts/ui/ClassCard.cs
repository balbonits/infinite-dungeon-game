using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Neutral class-preview shape passed to <see cref="ClassCard"/>. The card
/// depends only on these fields, not on any specific data source.
/// </summary>
public readonly record struct ClassPreview(
    PlayerClass Class,
    string Name,
    string Description,
    int Str, int Dex, int Sta, int Int,
    string SkillName,
    string SkillType,
    Color SkillColor,
    string SkillIconPath);

/// <summary>
/// Card that renders a <see cref="ClassPreview"/> for ClassSelect. Populates
/// content only; framing / focus / activation are inherited from <see cref="Card"/>.
/// </summary>
public partial class ClassCard : Card
{
    public static ClassCard Create(ClassPreview preview, Action onSelected)
    {
        var card = new ClassCard();
        card.Populate(preview);
        card.Selected += () => onSelected();
        return card;
    }

    private void Populate(ClassPreview p)
    {
        var nameLabel = new Label { Text = p.Name };
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(nameLabel);

        int classIdx = (int)p.Class;
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

        var descLabel = new Label { Text = p.Description };
        UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        descLabel.HorizontalAlignment = HorizontalAlignment.Center;
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        descLabel.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(descLabel);

        // Expanding spacer — pushes stats + skill to the bottom so every card
        // has its stats/skill row on the same baseline regardless of how long
        // the description wraps. Without this, short-description cards (Mage)
        // cluster their content at the top while long-description cards
        // (Warrior/Ranger) fill naturally, and the cards look uneven inside.
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        Content.AddChild(spacer);

        Content.AddChild(NonInteractiveSeparator());

        var statsGrid = new GridContainer();
        statsGrid.Columns = 4;
        statsGrid.AddThemeConstantOverride("h_separation", 6);
        statsGrid.AddThemeConstantOverride("v_separation", 4);
        statsGrid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        statsGrid.MouseFilter = MouseFilterEnum.Ignore;
        AddStatRow(statsGrid, "STR", p.Str, p.Str >= 3);
        AddStatRow(statsGrid, "DEX", p.Dex, p.Dex >= 3);
        AddStatRow(statsGrid, "STA", p.Sta, p.Sta >= 3);
        AddStatRow(statsGrid, "INT", p.Int, p.Int >= 3);
        Content.AddChild(statsGrid);

        Content.AddChild(NonInteractiveSeparator());

        var skillRow = new HBoxContainer();
        skillRow.AddThemeConstantOverride("separation", 8);
        skillRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        skillRow.MouseFilter = MouseFilterEnum.Ignore;

        var iconPanel = new PanelContainer();
        var iconStyle = new StyleBoxFlat();
        iconStyle.BgColor = new Color(p.SkillColor, 0.2f);
        iconStyle.BorderColor = p.SkillColor;
        iconStyle.SetBorderWidthAll(1);
        iconStyle.SetCornerRadiusAll(4);
        iconStyle.SetContentMarginAll(4);
        iconPanel.AddThemeStyleboxOverride("panel", iconStyle);
        iconPanel.CustomMinimumSize = new Vector2(36, 36);
        iconPanel.MouseFilter = MouseFilterEnum.Ignore;

        if (ResourceLoader.Exists(p.SkillIconPath))
        {
            var skillIcon = new TextureRect();
            skillIcon.Texture = GD.Load<Texture2D>(p.SkillIconPath);
            skillIcon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            skillIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            skillIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            skillIcon.MouseFilter = MouseFilterEnum.Ignore;
            iconPanel.AddChild(skillIcon);
        }
        skillRow.AddChild(iconPanel);

        var skillVbox = new VBoxContainer();
        skillVbox.AddThemeConstantOverride("separation", 0);
        skillVbox.MouseFilter = MouseFilterEnum.Ignore;

        var skillNameLabel = new Label { Text = p.SkillName };
        UiTheme.StyleLabel(skillNameLabel, p.SkillColor, UiTheme.FontSizes.Body);
        skillNameLabel.MouseFilter = MouseFilterEnum.Ignore;
        skillVbox.AddChild(skillNameLabel);

        var skillTypeLabel = new Label { Text = p.SkillType };
        UiTheme.StyleLabel(skillTypeLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        skillTypeLabel.MouseFilter = MouseFilterEnum.Ignore;
        skillVbox.AddChild(skillTypeLabel);

        skillRow.AddChild(skillVbox);
        Content.AddChild(skillRow);
    }

    private static void AddStatRow(GridContainer grid, string statName, int value, bool isPrimary)
    {
        var nameLabel = new Label { Text = statName };
        UiTheme.StyleLabel(nameLabel, isPrimary ? UiTheme.Colors.Accent : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(nameLabel);

        var valueLabel = new Label { Text = $"+{value}" };
        Color valueColor = value == 0
            ? new Color(UiTheme.Colors.Muted, 0.4f)
            : isPrimary ? UiTheme.Colors.Accent : UiTheme.Colors.Ink;
        UiTheme.StyleLabel(valueLabel, valueColor, UiTheme.FontSizes.Body);
        valueLabel.MouseFilter = MouseFilterEnum.Ignore;
        grid.AddChild(valueLabel);
    }
}

# Accessibility Settings

## Summary

Player-configurable visual settings for readability and accessibility. Font size, color adjustments, and effect toggles ensure the game is playable for users with visual impairments, color vision deficiency, or motion sensitivity.

## Current State

Design spec. `GameSettings.ShowCombatNumbers` exists as the first toggle. The full settings menu UI is not yet built.

## Design

### Font Settings

| Setting | Options | Default | Purpose |
|---------|---------|---------|---------|
| UI Font Size | Small / Medium / Large / XL | Medium | Scales all HUD text, labels, buttons |
| Combat Text Size | Small / Medium / Large | Medium | Scales floating damage/heal numbers |
| Combat Text | On / Off | On | Toggle floating combat numbers entirely |
| Enemy Level Labels | On / Off | On | Toggle "Lv.X" text above enemies |

**Font Size Scale Factors:**

| Size | Scale | Body Text | Heading | Title |
|------|-------|-----------|---------|-------|
| Small | 0.85x | 10px | 17px | 20px |
| Medium | 1.0x | 12px | 20px | 24px |
| Large | 1.25x | 15px | 25px | 30px |
| XL | 1.5x | 18px | 30px | 36px |

### Color Settings

| Setting | Options | Default | Purpose |
|---------|---------|---------|---------|
| Color Mode | Normal / Deuteranopia / Protanopia / Tritanopia | Normal | Adjust color gradient for color blindness |
| UI Contrast | Normal / High | Normal | Increase text/bg contrast |
| Enemy Color Intensity | 50-150% slider | 100% | Adjust how vivid enemy tint colors are |
| HUD Opacity | 25-100% slider | 75% | Adjust HUD panel transparency |

**Color Blindness Palettes:**

The level-relative gradient has 8 anchor colors. Each color blindness mode remaps these anchors to remain distinguishable:

| Gap | Normal | Deuteranopia | Protanopia | Tritanopia |
|-----|--------|--------------|------------|------------|
| Trivial | Grey `#9D9D9D` | Grey `#9D9D9D` | Grey `#9D9D9D` | Grey `#9D9D9D` |
| Low | Blue `#4A7DFF` | Blue `#4A7DFF` | Blue `#4A7DFF` | Blue `#4A7DFF` |
| Low-Mid | Cyan `#4AE8E8` | Light Blue `#6BAAFF` | Light Blue `#6BAAFF` | Cyan `#4AE8E8` |
| Even | Green `#6BFF89` | Yellow `#E8E84A` | Yellow `#E8E84A` | Green `#6BFF89` |
| Mid-High | Yellow `#FFDE66` | Orange `#FFB84A` | Orange `#FFB84A` | Pink `#FF8EC8` |
| High | Gold `#F5C86B` | Dark Orange `#E89B4A` | Dark Orange `#E89B4A` | Magenta `#D66BA5` |
| Very High | Orange `#FF9340` | Red-Orange `#FF6B4A` | Brown `#C88B4A` | Purple `#9B6BFF` |
| Extreme | Red `#FF6F6F` | Dark Red `#CC4A4A` | Dark Brown `#8B6B4A` | Dark Purple `#6B4AFF` |

**Key principle:** Blue is safe across all types. The differentiation effort is in the green-yellow-red range where deuteranopia and protanopia struggle.

### Effect Settings

| Setting | Options | Default | Purpose |
|---------|---------|---------|---------|
| Damage Flash | On / Off | On | Red flash on character when hit |
| Screen Effects | On / Off | On | Slash effects, death effects |
| Reduced Motion | On / Off | Off | Minimizes all animation/movement effects |

**Reduced Motion behavior:**
- Floating combat text appears/disappears instantly (no float animation)
- Flash effects are single-frame (no tween)
- Slash effects disabled
- Enemy spawn is instant (no future fade-in animation)

### Implementation

Settings are stored in `GameSettings.cs` (static class, persisted to `user://settings.json`):

```csharp
public static class GameSettings
{
    // Font
    public static float UiFontScale { get; set; } = 1.0f;
    public static float CombatTextScale { get; set; } = 1.0f;
    public static bool ShowCombatNumbers { get; set; } = true;
    public static bool ShowEnemyLevels { get; set; } = true;

    // Color
    public static int ColorMode { get; set; } = 0; // 0=Normal, 1=Deut, 2=Prot, 3=Trit
    public static bool HighContrast { get; set; } = false;
    public static float EnemyColorIntensity { get; set; } = 1.0f;
    public static float HudOpacity { get; set; } = 0.75f;

    // Effects
    public static bool ShowDamageFlash { get; set; } = true;
    public static bool ShowScreenEffects { get; set; } = true;
    public static bool ReducedMotion { get; set; } = false;
}
```

### Settings Menu Location

Accessible from:
1. **Pause menu** → Settings button
2. **Future: Main menu** → Settings button
3. **Future: Death screen** → Settings button (in case current settings make the game unplayable)

## Open Questions

- Should font family be configurable (e.g., dyslexia-friendly font option)?
- Should there be a "colorblind test" screen that shows all gradient colors side-by-side?
- Should high contrast mode affect tile/sprite rendering or only UI?
- How should settings persist across game restarts (JSON in user:// directory)?

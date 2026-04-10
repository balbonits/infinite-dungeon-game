# Game UI Design

## Why This Matters
Our pause menu rendered in the top-left corner, HUD labels had no anchors, and NPC panels used hardcoded pixel positions that broke at different sizes. Game UI fails differently than web UI — there's no DOM, no CSS, no flexbox. Understanding Godot's Control system prevents every positioning bug.

## Core Concepts

### Game UI vs Web UI
| Web | Godot Equivalent |
|-----|------------------|
| `<div>` | `Control` / `Panel` |
| `display: flex` | `VBoxContainer` / `HBoxContainer` |
| `margin: auto` | `CenterContainer` |
| `padding` | `MarginContainer` |
| `position: absolute` | Manual `Position` (avoid this) |
| `position: fixed` | `CanvasLayer` |
| `width: 100%` | `SizeFlags.ExpandFill` |
| `z-index` | `CanvasLayer.Layer` |
| `pointer-events: none` | `MouseFilter = Ignore` |

### Layout Containers — The Right Way
**Never use `Position = new Vector2(x, y)` for UI.** Use containers instead:

| Container | Does | Use For |
|-----------|------|---------|
| `CenterContainer` | Centers child at its minimum size | Centering a panel on screen |
| `VBoxContainer` | Stacks children vertically | Button lists, stat columns |
| `HBoxContainer` | Stacks children horizontally | Top bar (left/center/right) |
| `MarginContainer` | Adds padding around child | Inset content inside a panel |
| `GridContainer` | Grid layout with fixed columns | Inventory slots |
| `PanelContainer` | Styled background + single child | Cards, windows |
| `ScrollContainer` | Scrollable region | Long lists |

### Anchors and Presets
Every Control has 4 anchors (0.0 to 1.0) that define where it's pinned relative to its parent:

```csharp
// Fill entire parent
control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

// Pin to top-right corner
control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);

// Center on screen
control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
```

**The GrowDirection Bug (GitHub #86004):** When setting `Center` anchor programmatically, the default `GrowDirection = End` means the control only grows right and down. Negative offsets (to go left/up from center) get ignored. Fix: use `CenterContainer` instead, or set `GrowDirection = Both`.

### The CenterContainer Pattern
The most reliable way to center anything:

```csharp
// Root fills screen
var root = new Control();
root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

// CenterContainer also fills screen
var center = new CenterContainer();
center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
root.AddChild(center);

// Panel gets centered automatically at its CustomMinimumSize
var panel = new PanelContainer();
panel.CustomMinimumSize = new Vector2(400, 300);
center.AddChild(panel);
```

### Size Flags
Inside containers, children negotiate size using flags:

```csharp
label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;  // Take all available space
label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;  // Only take what you need, centered
```

### Resolution Independence
With `stretch/mode = "canvas_items"` and viewport 1920x1080, the logical coordinate system is always 1920x1080 regardless of window size. UI scales automatically. This means:
- Anchored UI always works
- Container-based layout always works
- Hardcoded `Position = new Vector2(960, 540)` breaks if the aspect ratio changes

### Theme and StyleBox
Godot's styling system for Controls:

```csharp
var style = new StyleBoxFlat();
style.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.88f);
style.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
style.SetBorderWidthAll(2);
style.SetCornerRadiusAll(6);
style.SetContentMarginAll(12);
panel.AddThemeStyleboxOverride("panel", style);
```

## Godot 4 + C# Implementation

```csharp
// Responsive HUD: top bar with left/center/right labels
var hud = new Control();
hud.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
hud.MouseFilter = MouseFilterEnum.Ignore;

var topMargin = new MarginContainer();
topMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
topMargin.OffsetBottom = 40;
topMargin.AddThemeConstantOverride("margin_left", 16);
topMargin.AddThemeConstantOverride("margin_right", 16);
topMargin.AddThemeConstantOverride("margin_top", 10);
hud.AddChild(topMargin);

var topBar = new HBoxContainer();
topMargin.AddChild(topBar);

var leftLabel = new Label { Text = "Lv.5" };
leftLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
topBar.AddChild(leftLabel);

var centerLabel = new Label { Text = "Floor 3" };
centerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
centerLabel.HorizontalAlignment = HorizontalAlignment.Center;
topBar.AddChild(centerLabel);

var rightLabel = new Label { Text = "500 Gold" };
rightLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
rightLabel.HorizontalAlignment = HorizontalAlignment.Right;
topBar.AddChild(rightLabel);
```

## Common Mistakes
1. **Using Position instead of anchors** — UI breaks at different window sizes (OUR BUG)
2. **Centering with anchors instead of CenterContainer** — GrowDirection gotcha causes top-left offset (OUR BUG)
3. **Forgetting SetAnchorsAndOffsetsPreset on root Control** — child anchors are relative to parent size, which defaults to (0,0)
4. **Forgetting MouseFilter.Ignore** — HUD captures mouse clicks meant for the game
5. **Nesting Controls in Node2D** — Controls inside Node2D don't anchor properly; use CanvasLayer
6. **Hardcoded Size instead of CustomMinimumSize** — Size is overridden by containers; MinimumSize is respected
7. **Not using containers** — manual positioning of every element is fragile and unmaintainable

## Checklist
- [ ] Every UI panel uses containers (VBox, HBox, Margin, Center), not Position
- [ ] Root Control has SetAnchorsAndOffsetsPreset(FullRect)
- [ ] CenterContainer used for centering (not manual anchors)
- [ ] All UI on a CanvasLayer (not in game world)
- [ ] MouseFilter = Ignore on non-interactive overlays
- [ ] CustomMinimumSize (not Size) for panels inside containers

## Sources
- [Godot GUI Tutorial](https://docs.godotengine.org/en/stable/tutorials/ui/index.html)
- [Godot Size and Anchors](https://docs.godotengine.org/en/stable/tutorials/ui/size_and_anchors.html)
- [Godot Containers](https://docs.godotengine.org/en/stable/tutorials/ui/gui_containers.html)
- [GrowDirection Bug #86004](https://github.com/godotengine/godot/issues/86004)
- [Godot Forum: Centering Not Working](https://godotforums.org/d/36829-anchors-preset-center-not-working-correctly-solved)

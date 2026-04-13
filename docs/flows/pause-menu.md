# Flow: Pause Menu

**Script:** `scripts/ui/PauseMenu.cs`
**Scene:** `scenes/pause_menu.tscn`

## Open/Close

| Input | State | Action |
|-------|-------|--------|
| Escape | Game running | `ShowMenu()` → pause game, show panel |
| Escape | Menu open | `OnResumePressed()` → unpause, hide |
| Escape | Death screen visible | Blocked (death screen takes priority) |
| D / action_circle | Menu open | `OnResumePressed()` |

### ShowMenu()

```
1. Visible = true
2. GetTree().Paused = true
3. UiTheme.FocusFirstButton(_buttonContainer)
```

### OnResumePressed()

```
1. Visible = false
2. GetTree().Paused = false
```

## Button List (scene node names)

| Node Name | Text | Action |
|-----------|------|--------|
| ResumeButton | "Resume" | Unpause, hide menu |
| BackpackButton | "Backpack" | Hide menu, open BackpackWindow |
| StatsButton | "Stats" | Hide menu, open StatAllocDialog |
| SkillsButton | "Skills" | Hide menu, open SkillTreeDialog |
| LedgerButton | "Dungeon Ledger" | Hide menu, open DungeonLedger |
| TutorialButton | "Tutorial" | Hide menu, open TutorialPanel |
| SettingsButton | "Settings" | Hide menu, open SettingsPanel |
| MainMenuButton | "Back to Main Menu" | Unpause, save, reload scene |
| QuitButton | "Quit Game" | `GetTree().Quit()` |

## Input (when visible)

| Input | Action |
|-------|--------|
| Up/Down | Navigate buttons (KeyboardNav) |
| S / action_cross | Press focused button |
| D / Escape | Resume (close menu) |
| All other keys | Consumed (blocks walking behind menu) |

## Sub-Dialog Behavior

When a sub-dialog opens:
1. PauseMenu sets `Visible = false`
2. Sub-dialog opens (modal, added to WindowStack)
3. When sub-dialog closes, callback fires:
   - PauseMenu sets `Visible = true`
   - `UiTheme.FocusFirstButton()` restores focus
4. Game remains paused (PauseMenu controls pause state)

Exception: Tutorial and Settings pass a close callback. Backpack, Stats, Skills, Ledger handle their own close → pause menu restore.

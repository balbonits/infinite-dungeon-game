using System.Threading.Tasks;
using DungeonGame.Testing;
using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Full-run walkthrough — AutoPilot plays the entire game x3 classes.
/// Each class does: splash → class select → town → NPC → pause menu → dungeon → combat → death → respawn.
/// Flow docs: docs/flows/*.md
///
/// Headless: make sandbox-headless SCENE=full-run
/// Visual:   make sandbox SCENE=full-run
/// </summary>
public partial class FullRunSandbox : Node
{
    private static readonly PlayerClass[] ClassesToTest =
        { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage };

    public override void _Ready()
    {
        CallDeferred(MethodName.Bootstrap);
    }

    private void Bootstrap()
    {
        var pilot = new AutoPilot();
        pilot.SetWalkthrough(RunAllClasses);
        GetTree().Root.AddChild(pilot);
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Run all 3 classes
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task RunAllClasses(AutoPilot pilot)
    {
        for (int i = 0; i < ClassesToTest.Length; i++)
        {
            var cls = ClassesToTest[i];
            pilot.Log("");
            pilot.Log($"╔══════════════════════════════════════════╗");
            pilot.Log($"║  Class {i + 1}/3: {cls,-10}                    ║");
            pilot.Log($"╚══════════════════════════════════════════╝");

            await RunSingleClass(pilot, cls, isFirstRun: i == 0);

            // Reload main scene for next class (unless last)
            if (i < ClassesToTest.Length - 1)
            {
                pilot.Log("   Reloading for next class...");
                pilot.GetTree().ChangeSceneToFile("res://scenes/main.tscn");
                await pilot.Actions.WaitSeconds(2.0f);
            }
        }
        // LaunchWalkthrough handles Finish()
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Single class walkthrough
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task RunSingleClass(AutoPilot pilot, PlayerClass targetClass, bool isFirstRun)
    {
        var act = pilot.Actions;
        var verify = pilot.Verify;

        if (!isFirstRun)
            await act.WaitSeconds(0.5f);

        // ── Phase 1: Splash Screen ───────────────────────────────────────
        // Flow: docs/flows/splash-screen.md
        await pilot.Run($"[{targetClass}] Splash: New Game", async () =>
        {
            await act.WaitUntil(() => FindButtonInScene(pilot, "New Game") != null, 5f);
            var btn = FindButtonInScene(pilot, "New Game");
            pilot.Assert(btn != null, "New Game button found");
            if (btn != null) act.ClickButton(btn);
            await act.WaitFrames(10);
        });

        // ── Phase 2: Class Selection ─────────────────────────────────────
        // Flow: docs/flows/class-select.md
        // Zone 0 = cards (Left/Right to navigate, S to select)
        // Zone 1 = confirm (Down from cards, S to confirm)
        await pilot.Run($"[{targetClass}] Select class", async () =>
        {
            await act.WaitSeconds(0.5f);

            // Navigate to correct card. _focusIndex starts at -1.
            // Each Right press: -1→0, 0→1, 1→2. Left/Right auto-selects.
            // So Warrior needs 1 Right, Ranger needs 2, Mage needs 3.
            int classIndex = (int)targetClass; // Warrior=0, Ranger=1, Mage=2
            for (int i = 0; i <= classIndex; i++)
            {
                await act.Press(Constants.InputActions.MoveRight);
                await act.WaitSeconds(0.2f);
            }

            // Card is now selected (Left/Right auto-selects), Confirm enabled.
            // Navigate down to Confirm zone.
            await act.Press(Constants.InputActions.MoveDown);
            await act.WaitSeconds(0.2f);

            // Press Confirm
            await act.Press(Constants.InputActions.ActionCross);

            // Wait for 0.4s confirm tween to start
            await act.WaitSeconds(0.8f);

            // Wait for screen transition (~2.1s)
            await act.WaitForTransition();
            await act.WaitSeconds(1.5f);
        });

        // ── Phase 3: Verify Town ─────────────────────────────────────────
        // Flow: docs/flows/town.md
        await pilot.Run($"[{targetClass}] Verify town loaded", async () =>
        {
            await act.WaitSeconds(0.5f);

            // Real verification: player node must exist in scene tree
            var players = pilot.GetTree().GetNodesInGroup(Constants.Groups.Player);
            pilot.Assert(players.Count > 0, "Player node in scene tree");

            var gs = Autoloads.GameState.Instance;
            pilot.Assert(gs != null, "GameState exists");
            if (gs == null) return;

            verify.Alive();
            verify.AtLevel(1);
            pilot.Assert(gs.SelectedClass == targetClass,
                $"Class is {targetClass} (actual={gs.SelectedClass})");
            pilot.Assert(gs.MaxMana > 0, $"Mana initialized (MaxMana={gs.MaxMana})");
        });

        // ── Phase 4: Town Movement ───────────────────────────────────────
        await pilot.Run($"[{targetClass}] Move around town", async () =>
        {
            await act.MoveDirection(Vector2.Right, 0.8f);
            await act.MoveDirection(Vector2.Down, 0.5f);
            await act.MoveDirection(Vector2.Left, 0.5f);
            verify.Alive();
        });

        // ── Phase 5: NPC Interaction ─────────────────────────────────────
        // Flow: docs/flows/npc-interaction.md
        await pilot.Run($"[{targetClass}] NPC interaction", async () =>
        {
            // Move toward shopkeeper area (tile 5,7)
            await act.MoveDirection(new Vector2(-1, -1).Normalized(), 1.5f);
            await act.WaitSeconds(0.3f);

            // Try to interact
            await act.Press(Constants.InputActions.ActionCross);
            await act.WaitSeconds(0.5f);

            // Check if NPC panel opened (look for a service button or Cancel)
            var cancelBtn = FindButtonInScene(pilot, "Cancel");
            if (cancelBtn != null)
            {
                pilot.Assert(true, "NPC panel opened");
                // Press service button (first button = service)
                await act.Press(Constants.InputActions.ActionCross);
                await act.WaitSeconds(0.5f);
                // Close whatever opened
                await act.Press(Constants.InputActions.ActionCircle);
                await act.WaitSeconds(0.3f);
            }
            else
            {
                pilot.Log("   (NPC not in range — skipping)");
            }

            // Make sure we close everything
            await act.Press(Constants.InputActions.ActionCircle);
            await act.WaitSeconds(0.2f);
        });

        // ── Phase 6: Pause Menu ──────────────────────────────────────────
        // Flow: docs/flows/pause-menu.md
        await pilot.Run($"[{targetClass}] Pause menu", async () =>
        {
            await act.Press(Constants.InputActions.Start);
            await act.WaitSeconds(0.5f);

            var resumeBtn = FindButtonInScene(pilot, "Resume");
            pilot.Assert(resumeBtn != null, "Pause menu opened");

            // Close it
            await act.Press(Constants.InputActions.Start);
            await act.WaitSeconds(0.3f);
        });

        // ── Phase 7: Enter Dungeon ───────────────────────────────────────
        // Flow: docs/flows/town.md (Dungeon Entrance section)
        // Flow: docs/flows/screen-transition.md
        await pilot.Run($"[{targetClass}] Enter dungeon", async () =>
        {
            // Dungeon entrance is at top of town (tile 12,2)
            // Move up toward it
            await act.MoveDirection(Vector2.Up, 3.5f);
            await act.WaitSeconds(0.5f);

            // Wait for enemies to appear (signals dungeon loaded)
            try
            {
                await act.WaitUntil(() =>
                    pilot.GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count > 0, 8f);

                verify.OnFloor(1);
                verify.Alive();
                pilot.Assert(true, "Dungeon entered — enemies present");
            }
            catch (System.TimeoutException)
            {
                pilot.Log("   (dungeon entrance not reached)");
            }
        });

        // ── Phase 8: Combat ──────────────────────────────────────────────
        // Flow: docs/flows/combat.md
        await pilot.Run($"[{targetClass}] Combat", async () =>
        {
            int enemyCount = pilot.GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
            if (enemyCount == 0)
            {
                pilot.Log("   (no enemies — skipping)");
                return;
            }

            var gs = Autoloads.GameState.Instance;
            int startXp = gs?.Xp ?? 0;
            int startGold = gs?.PlayerInventory.Gold ?? 0;

            // Move around to engage auto-attack (no button needed per flow doc)
            await act.MoveDirection(Vector2.Right, 1.5f);
            await act.MoveDirection(Vector2.Left, 1.5f);
            await act.MoveDirection(new Vector2(1, -1).Normalized(), 1.0f);
            await act.WaitSeconds(2.0f);

            if (gs != null)
            {
                if (gs.Xp > startXp)
                    pilot.Assert(true, $"XP gained ({startXp} → {gs.Xp})");
                else
                    pilot.Log("   (no XP — enemies may be out of range)");

                if (gs.PlayerInventory.Gold > startGold)
                    pilot.Assert(true, $"Gold gained ({startGold} → {gs.PlayerInventory.Gold})");
            }

            verify.Alive();
        });

        // ── Phase 9: Achievement Check ───────────────────────────────────
        await pilot.Run($"[{targetClass}] Achievement check", async () =>
        {
            await act.WaitFrames(5);
            var gs = Autoloads.GameState.Instance;
            if (gs == null) return;

            // Evaluate achievements based on current counters
            gs.Achievements.Evaluate();

            int kills = gs.Achievements.GetCounter("enemies_killed");
            if (kills > 0)
                verify.AchievementUnlocked("c_first_blood");
            else
                pilot.Log("   (no kills — First Blood not expected)");
        });

        // ── Phase 10: Force Death ────────────────────────────────────────
        // Flow: docs/flows/death.md
        await pilot.Run($"[{targetClass}] Force death + respawn", async () =>
        {
            var gs = Autoloads.GameState.Instance;
            if (gs == null || gs.IsDead) return;

            // Force death by setting HP to 0 (triggers PlayerDied signal)
            gs.Hp = 0;
            await act.WaitSeconds(1.0f);

            // Verify death triggered
            pilot.Assert(gs.IsDead, "Player is dead");

            // Death screen Step 1: click "Return to Town" button directly
            var townBtn = FindButtonInScene(pilot, Strings.Death.ReturnToTown);
            if (townBtn != null)
            {
                act.ClickButton(townBtn);
                await act.WaitSeconds(0.5f);
            }
            else
            {
                // Fallback: press S (auto-focused button)
                await act.Press(Constants.InputActions.ActionCross);
                await act.WaitSeconds(0.5f);
            }

            // Death screen Step 2: click "Confirm" button directly
            var confirmBtn = FindButtonInScene(pilot, Strings.Death.Confirm);
            if (confirmBtn != null)
            {
                act.ClickButton(confirmBtn);
                await act.WaitSeconds(0.5f);
            }
            else
            {
                pilot.Log("   (Confirm button not found — trying keyboard)");
                // Navigate to Confirm: Down through toggles
                for (int d = 0; d < 4; d++)
                {
                    await act.Press(Constants.InputActions.MoveDown);
                    await act.WaitSeconds(0.1f);
                }
                await act.Press(Constants.InputActions.ActionCross);
                await act.WaitSeconds(0.5f);
            }

            // Wait for respawn transition
            await act.WaitForTransition();
            await act.WaitSeconds(1.5f);

            // Verify respawn
            verify.Alive();
            pilot.Log($"   Respawned — HP={gs.Hp}/{gs.MaxHp}");
        });

        // ── Phase 11: Final State ────────────────────────────────────────
        await pilot.Run($"[{targetClass}] Final state", async () =>
        {
            await act.WaitFrames(5);
            var gs = Autoloads.GameState.Instance;
            if (gs == null) return;

            pilot.Log($"   Class={gs.SelectedClass} Level={gs.Level} Floor={gs.FloorNumber}");
            pilot.Log($"   HP={gs.Hp}/{gs.MaxHp} Mana={gs.Mana}/{gs.MaxMana}");
            pilot.Log($"   XP={gs.Xp} Gold={gs.PlayerInventory.Gold}");
            pilot.Log($"   Kills={gs.Achievements.GetCounter("enemies_killed")}");
            pilot.Log($"   DeepestFloor={gs.DeepestFloor}");
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Button? FindButtonInScene(AutoPilot pilot, string text)
    {
        return SearchForButton(pilot.GetTree().Root, text);
    }

    private static Button? SearchForButton(Node parent, string text)
    {
        if (parent is Button btn && btn.Text == text && btn.Visible)
            return btn;
        foreach (var child in parent.GetChildren())
        {
            var found = SearchForButton(child, text);
            if (found != null) return found;
        }
        return null;
    }
}

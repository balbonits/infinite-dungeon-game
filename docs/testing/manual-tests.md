# Manual Test Cases

## Summary

Detailed step-by-step manual test cases for every feature in "A Dungeon in the Middle of Nowhere." These tests verify visual correctness, game feel, and behaviors that cannot be easily automated.

## Current State

- 33 test cases defined covering all implemented systems
- Tests are written for the Godot 4 version of the game
- Run these tests after each significant code change or system implementation
- Estimated full playthrough time: 20-30 minutes

## Design

### Test Case Format

Each test follows this structure:
- **MT-XXX:** Unique identifier
- **Feature:** What system/behavior is being tested
- **Prerequisites:** What must be set up or true before running the test
- **Steps:** Numbered actions to perform
- **Expected Result:** What should happen if the feature works correctly
- **Edge Cases:** Special scenarios to verify within this test

### Test Categories

| Category | Test IDs | Count |
|----------|----------|-------|
| Core | MT-001 to MT-003 | 3 |
| Movement | MT-004 to MT-012 | 9 |
| Enemies | MT-013 to MT-017 | 5 |
| Combat | MT-018 to MT-027 | 10 |
| HUD | MT-028 to MT-029 | 2 |
| Death/Restart | MT-030 to MT-033 | 4 |
| **Total** | | **33** |

---

## Core

### MT-001: Game Launch
**Feature:** Basic game initialization
**Prerequisites:** Godot 4 project with main scene configured
**Steps:**
1. Open the project in Godot editor
2. Press F5 (or Play Scene button) to run the game
3. Observe the game window that appears
4. Check the Godot Output panel for errors
**Expected Result:**
- Game window appears at the configured resolution (1920x1080 or project default)
- No errors or warnings appear in the Godot Output/Debugger panel
- The game world is visible (dark background with floor tiles)
- The HUD overlay is visible in the top-left corner
- Player character is visible
- Enemies are visible
**Edge Cases:**
- Close and reopen the game multiple times -- no crashes on repeated launches
- Resize the window -- game should still render correctly
- Check that the window title matches the project name

### MT-002: Isometric Floor
**Feature:** Floor rendering and visual appearance
**Prerequisites:** Game is running (MT-001 passes)
**Steps:**
1. Look at the game world background
2. Observe the floor tile pattern
3. Look at the edges of the play area for wall borders
**Expected Result:**
- Floor is visible as a diamond-shaped isometric grid
- Floor tiles use dark blue coloring (matching the Phaser prototype's `#131927` background with `#24314a` grid lines)
- Wall borders are visible at the edges of the play area as lighter-colored tiles or boundary indicators
- The grid is aligned to the isometric axes (diamonds, not squares)
**Edge Cases:**
- Move the camera to all four corners of the play area -- floor tiles should render consistently
- No gaps or visual artifacts between tiles
- Grid lines should be subtle (low opacity, approximately 25% as in the Phaser prototype)

### MT-003: Player Visible
**Feature:** Player character rendering
**Prerequisites:** Game is running (MT-001 passes)
**Steps:**
1. Look at the center of the play area when the game starts
2. Identify the player character
3. Note the player's shape and color
**Expected Result:**
- A blue diamond-shaped character is visible at the center of the room
- Player color is `#8ed6ff` (light blue)
- Player is clearly distinguishable from enemies and floor tiles
- Player appears at approximately the center of the viewport on initial spawn
**Edge Cases:**
- Player should not spawn inside a wall or outside the play area
- Player should be rendered on top of floor tiles (correct z-ordering)
- Player should be behind the HUD overlay (HUD is on CanvasLayer 10)

---

## Movement

### MT-004: Move Up (W Key)
**Feature:** Upward movement with W key in isometric space
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Note the player's starting position
2. Press and hold the W key
3. Observe the player's movement direction on screen
4. Release the W key
**Expected Result:**
- Player moves **northeast** in the isometric view (up-right on screen)
- Movement is smooth and continuous while the key is held
- Player moves at the correct speed (190 pixels/second)
- Player stops immediately when the key is released (no momentum/drift)
**Edge Cases:**
- Tap W quickly multiple times -- player should move a small amount each tap, no stuttering
- Hold W for 5+ seconds -- movement should remain constant speed, no acceleration
- Press W at the very start of the game (before any other input) -- should work immediately

### MT-005: Move Down (S Key)
**Feature:** Downward movement with S key in isometric space
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Note the player's starting position
2. Press and hold the S key
3. Observe the player's movement direction on screen
4. Release the S key
**Expected Result:**
- Player moves **southwest** in the isometric view (down-left on screen)
- Movement is smooth and continuous while the key is held
- Player moves at the correct speed (190 pixels/second)
- Player stops immediately when the key is released
**Edge Cases:**
- Movement direction should be exactly opposite to W key movement
- Moving down from the starting position should not immediately hit a wall (player spawns near center)

### MT-006: Move Left (A Key)
**Feature:** Leftward movement with A key in isometric space
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Note the player's starting position
2. Press and hold the A key
3. Observe the player's movement direction on screen
4. Release the A key
**Expected Result:**
- Player moves **northwest** in the isometric view (up-left on screen)
- Movement is smooth and continuous while the key is held
- Player moves at the correct speed (190 pixels/second)
- Player stops immediately when the key is released
**Edge Cases:**
- Movement direction should be exactly opposite to D key movement
- Should feel equally responsive as W/S movement

### MT-007: Move Right (D Key)
**Feature:** Rightward movement with D key in isometric space
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Note the player's starting position
2. Press and hold the D key
3. Observe the player's movement direction on screen
4. Release the D key
**Expected Result:**
- Player moves **southeast** in the isometric view (down-right on screen)
- Movement is smooth and continuous while the key is held
- Player moves at the correct speed (190 pixels/second)
- Player stops immediately when the key is released
**Edge Cases:**
- Movement direction should be exactly opposite to A key movement
- Combined with W key, should produce pure eastward (rightward) movement

### MT-008: Arrow Keys
**Feature:** Arrow key movement parity with WASD
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Press and hold the Up Arrow key -- observe movement direction
2. Release, then press and hold the Down Arrow key -- observe movement direction
3. Release, then press and hold the Left Arrow key -- observe movement direction
4. Release, then press and hold the Right Arrow key -- observe movement direction
5. Compare each direction to the corresponding WASD key (MT-004 through MT-007)
**Expected Result:**
- Up Arrow produces the same movement as W key (northeast / up-right)
- Down Arrow produces the same movement as S key (southwest / down-left)
- Left Arrow produces the same movement as A key (northwest / up-left)
- Right Arrow produces the same movement as D key (southeast / down-right)
- Speed and responsiveness are identical to WASD
**Edge Cases:**
- Mix WASD and arrow keys: press W and Right Arrow simultaneously -- should produce combined input
- Press W and Up Arrow simultaneously -- should not move faster (both map to the same action)

### MT-009: Diagonal Movement
**Feature:** Simultaneous two-key movement with normalized speed
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Press and hold W + D simultaneously
2. Observe the player's movement direction and speed
3. Release, then press and hold W + A simultaneously
4. Observe the player's movement direction and speed
5. Compare diagonal movement speed to single-key cardinal movement speed (from MT-004)
**Expected Result:**
- W + D: player moves **east** (pure right on screen) -- the isometric combination of northeast + southeast
- W + A: player moves **north** (pure up on screen) -- the isometric combination of northeast + northwest
- S + D: player moves **south** (pure down on screen)
- S + A: player moves **west** (pure left on screen)
- **Diagonal speed is the same as cardinal speed** -- the input vector is normalized before applying speed, so pressing two keys does not make the player move faster
**Edge Cases:**
- Rapidly alternate between diagonal and cardinal movement -- transitions should be smooth
- Press all four keys simultaneously -- player should not move (vectors cancel out to zero)
- Press three keys (e.g., W + A + D) -- A and D cancel, result is same as W alone

### MT-010: Wall Collision
**Feature:** Player cannot move outside the play area boundaries
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Move the player toward the top-right wall (hold W key)
2. Continue holding W after the player reaches the wall boundary
3. Observe the player's behavior at the wall
4. While still at the wall, press D -- observe if the player slides along the wall
5. Repeat for all four walls/boundaries
**Expected Result:**
- Player stops at the wall boundary and cannot pass through
- Player **slides along the wall** when pressing a direction that has a component parallel to the wall (e.g., pressing W at the right wall should slide the player upward along the wall)
- No visual glitching, jittering, or player getting stuck
- Player does not partially overlap or clip through the wall
**Edge Cases:**
- Move into a corner (e.g., top-right) -- player should stop completely, no jitter
- Rapidly press movement keys while against a wall -- no stuck state
- Move away from the wall -- player moves normally, no lingering collision effects

### MT-011: Stop on Release
**Feature:** Player has zero momentum -- stops instantly when keys are released
**Prerequisites:** Game is running, player is moving (MT-004 passes)
**Steps:**
1. Hold W to move the player
2. While the player is moving at full speed, release all keys
3. Observe the player's behavior after key release
4. Repeat with each direction key (S, A, D)
5. Repeat while moving diagonally (W + D, then release both)
**Expected Result:**
- Player stops **immediately** upon releasing all movement keys
- No sliding, momentum, deceleration, or drift
- Player remains at the exact position where keys were released
- This applies regardless of how long the player was moving or in which direction
**Edge Cases:**
- Release one key of a diagonal pair (e.g., release D while holding W + D) -- player should immediately change to W-only movement direction, no brief pause
- Very quick tap and release -- player should move a tiny amount and stop

### MT-012: Camera Follow
**Feature:** Camera smoothly follows the player, keeping them centered
**Prerequisites:** Game is running, player is visible (MT-003 passes)
**Steps:**
1. Move the player from center toward the right edge of the play area (hold D)
2. Observe how the camera follows the player
3. Move the player to each corner of the play area
4. Observe the camera behavior at the boundaries of the play area
**Expected Result:**
- Camera smoothly follows the player, keeping the player near the center of the viewport
- Camera movement is smooth (not jerky or snapping to new positions)
- When the player reaches the edge of the play area, the camera should either:
  - Stop following (showing empty space would be avoided), OR
  - Continue following (camera limits clamped to world bounds)
- Player remains visible at all times
**Edge Cases:**
- Move the player quickly back and forth -- camera should follow smoothly without oscillation
- Camera should not show areas outside the game world (if camera limits are implemented)
- During camera shake (from taking damage), the camera should return to centered position afterward

---

## Enemies

### MT-013: Initial Spawn
**Feature:** 10 enemies spawn when the game starts
**Prerequisites:** Fresh game start (launch game or restart)
**Steps:**
1. Start a new game (F5 or restart)
2. Before moving the player, count all visible enemies on screen
3. Note the variety of enemy colors
**Expected Result:**
- Exactly **10 enemies** are visible on the screen at game start
- Enemies are a mix of green, yellow, and red (three tiers, random distribution)
- Enemies are distributed around the play area (spawned from edges)
- All enemies are moving toward the player
**Edge Cases:**
- Restart the game multiple times -- should always be 10 enemies initially
- Enemy positions should vary between restarts (random edge spawning)
- Tier distribution should vary between restarts (random tier assignment)

### MT-014: Enemy Colors
**Feature:** Three distinct enemy colors representing danger tiers
**Prerequisites:** Game is running, enemies are visible (MT-013 passes)
**Steps:**
1. Observe the enemies on screen
2. Identify three distinct color groups
3. Compare colors to the specification
**Expected Result:**
- **Green enemies** (`#6bff89`): Tier 1, low danger -- bright green, clearly distinct from yellow
- **Yellow enemies** (`#ffde66`): Tier 2, mid danger -- warm yellow/gold, clearly distinct from green and red
- **Red enemies** (`#ff6f6f`): Tier 3, high danger -- soft red/salmon, clearly distinct from yellow
- All three colors are easily distinguishable from each other
- All three colors are easily distinguishable from the player (blue `#8ed6ff`)
- All three colors are easily distinguishable from the floor tiles (dark blue)
**Edge Cases:**
- With 10 random enemies, it is statistically possible (though unlikely) to get all one tier -- this is acceptable
- Colors should be consistent: same tier always produces the same color

### MT-015: Enemy Chase
**Feature:** Enemies pursue the player
**Prerequisites:** Game is running, enemies are visible (MT-013 passes)
**Steps:**
1. Stand still (do not press any movement keys)
2. Observe the enemies' movement
3. Note the direction each enemy is moving
4. Move the player to a different location
5. Observe if enemies change direction to follow
**Expected Result:**
- All enemies move **directly toward the player's position**
- Enemies move in a straight line (no pathfinding, no avoidance)
- When the player moves, enemies adjust their direction to follow the new position
- Faster enemies (higher tier) visibly move faster than slower enemies (lower tier)
- Enemies do not stop or pause during pursuit
**Edge Cases:**
- Multiple enemies may overlap/stack when approaching from the same direction -- this is expected (no avoidance)
- Enemies at different speeds will arrive at different times -- tier 3 (red, speed 102) should arrive before tier 1 (green, speed 66) if starting from the same distance

### MT-016: Periodic Spawn
**Feature:** New enemies spawn on a 2.8-second repeating timer
**Prerequisites:** Game is running, some enemies have been killed (enemy count below 14)
**Steps:**
1. Kill one or more enemies to bring the count below 14
2. After the last kill, watch for new enemies to appear
3. Time the interval between new enemy appearances (should be approximately 2.8 seconds)
4. Note where the new enemy appears (should be at a screen edge)
**Expected Result:**
- A new enemy spawns approximately every **2.8 seconds** (periodic timer)
- New enemies spawn at the **edges of the play area** (not in the middle, not on the player)
- Spawning continues as long as the active enemy count is below 14
- New enemies immediately begin chasing the player
**Edge Cases:**
- If the count is already at 14, no new enemies should spawn from the periodic timer
- The periodic timer runs independently of respawn timers (killing an enemy also triggers a 1.4s respawn)

### MT-017: Soft Cap
**Feature:** Enemy count should not exceed 14 from periodic spawning
**Prerequisites:** Game is running (MT-013 passes)
**Steps:**
1. Wait without killing any enemies (let the periodic spawn timer run)
2. Count enemies periodically as time passes
3. Continue waiting for at least 30 seconds past when the count reaches 14
4. Count enemies again
**Expected Result:**
- Enemy count starts at 10 (initial spawn)
- Count gradually increases as the periodic timer spawns new enemies
- Count stops increasing once it reaches **14** (the soft cap)
- Even after waiting well past the cap, the count does not exceed 14
**Edge Cases:**
- Kill enemies to bring the count below 14, then wait -- count should rise back toward 14
- The soft cap only applies to the periodic timer; respawn-on-kill timers might briefly push the count to 14 (but the periodic timer will not add more)

---

## Combat

### MT-018: Auto-Attack Fires
**Feature:** Player automatically attacks the nearest enemy in range
**Prerequisites:** Game is running, enemies are visible (MT-013 passes)
**Steps:**
1. Move the player toward a group of enemies
2. Get close enough that at least one enemy is within attack range (78 pixels)
3. Wait for approximately 0.42 seconds (attack cooldown)
4. Observe whether a slash effect appears on the nearest enemy
**Expected Result:**
- A **gold-colored slash mark** appears at the position of the nearest enemy
- The attack fires automatically -- no button press required
- Attacks repeat approximately every **0.42 seconds** (420ms) as long as an enemy is in range
- The attack targets the **nearest** enemy, not a random one
**Edge Cases:**
- If two enemies are equidistant, either one being targeted is acceptable
- Attack should fire immediately when the first enemy enters range (after the cooldown expires from the last attack, or immediately if no recent attack)

### MT-019: Attack Range
**Feature:** Attacks only fire when an enemy is within 78 pixels
**Prerequisites:** Game is running, enemies are visible (MT-013 passes)
**Steps:**
1. Move the player **away** from all enemies (get far from any enemy)
2. Verify no attacks are firing (no slash effects appearing)
3. Slowly move the player **toward** an enemy
4. Note the distance at which the first attack fires
5. Move the player **away** again until attacks stop
6. Note the distance at which attacks stop
**Expected Result:**
- **No attacks** fire when all enemies are farther than 78 pixels from the player
- Attacks **begin** when the nearest enemy crosses within 78 pixels
- Attacks **stop** when the player moves far enough that no enemy is within 78 pixels
- The transition between attacking and not attacking is clean (no lingering attacks on far enemies)
**Edge Cases:**
- An enemy right at the 78-pixel boundary -- attack should fire (or not) consistently
- Move in and out of range rapidly -- attacks should start and stop reliably each time

### MT-020: Enemy HP Decreases
**Feature:** Attacking an enemy reduces its HP until it dies
**Prerequisites:** Game is running, player can attack (MT-018 passes)
**Steps:**
1. Move the player close to a single enemy (preferably tier 1 green, 30 HP)
2. Let auto-attacks hit the enemy repeatedly
3. Count the number of attacks before the enemy disappears
4. At level 1, player damage is 13 per hit: 30 HP / 13 = 2.3 -> **3 hits to kill a tier 1 enemy**
**Expected Result:**
- Each attack visibly reduces the enemy's HP (even though HP is not displayed, the enemy eventually dies)
- Tier 1 (green, 30 HP) dies after **3 hits** at level 1
- Tier 2 (yellow, 42 HP) dies after **4 hits** at level 1
- Tier 3 (red, 54 HP) dies after **5 hits** at level 1
- When HP reaches 0 or below, the enemy **disappears** (removed from the scene)
**Edge Cases:**
- Verify the enemy is actually removed, not just invisible (no phantom collisions)
- If the player levels up mid-fight (damage increases), the enemy should die faster

### MT-021: Slash Visual
**Feature:** Gold slash effect appears when attacking
**Prerequisites:** Game is running, player is attacking an enemy (MT-018 passes)
**Steps:**
1. Move close to an enemy to trigger auto-attack
2. Observe the slash effect carefully
3. Note the color, position, rotation, and animation
**Expected Result:**
- A **gold-colored** (`#f5c86b`) thin rectangle appears at the **enemy's position**
- The slash is approximately 26 x 4 pixels
- The slash has a **random rotation** (each slash is angled differently, between -1.2 and +1.2 radians)
- The slash **fades out** (alpha goes from ~0.95 to 0) while moving **upward by 8 pixels** over approximately **120ms**
- The slash is then removed from the scene (does not accumulate)
**Edge Cases:**
- Multiple rapid attacks should each produce their own slash effect
- Slash should not persist after the enemy dies (if the killing blow produces a slash, it should still animate and fade out normally)
- Slash appears at the enemy's position, not the player's position

### MT-022: XP Gain
**Feature:** Killing an enemy awards XP based on its tier
**Prerequisites:** Game is running, HUD is visible (MT-028 passes), player can kill enemies (MT-020 passes)
**Steps:**
1. Note the current XP value on the HUD
2. Kill a **green** (tier 1) enemy -- note the XP change
3. Kill a **yellow** (tier 2) enemy -- note the XP change
4. Kill a **red** (tier 3) enemy -- note the XP change
**Expected Result:**
- Killing a **tier 1 (green)** enemy awards **14 XP** (10 + 1 * 4)
- Killing a **tier 2 (yellow)** enemy awards **18 XP** (10 + 2 * 4)
- Killing a **tier 3 (red)** enemy awards **22 XP** (10 + 3 * 4)
- The HUD XP value updates **immediately** after the kill
**Edge Cases:**
- XP should be exact: no rounding errors or off-by-one
- Killing multiple enemies in rapid succession should award XP for each one

### MT-023: Level Up
**Feature:** Player levels up when XP reaches the threshold
**Prerequisites:** Game is running, player can gain XP (MT-022 passes)
**Steps:**
1. Note the current level (should be 1) and XP (should be 0) on the HUD
2. Kill enemies until XP reaches 90 (the level 1 threshold: level * 90 = 1 * 90 = 90)
3. Observe the HUD when the threshold is reached
4. Note the new level, XP, and HP values
**Expected Result:**
- When XP reaches or exceeds **90**, the player levels up to **level 2**
- XP resets but **excess carries over** (e.g., if XP was 0 and a tier 3 kill awards 22, and another awards 22, etc., the XP past 90 carries to the next level)
- Level displays as **2** on the HUD
- Max HP increases to **116** (100 + 2 * 8)
- Player is **healed by 18 HP** on level up, capped at new max HP
- If player was at full HP (100) before level up: HP becomes **min(116, 100 + 18) = 116**
**Edge Cases:**
- If the player has taken damage (e.g., HP = 50) and levels up: HP = min(116, 50 + 18) = 68
- Level 2 threshold is 180 XP (2 * 90) -- verify the next level up requires 180 XP, not 90
- Multiple level-ups from a single large XP award: if the carried-over XP exceeds the next threshold, another level-up should occur

### MT-024: Player Takes Damage
**Feature:** Enemies deal contact damage to the player
**Prerequisites:** Game is running, enemies are visible (MT-013 passes), HUD is visible (MT-028 passes)
**Steps:**
1. Note the current HP on the HUD (should be 100)
2. Allow a **green** (tier 1) enemy to touch the player
3. Observe the HP change on the HUD
4. Note the amount of damage taken
5. Repeat with yellow (tier 2) and red (tier 3) enemies
**Expected Result:**
- **Tier 1 (green)** deals **4 damage** per hit (3 + 1)
- **Tier 2 (yellow)** deals **5 damage** per hit (3 + 2)
- **Tier 3 (red)** deals **6 damage** per hit (3 + 3)
- HP on the HUD decreases by the exact damage amount
- HP update is **immediate** (no delay after contact)
**Edge Cases:**
- Damage should not be doubled or missed on the first frame of contact
- Multiple enemies touching the player simultaneously should each deal their own damage (independently)

### MT-025: Camera Shake
**Feature:** Screen shakes when the player takes damage
**Prerequisites:** Game is running, player takes damage (MT-024 passes)
**Steps:**
1. Allow an enemy to touch the player
2. At the moment of damage, observe the screen/camera
3. Note the intensity and duration of the shake
**Expected Result:**
- The screen **briefly shakes** when damage is dealt
- Shake displacement is approximately **+/-3 pixels**
- Shake duration is approximately **0.045 seconds** (very brief, just a quick jolt)
- After the shake, the camera returns to its normal centered position
- The shake is visible but not disorienting
**Edge Cases:**
- Taking rapid successive hits (multiple enemies) should each trigger their own shake
- Camera shake should not accumulate (overlapping shakes should not result in extreme displacement)
- Camera shake should not permanently offset the camera

### MT-026: Hit Cooldown
**Feature:** Enemy damage has a 0.7-second cooldown per enemy
**Prerequisites:** Game is running, player takes damage (MT-024 passes)
**Steps:**
1. Allow a single enemy to remain in contact with the player
2. Observe the rate at which HP decreases
3. Time the interval between HP changes (should be approximately 0.7 seconds)
4. Verify the enemy does not deal damage every frame
**Expected Result:**
- A single enemy deals damage approximately every **0.7 seconds** while in contact
- Damage is **not** dealt every frame (which would drain HP instantly)
- The first hit occurs immediately on contact
- Subsequent hits occur every ~0.7 seconds as long as the enemy remains in contact
- If the player moves away and returns, the cooldown resets (first hit is immediate again)
**Edge Cases:**
- Two enemies touching the player simultaneously should each have **independent** cooldowns (both deal damage at their own 0.7s rate)
- Brief contact (touch and immediately move away) should deal exactly one hit

### MT-027: Enemy Respawn
**Feature:** Dead enemies respawn after 1.4 seconds
**Prerequisites:** Game is running, player can kill enemies (MT-020 passes)
**Steps:**
1. Count the current number of enemies
2. Kill one enemy
3. Start timing from the moment the enemy disappears
4. Watch for a new enemy to appear at a screen edge
5. Note the time elapsed
**Expected Result:**
- A new enemy appears approximately **1.4 seconds** after the killed enemy disappears
- The new enemy spawns at a **random edge** of the play area (not at the position where the old enemy died)
- The new enemy has a **random tier** (may be different from the killed enemy)
- The new enemy immediately begins chasing the player
**Edge Cases:**
- Kill multiple enemies rapidly -- each should trigger its own independent 1.4s respawn timer
- The respawn timer is separate from the 2.8s periodic spawn timer (both can add enemies)

---

## HUD

### MT-028: HUD Visible
**Feature:** HUD overlay is visible with correct content and styling
**Prerequisites:** Game is running (MT-001 passes)
**Steps:**
1. Look at the **top-left corner** of the screen
2. Identify the HUD panel
3. Read the panel contents
4. Verify the styling (colors, border, background)
**Expected Result:**
- A semi-transparent **dark panel** is visible in the top-left corner
- Panel has a **gold border** (subtle, `rgba(245, 200, 107, 0.3)`)
- Panel has **rounded corners** (10px radius)
- Panel is offset from the corner by approximately **12 pixels** (left and top)
- Panel contains three text elements:
  1. **Title:** "A DUNGEON IN THE MIDDLE OF NOWHERE" in gold/accent color (`#f5c86b`), font size ~13
  2. **Controls:** "Move: WASD / Arrow keys" and "Auto-attack: nearest enemy in range" in muted color (`#b6bfdb`), font size ~12
  3. **Stats:** "HP: 100 | XP: 0 | LVL: 1 | Floor: 1" in white/ink color (`#ecf0ff`), font size ~12
**Edge Cases:**
- HUD should not block clicks on the game world (mouse clicks pass through)
- HUD should remain visible and correctly positioned after camera movement
- HUD text should be readable against the dark background at all times

### MT-029: Stats Update
**Feature:** HUD stats update in real-time when values change
**Prerequisites:** Game is running, HUD is visible (MT-028 passes)
**Steps:**
1. Note the initial stats: "HP: 100 | XP: 0 | LVL: 1 | Floor: 1"
2. Let an enemy hit the player -- observe the HP value change
3. Kill an enemy -- observe the XP value change
4. Kill enough enemies to level up -- observe the LVL value change
**Expected Result:**
- **HP** updates immediately when damage is taken (e.g., 100 -> 96 after a tier 1 hit)
- **XP** updates immediately when an enemy is killed (e.g., 0 -> 14 after a tier 1 kill)
- **LVL** updates immediately when a level-up occurs (e.g., 1 -> 2)
- **Floor** displays correctly (should be 1 for the current implementation)
- All updates happen with **no visible delay** -- the signal-based system should update on the same frame
**Edge Cases:**
- Rapid changes (multiple hits or kills in quick succession) -- all should register and display correctly
- HP going to 0 should display "HP: 0" before the death screen appears
- After level-up, HP should show the healed value (not the pre-heal value)

---

## Death / Restart

### MT-030: Death Screen
**Feature:** Death screen appears when HP reaches 0
**Prerequisites:** Game is running, player can take damage (MT-024 passes)
**Steps:**
1. Allow enemies to attack the player repeatedly until HP reaches 0
2. Observe what happens when HP hits 0
3. Note the death screen appearance
**Expected Result:**
- When HP reaches **0**, a **dark overlay** covers the game screen
- The overlay is semi-transparent (black at approximately 75% opacity)
- **"You Died"** text appears prominently in the center of the screen
- Restart instructions are displayed: mention of R key and/or a restart button
- Text color is warm/gold (`#ffe1b0` or similar from the Phaser prototype)
- Text is centered on screen
- Enemies **stop moving** when the death screen appears
- Player character is no longer visible or is clearly "dead"
**Edge Cases:**
- HP should display as exactly 0 on the HUD (not negative)
- The death screen should appear immediately (no delay after HP reaches 0)
- No further damage should be dealt after death (enemies should stop dealing damage)

### MT-031: R Key Restart
**Feature:** Pressing R on the death screen restarts the game
**Prerequisites:** Death screen is showing (MT-030 passes)
**Steps:**
1. On the death screen, press the **R** key
2. Observe what happens
**Expected Result:**
- The game **restarts immediately** upon pressing R
- Death overlay disappears
- Game world is regenerated with fresh enemies
- Player is alive with full HP
- See MT-033 for full clean restart verification
**Edge Cases:**
- Pressing R before death should **not** restart the game (R only works on death screen)
- Pressing other keys on the death screen should do nothing
- Double-pressing R quickly should not cause issues (only one restart)

### MT-032: Button Restart
**Feature:** Clicking a Restart button on the death screen restarts the game
**Prerequisites:** Death screen is showing (MT-030 passes)
**Steps:**
1. On the death screen, look for a **Restart button** (or clickable text)
2. Click/tap the restart option
3. Observe what happens
**Expected Result:**
- The game **restarts immediately** upon clicking the restart button/area
- Same result as pressing R (MT-031)
- Death overlay disappears, game world regenerates
**Edge Cases:**
- The button/click area should be large enough to easily tap on touch devices
- If no explicit button exists, clicking anywhere on the screen should work (matching Phaser prototype's `pointerdown` handler)

### MT-033: Clean Restart
**Feature:** After restart, all game state is reset to initial values
**Prerequisites:** Game has been restarted (MT-031 or MT-032 passes)
**Steps:**
1. After restarting, immediately check the HUD stats
2. Count the number of enemies on screen
3. Look for any lingering visual effects (slash marks, death overlay remnants)
4. Verify the player's position and state
**Expected Result:**
- **HP = 100** (reset to full starting HP)
- **XP = 0** (reset to zero)
- **LVL = 1** (reset to level 1)
- **Floor = 1** (reset to floor 1)
- **10 enemies** are present (fresh initial spawn)
- **No lingering slash effects** from the previous game
- **No death overlay** remnants visible
- Player is positioned at the **center** of the play area
- Player is alive and can move normally
- Enemies are chasing the player normally
- Auto-attack works normally
**Edge Cases:**
- Play a full game (reach level 3+, take lots of damage) then restart -- all values should be pristine
- Restart multiple times in a row -- each restart should produce the same clean initial state
- Timer-based systems (periodic spawn, respawn) should be reset -- no phantom enemy spawns from the previous game's timers

## Implementation Notes

- These tests should be run in order within each category, but categories can be tested independently
- When a test has prerequisites, the prerequisite test must pass first
- Timing-based tests (MT-016, MT-026, MT-027) allow +/-0.5 second tolerance for human perception
- Color verification (MT-014) can be approximate -- the key is that three distinct colors are visible and distinguishable
- Speed verification (MT-004 through MT-009) is qualitative -- the player should "feel" like they move at a consistent speed

## Open Questions

- Should these manual tests be converted to a checklist format for tracking pass/fail?
- Should screenshots be captured for visual reference?
- Should timing-sensitive tests have automated equivalents?
- How should test results be recorded between testing sessions?

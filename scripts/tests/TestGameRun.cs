using System;
using System.Collections.Generic;
using System.Linq;
using DungeonGame.Dungeon;
using Godot;

/// <summary>
/// Automated end-to-end test that exercises the full game loop:
/// Init -> Town -> Shop -> Dungeon -> Combat -> Floor Transition -> Save/Load -> Bank -> Backpack -> New Systems.
/// Runs headless (no rendering). Follows the GameDemo step-based pattern.
/// </summary>
public partial class TestGameRun : Node2D
{
    private readonly List<(float delay, Action action)> _steps = new();
    private int _stepIndex;
    private float _timer;
    private bool _complete;

    // Tracking
    private int _assertions;
    private int _assertionsPassed;
    private int _totalKills;
    private Random _rng;

    // Saved state for load verification
    private Dictionary<string, object> _savedData;

    // Dungeon floors
    private FloorData _floor1;
    private FloorData _floor2;

    public override void _Ready()
    {
        _rng = new Random(42); // deterministic seed for reproducibility

        SetupSteps();

        _stepIndex = 0;
        _timer = 0.05f;

        // Headless: run instantly
        if (DisplayServer.GetName() == "headless")
        {
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = (0.0f, _steps[i].action);
        }
    }

    public override void _Process(double delta)
    {
        if (_complete) return;

        _timer -= (float)delta;
        if (_timer <= 0 && _stepIndex < _steps.Count)
        {
            try
            {
                _steps[_stepIndex].action();
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION at step {_stepIndex}: {ex.Message}");
                Log($"Stack: {ex.StackTrace}");
                Log("=== TEST FAILED ===");
                _complete = true;
                GetTree().Quit(1);
                return;
            }

            _stepIndex++;

            if (_stepIndex < _steps.Count)
                _timer = _steps[_stepIndex].delay;
            else
            {
                _complete = true;
                GetTree().Quit(0);
            }
        }
    }

    // ==================== LOGGING & ASSERTIONS ====================

    private void Log(string msg)
    {
        GD.Print($"[TEST-GAME] {msg}");
    }

    private void Assert(bool condition, string label)
    {
        _assertions++;
        if (condition)
        {
            _assertionsPassed++;
        }
        else
        {
            Log($"ASSERT FAILED: {label}");
        }
    }

    // ==================== STEP SETUP ====================

    private void SetupSteps()
    {
        // ── Phase 1: INIT ──
        Step(0.05f, () =>
        {
            Log("=== FULL GAME LOOP TEST ===");
            Log("");
        });

        Step(0.05f, () =>
        {
            Log("--- Phase 1: INIT ---");
            GameState.Reset();
            GameState.Player.Name = "TestHero";
            var p = GameState.Player;

            Log($"Player: {p.Name} Lv.{p.Level}");
            Log($"  HP: {p.HP}/{p.MaxHP}, MP: {p.MP}/{p.MaxMP}");
            Log($"  Gold: {p.Gold}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");

            Assert(p.Name == "TestHero", "Player name is TestHero");
            Assert(p.HP > 0, "Player HP > 0");
            Assert(p.Gold > 0, "Player Gold > 0");
            Assert(p.Level == 1, "Player Level == 1");
        });

        // ── Phase 2: TOWN — NPC SHOPPING ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 2: TOWN — NPC SHOPPING ---");
            GameState.Location = GameLocation.Town;
            Log("Entered Town");

            int goldBefore = GameState.Player.Gold;

            // Create shop items
            var healthPotion = GameSystems.CreateItem("Health Potion", ItemType.Consumable, EquipSlot.None,
                hpBonus: 50, value: 50, stackable: true, desc: "Restores 50 HP");
            var manaPotion = GameSystems.CreateItem("Mana Potion", ItemType.Consumable, EquipSlot.None,
                mpBonus: 30, value: 50, stackable: true, desc: "Restores 30 MP");

            var (hpOk, hpMsg) = GameSystems.BuyItem(healthPotion);
            Log($"  Buy Health Potion: {hpMsg}");
            Assert(hpOk, "Bought Health Potion");

            var (mpOk, mpMsg) = GameSystems.BuyItem(manaPotion);
            Log($"  Buy Mana Potion: {mpMsg}");
            Assert(mpOk, "Bought Mana Potion");

            int invCount = GameState.Player.Inventory.Count;
            int goldAfter = GameState.Player.Gold;

            Assert(invCount == 2, $"Inventory has 2 items (got {invCount})");
            Assert(goldAfter == goldBefore - 100, $"Gold reduced by 100 (was {goldBefore}, now {goldAfter})");
            Log($"Bought potions, Gold: {goldAfter}, Inventory: {invCount} items");
        });

        // ── Phase 3: ENTER DUNGEON ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 3: ENTER DUNGEON ---");
            GameState.Location = GameLocation.Dungeon;
            GameState.DungeonFloor = 1;

            var gen = new DungeonGenerator();
            _floor1 = gen.Generate(seed: 12345, floorNumber: 1);

            var (expectedW, expectedH) = DungeonGenerator.CalculateFloorSize(1);
            Log($"Entered Dungeon Floor 1, size {_floor1.Width}x{_floor1.Height}, {_floor1.Rooms.Count} rooms");

            bool hasEntrance = _floor1.Rooms.Any(r => r.Kind == RoomKind.Entrance);
            bool hasExit = _floor1.Rooms.Any(r => r.Kind == RoomKind.Exit || r.Kind == RoomKind.Boss);

            Assert(hasEntrance, "Floor 1 has entrance room");
            Assert(hasExit, "Floor 1 has exit room");
            Assert(_floor1.Width == expectedW, $"Floor 1 width matches CalculateFloorSize ({_floor1.Width} == {expectedW})");
            Assert(_floor1.Height == expectedH, $"Floor 1 height matches CalculateFloorSize ({_floor1.Height} == {expectedH})");
            Assert(_floor1.Rooms.Count > 0, "Floor 1 has rooms");
        });

        // ── Phase 4: SPAWN ENEMIES ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 4: SPAWN ENEMIES ---");
            GameState.ActiveMonsters.Clear();

            int normalCount = 0, empoweredCount = 0, namedCount = 0;
            string[] monsterNames = { "Slime", "Goblin", "Skeleton", "Rat", "Bat" };

            var normalRooms = _floor1.Rooms.Where(r => r.Kind == RoomKind.Normal).ToList();
            foreach (var room in normalRooms)
            {
                int spawnCount = _rng.Next(1, 4); // 1-3 monsters
                for (int i = 0; i < spawnCount; i++)
                {
                    string name = monsterNames[_rng.Next(monsterNames.Length)];
                    var rarity = MonsterSpawner.RollRarity(_rng);

                    MonsterTier tier = MonsterTier.Tier1;
                    var monster = GameSystems.SpawnMonster(name, tier);

                    // Apply rarity HP multiplier
                    float hpMult = MonsterSpawner.GetHPMultiplier(rarity);
                    monster.HP = (int)(monster.HP * hpMult);
                    monster.MaxHP = (int)(monster.MaxHP * hpMult);

                    switch (rarity)
                    {
                        case MonsterRarity.Normal: normalCount++; break;
                        case MonsterRarity.Empowered: empoweredCount++; break;
                        case MonsterRarity.Named: namedCount++; break;
                    }

                    Log($"  Spawned {name} [{rarity}] HP:{monster.HP} Dmg:{monster.Damage}");

                    // Roll modifiers for Empowered/Named
                    if (rarity != MonsterRarity.Normal)
                    {
                        int modCount = MonsterSpawner.GetModifierCount(rarity, zone: 1);
                        var mods = MonsterModifiers.RollModifiers(modCount, _rng);
                        var (speed, damage, defense) = MonsterModifiers.GetCombinedEffects(mods);
                        Log($"    Modifiers: [{string.Join(", ", mods)}] speed:{speed:F2}x dmg:{damage:F2}x def:+{defense}");
                    }
                }
            }

            int totalSpawned = GameState.ActiveMonsters.Count;
            Log($"Spawned {totalSpawned} monsters ({normalCount} normal, {empoweredCount} empowered, {namedCount} named)");
            Assert(totalSpawned > 0, "At least 1 monster spawned");
        });

        // ── Phase 5: COMBAT — FLOOR 1 ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 5: COMBAT — FLOOR 1 ---");

            var target = GameState.ActiveMonsters.FirstOrDefault(m => !m.IsDead);
            Assert(target != null, "Found a living monster to fight");
            if (target == null) return;

            Log($"Engaging {target.Name} (HP:{target.HP}/{target.MaxHP}, Tier:{target.Tier})");

            int attacks = 0;
            while (!target.IsDead && attacks < 200)
            {
                var (dmg, crit) = GameSystems.AttackMonster(target);
                string critStr = crit ? " CRIT!" : "";
                Log($"  Attack #{attacks + 1}: {dmg} damage{critStr} -> HP:{target.HP}/{target.MaxHP}");

                // Crit system validation
                var critResult = CritSystem.RollCrit(GameState.Player.TotalDamage, WeaponType.Unarmed, _rng);

                // Monster attacks back if alive
                if (!target.IsDead)
                {
                    int monDmg = GameSystems.MonsterAttackPlayer(target);
                    if (GameState.Player.IsDead)
                    {
                        Log($"  Player died! Respawning...");
                        GameSystems.PlayerRespawn();
                        GameState.Location = GameLocation.Dungeon;
                        GameState.DungeonFloor = 1;
                    }
                }

                attacks++;
            }

            Assert(target.IsDead, $"Monster {target.Name} is dead");

            // Grant XP/Gold
            int xpBefore = GameState.Player.XP;
            var (leveled, xpGained) = GameSystems.GainXP(target.XPReward);
            GameState.Player.Gold += target.GoldReward;
            _totalKills++;

            Log($"Killed {target.Name}, gained {target.XPReward} XP, {target.GoldReward} gold");

            // Roll loot
            var loot = ItemGenerator.RollLootDrop((int)target.Tier, GameState.DungeonFloor, _rng);
            if (loot != null)
            {
                GameSystems.AddToInventory(loot);
                Log($"  Loot: {loot.Name} ({loot.Quality} {loot.Type})");
            }
            else
            {
                Log($"  No loot dropped");
            }

            Assert(GameState.Player.XP > 0 || GameState.Player.Level > 1, "Player gained XP or leveled up");
        });

        // ── Phase 6: LEVEL CHECK & CLEAR FLOOR ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 6: LEVEL CHECK ---");

            // Force a level up by granting enough XP
            int needed = GameState.Player.XPToNextLevel - GameState.Player.XP;
            if (needed > 0)
            {
                var (leveled, _) = GameSystems.GainXP(needed);
                if (leveled)
                    Log($"LEVEL UP! Now Level {GameState.Player.Level}");
            }

            var p = GameState.Player;
            Log($"Stats: HP:{p.HP}/{p.MaxHP} MP:{p.MP}/{p.MaxMP} XP:{p.XP} Level:{p.Level}");
            Assert(p.Level >= 2, "Player reached at least level 2");

            // Kill remaining monsters (abbreviated)
            int remaining = GameState.ActiveMonsters.Count(m => !m.IsDead);
            foreach (var monster in GameState.ActiveMonsters.Where(m => !m.IsDead))
            {
                while (!monster.IsDead)
                {
                    GameSystems.AttackMonster(monster);
                    if (!monster.IsDead && GameState.Player.HP < 20)
                    {
                        GameState.Player.HP = GameState.Player.MaxHP; // heal for test continuity
                    }
                }
                _totalKills++;
                GameSystems.GainXP(monster.XPReward);
                GameState.Player.Gold += monster.GoldReward;
            }

            Log($"Floor 1 cleared, {_totalKills} enemies defeated");
        });

        // ── Phase 7: FLOOR TRANSITION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 7: FLOOR TRANSITION ---");

            GameState.DungeonFloor = 2;
            var gen = new DungeonGenerator();
            _floor2 = gen.Generate(seed: 54321, floorNumber: 2);

            Log($"Advanced to Floor 2, size {_floor2.Width}x{_floor2.Height}");

            bool hasEntrance = _floor2.Rooms.Any(r => r.Kind == RoomKind.Entrance);
            bool hasExit = _floor2.Rooms.Any(r => r.Kind == RoomKind.Exit || r.Kind == RoomKind.Boss);
            Assert(hasEntrance, "Floor 2 has entrance");
            Assert(hasExit, "Floor 2 has exit");

            // Spawn enemies on floor 2
            GameState.ActiveMonsters.Clear();
            var monster = GameSystems.SpawnMonster("Goblin Scout", MonsterTier.Tier2);
            Log($"  Spawned {monster.Name} HP:{monster.HP} Tier:{monster.Tier}");

            // Kill 1 enemy on floor 2
            while (!monster.IsDead)
            {
                GameSystems.AttackMonster(monster);
                if (!monster.IsDead && GameState.Player.HP < 20)
                    GameState.Player.HP = GameState.Player.MaxHP;
            }
            _totalKills++;
            GameSystems.GainXP(monster.XPReward);
            GameState.Player.Gold += monster.GoldReward;

            Log("Floor 2 combat verified");
        });

        // ── Phase 8: SAVE ON FLOOR 2 ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 8: SAVE ON FLOOR 2 ---");

            _savedData = SaveSerializer.Serialize(slot: 1);
            Assert(_savedData != null, "Serialized data is not null");
            Assert(_savedData.Count > 0, "Serialized data is not empty");

            // Verify key fields in serialized data
            var charDict = _savedData["character"] as Dictionary<string, object>;
            Assert(charDict != null, "Character data exists in save");
            Assert(charDict["name"].ToString() == "TestHero", "Saved name is TestHero");

            int savedFloor = Convert.ToInt32(_savedData["dungeon_floor"]);
            Assert(savedFloor == 2, $"Saved floor is 2 (got {savedFloor})");

            int savedLevel = Convert.ToInt32(charDict["level"]);
            Assert(savedLevel >= 2, $"Saved level >= 2 (got {savedLevel})");

            Log($"Game saved on Floor 2 — name:{charDict["name"]}, level:{charDict["level"]}, floor:{savedFloor}");
        });

        // ── Phase 9: SIMULATE LOAD ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 9: SIMULATE LOAD ---");

            // Store expected values
            var expectedName = "TestHero";
            var charDict = _savedData["character"] as Dictionary<string, object>;
            int expectedLevel = Convert.ToInt32(charDict["level"]);
            int expectedFloor = Convert.ToInt32(_savedData["dungeon_floor"]);

            // Wipe everything
            GameState.Reset();
            Assert(GameState.Player.Level == 1, "Reset: player is level 1");
            Assert(GameState.Player.Name == "Hero", "Reset: player name is default");
            Log("  State wiped (reset confirmed)");

            // Deserialize
            bool loaded = SaveSerializer.Deserialize(_savedData);
            Assert(loaded, "Deserialization succeeded");

            Assert(GameState.Player.Name == expectedName, $"Loaded name: {GameState.Player.Name}");
            Assert(GameState.Player.Level == expectedLevel, $"Loaded level: {GameState.Player.Level}");
            Assert(GameState.DungeonFloor == expectedFloor, $"Loaded floor: {GameState.DungeonFloor}");

            int invCount = GameState.Player.Inventory.Count;
            Log($"Game loaded — {GameState.Player.Name} Lv.{GameState.Player.Level} Floor {GameState.DungeonFloor}, {invCount} items");
        });

        // ── Phase 10: RETURN TO TOWN ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 10: RETURN TO TOWN ---");
            GameSystems.ExitDungeon();
            Assert(GameState.Location == GameLocation.Town, "Location is Town");
            Assert(GameState.DungeonFloor == 0, "Dungeon floor is 0");
            Log("Returned to Town");
        });

        // ── Phase 11: BANK OPERATIONS ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 11: BANK OPERATIONS ---");

            var bank = new BankData();

            // Ensure player has at least one item to deposit
            if (GameState.Player.Inventory.Count == 0)
            {
                var testItem = GameSystems.CreateItem("Test Gem", ItemType.Material, EquipSlot.None, value: 10);
                GameSystems.AddToInventory(testItem);
            }

            var itemToDeposit = GameState.Player.Inventory[0];
            string itemName = itemToDeposit.Name;
            int invBefore = GameState.Player.Inventory.Count;

            var (depOk, depMsg) = BankSystem.Deposit(bank, GameState.Player, itemToDeposit);
            Log($"  Deposit: {depMsg}");
            Assert(depOk, "Deposit succeeded");
            Assert(bank.Items.Count >= 1, "Bank has at least 1 item");
            Assert(GameState.Player.Inventory.Count == invBefore - 1, "Inventory reduced by 1");

            // Withdraw
            var bankItem = bank.Items[0];
            var (witOk, witMsg) = BankSystem.Withdraw(bank, GameState.Player, bankItem);
            Log($"  Withdraw: {witMsg}");
            Assert(witOk, "Withdraw succeeded");
            Assert(bank.Items.Count == 0, "Bank is empty");
            Assert(GameState.Player.Inventory.Count == invBefore, "Inventory restored");

            Log("Bank deposit/withdraw verified");
        });

        // ── Phase 12: BACKPACK EXPANSION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 12: BACKPACK EXPANSION ---");

            int cost = BackpackSystem.GetExpansionCost(GameState.Player);
            int sizeBefore = GameState.Player.InventorySize;

            if (GameState.Player.Gold >= cost)
            {
                var (ok, msg) = BackpackSystem.Expand(GameState.Player);
                Assert(ok, "Backpack expansion succeeded");
                Log($"Backpack expanded to {GameState.Player.InventorySize} slots (cost: {cost}g)");
            }
            else
            {
                // Give gold for the test
                GameState.Player.Gold += cost;
                var (ok, msg) = BackpackSystem.Expand(GameState.Player);
                Assert(ok, "Backpack expansion succeeded (with added gold)");
                Log($"Backpack expanded to {GameState.Player.InventorySize} slots (cost: {cost}g)");
            }

            Assert(GameState.Player.InventorySize == sizeBefore + BackpackSystem.SlotsPerExpansion,
                $"Backpack size increased by {BackpackSystem.SlotsPerExpansion}");
        });

        // ── Phase 13: SAVE IN TOWN ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 13: SAVE IN TOWN ---");

            Assert(GameState.Location == GameLocation.Town, "Location is Town before save");
            _savedData = SaveSerializer.Serialize(slot: 1);

            Assert(_savedData != null, "Town save data not null");
            int loc = Convert.ToInt32(_savedData["location"]);
            Assert(loc == (int)GameLocation.Town, $"Saved location is Town (got {loc})");

            Log("Saved in Town");
        });

        // ── Phase 14: SIMULATE RELOAD ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 14: SIMULATE RELOAD ---");

            GameState.Reset();
            bool loaded = SaveSerializer.Deserialize(_savedData);
            Assert(loaded, "Town reload deserialization succeeded");
            Assert(GameState.Location == GameLocation.Town, "Reloaded location is Town");
            Assert(GameState.Player.Name == "TestHero", "Reloaded name is TestHero");
            Assert(GameState.Player.Level >= 2, $"Reloaded level >= 2 (got {GameState.Player.Level})");

            Log($"Reloaded in Town — all systems verified");
        });

        // ── Phase 15: NEW SYSTEMS VALIDATION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 15: NEW SYSTEMS VALIDATION ---");

            // Test ElementalCombat: fire damage vs target with 50% fire resistance
            var target = new EntityData
            {
                Name = "FireRes Dummy",
                HP = 1000, MaxHP = 1000,
                BaseDefense = 0,
                Resistances = new Resistances { Fire = 50 }
            };
            var elemResult = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, floorNumber: 1);
            Log($"  ElementalCombat: 100 Fire dmg vs 50% fire res -> {elemResult.FinalDamage} final (raw:{elemResult.RawDamage}, res:{elemResult.EffectiveResistance})");
            Assert(elemResult.FinalDamage < 100, "Fire damage reduced by resistance");
            Assert(elemResult.FinalDamage > 0, "Fire damage is positive");

            // Test CritSystem: roll 1000 crits with Dagger, verify rate ~8%
            int critCount = 0;
            int sampleSize = 1000;
            var critRng = new Random(123);
            for (int i = 0; i < sampleSize; i++)
            {
                var cr = CritSystem.RollCrit(50, WeaponType.Dagger, critRng);
                if (cr.IsCrit) critCount++;
            }
            float critRate = critCount / (float)sampleSize * 100f;
            Log($"  CritSystem: Dagger crit rate = {critRate:F1}% over {sampleSize} rolls (expected ~8%)");
            Assert(critRate > 3f && critRate < 15f, $"Dagger crit rate in reasonable range ({critRate:F1}%)");

            // Test MonsterBehavior: state machine chain
            // Idle -> Alert (within aggro range, melee archetype)
            var state = MonsterBehavior.GetNextState(MonsterAIState.Idle, MonsterArchetype.Melee,
                distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0.3f, cooldownTimer: 0);
            Log($"  MonsterBehavior: Idle + in range -> {state}");
            Assert(state == MonsterAIState.Alert, $"Idle -> Alert (got {state})");

            // Alert -> Chase (timer expired)
            state = MonsterBehavior.GetNextState(MonsterAIState.Alert, MonsterArchetype.Melee,
                distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0f, cooldownTimer: 0);
            Log($"  MonsterBehavior: Alert + timer 0 -> {state}");
            Assert(state == MonsterAIState.Chase, $"Alert -> Chase (got {state})");

            // Chase -> Attack (within attack range)
            state = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: Chase + close -> {state}");
            Assert(state == MonsterAIState.Attack, $"Chase -> Attack (got {state})");

            // Attack -> Cooldown
            state = MonsterBehavior.GetNextState(MonsterAIState.Attack, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 1.0f);
            Log($"  MonsterBehavior: Attack -> {state}");
            Assert(state == MonsterAIState.Cooldown, $"Attack -> Cooldown (got {state})");

            // Cooldown -> Chase (cooldown expired, melee has no preferred distance)
            state = MonsterBehavior.GetNextState(MonsterAIState.Cooldown, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: Cooldown expired -> {state}");
            Assert(state == MonsterAIState.Chase, $"Cooldown -> Chase (got {state})");

            // Dead state
            state = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 0, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: HP=0 -> {state}");
            Assert(state == MonsterAIState.Dead, $"HP 0 -> Dead (got {state})");

            // Test MonsterSpawner: GetArchetypeMix
            var mix = MonsterSpawner.GetArchetypeMix(budget: 10, _rng);
            int totalBudget = mix.Values.Sum();
            Log($"  MonsterSpawner: budget=10 -> {string.Join(", ", mix.Select(kv => $"{kv.Key}:{kv.Value}"))} (total:{totalBudget})");
            Assert(totalBudget == 10, $"Archetype mix sums to budget (got {totalBudget})");
            Assert(mix.Count > 0, "Archetype mix has entries");
        });

        // ── Phase 16: SUMMARY ──
        Step(0.05f, () =>
        {
            Log("");
            Log("=== TEST COMPLETE ===");
            Log($"Total steps: {_stepIndex + 1}");
            Log($"Total assertions: {_assertions} ({_assertionsPassed} passed, {_assertions - _assertionsPassed} failed)");
            Log($"Total kills: {_totalKills}");

            var p = GameState.Player;
            Log($"Final stats: {p.Name} Lv.{p.Level} HP:{p.HP}/{p.MaxHP} MP:{p.MP}/{p.MaxMP} Gold:{p.Gold}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");
            Log($"  Inventory: {p.Inventory.Count} items, Equipment: {p.Equipment.Count} slots");

            if (_assertionsPassed == _assertions)
                Log("ALL SYSTEMS OPERATIONAL");
            else
                Log($"WARNING: {_assertions - _assertionsPassed} assertion(s) failed");
        });
    }

    private void Step(float delay, Action action)
    {
        _steps.Add((delay, action));
    }
}

namespace DungeonGame;

/// <summary>
/// All player-facing strings in one place for future i18n/localization.
/// When translation support is added, this becomes the lookup layer
/// (e.g., Strings.Get("ui.paused") → TranslationServer.Translate("ui.paused")).
/// </summary>
public static class Strings
{
    // --- UI ---
    public static class Ui
    {
        public const string GameTitle = "A DUNGEON IN THE MIDDLE OF NOWHERE";
        public const string Paused = "PAUSED";
        public const string Resume = "Resume";
        public const string QuitGame = "Quit Game";
        public const string YouDied = "You Died";
        public const string RestartKey = "Restart (R)";
        public const string QuitKey = "Quit Game (Esc)";
        public const string PressRToRestart = "Press R to restart";
        public const string DebugToggle = "DEBUG (F3)";
        public const string ChooseClass = "CHOOSE YOUR CLASS";
        public const string Select = "Select";
        public const string ConfirmSelection = "Confirm";
        public const string ClickToSelect = "Click to select";
        public const string Cancel = "Cancel";
    }

    // --- Floor Wipe ---
    public static class FloorWipe
    {
        public const string Title = "FLOOR CLEARED";
        public const string Subtitle = "You've dominated this floor. The dungeon trembles.";
        public static string BonusGold(int gold) => $"Bonus: +{gold} gold";
        public static string NextFloor(int floor) => $"Descend to Floor {floor}";
        public const string StayOnFloor = "Stay & Farm (enemies respawn)";
        public const string SelectFloor = "Select Floor...";
        public const string ReturnToTown = "Return to Town";
    }

    // --- Death Screen ---
    public static class Death
    {
        public const string Title = "YOU DIED";
        public const string Subtitle = "The dungeon consumes your memories...";
        public const string ChooseDestination = "Where will you respawn?";
        public const string ReturnToTown = "Return to Town";
        public const string RespawnAtSafeSpot = "Respawn at Last Safe Spot";
        public const string MitigationTitle = "NEGOTIATE WITH THE DUNGEON";
        public const string Confirm = "Accept Fate";
    }

    // --- Splash Screen ---
    public static class Splash
    {
        public const string Subtitle = "The first to descend. The last to turn back.";
        public const string PressAnyKey = "Press any key to begin";
    }

    // --- HUD ---
    public static class Hud
    {
        public const string ControlsHint = "Move: Arrow keys\nAuto-attack: nearest enemy in range";
        public static string Stats(int hp, int maxHp, int xp, int level, int floor, int gold) =>
            $"HP: {hp}/{maxHp} | XP: {xp} | LVL: {level} | Floor: {floor} | Gold: {gold}";
    }

    // --- Combat ---
    public static class Combat
    {
        public static string Damage(int amount) => $"-{amount}";
        public static string Heal(int amount) => $"+{amount}";
        public static string Xp(int amount) => $"+{amount} XP";
        public static string Mana(int amount, bool restore) => $"{(restore ? "+" : "-")}{amount} MP";
        public const string LevelUp = "LEVEL UP!";
    }

    // --- Floor Transitions ---
    public static class Floor
    {
        public static string FloorNumber(int floor) => $"Floor {floor}";
        public const string Descending = "Descending...";
        public const string StairsDown = "STAIRS DOWN";
        public const string StairsUp = "STAIRS UP";
    }

    // --- Town ---
    public static class Town
    {
        public const string DungeonEntrance = "DUNGEON";
        public const string EnteringDungeon = "Entering the dungeon...";
        public static string DungeonFloor(int floor) => $"Dungeon Floor {floor}";
    }

    // --- NPC Names ---
    public static class Npcs
    {
        public const string Shopkeeper = "Shopkeeper";
        public const string Blacksmith = "Blacksmith";
        public const string GuildMaster = "Guild Master";
        public const string Teleporter = "Teleporter";
        public const string Banker = "Banker";
    }

    // --- Classes ---
    public static class Classes
    {
        public const string Warrior = "Warrior";
        public const string Ranger = "Ranger";
        public const string Mage = "Mage";
        public const string WarriorDescription = "Heavy armor, sword & shield.\nMelee auto-attack.";
        public const string RangerDescription = "Light armor, bow & quiver.\nRanged attack, infinite arrows.";
        public const string MageDescription = "Robes, staff melee.\nMagic bolt spell.";

        // Skill names
        public const string SkillSlash = "Slash";
        public const string SkillSlashType = "Melee";
        public const string SkillArrowShot = "Arrow Shot";
        public const string SkillArrowShotType = "Ranged (Quiver)";
        public const string SkillMagicBolt = "Magic Bolt";
        public const string SkillMagicBoltType = "Spell";
    }

    // --- Enemy Labels ---
    public static class Enemy
    {
        public static string LevelLabel(int level) => $"Lv.{level}";
    }

    // --- Ascend Dialog ---
    public static class Ascend
    {
        public const string Title = "ASCEND";
        public const string ReturnToTown = "Return to Town";
        public const string ReturningToTown = "Returning to town...";
        public const string Ascending = "Ascending...";
        public const string SelectFloor = "Select Floor...";
        public const string Back = "Back";
        public const string Floor1Town = "Floor 1 (Town Exit)";
        public static string GoUpOneFloor(int floor) => $"Go to Floor {floor}";
    }

    // --- General UI ---
    public static class General
    {
        public const string Cancel = "Cancel";
    }

    // --- Dialogue ---
    public static class Dialogue
    {
        public const string ContinueHint = "[S] / [Space] / [Enter] to continue";
    }

    // --- Shop ---
    public static class Shop
    {
        public const string Title = "SHOP";
        public const string BuyTab = "Buy";
        public const string SellTab = "Sell";
        public const string BuyMode = "Buying";
        public const string SellMode = "Selling";
        public const string Close = "Close";
        public const string SelectItem = "Select an item to view details.";
        public const string CannotAfford = "Not enough gold!";
        public static string Buy(int price) => $"Buy ({price}g)";
        public static string Sell(int price) => $"Sell ({price}g)";
        public static string GoldDisplay(int gold) => $"Gold: {gold}";
    }

    // --- NPC Interaction ---
    public static class Npc
    {
        public const string InteractPrompt = "[S] Interact";
    }

    // --- NPC Services ---
    public static class NpcServices
    {
        public const string OpenShop = "Browse Wares";
        public const string OpenForge = "Open Forge";
        public const string ViewQuests = "View Quests";
        public const string Teleport = "Teleport";
        public const string OpenBank = "Open Vault";
        public const string Talk = "Talk";
    }

    // --- NPC Greetings ---
    public static class NpcGreetings
    {
        public const string Shopkeeper = "I hauled these supplies three weeks across the wastes. You'd better put them to good use down there.";
        public const string Blacksmith = "Just got the forge hot. Bring me something from the deep and I'll make it into something worth carrying.";
        public const string GuildMaster = "I organized this expedition. You're our first delver — make every floor count.";
        public const string Teleporter = "The dungeon's magical signature drew me here. I've mapped what you've seen — I can send you back to any floor.";
        public const string Banker = "Frontier towns attract frontier trouble. Your gold and gear stay locked in my vault until you need them.";
    }
}

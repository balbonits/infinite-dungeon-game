using System.Text.Json;

namespace DungeonGame;

/// <summary>
/// All game settings, organized by category. Persisted to user://settings.json.
/// </summary>
public static class GameSettings
{
    // --- Gameplay ---
    public static bool ShowCombatNumbers { get; set; } = true;
    public static bool ShowXpNumbers { get; set; } = true;
    public static bool ShowEnemyLevels { get; set; } = true;
    public static bool AutoLoot { get; set; } = true;
    public static bool ConfirmFloorDescent { get; set; } = true;
    public static bool ShowToastNotifications { get; set; } = true;

    // --- Display ---
    public static bool CameraShakeOnDamage { get; set; } = false; // off by default per user preference
    public static bool ShowMinimap { get; set; } = true;
    public static bool ScreenFlash { get; set; } = true;
    public static bool ShowSkillBar { get; set; } = true;
    public static bool ShowHudOrbs { get; set; } = true;
    public static bool ShowStairsCompass { get; set; } = true;
    public static int UiScale { get; set; } = 100; // percentage: 75, 100, 125, 150

    // --- Audio ---
    public static int MasterVolume { get; set; } = 80;  // 0-100
    public static int MusicVolume { get; set; } = 70;
    public static int SfxVolume { get; set; } = 100;
    public static int AmbientVolume { get; set; } = 60;
    public static bool MuteOnFocusLoss { get; set; } = true;

    // --- Controls ---
    public static bool ShowControlHints { get; set; } = true;
    public static int ControllerScheme { get; set; } = 0; // 0=keyboard, 1=gamepad

    // --- Serialization ---

    private const string SavePath = "user://settings.json";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static void Save()
    {
        var data = new SettingsData
        {
            ShowCombatNumbers = ShowCombatNumbers,
            ShowXpNumbers = ShowXpNumbers,
            ShowEnemyLevels = ShowEnemyLevels,
            AutoLoot = AutoLoot,
            ConfirmFloorDescent = ConfirmFloorDescent,
            ShowToastNotifications = ShowToastNotifications,
            CameraShakeOnDamage = CameraShakeOnDamage,
            ShowMinimap = ShowMinimap,
            ScreenFlash = ScreenFlash,
            ShowSkillBar = ShowSkillBar,
            ShowHudOrbs = ShowHudOrbs,
            ShowStairsCompass = ShowStairsCompass,
            UiScale = UiScale,
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            AmbientVolume = AmbientVolume,
            MuteOnFocusLoss = MuteOnFocusLoss,
            ShowControlHints = ShowControlHints,
            ControllerScheme = ControllerScheme,
        };
        string json = JsonSerializer.Serialize(data, JsonOpts);
        using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }

    public static void Load()
    {
        if (!Godot.FileAccess.FileExists(SavePath)) return;
        using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
        if (file == null) return;
        try
        {
            var data = JsonSerializer.Deserialize<SettingsData>(file.GetAsText(), JsonOpts);
            if (data == null) return;
            ShowCombatNumbers = data.ShowCombatNumbers;
            ShowXpNumbers = data.ShowXpNumbers;
            ShowEnemyLevels = data.ShowEnemyLevels;
            AutoLoot = data.AutoLoot;
            ConfirmFloorDescent = data.ConfirmFloorDescent;
            ShowToastNotifications = data.ShowToastNotifications;
            CameraShakeOnDamage = data.CameraShakeOnDamage;
            ShowMinimap = data.ShowMinimap;
            ScreenFlash = data.ScreenFlash;
            ShowSkillBar = data.ShowSkillBar;
            ShowHudOrbs = data.ShowHudOrbs;
            ShowStairsCompass = data.ShowStairsCompass;
            UiScale = data.UiScale;
            MasterVolume = data.MasterVolume;
            MusicVolume = data.MusicVolume;
            SfxVolume = data.SfxVolume;
            AmbientVolume = data.AmbientVolume;
            MuteOnFocusLoss = data.MuteOnFocusLoss;
            ShowControlHints = data.ShowControlHints;
            ControllerScheme = data.ControllerScheme;
        }
        catch { /* corrupted settings — use defaults */ }
    }

    private record SettingsData
    {
        public bool ShowCombatNumbers { get; init; } = true;
        public bool ShowXpNumbers { get; init; } = true;
        public bool ShowEnemyLevels { get; init; } = true;
        public bool AutoLoot { get; init; } = true;
        public bool ConfirmFloorDescent { get; init; } = true;
        public bool ShowToastNotifications { get; init; } = true;
        public bool CameraShakeOnDamage { get; init; }
        public bool ShowMinimap { get; init; } = true;
        public bool ScreenFlash { get; init; } = true;
        public bool ShowSkillBar { get; init; } = true;
        public bool ShowHudOrbs { get; init; } = true;
        public bool ShowStairsCompass { get; init; } = true;
        public int UiScale { get; init; } = 100;
        public int MasterVolume { get; init; } = 80;
        public int MusicVolume { get; init; } = 70;
        public int SfxVolume { get; init; } = 100;
        public int AmbientVolume { get; init; } = 60;
        public bool MuteOnFocusLoss { get; init; } = true;
        public bool ShowControlHints { get; init; } = true;
        public int ControllerScheme { get; init; }
    }
}

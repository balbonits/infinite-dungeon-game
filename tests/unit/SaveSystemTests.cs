using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Save-system unit tests (TEST-01 + TEST-02). Covers the pure-logic serialization
/// round-trip and the <see cref="ISaveStorage"/> abstraction. The runtime Godot
/// SaveManager is intentionally NOT tested here — it's a thin wrapper over these
/// pieces; its orchestration is covered by GoDotTest keyboard-nav suites.
/// </summary>
public class SaveSystemTests
{
    // ── FakeSaveStorage (TEST-01) ────────────────────────────────────────

    [Fact]
    public void FakeStorage_StartsEmpty()
    {
        var storage = new FakeSaveStorage();
        storage.Exists("any").Should().BeFalse();
        storage.Read("any").Should().BeNull();
        storage.Count.Should().Be(0);
    }

    [Fact]
    public void FakeStorage_WriteReadRoundTrip()
    {
        var storage = new FakeSaveStorage();
        storage.Write("slot_0", "{\"hello\":1}");
        storage.Exists("slot_0").Should().BeTrue();
        storage.Read("slot_0").Should().Be("{\"hello\":1}");
        storage.Count.Should().Be(1);
    }

    [Fact]
    public void FakeStorage_Write_Overwrites()
    {
        var storage = new FakeSaveStorage();
        storage.Write("k", "first");
        storage.Write("k", "second");
        storage.Read("k").Should().Be("second");
        storage.Count.Should().Be(1);
    }

    [Fact]
    public void FakeStorage_Delete_RemovesKey()
    {
        var storage = new FakeSaveStorage();
        storage.Write("k", "v");
        storage.Delete("k");
        storage.Exists("k").Should().BeFalse();
        storage.Count.Should().Be(0);
    }

    [Fact]
    public void FakeStorage_Delete_MissingKey_NoOp()
    {
        var storage = new FakeSaveStorage();
        storage.Delete("nope"); // Must not throw.
        storage.Count.Should().Be(0);
    }

    [Fact]
    public void FakeStorage_MultipleKeys_Independent()
    {
        var storage = new FakeSaveStorage();
        storage.Write("a", "1");
        storage.Write("b", "2");
        storage.Write("c", "3");
        storage.Count.Should().Be(3);
        storage.Delete("b");
        storage.Exists("a").Should().BeTrue();
        storage.Exists("b").Should().BeFalse();
        storage.Exists("c").Should().BeTrue();
    }

    // ── SaveData JSON round-trip (TEST-02) ───────────────────────────────

    [Fact]
    public void SaveData_Empty_RoundTripsViaSerialize()
    {
        var original = new SaveData();
        var json = SaveDataJson.Serialize(original);
        var restored = SaveDataJson.Deserialize(json);
        restored.Should().NotBeNull();
        restored!.Level.Should().Be(1);
        restored.Hp.Should().Be(100);
    }

    [Fact]
    public void SaveData_Populated_RoundTripsAllFields()
    {
        var original = new SaveData
        {
            SaveDate = "2025-01-01 12:00:00",
            SelectedClass = PlayerClass.Ranger,
            Level = 42,
            Hp = 120,
            MaxHp = 150,
            Mana = 30,
            MaxMana = 60,
            Xp = 5000,
            FloorNumber = 25,
            DeepestFloor = 30,
            Str = 10,
            Dex = 20,
            Sta = 15,
            Int = 5,
            FreePoints = 3,
            Gold = 9999,
            SkillPoints = 7,
            AbilityPoints = 12,
        };

        var json = SaveDataJson.Serialize(original);
        var restored = SaveDataJson.Deserialize(json);

        restored.Should().NotBeNull();
        restored!.SelectedClass.Should().Be(PlayerClass.Ranger);
        restored.Level.Should().Be(42);
        restored.Hp.Should().Be(120);
        restored.MaxHp.Should().Be(150);
        restored.Mana.Should().Be(30);
        restored.MaxMana.Should().Be(60);
        restored.Xp.Should().Be(5000);
        restored.FloorNumber.Should().Be(25);
        restored.DeepestFloor.Should().Be(30);
        restored.Str.Should().Be(10);
        restored.Dex.Should().Be(20);
        restored.Sta.Should().Be(15);
        restored.Int.Should().Be(5);
        restored.FreePoints.Should().Be(3);
        restored.Gold.Should().Be(9999);
        restored.SkillPoints.Should().Be(7);
        restored.AbilityPoints.Should().Be(12);
    }

    [Fact]
    public void SaveData_Equipment_RoundTripsAllSlots()
    {
        var original = new SaveData
        {
            EquipmentData = new SavedEquipment
            {
                Head = "head_warrior_helmet_t2",
                Body = "body_warrior_armor_t2",
                MainHand = "mainhand_warrior_sword_t2",
                OffHand = "offhand_warrior_shield_t2",
                Ammo = null,
                Neck = "neck_t2_offense",
                Rings = new string?[]
                {
                    "ring_t1_str", null, "ring_t2_crit", null, null,
                    null, null, "ring_t3_dex", null, "ring_t5_haste"
                }
            }
        };

        var json = SaveDataJson.Serialize(original);
        var restored = SaveDataJson.Deserialize(json);

        restored.Should().NotBeNull();
        restored!.EquipmentData.Should().NotBeNull();
        restored.EquipmentData!.Head.Should().Be("head_warrior_helmet_t2");
        restored.EquipmentData.Body.Should().Be("body_warrior_armor_t2");
        restored.EquipmentData.MainHand.Should().Be("mainhand_warrior_sword_t2");
        restored.EquipmentData.OffHand.Should().Be("offhand_warrior_shield_t2");
        restored.EquipmentData.Ammo.Should().BeNull();
        restored.EquipmentData.Neck.Should().Be("neck_t2_offense");
        restored.EquipmentData.Rings.Should().HaveCount(10);
        restored.EquipmentData.Rings[0].Should().Be("ring_t1_str");
        restored.EquipmentData.Rings[2].Should().Be("ring_t2_crit");
        restored.EquipmentData.Rings[7].Should().Be("ring_t3_dex");
        restored.EquipmentData.Rings[9].Should().Be("ring_t5_haste");
    }

    [Fact]
    public void SaveData_Items_RoundTripsItemStacks()
    {
        var original = new SaveData
        {
            Items = new[]
            {
                new SavedItemStack { ItemId = "consumable_hp_small", Count = 5, Locked = false },
                new SavedItemStack { ItemId = "consumable_sacrificial_idol", Count = 1, Locked = true },
                new SavedItemStack { ItemId = "material_ore_t3", Count = 1_000_000_000L },
            }
        };

        var json = SaveDataJson.Serialize(original);
        var restored = SaveDataJson.Deserialize(json);

        restored.Should().NotBeNull();
        restored!.Items.Should().HaveCount(3);
        restored.Items[0].ItemId.Should().Be("consumable_hp_small");
        restored.Items[0].Count.Should().Be(5);
        restored.Items[1].Locked.Should().BeTrue();
        restored.Items[2].Count.Should().Be(1_000_000_000L);
    }

    [Fact]
    public void SaveData_CorruptJson_ReturnsNull()
    {
        SaveDataJson.Deserialize("{not valid json").Should().BeNull();
        SaveDataJson.Deserialize("").Should().BeNull();
    }

    // ── Storage + serialize interplay (integration of TEST-01 + TEST-02) ──

    [Fact]
    public void FakeStorage_PersistsSerializedSaveData()
    {
        var storage = new FakeSaveStorage();
        var original = new SaveData { Level = 7, Hp = 42, SelectedClass = PlayerClass.Mage };
        string slotKey = "user://saves/save_0.json";

        // Write
        storage.Write(slotKey, SaveDataJson.Serialize(original));

        // Read → deserialize
        var json = storage.Read(slotKey);
        json.Should().NotBeNull();
        var restored = SaveDataJson.Deserialize(json!);

        restored!.Level.Should().Be(7);
        restored.Hp.Should().Be(42);
        restored.SelectedClass.Should().Be(PlayerClass.Mage);
    }

    [Fact]
    public void FakeStorage_ThreeSlots_AreIndependent()
    {
        var storage = new FakeSaveStorage();
        var slots = new Dictionary<int, SaveData>
        {
            [0] = new() { SelectedClass = PlayerClass.Warrior, Level = 1 },
            [1] = new() { SelectedClass = PlayerClass.Ranger, Level = 10 },
            [2] = new() { SelectedClass = PlayerClass.Mage, Level = 25 },
        };

        foreach (var (i, data) in slots)
            storage.Write($"user://saves/save_{i}.json", SaveDataJson.Serialize(data));

        storage.Count.Should().Be(3);
        foreach (var (i, data) in slots)
        {
            var json = storage.Read($"user://saves/save_{i}.json");
            var restored = SaveDataJson.Deserialize(json!);
            restored!.SelectedClass.Should().Be(data.SelectedClass);
            restored.Level.Should().Be(data.Level);
        }

        // Delete slot 1 — others survive.
        storage.Delete("user://saves/save_1.json");
        storage.Exists("user://saves/save_0.json").Should().BeTrue();
        storage.Exists("user://saves/save_1.json").Should().BeFalse();
        storage.Exists("user://saves/save_2.json").Should().BeTrue();
    }
}

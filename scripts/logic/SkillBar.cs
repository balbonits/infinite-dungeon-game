using System;

namespace DungeonGame;

/// <summary>
/// Diablo-style skill hotbar. 4 slots mapped to keys 1-4.
/// Players assign unlocked specific skills to slots and trigger them in combat.
/// Pure logic — no Godot dependency.
/// </summary>
public class SkillBar
{
    public const int SlotCount = 4;
    private readonly string?[] _slots = new string?[SlotCount];
    private readonly float[] _cooldowns = new float[SlotCount];

    /// <summary>Get the skill ID assigned to a slot (0-3). Null if empty.</summary>
    public string? GetSlot(int index) => index >= 0 && index < SlotCount ? _slots[index] : null;

    /// <summary>Assign a skill to a slot. Pass null to clear.</summary>
    public void SetSlot(int index, string? skillId)
    {
        if (index >= 0 && index < SlotCount)
            _slots[index] = skillId;
    }

    /// <summary>Get remaining cooldown for a slot (seconds).</summary>
    public float GetCooldown(int index) => index >= 0 && index < SlotCount ? _cooldowns[index] : 0;

    /// <summary>Check if a slot is ready to use.</summary>
    public bool IsReady(int index) => index >= 0 && index < SlotCount &&
        _slots[index] != null && _cooldowns[index] <= 0;

    /// <summary>Trigger a slot. Returns the skill ID if successful, null if on cooldown or empty.</summary>
    public string? TryActivate(int index, float cooldownDuration)
    {
        if (!IsReady(index)) return null;
        _cooldowns[index] = cooldownDuration;
        return _slots[index];
    }

    /// <summary>Tick cooldowns. Call every frame with delta.</summary>
    public void Update(float delta)
    {
        for (int i = 0; i < SlotCount; i++)
            if (_cooldowns[i] > 0)
                _cooldowns[i] = Math.Max(0, _cooldowns[i] - delta);
    }

    /// <summary>Get all slot assignments for serialization.</summary>
    public string?[] ExportSlots() => (string?[])_slots.Clone();

    /// <summary>Restore slot assignments from save data.</summary>
    public void ImportSlots(string?[]? slots)
    {
        Array.Clear(_slots, 0, SlotCount);
        Array.Clear(_cooldowns, 0, SlotCount);
        if (slots == null) return;
        for (int i = 0; i < Math.Min(slots.Length, SlotCount); i++)
            _slots[i] = slots[i];
    }

    public void Reset()
    {
        Array.Clear(_slots, 0, SlotCount);
        Array.Clear(_cooldowns, 0, SlotCount);
    }
}

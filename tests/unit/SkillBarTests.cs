using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class SkillBarTests
{
    // -- Defaults --

    [Fact]
    public void NewSkillBar_AllSlotsEmpty()
    {
        var bar = new SkillBar();
        for (int i = 0; i < SkillBar.SlotCount; i++)
            bar.GetSlot(i).Should().BeNull();
    }

    [Fact]
    public void SlotCount_Is4()
    {
        SkillBar.SlotCount.Should().Be(4);
    }

    // -- SetSlot / GetSlot --

    [Fact]
    public void SetSlot_GetSlot_RoundTrips()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.GetSlot(0).Should().Be("fireball");
    }

    [Fact]
    public void SetSlot_Null_ClearsSlot()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.SetSlot(0, null);
        bar.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void SetSlot_OutOfBounds_Ignored()
    {
        var bar = new SkillBar();
        bar.SetSlot(-1, "test");
        bar.SetSlot(99, "test");
        // no exception thrown, slots unchanged
        bar.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void GetSlot_OutOfBounds_ReturnsNull()
    {
        var bar = new SkillBar();
        bar.GetSlot(-1).Should().BeNull();
        bar.GetSlot(99).Should().BeNull();
    }

    // -- IsReady --

    [Fact]
    public void IsReady_EmptySlot_ReturnsFalse()
    {
        new SkillBar().IsReady(0).Should().BeFalse();
    }

    [Fact]
    public void IsReady_AssignedNoCooldown_ReturnsTrue()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.IsReady(0).Should().BeTrue();
    }

    [Fact]
    public void IsReady_OnCooldown_ReturnsFalse()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 2.0f);
        bar.IsReady(0).Should().BeFalse();
    }

    // -- TryActivate --

    [Fact]
    public void TryActivate_ReturnsSkillId()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 1.5f).Should().Be("fireball");
    }

    [Fact]
    public void TryActivate_SetsCooldown()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 2.0f);
        bar.GetCooldown(0).Should().Be(2.0f);
    }

    [Fact]
    public void TryActivate_OnCooldown_ReturnsNull()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 2.0f);
        bar.TryActivate(0, 2.0f).Should().BeNull();
    }

    [Fact]
    public void TryActivate_EmptySlot_ReturnsNull()
    {
        new SkillBar().TryActivate(0, 1.0f).Should().BeNull();
    }

    // -- Update (cooldown tick) --

    [Fact]
    public void Update_DecreasesCooldown()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 2.0f);
        bar.Update(0.5f);
        bar.GetCooldown(0).Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void Update_CooldownFloorsAtZero()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 1.0f);
        bar.Update(5.0f); // way past cooldown
        bar.GetCooldown(0).Should().Be(0f);
    }

    [Fact]
    public void Update_AfterCooldownExpires_IsReadyAgain()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 1.0f);
        bar.Update(1.0f);
        bar.IsReady(0).Should().BeTrue();
    }

    // -- Serialization --

    [Fact]
    public void ExportImportSlots_RoundTrips()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.SetSlot(2, "heal");
        var exported = bar.ExportSlots();

        var restored = new SkillBar();
        restored.ImportSlots(exported);
        restored.GetSlot(0).Should().Be("fireball");
        restored.GetSlot(1).Should().BeNull();
        restored.GetSlot(2).Should().Be("heal");
    }

    [Fact]
    public void ImportSlots_Null_ClearsAll()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.ImportSlots(null);
        bar.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void ImportSlots_ClearsCooldowns()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 5.0f);
        bar.ImportSlots(bar.ExportSlots());
        bar.GetCooldown(0).Should().Be(0f);
    }

    [Fact]
    public void Reset_ClearsSlotsAndCooldowns()
    {
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.TryActivate(0, 5.0f);
        bar.Reset();
        bar.GetSlot(0).Should().BeNull();
        bar.GetCooldown(0).Should().Be(0f);
    }
}

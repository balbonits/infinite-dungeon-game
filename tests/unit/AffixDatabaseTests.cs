using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for <see cref="AffixDatabase"/>.
/// Existing code had <see cref="AffixDatabase.GetMaxTier"/> claiming tiers
/// 5 and 6 at item levels 75 / 100, but the registry only contained T1-T4
/// (AUDIT-10 bug). This suite covers the T5/T6 registrations added per
/// SPEC-AFFIX-TIER-LADDER-01 plus the sanity of the existing tier curve
/// so future rebalances don't silently drop families.
/// </summary>
public class AffixDatabaseTests
{
    // ── Tier ceiling: see DepthGearTierTests.AffixDatabase_GetMaxTier_ByItemLevel.
    // The prior GetMaxTier_ReturnsExpected theory here duplicated that test;
    // removed per Copilot PR #37 so a future threshold shift only has to
    // update one place instead of two silently-paired ones.

    // ── T5/T6 registrations exist (AUDIT-10 impl) ──────────────────────────

    [Theory]
    [InlineData("keen_5")]
    [InlineData("vicious_5")]
    [InlineData("sturdy_5")]
    [InlineData("warding_5")]
    [InlineData("striking_5")]
    [InlineData("ruin_5")]
    [InlineData("bear_5")]
    [InlineData("swiftness_5")]
    public void T5Affix_IsRegistered(string id)
    {
        var def = AffixDatabase.Get(id);
        def.Should().NotBeNull($"T5 affix '{id}' must exist per SPEC-AFFIX-TIER-LADDER-01");
        def!.Tier.Should().Be(5);
        def.MinItemLevel.Should().Be(75);
    }

    [Theory]
    [InlineData("keen_6")]
    [InlineData("vicious_6")]
    [InlineData("sturdy_6")]
    [InlineData("warding_6")]
    [InlineData("striking_6")]
    [InlineData("ruin_6")]
    [InlineData("bear_6")]
    [InlineData("swiftness_6")]
    public void T6Affix_IsRegistered(string id)
    {
        var def = AffixDatabase.Get(id);
        def.Should().NotBeNull($"T6 affix '{id}' must exist per SPEC-AFFIX-TIER-LADDER-01");
        def!.Tier.Should().Be(6);
        def.MinItemLevel.Should().Be(100);
    }

    // ── Spec-locked registry totals ────────────────────────────────────────

    [Fact]
    public void Registry_Has46Total_AfterAudit10()
    {
        // 30 T1-T4 (pre-existing, the spec counted 28 — bump this test + the
        // spec footnote together if either was revised) + 16 T5-T6 = 46.
        // If this number shifts, either AUDIT-10 was reverted or a family was
        // added without updating both spec + this test. Either case needs
        // a conscious bump.
        // Count via GetAvailable(int.MaxValue) — large enough that no realistic
        // MinItemLevel could gate anything out. Copilot PR #37 round-2 asked to
        // avoid adding a test-only public API; int.MaxValue catches every
        // registered affix without exposing Count.
        int totalCount = AffixDatabase.GetAvailable(itemLevel: int.MaxValue).Count();
        totalCount.Should().Be(46);
    }

    [Fact]
    public void T5_Has8Registrations()
    {
        // "Family" in the spec (keen, vicious, sturdy, ...) maps 1:1 to a
        // distinct registration at this tier — no family has >1 tier-5 def.
        // Counted as defs here since that's what the registry actually stores.
        int t5Count = AffixDatabase.GetAvailable(itemLevel: 75).Count(a => a.Tier == 5);
        t5Count.Should().Be(8, "8 T5 registrations (one per family): keen/vicious/sturdy/warding + striking/ruin/bear/swiftness");
    }

    [Fact]
    public void T6_Has8Registrations()
    {
        int t6Count = AffixDatabase.GetAvailable(itemLevel: 100).Count(a => a.Tier == 6);
        t6Count.Should().Be(8, "8 T6 registrations (one per family): same roster as T5");
    }

    // ── Spec-locked power curve (values) ───────────────────────────────────

    [Fact]
    public void KeenCurve_IsStrictlyIncreasing_AcrossTiers()
    {
        // Spec: T4=22 → T5=35 → T6=50 (≈1.5× per tier above T4, tapering).
        AffixDatabase.Get("keen_1")!.Value.Should().Be(3);
        AffixDatabase.Get("keen_2")!.Value.Should().Be(7);
        AffixDatabase.Get("keen_3")!.Value.Should().Be(13);
        AffixDatabase.Get("keen_4")!.Value.Should().Be(22);
        AffixDatabase.Get("keen_5")!.Value.Should().Be(35);
        AffixDatabase.Get("keen_6")!.Value.Should().Be(50);
    }

    [Fact]
    public void BearMaxHpCurve_SpecValues()
    {
        AffixDatabase.Get("bear_5")!.Value.Should().Be(90);
        AffixDatabase.Get("bear_6")!.Value.Should().Be(130);
    }

    [Fact]
    public void PercentAffixes_CapAt50_AtT6()
    {
        // Spec: "No percent affix exceeds 50% at T6 to preserve additive
        // stacking headroom across the 10-ring build space."
        var t6Percents = new[] { "vicious_6", "warding_6", "striking_6", "ruin_6", "swiftness_6" };
        foreach (var id in t6Percents)
        {
            var def = AffixDatabase.Get(id);
            def.Should().NotBeNull();
            def!.IsPercent.Should().BeTrue($"{id} must be a percent affix per spec");
            def.Value.Should().BeLessThanOrEqualTo(50, $"{id} exceeds 50% cap");
        }
    }

    // ── Gold cost curve ────────────────────────────────────────────────────

    [Fact]
    public void GoldCost_IncreasesSteadilyAcrossTiers_ForKeen()
    {
        // Exact values from the items.md T5+T6 table — regression guard.
        // The spec's narrative text ("T2≈170, T3≈440, ...") rounds for
        // prose; the locked table carries the precise registry numbers below.
        AffixDatabase.Get("keen_1")!.GoldCost.Should().Be(50);
        AffixDatabase.Get("keen_2")!.GoldCost.Should().Be(150);
        AffixDatabase.Get("keen_3")!.GoldCost.Should().Be(400);
        AffixDatabase.Get("keen_4")!.GoldCost.Should().Be(800);
        AffixDatabase.Get("keen_5")!.GoldCost.Should().Be(2000);
        AffixDatabase.Get("keen_6")!.GoldCost.Should().Be(4200);
    }

    // ── GetAvailable gating ────────────────────────────────────────────────

    [Fact]
    public void GetAvailable_BelowLevel75_ExcludesT5()
    {
        AffixDatabase.GetAvailable(itemLevel: 74)
            .Any(a => a.Tier == 5)
            .Should().BeFalse("T5 should not appear below item level 75");
    }

    [Fact]
    public void GetAvailable_BelowLevel100_ExcludesT6()
    {
        AffixDatabase.GetAvailable(itemLevel: 99)
            .Any(a => a.Tier == 6)
            .Should().BeFalse("T6 should not appear below item level 100");
    }

    [Fact]
    public void GetAvailable_AtLevel100_IncludesAllTiers()
    {
        var tiers = AffixDatabase.GetAvailable(itemLevel: 100)
            .Select(a => a.Tier)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();
        tiers.Should().Equal(new[] { 1, 2, 3, 4, 5, 6 });
    }
}

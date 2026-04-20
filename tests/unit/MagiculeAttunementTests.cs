using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class MagiculeAttunementTests
{
    // -- Initial state --

    [Fact]
    public void NewAttunement_IsNotUnlocked()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked.Should().BeFalse();
        ma.TotalPoints.Should().Be(0);
        ma.AvailablePoints.Should().Be(0);
    }

    // -- RecordFloorClear --

    [Fact]
    public void RecordFloorClear_AtFloor50_UnlocksSystem()
    {
        var ma = new MagiculeAttunement();
        ma.RecordFloorClear(50);
        ma.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public void RecordFloorClear_AboveFloor50_AwardsPoint()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked = true;
        ma.RecordFloorClear(51).Should().BeTrue();
        ma.TotalPoints.Should().Be(1);
    }

    [Fact]
    public void RecordFloorClear_SameFloorTwice_NoDoubleAward()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked = true;
        ma.RecordFloorClear(51);
        ma.RecordFloorClear(51).Should().BeFalse();
        ma.TotalPoints.Should().Be(1);
    }

    [Fact]
    public void RecordFloorClear_Floor50OrBelow_NoPoints()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked = true;
        ma.RecordFloorClear(50).Should().BeFalse();
        ma.RecordFloorClear(30).Should().BeFalse();
        ma.TotalPoints.Should().Be(0);
    }

    // -- Node types --

    [Theory]
    [InlineData(0, MagiculeAttunement.NodeType.Small)]
    [InlineData(11, MagiculeAttunement.NodeType.Small)]
    [InlineData(12, MagiculeAttunement.NodeType.Connector)]
    [InlineData(27, MagiculeAttunement.NodeType.Connector)]
    [InlineData(28, MagiculeAttunement.NodeType.Medium)]
    [InlineData(35, MagiculeAttunement.NodeType.Medium)]
    [InlineData(36, MagiculeAttunement.NodeType.Keystone)]
    [InlineData(39, MagiculeAttunement.NodeType.Keystone)]
    public void GetNodeType_CorrectClassification(int index, MagiculeAttunement.NodeType expected)
    {
        MagiculeAttunement.GetNodeType(index).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 2)]   // Small
    [InlineData(12, 1)]  // Connector
    [InlineData(28, 5)]  // Medium
    [InlineData(36, 15)] // Keystone
    public void GetNodeCost_CorrectPerType(int index, int expected)
    {
        MagiculeAttunement.GetNodeCost(index).Should().Be(expected);
    }

    // -- TryUnlockNode --

    [Fact]
    public void TryUnlockNode_Ring1_AlwaysAvailable()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 10;
        ma.TryUnlockNode(0).Should().BeTrue(); // STR small node
        ma.IsNodeUnlocked(0).Should().BeTrue();
    }

    [Fact]
    public void TryUnlockNode_CostsPoints()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 10;
        ma.TryUnlockNode(0); // costs 2
        ma.AvailablePoints.Should().Be(8);
    }

    [Fact]
    public void TryUnlockNode_InsufficientPoints_Fails()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 1; // need 2 for small node
        ma.TryUnlockNode(0).Should().BeFalse();
    }

    [Fact]
    public void TryUnlockNode_AlreadyUnlocked_Fails()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 10;
        ma.TryUnlockNode(0);
        ma.TryUnlockNode(0).Should().BeFalse();
    }

    [Fact]
    public void TryUnlockNode_Connector_NeedsRing1InAdjacentBranch()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 50;

        // Connector 12 is STR-DEX (branch 0). Needs STR or DEX ring 1 node
        ma.CanUnlock(12).Should().BeFalse();
        ma.TryUnlockNode(0); // STR ring 1
        ma.CanUnlock(12).Should().BeTrue();
    }

    [Fact]
    public void TryUnlockNode_Ring2_Needs2Ring1InSameBranch()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 50;

        // Ring 2 node 28 is STR branch. Need 2 STR small nodes (0, 1, 2)
        ma.TryUnlockNode(0); // 1 STR small
        ma.CanUnlock(28).Should().BeFalse();
        ma.TryUnlockNode(1); // 2 STR small
        ma.CanUnlock(28).Should().BeTrue();
    }

    [Fact]
    public void TryUnlockNode_Keystone_NeedsRing2InSameBranch()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 50;

        // Keystone 36 (STR) needs Ring 2 in STR branch (28 or 29)
        ma.TryUnlockNode(0); // STR small
        ma.TryUnlockNode(1); // STR small
        ma.CanUnlock(36).Should().BeFalse();
        ma.TryUnlockNode(28); // STR medium
        ma.CanUnlock(36).Should().BeTrue();
    }

    [Fact]
    public void TryUnlockNode_Keystone_SetsActiveKeystone()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 50;

        ma.TryUnlockNode(0);
        ma.TryUnlockNode(1);
        ma.TryUnlockNode(28);
        ma.TryUnlockNode(36); // STR keystone

        ma.ActiveKeystone.Should().Be(0); // STR = index 0
        ma.HasJuggernaut.Should().BeTrue();
    }

    [Fact]
    public void Keystone_LastOneWins()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 200;

        // Unlock STR keystone
        ma.TryUnlockNode(0); ma.TryUnlockNode(1); ma.TryUnlockNode(28); ma.TryUnlockNode(36);
        ma.ActiveKeystone.Should().Be(0);

        // Unlock DEX keystone
        ma.TryUnlockNode(3); ma.TryUnlockNode(4); ma.TryUnlockNode(30); ma.TryUnlockNode(37);
        ma.ActiveKeystone.Should().Be(1);
        ma.HasPhantom.Should().BeTrue();
        ma.HasJuggernaut.Should().BeFalse();
    }

    // -- Stat bonuses --

    [Fact]
    public void FlatMeleeDamage_Node0_Gives5()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 10;
        ma.FlatMeleeDamage.Should().Be(0f);
        ma.TryUnlockNode(0);
        ma.FlatMeleeDamage.Should().Be(5f);
    }

    [Fact]
    public void FlatMaxMana_Node9_Gives40()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 10;
        ma.TryUnlockNode(9); // INT: Expanded Mind
        ma.FlatMaxMana.Should().Be(40);
    }

    [Fact]
    public void MediumNode_HasOverkill()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 50;
        ma.TryUnlockNode(0); ma.TryUnlockNode(1);
        ma.TryUnlockNode(28);
        ma.HasOverkill.Should().BeTrue();
    }

    // -- Serialization --

    [Fact]
    public void ExportImport_RoundTrips()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked = true;
        ma.TotalPoints = 30;
        ma.TryUnlockNode(0);
        ma.TryUnlockNode(1);
        ma.RecordFloorClear(51);
        ma.RecordFloorClear(52);

        var nodes = ma.ExportNodes();
        var floors = ma.ExportClearedFloors();

        var restored = new MagiculeAttunement();
        restored.ImportState(nodes, floors, ma.ActiveKeystone, true);
        restored.IsNodeUnlocked(0).Should().BeTrue();
        restored.IsNodeUnlocked(1).Should().BeTrue();
        restored.IsUnlocked.Should().BeTrue();
    }

    // AUDIT-04 regression: ImportState must filter cleared-floors by > UnlockFloor,
    // mirroring RecordFloorClear's in-game filter. Without the filter, a corrupt
    // save could inflate TotalPoints by carrying pre-unlock floors (e.g. 1, 2, 50)
    // in the ExportClearedFloors array.

    [Fact]
    public void ImportState_FiltersFloorsAtOrBelowUnlockFloor()
    {
        var ma = new MagiculeAttunement();
        // Simulate a corrupt / pre-unlock-era save that includes floors ≤ 50.
        int[] corrupt = { 1, 25, 50, 51, 52, 100 };
        ma.ImportState(
            nodes: null,
            clearedFloors: corrupt,
            activeKeystone: -1,
            isUnlocked: true);

        // Only floors 51, 52, 100 qualify (> UnlockFloor). TotalPoints = 3.
        ma.TotalPoints.Should().Be(3);
    }

    [Fact]
    public void ImportState_OnlyQualifyingFloorsAreStored()
    {
        var ma = new MagiculeAttunement();
        int[] mixed = { 10, 50, 51, 75 };
        ma.ImportState(
            nodes: null,
            clearedFloors: mixed,
            activeKeystone: -1,
            isUnlocked: true);

        // Exporting should round-trip only the qualifying floors.
        var reexported = ma.ExportClearedFloors();
        reexported.Should().BeEquivalentTo(new[] { 51, 75 });
    }

    [Fact]
    public void ImportState_AllBelowUnlockFloor_YieldsZeroPoints()
    {
        var ma = new MagiculeAttunement();
        ma.ImportState(
            nodes: null,
            clearedFloors: new[] { 1, 25, 49, 50 },
            activeKeystone: -1,
            isUnlocked: true);

        ma.TotalPoints.Should().Be(0);
        ma.AvailablePoints.Should().Be(0);
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        var ma = new MagiculeAttunement();
        ma.IsUnlocked = true;
        ma.TotalPoints = 10;
        ma.TryUnlockNode(0);
        ma.Reset();
        ma.IsUnlocked.Should().BeFalse();
        ma.TotalPoints.Should().Be(0);
        ma.IsNodeUnlocked(0).Should().BeFalse();
        ma.ActiveKeystone.Should().Be(-1);
    }

    // -- Branch calculation --

    [Theory]
    [InlineData(0, 0)]  // STR ring 1
    [InlineData(3, 1)]  // DEX ring 1
    [InlineData(6, 2)]  // STA ring 1
    [InlineData(9, 3)]  // INT ring 1
    [InlineData(36, 0)] // STR keystone
    [InlineData(39, 3)] // INT keystone
    public void GetBranch_CorrectForAllNodeTypes(int nodeIndex, int expected)
    {
        MagiculeAttunement.GetBranch(nodeIndex).Should().Be(expected);
    }

    [Fact]
    public void OutOfBounds_NodeIndex_Fails()
    {
        var ma = new MagiculeAttunement();
        ma.TotalPoints = 100;
        ma.TryUnlockNode(-1).Should().BeFalse();
        ma.TryUnlockNode(40).Should().BeFalse();
    }
}

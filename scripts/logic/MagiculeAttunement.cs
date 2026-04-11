using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Post-cap passive tree. 40 nodes in 3 rings with 4 branches.
/// Unlocked after clearing floor 50. Points earned 1 per new floor cleared past 50.
/// Spec: docs/systems/magicule-attunement.md
/// </summary>
public class MagiculeAttunement
{
    public const int TotalNodes = 40;
    public const int UnlockFloor = 50;
    public const int SmallNodeCount = 12;   // Ring 1, 2 points each
    public const int ConnectorCount = 16;   // Between rings, 1 point each
    public const int MediumNodeCount = 8;   // Ring 2, 5 points each
    public const int KeystoneCount = 4;     // Ring 3, 15 points each

    // Node costs by type
    private const int SmallCost = 2;
    private const int ConnectorCost = 1;
    private const int MediumCost = 5;
    private const int KeystoneCost = 15;

    /// <summary>Whether the system is unlocked (floor 50 cleared).</summary>
    public bool IsUnlocked { get; set; }

    /// <summary>Total attunement points earned (never lost).</summary>
    public int TotalPoints { get; set; }

    /// <summary>Points spent on unlocked nodes.</summary>
    public int SpentPoints { get; private set; }

    /// <summary>Available points to spend.</summary>
    public int AvailablePoints => TotalPoints - SpentPoints;

    /// <summary>Set of floors cleared (past floor 50) that granted points.</summary>
    private readonly HashSet<int> _clearedFloors = new();

    /// <summary>Bitmask of unlocked nodes (indexed 0-39).</summary>
    private readonly bool[] _nodes = new bool[TotalNodes];

    /// <summary>Active keystone index (-1 = none, 0-3 = STR/DEX/STA/INT).</summary>
    public int ActiveKeystone { get; private set; } = -1;

    // --- Node layout ---
    // Nodes 0-11: Ring 1 small (3 per branch: STR 0-2, DEX 3-5, STA 6-8, INT 9-11)
    // Nodes 12-27: Connectors (4 per adjacent pair)
    // Nodes 28-35: Ring 2 medium (2 per branch: STR 28-29, DEX 30-31, STA 32-33, INT 34-35)
    // Nodes 36-39: Ring 3 keystones (STR 36, DEX 37, STA 38, INT 39)

    public enum NodeType { Small, Connector, Medium, Keystone }

    public static NodeType GetNodeType(int nodeIndex)
    {
        if (nodeIndex < SmallNodeCount) return NodeType.Small;
        if (nodeIndex < SmallNodeCount + ConnectorCount) return NodeType.Connector;
        if (nodeIndex < SmallNodeCount + ConnectorCount + MediumNodeCount) return NodeType.Medium;
        return NodeType.Keystone;
    }

    public static int GetNodeCost(int nodeIndex) => GetNodeType(nodeIndex) switch
    {
        NodeType.Small => SmallCost,
        NodeType.Connector => ConnectorCost,
        NodeType.Medium => MediumCost,
        NodeType.Keystone => KeystoneCost,
        _ => 0,
    };

    public static int GetBranch(int nodeIndex)
    {
        if (nodeIndex < 12) return nodeIndex / 3;           // Ring 1: 3 per branch
        if (nodeIndex < 28) return (nodeIndex - 12) / 4;    // Connectors: 4 per pair
        if (nodeIndex < 36) return (nodeIndex - 28) / 2;    // Ring 2: 2 per branch
        return nodeIndex - 36;                               // Ring 3: 1 per branch
    }

    public bool IsNodeUnlocked(int nodeIndex) =>
        nodeIndex >= 0 && nodeIndex < TotalNodes && _nodes[nodeIndex];

    /// <summary>Attempt to unlock a node. Returns true if successful.</summary>
    public bool TryUnlockNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= TotalNodes) return false;
        if (_nodes[nodeIndex]) return false; // already unlocked
        int cost = GetNodeCost(nodeIndex);
        if (AvailablePoints < cost) return false;
        if (!CanUnlock(nodeIndex)) return false;

        _nodes[nodeIndex] = true;
        SpentPoints += cost;

        // Keystone handling: only one active at a time
        if (GetNodeType(nodeIndex) == NodeType.Keystone)
            ActiveKeystone = nodeIndex - 36;

        return true;
    }

    /// <summary>Check if a node can be unlocked (pathing rules).</summary>
    public bool CanUnlock(int nodeIndex)
    {
        var type = GetNodeType(nodeIndex);
        int branch = GetBranch(nodeIndex);

        return type switch
        {
            // Ring 1: always unlockable (connected to origin)
            NodeType.Small => true,
            // Connectors: need at least 1 Ring 1 node in either adjacent branch
            NodeType.Connector => HasAnySmallNodeInBranch(branch % 4) ||
                                  HasAnySmallNodeInBranch((branch + 1) % 4),
            // Ring 2: need 2+ Ring 1 nodes in same branch
            NodeType.Medium => CountSmallNodesInBranch(branch) >= 2,
            // Ring 3: need the Ring 2 node(s) in same branch
            NodeType.Keystone => HasMediumNodeInBranch(branch),
            _ => false,
        };
    }

    private bool HasAnySmallNodeInBranch(int branch)
    {
        int start = branch * 3;
        return _nodes[start] || _nodes[start + 1] || _nodes[start + 2];
    }

    private int CountSmallNodesInBranch(int branch)
    {
        int start = branch * 3;
        int count = 0;
        for (int i = start; i < start + 3; i++)
            if (_nodes[i]) count++;
        return count;
    }

    private bool HasMediumNodeInBranch(int branch)
    {
        int start = 28 + branch * 2;
        return _nodes[start] || _nodes[start + 1];
    }

    // --- Floor clearing ---

    /// <summary>Record a floor clear. Awards 1 point if floor > 50 and not previously cleared.</summary>
    public bool RecordFloorClear(int floor)
    {
        if (!IsUnlocked && floor >= UnlockFloor)
            IsUnlocked = true;

        if (floor <= UnlockFloor) return false;
        if (_clearedFloors.Contains(floor)) return false;

        _clearedFloors.Add(floor);
        TotalPoints++;
        return true;
    }

    // --- Stat bonuses (flat, bypass diminishing returns) ---

    // Ring 1 small nodes
    public float FlatMeleeDamage => IsNodeUnlocked(0) ? 5f : 0f;     // STR: Hardened Muscles
    public int FlatMaxHpStr => IsNodeUnlocked(1) ? 30 : 0;           // STR: Bone Density
    public float MeleeDamagePercent => IsNodeUnlocked(2) ? 0.03f : 0f; // STR: Crushing Force
    public float AttackSpeedPercent => IsNodeUnlocked(3) ? 0.03f : 0f; // DEX: Nimble Fingers
    public float FlatDodgeChance => IsNodeUnlocked(4) ? 0.08f : 0f;   // DEX: Fleet Footed
    public float FlatCritChance => IsNodeUnlocked(5) ? 0.02f : 0f;    // DEX: Sharp Eyes
    public int FlatDefense => IsNodeUnlocked(6) ? 20 : 0;             // STA: Thick Skin
    public float FlatHpRegen => IsNodeUnlocked(7) ? 0.5f : 0f;        // STA: Deep Breath
    public int FlatMaxHpSta => IsNodeUnlocked(8) ? 50 : 0;            // STA: Enduring Body
    public int FlatMaxMana => IsNodeUnlocked(9) ? 40 : 0;             // INT: Expanded Mind
    public float ProcessingEfficiency => IsNodeUnlocked(10) ? 0.05f : 0f; // INT: Efficient Processing
    public float SpellDamagePercent => IsNodeUnlocked(11) ? 0.08f : 0f;   // INT: Magicule Affinity

    // Connector totals (each of 16 connectors gives a small hybrid bonus)
    public float ConnectorMeleeDamage
    {
        get
        {
            float total = 0;
            // STR-DEX connectors (12-15): +2 flat melee each
            for (int i = 12; i < 16; i++) if (_nodes[i]) total += 2f;
            // INT-STR connectors (24-27): +2 flat melee each
            for (int i = 24; i < 28; i++) if (_nodes[i]) total += 2f;
            return total;
        }
    }

    public float ConnectorAttackSpeed
    {
        get
        {
            float total = 0;
            // STR-DEX connectors (12-15): +1% attack speed each
            for (int i = 12; i < 16; i++) if (_nodes[i]) total += 0.01f;
            return total;
        }
    }

    public int ConnectorMaxHp
    {
        get
        {
            int total = 0;
            // DEX-STA connectors (16-19): +15 max HP each
            for (int i = 16; i < 20; i++) if (_nodes[i]) total += 15;
            // STA-INT connectors (20-23): +10 max HP each
            for (int i = 20; i < 24; i++) if (_nodes[i]) total += 10;
            return total;
        }
    }

    public int ConnectorMaxMana
    {
        get
        {
            int total = 0;
            // STA-INT connectors (20-23): +15 max mana each
            for (int i = 20; i < 24; i++) if (_nodes[i]) total += 15;
            return total;
        }
    }

    public float ConnectorDodge
    {
        get
        {
            float total = 0;
            // DEX-STA connectors (16-19): +2% dodge each
            for (int i = 16; i < 20; i++) if (_nodes[i]) total += 0.02f;
            return total;
        }
    }

    public float ConnectorSpellDamage
    {
        get
        {
            float total = 0;
            // INT-STR connectors (24-27): +3% spell damage each
            for (int i = 24; i < 28; i++) if (_nodes[i]) total += 0.03f;
            return total;
        }
    }

    // Ring 2 medium nodes (mechanic flags)
    public bool HasOverkill => IsNodeUnlocked(28);          // STR: 25% excess damage splash
    public bool HasBerserkersEdge => IsNodeUnlocked(29);    // STR: +20% melee below 30% HP
    public bool HasChainShot => IsNodeUnlocked(30);         // DEX: 10% free projectile
    public bool HasAfterimage => IsNodeUnlocked(31);        // DEX: +30% move speed after dodge
    public bool HasSecondWind => IsNodeUnlocked(32);        // STA: recover 15% HP once/floor
    public bool HasIronConstitution => IsNodeUnlocked(33);  // STA: -40% DoT damage
    public bool HasSpellEcho => IsNodeUnlocked(34);         // INT: 8% double cast
    public bool HasManaShield => IsNodeUnlocked(35);        // INT: redirect 20% dmg to mana

    // Ring 3 keystones
    public bool IsKeystoneActive(int keystoneIndex) => ActiveKeystone == keystoneIndex;
    public bool HasJuggernaut => ActiveKeystone == 0;
    public bool HasPhantom => ActiveKeystone == 1;
    public bool HasUndying => ActiveKeystone == 2;
    public bool HasArcaneOverload => ActiveKeystone == 3;

    // --- Serialization ---

    public bool[] ExportNodes() => (bool[])_nodes.Clone();
    public int[] ExportClearedFloors() => _clearedFloors.ToArray();

    public void ImportState(bool[]? nodes, int[]? clearedFloors, int activeKeystone, bool isUnlocked)
    {
        Array.Clear(_nodes, 0, TotalNodes);
        _clearedFloors.Clear();
        SpentPoints = 0;

        IsUnlocked = isUnlocked;
        ActiveKeystone = Math.Clamp(activeKeystone, -1, 3);

        if (nodes != null)
        {
            for (int i = 0; i < Math.Min(nodes.Length, TotalNodes); i++)
            {
                _nodes[i] = nodes[i];
                if (nodes[i])
                    SpentPoints += GetNodeCost(i);
            }
        }

        if (clearedFloors != null)
        {
            foreach (int f in clearedFloors)
                _clearedFloors.Add(f);
        }

        TotalPoints = _clearedFloors.Count;
    }

    public void Reset()
    {
        Array.Clear(_nodes, 0, TotalNodes);
        _clearedFloors.Clear();
        IsUnlocked = false;
        TotalPoints = 0;
        SpentPoints = 0;
        ActiveKeystone = -1;
    }
}

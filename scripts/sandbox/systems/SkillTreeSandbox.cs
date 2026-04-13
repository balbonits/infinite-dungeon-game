using Godot;
using System.Collections.Generic;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Skill Tree
/// Browse skills per class. Shows categories, base skills, specific skills, passive bonuses.
/// Headless checks: ID uniqueness, parent resolution, all classes load.
/// Run: make sandbox SCENE=skill-tree
/// </summary>
public partial class SkillTreeSandbox : SandboxBase
{
    protected override string SandboxTitle => "🌿  Skill Tree Sandbox";

    private PlayerClass _class = PlayerClass.Warrior;

    protected override void _SandboxReady()
    {
        AddSectionLabel("Class");
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var c = cls;
            AddButton(cls.ToString(), () => { _class = c; ShowTree(); });
        }
        ShowTree();
    }

    protected override void _Reset() => ShowTree();

    private void ShowTree()
    {
        Log($"── {_class} Skill Tree ──");
        var categories = SkillDatabase.GetCategories(_class);
        foreach (var catId in categories)
        {
            Log($"  [{SkillDatabase.GetCategoryName(catId)}]");
            foreach (var baseSkill in SkillDatabase.GetBaseSkillsInCategory(catId))
            {
                Log($"    ▸ {baseSkill.Name}  ({baseSkill.PassiveType} ×{baseSkill.PassiveMultiplier:F2})");
                foreach (var specific in SkillDatabase.GetSpecificSkills(baseSkill.Id))
                    Log($"      → {specific.Name}  cost:{specific.ManaCost}mp  cd:{specific.Cooldown:F1}s");
            }
        }
        Log("");
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");

        // All skill IDs unique across all classes
        var allIds = new HashSet<string>();
        int total = 0;
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            foreach (var skill in SkillDatabase.GetByClass(cls))
            {
                Assert(!allIds.Contains(skill.Id), $"Skill ID unique: {skill.Id}");
                allIds.Add(skill.Id);
                total++;

                // Parent reference resolves if set
                if (skill.ParentBaseSkillId != null)
                    Assert(SkillDatabase.Get(skill.ParentBaseSkillId) != null,
                        $"{skill.Id}: parent '{skill.ParentBaseSkillId}' resolves");
            }
        }
        Assert(total > 0, $"Skills registered: {total}");

        // Categories exist for all classes
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var cats = SkillDatabase.GetCategories(cls);
            Assert(cats.Length > 0, $"{cls} has categories");
        }

        FinishHeadless();
    }
}

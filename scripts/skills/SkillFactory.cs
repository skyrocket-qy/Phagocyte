using Godot;
using Godot.Collections;
using Game.Core;

namespace Game.Skills;

/// <summary>
/// Id-routed skill factory. No caller names a concrete skill class:
/// the catalog row's archetype selects the executor, params tune it.
/// A new skill is a new JSON row; a new behavior extends an archetype.
/// </summary>
public static class SkillFactory
{
    public static BaseSkill? CreateActive(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || !GameManager.SkillCatalog.ContainsKey(skillId))
            return null;
        var info = (Dictionary)GameManager.SkillCatalog[skillId];
        BaseSkill? skill = CatalogLoader.GetString(info, "archetype") switch
        {
            "salvo" => new SalvoSkill(),
            "beam" => new BeamSkill(),
            "nova" => new NovaSkill(),
            "zone" => new ZoneSkill(),
            "strike" => new StrikeSkill(),
            "aura" => new AuraSkill(),
            _ => null
        };
        if (skill == null)
            return null;
        skill.SkillId = skillId;
        skill.IsPassive = false;
        skill.IsInnate = CatalogLoader.GetString(info, "type") == "innate";
        skill.MaxLevel = CatalogLoader.GetInt(info, "max_level", 5);
        skill.Cooldown = CatalogLoader.GetFloat(info, "cooldown", 3.0f);
        skill.NameKey = CatalogLoader.GetString(info, "name_key");
        skill.DescKey = CatalogLoader.GetString(info, "desc_key");
        skill.BioKey = CatalogLoader.GetString(info, "bio_key");
        skill.IconSymbol = CatalogLoader.GetString(info, "icon", "?");
        return skill;
    }

    public static BaseSkill? CreatePassive(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || !GameManager.SkillCatalog.ContainsKey(skillId))
            return null;
        var skill = new StatPassive
        {
            SkillId = skillId,
            IsPassive = true,
            MaxLevel = 5
        };
        var info = (Dictionary)GameManager.SkillCatalog[skillId];
        skill.MaxLevel = CatalogLoader.GetInt(info, "max_level", 5);
        skill.NameKey = CatalogLoader.GetString(info, "name_key");
        skill.DescKey = CatalogLoader.GetString(info, "desc_key");
        skill.BioKey = CatalogLoader.GetString(info, "bio_key");
        skill.IconSymbol = CatalogLoader.GetString(info, "icon", "?");
        return skill;
    }
}

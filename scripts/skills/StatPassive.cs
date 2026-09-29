using Godot;
using Godot.Collections;
using Game.Core;

namespace Game.Skills;

/// <summary>Passive skill applying data-driven per-level stat modifiers.</summary>
public partial class StatPassive : BaseSkill
{
    public override void ApplyPassiveModifiers()
    {
        if (!HasValidHost())
            return;
        foreach (var mod in Mods())
        {
            float value = mod.Mode == "flat_once" ? mod.PerLevel : mod.PerLevel * Level;
            if (mod.Mode == "mult")
                ApplyStat(mod.Stat, 0.0f, value);
            else
                ApplyStat(mod.Stat, value, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (!HasValidHost())
            return;
        foreach (var mod in Mods())
        {
            float value = mod.Mode == "flat_once" ? mod.PerLevel : mod.PerLevel * Level;
            if (mod.Mode == "mult")
                RemoveStat(mod.Stat, 0.0f, value);
            else
                RemoveStat(mod.Stat, value, 0.0f);
        }
    }

    private sealed class Mod
    {
        public string Stat = "";
        public float PerLevel;
        public string Mode = "flat";
    }

    private System.Collections.Generic.List<Mod> Mods()
    {
        var outMods = new System.Collections.Generic.List<Mod>();
        if (string.IsNullOrEmpty(SkillId))
            return outMods;
        if (!GameManager.SkillCatalog.TryGetValue(SkillId, out var raw)
            || raw.VariantType != Variant.Type.Dictionary)
            return outMods;
        var info = (Dictionary)raw;
        if (!info.TryGetValue("mods", out Variant mv) || mv.VariantType != Variant.Type.Array)
            return outMods;
        foreach (var item in (Array)mv)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;
            var md = (Dictionary)item;
            string stat = CatalogLoader.GetString(md, "stat");
            if (stat == "")
                continue;
            outMods.Add(new Mod
            {
                Stat = stat,
                PerLevel = CatalogLoader.GetFloat(md, "per_level", 0.0f),
                Mode = CatalogLoader.GetString(md, "mode", "flat")
            });
        }
        return outMods;
    }
}

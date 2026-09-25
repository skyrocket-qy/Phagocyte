using Godot;
using Godot.Collections;
using System;

namespace Phagocyte.Skills;

/// <summary>
/// Manages 5 Active Cytokine Weapon slots and 5 Passive Organelle Trait slots.
/// Separates weapon ticks and stat modifier lifecycles cleanly.
/// </summary>
public partial class SkillManager : Node2D
{
    [Signal]
    public delegate void SkillsChangedEventHandler();

    public const int MaxActiveSlots = 5;
    public const int MaxPassiveSlots = 5;

    public Array<BaseSkill?> ActiveSlots { get; private set; } = new();
    public Array<BaseSkill?> PassiveSlots { get; private set; } = new();

    public CharacterBody2D? Host { get; private set; } = null;

    public SkillManager()
    {
        for (int i = 0; i < MaxActiveSlots; i++)
        {
            ActiveSlots.Add(null);
        }
        for (int i = 0; i < MaxPassiveSlots; i++)
        {
            PassiveSlots.Add(null);
        }
    }

    public void Setup(CharacterBody2D pHost)
    {
        Host = pHost;
    }

    /// <summary>
    /// General equip method that automatically routes based on skill.IsPassive
    /// </summary>
    public bool EquipSkill(BaseSkill? skill, int targetSlot = -1)
    {
        if (skill == null)
            return false;
        if (skill.IsPassive)
            return EquipPassive(skill, targetSlot);
        else
            return EquipActive(skill, targetSlot);
    }

    /// <summary>
    /// Equips an Active weapon skill into ActiveSlots (0..4)
    /// </summary>
    public bool EquipActive(BaseSkill? skill, int targetSlot = -1)
    {
        if (skill == null || skill.IsPassive)
            return false;

        if (targetSlot >= 0 && targetSlot < MaxActiveSlots)
        {
            if (ActiveSlots[targetSlot] != null && ActiveSlots[targetSlot]!.IsInnate)
                return false;
            AssignActiveSlot(skill, targetSlot);
            return true;
        }

        for (int i = 0; i < MaxActiveSlots; i++)
        {
            if (ActiveSlots[i] == null)
            {
                AssignActiveSlot(skill, i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Equips a Passive organelle trait into PassiveSlots (0..4)
    /// </summary>
    public bool EquipPassive(BaseSkill? skill, int targetSlot = -1)
    {
        if (skill == null || !skill.IsPassive)
            return false;

        if (targetSlot >= 0 && targetSlot < MaxPassiveSlots)
        {
            if (PassiveSlots[targetSlot] != null && PassiveSlots[targetSlot]!.IsInnate)
                return false;
            AssignPassiveSlot(skill, targetSlot);
            return true;
        }

        for (int i = 0; i < MaxPassiveSlots; i++)
        {
            if (PassiveSlots[i] == null)
            {
                AssignPassiveSlot(skill, i);
                return true;
            }
        }
        return false;
    }

    private void AssignActiveSlot(BaseSkill skill, int slotIdx)
    {
        if (ActiveSlots[slotIdx] != null && GodotObject.IsInstanceValid(ActiveSlots[slotIdx]))
        {
            ActiveSlots[slotIdx]!.QueueFree();
        }

        ActiveSlots[slotIdx] = skill;
        AddChild(skill);
        if (Host != null)
            skill.Setup(Host);
        EmitSignal(SignalName.SkillsChanged);
    }

    private void AssignPassiveSlot(BaseSkill skill, int slotIdx)
    {
        if (PassiveSlots[slotIdx] != null && GodotObject.IsInstanceValid(PassiveSlots[slotIdx]))
        {
            PassiveSlots[slotIdx]!.QueueFree();
        }

        PassiveSlots[slotIdx] = skill;
        AddChild(skill);
        if (Host != null)
            skill.Setup(Host);
        EmitSignal(SignalName.SkillsChanged);
    }

    public BaseSkill? GetActiveSlot(int slotIdx)
    {
        if (slotIdx >= 0 && slotIdx < MaxActiveSlots)
            return ActiveSlots[slotIdx];
        return null;
    }

    public BaseSkill? GetPassiveSlot(int slotIdx)
    {
        if (slotIdx >= 0 && slotIdx < MaxPassiveSlots)
            return PassiveSlots[slotIdx];
        return null;
    }

    /// <summary>
    /// Update loop: Only active weapons tick cooldowns and trigger
    /// </summary>
    public void UpdateAllSkills(double delta)
    {
        foreach (var skill in ActiveSlots)
        {
            if (skill != null && GodotObject.IsInstanceValid(skill))
            {
                skill.UpdateSkill(delta);
            }
        }
    }

    /// <summary>
    /// Categorized UI Data for Dual-Row HUD (5 Active + 5 Passive)
    /// </summary>
    public Dictionary GetUiData()
    {
        var activeList = new Array<Dictionary>();
        for (int i = 0; i < MaxActiveSlots; i++)
        {
            var s = ActiveSlots[i];
            if (s != null && GodotObject.IsInstanceValid(s))
                activeList.Add(s.GetUiData());
            else
                activeList.Add(GetEmptySlotData(false));
        }

        var passiveList = new Array<Dictionary>();
        for (int i = 0; i < MaxPassiveSlots; i++)
        {
            var s = PassiveSlots[i];
            if (s != null && GodotObject.IsInstanceValid(s))
                passiveList.Add(s.GetUiData());
            else
                passiveList.Add(GetEmptySlotData(true));
        }

        return new Dictionary
        {
            ["actives"] = activeList,
            ["passives"] = passiveList
        };
    }

    /// <summary>
    /// Flat UI Data array (10 items total: 0..4 actives, 5..9 passives)
    /// </summary>
    public Array<Dictionary> GetAllUiData()
    {
        var res = new Array<Dictionary>();
        var uiDict = GetUiData();
        if (uiDict.TryGetValue("actives", out var activesVar))
            res.AddRange(activesVar.AsGodotArray<Dictionary>());
        if (uiDict.TryGetValue("passives", out var passivesVar))
            res.AddRange(passivesVar.AsGodotArray<Dictionary>());
        return res;
    }

    public Array<Dictionary> get_all_ui_data() => GetAllUiData();

    private Dictionary GetEmptySlotData(bool isPass)
    {
        return new Dictionary
        {
            ["id"] = "",
            ["name"] = Tr("SKILL_EMPTY"),
            ["description"] = "",
            ["biochemistry"] = "",
            ["icon"] = "+",
            ["image_path"] = "",
            ["level"] = 0,
            ["max_level"] = 0,
            ["is_passive"] = isPass,
            ["is_innate"] = false,
            ["cooldown_max"] = 0.0f,
            ["cooldown_ratio"] = 0.0f,
            ["cooldown_time"] = 0.0f
        };
    }
}

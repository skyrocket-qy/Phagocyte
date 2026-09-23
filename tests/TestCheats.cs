using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

/// <summary>
/// One-call full-unlock / full-build setup for test demand (headless suites
/// and headed debug runs). The meta helpers compose the scattered per-suite
/// unlock loops into a single entry point; <see cref="MaxOutPlayer"/> builds
/// a max-level run loadout on top. Everything is additive — call
/// <see cref="LockToBaseline"/> first when an exact state is required.
/// The passive tree is intentionally left unallocated: suites spend the
/// granted points explicitly.
/// </summary>
public static class TestCheats
{
    /// <summary>Headed user arg (after `--`): unlock everything + max the run build.</summary>
    public const string CheatArgAll = "--cheats=all";

    /// <summary>Headed user arg (after `--`): wipe meta progression back to a fresh profile.</summary>
    public const string CheatArgReset = "--cheats-reset";

    /// <summary>Headed user arg (after `--`): invulnerable on top of <see cref="CheatArgAll"/>.</summary>
    public const string CheatArgGodmode = "--godmode";

    public const int DefaultMetaTreeLevel = 15;
    public const int DefaultBonusPoints = 50;
    public const int DefaultRunLevel = 30;
    public const int DefaultSkillLevel = 5;

    /// <summary>
    /// Unlocks every achievement (cascades to all classes, maps + Hard modes,
    /// talent points and Endless), every organelle, max tree levels for every
    /// cell and a grant of spendable bonus points.
    /// </summary>
    public static void UnlockAllMeta(int treeLevel = DefaultMetaTreeLevel, int bonusPoints = DefaultBonusPoints)
    {
        var ids = new List<string>();
        foreach (string achId in AchievementManager.Achievements.Keys)
            ids.Add(achId);
        foreach (string achId in ids)
            AchievementManager.Unlock(achId);

        OrganelleUnlockManager.UnlockAll();
        MaxTreeLevels(treeLevel);
        GrantTreePoints(bonusPoints);
    }

    /// <summary>
    /// Wipes meta progression back to a fresh profile: macrophage-only, wound
    /// Normal-only, empty organelle vault, level-1 trees, zero bonus points,
    /// empty loadouts. Headed runs need this because menu cheats write the
    /// real profile (headless suites stay isolated via
    /// <see cref="TestHarness"/> save paths instead).
    /// </summary>
    public static void LockToBaseline()
    {
        AchievementManager.ResetAll();
        OrganelleUnlockManager.ResetAll();
        PassiveTreeManager.ResetAll();
        LoadoutManager.ResetCache();
        JsonStore.Delete(LoadoutManager.SavePath);
    }

    /// <summary>Records <paramref name="treeLevel"/> as the run level of every known cell.</summary>
    public static void MaxTreeLevels(int treeLevel = DefaultMetaTreeLevel)
    {
        foreach (var keyVar in GameManager.ClassData.Keys)
            PassiveTreeManager.RecordRunLevel(keyVar.AsString(), treeLevel);
    }

    /// <summary>Grants spendable shared talent points (no-op for non-positive amounts).</summary>
    public static void GrantTreePoints(int amount = DefaultBonusPoints)
    {
        if (amount > 0)
            PassiveTreeManager.AddBonusPoints(amount);
    }

    /// <summary>
    /// Builds a max-level run loadout on <paramref name="main"/>'s player:
    /// run level, maxed innate + filled active/passive slots, full organelle
    /// vault + best-effort chamber, zeroed block/evasion RNG. Godmode stays
    /// opt-in (default false) so damage-sensitive suites keep their signal.
    /// Idempotent — safe to call twice (e.g. once plain, once with godmode).
    /// Returns false when the run has no usable player.
    /// </summary>
    public static bool MaxOutPlayer(Main main, int runLevel = DefaultRunLevel, int skillLevel = DefaultSkillLevel, bool godmode = false)
    {
        if (main == null || !GodotObject.IsInstanceValid(main))
            return false;
        if (main.Player is not BaseCell player || !GodotObject.IsInstanceValid(player))
            return false;

        // Determinism: full-build suites must never dodge or block unscripted.
        if (player.Stats != null)
        {
            player.Stats.SetBase("block", 0.0f);
            player.Stats.SetBase("evasion", 0.0f);
        }

        // Direct level assignment following the AddExp curve — no signals fire,
        // so HUD suites keep resetting their snapshots explicitly per AGENTS.md.
        runLevel = Math.Max(1, runLevel);
        player.ExpToNextLevel = 30.0f;
        for (int i = 1; i < runLevel; i++)
            player.ExpToNextLevel = player.ExpToNextLevel * 1.35f + 15.0f;
        player.CurrentLevel = runLevel;
        player.CurrentExp = 0.0f;

        MaxOutSkills(player, skillLevel);

        OrganelleUnlockManager.UnlockAll();
        MaxOutChamber(player);

        if (godmode && player.Stats != null)
        {
            player.Stats.SetBase("max_health", 999999.0f);
            player.Health = 999999.0f;
        }
        return true;
    }

    /// <summary>Headed menu entry: applies --cheats=all / --cheats-reset. Debug builds only; no-op in release.</summary>
    public static void ApplyHeadedMenuCheats()
    {
        if (!OS.IsDebugBuild())
            return;
        if (HasUserArg(CheatArgReset))
        {
            LockToBaseline();
            GD.Print("[Cheats] Meta progression reset to a fresh profile.");
            return;
        }
        if (HasUserArg(CheatArgAll))
        {
            UnlockAllMeta();
            GD.Print("[Cheats] All meta progression unlocked (classes, maps+hard, endless, organelles, tree levels).");
        }
    }

    /// <summary>Headed run entry: maxes the deployed player. Debug builds only; --godmode adds invulnerability.</summary>
    public static void ApplyHeadedRunCheats(Main main)
    {
        if (!OS.IsDebugBuild() || !HasUserArg(CheatArgAll))
            return;
        if (MaxOutPlayer(main, DefaultRunLevel, DefaultSkillLevel, HasUserArg(CheatArgGodmode)))
            GD.Print("[Cheats] Run player maxed out.");
    }

    private static bool HasUserArg(string arg)
    {
        if (OS.GetCmdlineUserArgs().Contains(arg))
            return true;
        // The editor's Run button (F5) spawns the game with no CLI args, so
        // also honor PHAGOCYTE_CHEATS from the environment ("all", "reset",
        // "godmode", comma/space separated). Normalizes both "--cheats=all"
        // and bare "all" to the same token.
        string env = OS.GetEnvironment("PHAGOCYTE_CHEATS");
        if (string.IsNullOrEmpty(env))
            return false;
        string token = NormalizeCheatToken(arg);
        foreach (string part in env.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (NormalizeCheatToken(part) == token)
                return true;
        }
        return false;
    }

    private static string NormalizeCheatToken(string raw)
    {
        string token = raw.Trim().TrimStart('-').ToLowerInvariant();
        if (token.StartsWith("cheats=", StringComparison.Ordinal))
            token = token.Substring("cheats=".Length);
        return token;
    }

    private static void MaxOutSkills(BaseCell player, int skillLevel)
    {
        var sm = player.CellSkillManager;
        if (sm == null || !GodotObject.IsInstanceValid(sm))
            return;

        int target = Math.Max(1, skillLevel);
        var seen = new HashSet<string>();

        foreach (var equipped in sm.ActiveSlots)
        {
            if (equipped == null || !GodotObject.IsInstanceValid(equipped))
                continue;
            seen.Add(equipped.SkillId);
            while (equipped.Level < Math.Min(target, equipped.MaxLevel))
                equipped.Upgrade();
        }
        foreach (var equipped in sm.PassiveSlots)
        {
            if (equipped == null || !GodotObject.IsInstanceValid(equipped))
                continue;
            seen.Add(equipped.SkillId);
            while (equipped.Level < Math.Min(target, equipped.MaxLevel))
                equipped.Upgrade();
        }

        // The innate active in slot 0 is class identity: maxed above, never replaced.
        for (int slot = 0; slot < SkillManager.MaxActiveSlots; slot++)
        {
            if (sm.GetActiveSlot(slot) != null)
                continue;
            if (!TryCreateUnseen(UpgradeManager.ActiveCatalog, seen, out var skill) || skill == null)
                break;
            if (!sm.EquipActive(skill, slot))
            {
                skill.Free();
                continue;
            }
            while (skill.Level < Math.Min(target, skill.MaxLevel))
                skill.Upgrade();
        }
        for (int slot = 0; slot < SkillManager.MaxPassiveSlots; slot++)
        {
            if (sm.GetPassiveSlot(slot) != null)
                continue;
            if (!TryCreateUnseen(UpgradeManager.PassiveCatalog, seen, out var skill) || skill == null)
                break;
            if (!sm.EquipPassive(skill, slot))
            {
                skill.Free();
                continue;
            }
            while (skill.Level < Math.Min(target, skill.MaxLevel))
                skill.Upgrade();
        }
    }

    private static bool TryCreateUnseen(Godot.Collections.Array<Godot.Collections.Dictionary> catalog, HashSet<string> seen, out BaseSkill? skill)
    {
        skill = null;
        foreach (var entry in catalog)
        {
            if (!entry.TryGetValue("id", out var idVar) || !entry.TryGetValue("class_type", out var classVar))
                continue;
            string id = idVar.AsString();
            if (string.IsNullOrEmpty(id) || seen.Contains(id))
                continue;
            Type? skillType = Type.GetType(classVar.AsString());
            if (skillType == null)
                continue;
            if (Activator.CreateInstance(skillType) is not BaseSkill created)
                continue;
            seen.Add(id);
            skill = created;
            return true;
        }
        return false;
    }

    private static void MaxOutChamber(BaseCell player)
    {
        var chamber = player.CellOrganelleChamber;
        if (chamber == null || !GodotObject.IsInstanceValid(chamber))
            return;

        var ids = new List<string>();
        foreach (var keyVar in GameManager.OrganelleCatalog.Keys)
            ids.Add(keyVar.AsString());
        foreach (string id in ids)
        {
            if (!chamber.Owns(id))
                chamber.AddToBackpack(id);
        }

        // Best-effort equip: first legal backpack id per free slot.
        for (int slot = 0; slot < OrganelleChamber.MaxSlots; slot++)
        {
            if (!string.IsNullOrEmpty(chamber.GetSlot(slot)))
                continue;
            var pack = new List<string>(chamber.Backpack);
            foreach (string id in pack)
            {
                if (chamber.CanEquip(id, slot, out _))
                {
                    chamber.Equip(id, slot);
                    break;
                }
            }
        }
    }
}

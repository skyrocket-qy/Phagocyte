using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Core;
using Phagocyte.Endgame;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Directors;

/// <summary>
/// Run lifecycle owner: achievement event wiring plus combat settlement
/// (victory/defeat validation, telemetry, records, leaderboards, modal).
/// Extracted from <c>Main.EndRun / ConnectAchievementEvents / RecordTreeLevel</c>.
/// Settlement is requested through <see cref="IRunContext.EndRun"/>; the
/// <c>RunEnded</c> flag itself stays on <see cref="Main"/>.
/// </summary>
public partial class RunSettlementService : Node
{
    /// <summary>Run context (Main). Must be assigned before use.</summary>
    public IRunContext? Context { get; set; }

    /// <summary>
    /// Ends the run and persists the settlement record. Returns true when the
    /// run actually settled (Main then raises its RunEnded flag).
    /// Victory (docs/record.md §3.1): survive to 15:00 AND neutralize the terminal boss.
    /// Defeat (docs/record.md §3.2): cell membrane integrity reaches zero (SIRS).
    /// </summary>
    public bool TryEndRun(bool victory, string cause = "")
    {
        var ctx = Context;
        if (ctx == null || ctx.RunEnded)
            return false;

        if (victory && !RunRecordManager.CanSettleVictory(ctx.EnvironmentTime, ctx.TerminalBossNeutralized, ctx.IsEndlessRun))
        {
            if (ctx.IsEndlessRun)
            {
                GD.PushWarning("[Main] Endless overdrive runs can only settle as defeat (membrane rupture).");
            }
            else
            {
                GD.PushWarning($"[Main] Victory rejected: 15:00 survival + terminal boss neutralization required " +
                               $"(survival={ctx.EnvironmentTime:F1}s, boss_neutralized={ctx.TerminalBossNeutralized}).");
            }
            return false;
        }

        if (victory)
        {
            AudioManager.Instance?.PlayBgm("victory", 0.3f);
            AudioManager.Instance?.PlaySfx("wave_complete");
            AchievementManager.RecordMapClear(ctx.MapId, ctx.RunDifficulty == RunRecordManager.DifficultyHard);
        }
        else
        {
            AudioManager.Instance?.PlayBgm("defeat", 0.3f);
            AudioManager.Instance?.PlaySfx("game_over");
        }

        if (string.IsNullOrEmpty(cause))
        {
            cause = victory ? RunRecordManager.CauseSpecificNeutralization : RunRecordManager.CauseMembraneRupture;
        }

        var cell = ctx.Player as BaseCell;
        string classId = GameManager.SelectedClass;

        var skillIds = new List<string>();
        var sm = ctx.Player?.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm != null)
        {
            foreach (var skill in sm.ActiveSlots)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.SkillId))
                    skillIds.Add(skill.SkillId);
            }
        }

        int kills = RunTelemetryManager.Instance?.KillCount ?? 0;
        int killScore = RunTelemetryManager.Instance?.KillScore ?? 0;

        RunTelemetryManager.Instance?.EndRun();

        float afflictionMultiplier = ctx.IsEndlessRun ? AfflictionManager.ScoreMultiplier : 1.0f;
        string[] afflictionIds = ctx.IsEndlessRun
            ? AfflictionManager.SelectedIds.ToArray()
            : Array.Empty<string>();

        var record = RunRecordManager.RecordRun(
            victory ? RunRecordManager.ResultVictory : RunRecordManager.ResultDefeat,
            classId,
            ctx.MapId,
            ctx.EnvironmentTime,
            cell?.CurrentLevel ?? 1,
            cell?.DigestedCount ?? 0,
            PassiveTreeManager.GetSpentPoints(classId),
            skillIds.ToArray(),
            ctx.TerminalBossNeutralized,
            cause,
            kills,
            killScore,
            ctx.RunDifficulty,
            ctx.IsEndlessRun,
            afflictionMultiplier,
            afflictionIds);

        if (RunTelemetryManager.Instance != null)
        {
            record["telemetry"] = RunTelemetryManager.Instance.GetTelemetryDictionary();
        }

        // Steam global leaderboards: reserved upload for endless overdrive runs
        // (docs/endgame.md §5.3). No-op without the SDK; the local record above
        // remains the offline fallback.
        if (ctx.IsEndlessRun)
        {
            SteamBridge.SubmitEndlessLeaderboard(
                Mathf.RoundToInt(ctx.EnvironmentTime),
                record.GetValueOrDefault("score", 0).AsInt32());
        }

        if (ctx.HudNode != null)
        {
            ctx.HudNode.PauseInputSuppressed = true;
            ctx.HudNode.ResumeGame();
        }

        if (ctx is Node ctxNode)
        {
            var modal = ctxNode.GetNodeOrNull<RunRecordsModal>("UIOverlay/RunRecordsModal");
            if (modal != null)
                modal.OpenSettlement(record);

            ctxNode.GetTree().Paused = true;
        }

        return true;
    }

    /// <summary>
    /// Dual-track achievement wiring (typed C# signals with GDScript fallback)
    /// plus membrane-rupture settlement. Former <c>Main.ConnectAchievementEvents</c>.
    /// </summary>
    public void ConnectRunEvents(CharacterBody2D player)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        if (player is BaseCell bc)
        {
            bc.PathogenDigested += (pathogen, atp) =>
            {
                AchievementManager.RecordEvent("pathogen_digested", bc.DigestedCount);
            };

            bc.LevelUp += (lvl) =>
            {
                AchievementManager.RecordEvent("level_up", lvl);
                RecordTreeLevel(lvl);
            };

            bc.StatsChanged += (health, maxHealth, radiusRatio) =>
            {
                AchievementManager.RecordEvent("radius_ratio", radiusRatio);
            };

            bc.Died += () => ctx.EndRun(false, RunRecordManager.CauseMembraneRupture);
        }
        else
        {
            if (player.HasSignal("pathogen_digested"))
            {
                player.Connect("pathogen_digested", Callable.From((Node2D _p, float _atp) =>
                {
                    var digVal = player.Get("digested_count");
                    AchievementManager.RecordEvent("pathogen_digested", digVal.VariantType == Variant.Type.Int ? digVal.AsInt32() : 0);
                }));
            }
            if (player.HasSignal("level_up"))
            {
                player.Connect("level_up", Callable.From((int lvl) =>
                {
                    AchievementManager.RecordEvent("level_up", lvl);
                    RecordTreeLevel(lvl);
                }));
            }
            if (player.HasSignal("stats_changed"))
            {
                player.Connect("stats_changed", Callable.From((float _h, float _mh, float rr) =>
                {
                    AchievementManager.RecordEvent("radius_ratio", rr);
                }));
            }
            if (player.HasSignal("died"))
            {
                player.Connect("died", Callable.From(() => ctx.EndRun(false, RunRecordManager.CauseMembraneRupture)));
            }
        }

        var sm = player.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm != null)
        {
            sm.SkillsChanged += () =>
            {
                int count = 0;
                foreach (var s in sm.ActiveSlots)
                {
                    if (s != null) count++;
                }
                AchievementManager.RecordEvent("active_skills_count", count);
            };
        }
    }

    private void RecordTreeLevel(int level)
    {
        string classId = GameManager.SelectedClass;
        if (GameManager.ClassData.ContainsKey(classId))
            PassiveTreeManager.RecordRunLevel(classId, level);
    }
}

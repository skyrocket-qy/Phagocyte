using Godot;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Directors;

/// <summary>
/// Sub-lord + terminal-boss lifecycle (09:00 sub-boss showdown, 15:00 terminal
/// lockdown) plus endless multi-boss incursions. Extracted from Main; the 09:00
/// trigger is evaluated here so <see cref="WaveDirectorComponent"/> never
/// touches boss state — the 15:00 lockdown arrives via
/// <see cref="WaveDirectorComponent.TerminalPhaseReached"/>.
/// Victory/defeat settlement is requested through <see cref="IRunContext.EndRun"/>
/// (served by <see cref="RunSettlementService"/>).
/// </summary>
public partial class BossEncounterManager : Node
{
    /// <summary>Run context (Main). Must be assigned before the first physics tick.</summary>
    public IRunContext? Context { get; set; }

    public bool SubBossTriggered { get; private set; } = false;
    public bool BossLockdownActive { get; private set; } = false;
    public bool SubBossRewardGranted { get; private set; } = false;
    public BaseEnemy? SubBoss { get; private set; }
    public BaseEnemy? TerminalBoss { get; private set; }

    /// <summary>Set only when the terminal primary boss is actually killed (victory criterion).</summary>
    public bool TerminalBossNeutralized { get; private set; } = false;

    private readonly List<BaseEnemy> _raidBosses = new();
    private int _nextRaidCycle = 2; // first incursion at 18:00 (cycle 2)

    /// <summary>Alive multi-boss incursion bosses drawn from other organs.</summary>
    public IReadOnlyList<BaseEnemy> RaidBosses => _raidBosses;

    /// <summary>Alive boss-incursion count (GDScript-friendly scalar view).</summary>
    public int RaidBossCount => _raidBosses.Count;

    /// <summary>Evaluates the 09:00 sub-boss trigger. Call once per physics frame.</summary>
    public void PhysicsTick(float dt)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        if (!SubBossTriggered && ctx.EnvironmentTime >= PathogenSpawner.EscalationInterval * 3.0f)
        {
            SubBossTriggered = true;
            TriggerSubBossEncounter();
        }
    }

    public void TriggerSubBossEncounter()
    {
        FrameSpikeLog.MarkBoss();
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        SubBoss = PathogenSpawner.SpawnSubBoss(container, player, ctx.ArenaSize, ctx.MapId, ctx.EnvironmentTime);
        if (SubBoss != null)
        {
            SubBoss.EnemyDied += OnSubBossDefeated;
            SubBoss.Digested += OnSubBossDefeated;
        }

        AudioManager.Instance?.PlaySfx("wave_start", -2.0f);
        AudioManager.Instance?.PlayBgm("boss");
        GD.Print("[WaveDirector] 09:00 Sub-boss showdown started.");
    }

    public void EnterBossLockdown()
    {
        FrameSpikeLog.MarkBoss();
        var ctx = Context;
        BossLockdownActive = true;

        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
        {
            ctx?.EndRun(false, RunRecordManager.CauseSystemFailure);
            return;
        }

        TerminalBoss = PathogenSpawner.SpawnTerminalBoss(container, player, ctx.ArenaSize, ctx.MapId, ctx.EnvironmentTime);
        if (TerminalBoss == null)
        {
            // No boss entity available for this map: the clear condition cannot be met.
            ctx.EndRun(false, RunRecordManager.CauseSystemFailure);
            return;
        }

        TerminalBoss.EnemyDied += OnTerminalBossDefeated;
        TerminalBoss.Digested += OnTerminalBossDefeated;
        AudioManager.Instance?.PlaySfx("wave_start", -2.0f);
        AudioManager.Instance?.PlayBgm(ctx.IsEndlessRun ? "boss_final" : "boss");
        GD.Print("[WaveDirector] 15:00 Terminal boss lockdown! Specific neutralization required.");
    }

    private void OnSubBossDefeated(BaseEnemy boss)
    {
        if (SubBossRewardGranted)
            return;
        SubBossRewardGranted = true;

        AudioManager.Instance?.PlaySfx("wave_complete");
        AudioManager.Instance?.PlayMapBgm(Context?.MapId ?? "");

        // Guaranteed superweapon chest: the epigenetic evolution system is not
        // online yet, so the reward is currently a guaranteed level-up draft.
        if (Context?.Player is BaseCell cell)
        {
            float missing = Mathf.Max(0.0f, cell.ExpToNextLevel - cell.CurrentExp);
            if (missing > 0.0f)
                cell.AddExp(missing);
        }

        GD.Print("[WaveDirector] Sub-boss neutralized. Guaranteed evolution reward granted.");
    }

    private void OnTerminalBossDefeated(BaseEnemy boss)
    {
        var ctx = Context;
        if (ctx == null || ctx.RunEnded)
            return;

        TerminalBossNeutralized = true;

        if (ctx.IsEndlessRun)
        {
            // Endless overdrive (docs/endgame.md §3.1): the 15:00 clear does not
            // settle the run — the uncapped timeline keeps escalating.
            TerminalBoss = null;
            BossLockdownActive = false;
            AudioManager.Instance?.PlaySfx("wave_complete");
            AudioManager.Instance?.PlayBgm("battle_bgm_2");
            GD.Print("[WaveDirector] Terminal boss neutralized. Endless overdrive continues past 15:00.");
            return;
        }

        GD.Print("[WaveDirector] Terminal boss neutralized. Specific neutralization complete.");
        ctx.EndRun(true, RunRecordManager.CauseSpecificNeutralization);
    }

    public void CheckTerminalBossState()
    {
        var ctx = Context;
        if (TerminalBoss == null || ctx == null)
            return;

        if (!GodotObject.IsInstanceValid(TerminalBoss) || TerminalBoss.IsQueuedForDeletion())
        {
            TerminalBoss = null;

            if (ctx.IsEndlessRun)
            {
                // No clear criterion to protect in endless mode: release the lockdown.
                BossLockdownActive = false;
                GD.PushWarning("[WaveDirector] Terminal boss vanished in endless mode; lockdown released.");
                return;
            }

            // The boss vanished without a confirmed kill: the clear criterion is not met.
            GD.PushWarning("[WaveDirector] Terminal boss vanished without a kill; settling as defeat.");
            ctx.EndRun(false, RunRecordManager.CauseSystemFailure);
        }
    }

    /// <summary>
    /// Multi-boss incursion (docs/endgame.md §3.3): every 3-minute overdrive
    /// cycle from 18:00 draws two terminal bosses from other organs onto the
    /// field; from 30:00 the siege escalates to a triple-boss assault.
    /// Trigger timing is owned by <see cref="OverdriveDirector"/>; the raid
    /// lifecycle lives here with all other boss state.
    /// </summary>
    public void ProcessBossRaids(int cycle)
    {
        var ctx = Context;
        _raidBosses.RemoveAll(b => !GodotObject.IsInstanceValid(b) || b.IsQueuedForDeletion());

        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (cycle < _nextRaidCycle || container == null || player == null || ctx == null)
            return;

        _nextRaidCycle = cycle + 1;
        FrameSpikeLog.MarkBoss();

        bool tripleSiege = ctx.EnvironmentTime >= 1800.0f; // 30:00+ terminal siege
        int count = tripleSiege ? 3 : 2;

        var candidates = new List<string>();
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string candidateMap = keyVar.AsString();
            if (candidateMap != ctx.MapId)
                candidates.Add(candidateMap);
        }

        // Fisher-Yates shuffle: random cross-organ draw without duplicates.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = (int)GD.RandRange(0, i);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var bossNames = new List<string>();
        for (int i = 0; i < count && i < candidates.Count; i++)
        {
            float angle = Mathf.Tau * i / count + (float)GD.RandRange(-0.4, 0.4);
            var boss = PathogenSpawner.SpawnRaidBoss(container, player, ctx.ArenaSize, candidates[i], ctx.EnvironmentTime, angle);
            if (boss == null)
                continue;

            boss.EnemyDied += OnRaidBossDefeated;
            _raidBosses.Add(boss);
            bossNames.Add(Tr(boss.DisplayNameKey));
        }

        if (bossNames.Count > 0)
        {
            string title = Tr("OVERDRIVE_RAID_TITLE");
            string desc = TextFormatter.Format(
                Tr(tripleSiege ? "OVERDRIVE_RAID_TRIPLE_FMT" : "OVERDRIVE_RAID_TWIN_FMT"),
                string.Join(" · ", bossNames));
            ctx.HudNode?.ShowOverdriveAlert(title, desc);
            GD.Print($"[Overdrive] Boss incursion ×{bossNames.Count}: {string.Join(", ", bossNames)}");
        }
    }

    private void OnRaidBossDefeated(BaseEnemy boss)
    {
        _raidBosses.Remove(boss);
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print($"[Overdrive] Raid boss neutralized: {boss.EnemyId}.");
    }
}

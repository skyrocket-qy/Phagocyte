using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Pathogen Spawner & Wave Director.
/// Drives the 15:00 standard infection timeline with a 3-minute escalation loop:
///   00:00-03:00 colonization  -> 03:00 elite raid
///   03:00-06:00 local inflammation -> 06:00 first swarm + elite pincer
///   06:00-09:00 tissue infiltration -> 09:00 sub-boss showdown
///   09:00-12:00 systemic spread -> 12:00 extreme swarm
///   12:00-15:00 terminal crisis -> 15:00 terminal boss lockdown
/// </summary>
public static class PathogenSpawner
{
    /// <summary>Length of one escalation phase (03:00).</summary>
    public const float EscalationInterval = 180.0f;

    /// <summary>Standard run duration (15:00).</summary>
    public const float StandardRunDuration = 900.0f;

    public const int PhaseCount = 5;

    // --- Screen Active Cap & Kill-Driven Dynamic Backfill ---
    /// <summary>Normal wave on-screen active pathogen cap.</summary>
    public const int MaxActiveNormal = 300;

    /// <summary>Extreme swarm event on-screen active pathogen cap.</summary>
    public const int MaxActiveSwarm = 450;

    /// <summary>Duration of the 06:00 / 12:00 swarm window using the raised cap.</summary>
    public const float SwarmWindowSeconds = 30.0f;

    /// <summary>Maximum backfill spawns per poll to avoid frame spikes.</summary>
    public const int MaxBackfillPerTick = 24;

    /// <summary>Minimum distance outside the camera view for backfill spawns.</summary>
    public const float BackfillMarginMin = 150.0f;

    /// <summary>Maximum distance outside the camera view for backfill spawns.</summary>
    public const float BackfillMarginMax = 250.0f;

    private static readonly string[] Phase1Pool =
    {
        "staph", "staph", "norovirus", "e_coli", "s_virus"
    };

    private static readonly string[] Phase2Pool =
    {
        "staph", "e_coli", "norovirus", "s_virus",
        "pseudomonas", "tb", "h_pylori", "rabies", "candida", "plasmodium"
    };

    private static readonly string[] Phase3Pool =
    {
        "pseudomonas", "tb", "h_pylori", "rabies", "candida", "plasmodium",
        "flu_drift", "tetanus", "anthrax_spore", "varicella_zoster", "aspergillus", "toxoplasma", "hiv", "ebola"
    };

    private static readonly string[] Phase4Pool =
    {
        "flu_drift", "tetanus", "anthrax_spore", "varicella_zoster", "aspergillus", "toxoplasma", "hiv", "ebola",
        "malignant_cell", "prion"
    };

    private static readonly string[] Phase5Pool =
    {
        "malignant_cell", "prion", "ebola", "hiv",
        "anthrax_spore", "toxoplasma", "flu_drift", "aspergillus"
    };

    private static readonly string[] SwarmPool =
    {
        "norovirus", "norovirus", "staph", "s_virus"
    };

    public static BaseEnemy? CreatePathogen(string pathogenId)
    {
        return pathogenId switch
        {
            "staph" => new StaphEnemy(),
            "s_virus" => new SVirusEnemy(),
            "flu_drift" => new FluDriftEnemy(),
            "malignant_cell" => new MalignantCellEnemy(),
            "pseudomonas" => new PseudomonasEnemy(),
            "e_coli" => new EColiEnemy(),
            "tb" => new TbEnemy(),
            "tetanus" => new TetanusEnemy(),
            "h_pylori" => new HpyloriEnemy(),
            "anthrax_spore" => new AnthraxSporeEnemy(),
            "hiv" => new HivEnemy(),
            "rabies" => new RabiesEnemy(),
            "ebola" => new EbolaEnemy(),
            "norovirus" => new NorovirusEnemy(),
            "varicella_zoster" => new VaricellaZosterEnemy(),
            "candida" => new CandidaEnemy(),
            "aspergillus" => new AspergillusEnemy(),
            "plasmodium" => new PlasmodiumCarrierEnemy(),
            "toxoplasma" => new ToxoplasmaEnemy(),
            "prion" => new PrionEnemy(),
            "anthrax_bacillus" => new AnthraxBacillus(),
            "prion_fragment" => new PrionFragment(),
            "plasmodium_merozoite" => new PlasmodiumMerozoite(),
            "streptococcus_chain_lord" => new StreptococcusChainLord(),
            "flu_drift_cyclone" => new FluDriftCyclone(),
            "tb_granuloma_behemoth" => new TbGranulomaBehemoth(),
            "vaca_secretor" => new VacASecretor(),
            "toxoplasma_mega_cyst" => new ToxoplasmaMegaCyst(),
            "tachyzoite" => new Tachyzoite(),
            "mrsa_super_colony" => new MrsASuperColony(),
            "mrsa_enraged_elite" => new MrsaEnragedElite(),
            "syncytial_mega_capsid" => new SyncytialMegaCapsid(),
            "plasmodium_macro_schizont" => new PlasmodiumMacroSchizont(),
            "hpylori_biofilm_core" => new HpyloriBiofilmCore(),
            "prpsc_amyloid_aggregate" => new PrpscAmyloidAggregate(),
            _ => new StaphEnemy()
        };
    }

    /// <summary>
    /// Resolves the timeline phase index (0..4) for a given survival time.
    /// </summary>
    public static int GetPhaseIndex(float gameTime)
    {
        if (gameTime < EscalationInterval)
            return 0;
        if (gameTime < EscalationInterval * 2.0f)
            return 1;
        if (gameTime < EscalationInterval * 3.0f)
            return 2;
        if (gameTime < EscalationInterval * 4.0f)
            return 3;
        return 4;
    }

    public static string[] GetPhasePool(int phaseIndex)
    {
        return Mathf.Clamp(phaseIndex, 0, PhaseCount - 1) switch
        {
            0 => Phase1Pool,
            1 => Phase2Pool,
            2 => Phase3Pool,
            3 => Phase4Pool,
            _ => Phase5Pool
        };
    }

    public static string GetPhaseNameKey(int phaseIndex)
    {
        return Mathf.Clamp(phaseIndex, 0, PhaseCount - 1) switch
        {
            0 => "WAVE_PHASE_COLONIZATION",
            1 => "WAVE_PHASE_INFLAMMATION",
            2 => "WAVE_PHASE_INFILTRATION",
            3 => "WAVE_PHASE_DISSEMINATION",
            _ => "WAVE_PHASE_TERMINAL"
        };
    }

    /// <summary>
    /// Standard phase pool wave used by the periodic population maintenance.
    /// </summary>
    public static void SpawnWave(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, float gameTime, int count = 1)
    {
        if (enemyContainer == null || player == null)
            return;

        string[] pool = GetPhasePool(GetPhaseIndex(gameTime));
        for (int i = 0; i < count; i++)
        {
            string chosenId = pool[(int)GD.RandRange(0, pool.Length - 1)];
            SpawnSingle(enemyContainer, player, arenaSize, chosenId, 450.0f, 1000.0f);
        }
    }

    /// <summary>
    /// 03:00 / 06:00 escalation elite. Heavier, shielded and worth bonus ATP.
    /// </summary>
    public static BaseEnemy? SpawnElite(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, float gameTime, int tier = 1, float spawnAngle = -1.0f)
    {
        if (enemyContainer == null || player == null)
            return null;

        string[] pool = GetPhasePool(GetPhaseIndex(gameTime));
        string chosenId = pool[(int)GD.RandRange(0, pool.Length - 1)];
        var enemy = CreatePathogen(chosenId);
        if (enemy == null)
            return null;

        ApplyEliteBoost(enemy, tier);

        enemy.GlobalPosition = spawnAngle >= 0.0f
            ? GetPointAtAngle(player.GlobalPosition, arenaSize, spawnAngle, 560.0f)
            : GetSpawnPoint(player.GlobalPosition, arenaSize, (float)GD.RandRange(520.0f, 820.0f));

        enemyContainer.AddChild(enemy);
        return enemy;
    }

    /// <summary>
    /// 06:00 / 12:00 swarm tide burst. Returns the number of spawned units.
    /// </summary>
    public static int SpawnSwarm(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, float gameTime, int clusters)
    {
        if (enemyContainer == null || player == null)
            return 0;

        int spawned = 0;
        for (int i = 0; i < clusters; i++)
        {
            string chosenId = SwarmPool[(int)GD.RandRange(0, SwarmPool.Length - 1)];
            spawned += SpawnSingle(enemyContainer, player, arenaSize, chosenId, 420.0f, 900.0f);
        }
        return spawned;
    }

    /// <summary>
    /// 09:00 secondary lord. Guaranteed superweapon chest drop is routed by Main.
    /// </summary>
    public static BaseEnemy? SpawnSubBoss(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, string mapId)
    {
        if (enemyContainer == null || player == null)
            return null;

        var enemy = CreateSubBoss(mapId);
        if (enemy == null)
            return null;

        AttachBossPhases(enemy, 120.0f);

        enemy.GlobalPosition = GetSpawnPoint(player.GlobalPosition, arenaSize, 520.0f);
        enemyContainer.AddChild(enemy);
        return enemy;
    }

    /// <summary>
    /// Instantiates the map-specific 09:00 sub-boss entity.
    /// </summary>
    public static BaseEnemy? CreateSubBoss(string mapId)
    {
        return mapId switch
        {
            "alveolar_space" => new FluDriftCyclone(),
            "hepatic_sinusoid" => new TbGranulomaBehemoth(),
            "gastric_lumen" => new VacASecretor(),
            "blood_brain_barrier" => new ToxoplasmaMegaCyst(),
            _ => new StreptococcusChainLord()
        };
    }

    /// <summary>
    /// 15:00 terminal primary pathogen boss for the lockdown showdown.
    /// </summary>
    public static BaseEnemy? SpawnTerminalBoss(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, string mapId)
    {
        if (enemyContainer == null || player == null)
            return null;

        var enemy = CreateTerminalBoss(mapId);
        if (enemy == null)
            return null;

        AttachBossPhases(enemy, 150.0f);

        enemy.GlobalPosition = GetSpawnPoint(player.GlobalPosition, arenaSize, 460.0f);
        enemyContainer.AddChild(enemy);
        return enemy;
    }

    /// <summary>
    /// Instantiates the map-specific 15:00 terminal boss entity.
    /// </summary>
    public static BaseEnemy? CreateTerminalBoss(string mapId)
    {
        return mapId switch
        {
            "alveolar_space" => new SyncytialMegaCapsid(),
            "hepatic_sinusoid" => new PlasmodiumMacroSchizont(),
            "gastric_lumen" => new HpyloriBiofilmCore(),
            "blood_brain_barrier" => new PrpscAmyloidAggregate(),
            _ => new MrsASuperColony()
        };
    }

    /// <summary>
    /// Kill-driven dynamic backfill: spawns exactly <paramref name="amount"/> single
    /// pathogens just outside the camera view (150-250px past the visible edge) so
    /// the active population instantly refills after kills or engulfment.
    /// </summary>
    public static int Backfill(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, Vector2 viewWorldSize, float gameTime, int amount)
    {
        if (enemyContainer == null || player == null || amount <= 0)
            return 0;

        string[] pool = GetPhasePool(GetPhaseIndex(gameTime));
        int spawned = 0;

        for (int i = 0; i < amount; i++)
        {
            string chosenId = pool[(int)GD.RandRange(0, pool.Length - 1)];
            var enemy = CreatePathogen(chosenId);
            if (enemy == null)
                continue;

            float margin = (float)GD.RandRange(BackfillMarginMin, BackfillMarginMax);
            enemy.GlobalPosition = GetOffscreenSpawnPoint(player.GlobalPosition, arenaSize, viewWorldSize, margin);
            enemyContainer.AddChild(enemy);
            spawned++;
        }

        return spawned;
    }

    /// <summary>
    /// Picks a point on the camera view boundary expanded by <paramref name="margin"/>px.
    /// Falls back to a fixed radius when the visible size is unknown.
    /// </summary>
    public static Vector2 GetOffscreenSpawnPoint(Vector2 center, Vector2 arenaSize, Vector2 viewWorldSize, float margin)
    {
        if (viewWorldSize.X <= 1.0f || viewWorldSize.Y <= 1.0f)
            return GetSpawnPoint(center, arenaSize, 700.0f + margin);

        Vector2 half = viewWorldSize * 0.5f + new Vector2(margin, margin);
        Vector2 offset = RandomEdgeOffset(half);

        Vector2 candidate = center + offset;
        Vector2 clamped = ClampToArena(candidate, arenaSize);

        // Near the arena border the outward edge can be clamped back on screen:
        // mirror the offset to the opposite edge so the spawn stays out of view.
        if (candidate.DistanceSquaredTo(clamped) > 1.0f)
        {
            clamped = ClampToArena(center - offset, arenaSize);
        }

        return clamped;
    }

    private static Vector2 RandomEdgeOffset(Vector2 half)
    {
        float t = GD.Randf();
        return (int)GD.RandRange(0, 3) switch
        {
            0 => new Vector2(Mathf.Lerp(-half.X, half.X, t), -half.Y),
            1 => new Vector2(half.X, Mathf.Lerp(-half.Y, half.Y, t)),
            2 => new Vector2(Mathf.Lerp(-half.X, half.X, t), half.Y),
            _ => new Vector2(-half.X, Mathf.Lerp(-half.Y, half.Y, t))
        };
    }

    private static Vector2 ClampToArena(Vector2 pos, Vector2 arenaSize)
    {
        float halfW = (arenaSize.X * 0.5f) - 80.0f;
        float halfH = (arenaSize.Y * 0.5f) - 80.0f;
        pos.X = Mathf.Clamp(pos.X, -halfW, halfW);
        pos.Y = Mathf.Clamp(pos.Y, -halfH, halfH);
        return pos;
    }

    private static int SpawnSingle(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, string chosenId, float minDist, float maxDist)
    {
        float dist = (float)GD.RandRange(minDist, maxDist);

        if (chosenId == "staph")
        {
            // Cluster spawn (3 cocci)
            Vector2 center = GetSpawnPoint(player.GlobalPosition, arenaSize, dist);
            for (int s = 0; s < 3; s++)
            {
                var staph = new StaphEnemy
                {
                    GlobalPosition = center + new Vector2((float)GD.RandRange(-30, 30), (float)GD.RandRange(-30, 30)),
                    FibrinShield = 1
                };
                enemyContainer.AddChild(staph);
            }
            return 3;
        }

        if (chosenId == "norovirus")
        {
            // Micro-swarm (10 units)
            Vector2 center = GetSpawnPoint(player.GlobalPosition, arenaSize, dist);
            for (int n = 0; n < 10; n++)
            {
                var noro = new NorovirusEnemy
                {
                    GlobalPosition = center + new Vector2((float)GD.RandRange(-50, 50), (float)GD.RandRange(-50, 50))
                };
                enemyContainer.AddChild(noro);
            }
            return 10;
        }

        var enemy = CreatePathogen(chosenId);
        if (enemy == null)
            return 0;

        enemy.GlobalPosition = GetSpawnPoint(player.GlobalPosition, arenaSize, dist);
        enemyContainer.AddChild(enemy);
        return 1;
    }

    private static void ApplyEliteBoost(BaseEnemy enemy, int tier)
    {
        float hpMult = 1.0f + 0.6f * tier;
        enemy.MaxHealth *= hpMult;
        enemy.Armor += 1.0f * tier;
        enemy.AtpValue *= 2.0f + tier;
        enemy.IsElite = true;
        enemy.Scale *= 1.0f + 0.12f * tier;
    }

    private static void AttachBossPhases(BaseEnemy enemy, float hardEnrageSeconds)
    {
        var phases = new BossPhaseComponent
        {
            Name = "BossPhaseComponent",
            BossId = enemy.EnemyId,
            HardEnrageSeconds = hardEnrageSeconds
        };
        phases.SetupDefaultPhases();
        enemy.AddChild(phases);
    }

    private static Vector2 GetSpawnPoint(Vector2 playerPos, Vector2 arenaSize, float dist)
    {
        float angle = GD.Randf() * Mathf.Tau;
        return ClampToArena(playerPos + Vector2.FromAngle(angle) * dist, arenaSize);
    }

    private static Vector2 GetPointAtAngle(Vector2 playerPos, Vector2 arenaSize, float angle, float dist)
    {
        return ClampToArena(playerPos + Vector2.FromAngle(angle) * dist, arenaSize);
    }
}

using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Pathogen Spawner & Wave Director.
/// Controls pathological enemy wave escalation based on survival time and pathology stages.
/// </summary>
public static class PathogenSpawner
{
    private static readonly string[] Phase1Pool = new[]
    {
        "staph", "staph", "norovirus", "e_coli", "s_virus"
    };

    private static readonly string[] Phase2Pool = new[]
    {
        "staph", "e_coli", "norovirus", "s_virus",
        "pseudomonas", "tb", "h_pylori", "rabies", "candida", "plasmodium"
    };

    private static readonly string[] Phase3Pool = new[]
    {
        "pseudomonas", "tb", "h_pylori", "rabies", "candida", "plasmodium",
        "flu_drift", "tetanus", "anthrax_spore", "varicella_zoster", "aspergillus", "toxoplasma", "hiv", "ebola"
    };

    private static readonly string[] Phase4Pool = new[]
    {
        "flu_drift", "tetanus", "anthrax_spore", "varicella_zoster", "aspergillus", "toxoplasma", "hiv", "ebola",
        "malignant_cell", "prion"
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
            _ => new StaphEnemy()
        };
    }

    public static void SpawnWave(Node2D enemyContainer, CharacterBody2D player, Vector2 arenaSize, float gameTime, int count = 1)
    {
        if (enemyContainer == null || player == null)
            return;

        string[] pool = gameTime switch
        {
            < 30.0f => Phase1Pool,
            < 90.0f => Phase2Pool,
            < 180.0f => Phase3Pool,
            _ => Phase4Pool
        };

        for (int i = 0; i < count; i++)
        {
            string chosenId = pool[(int)GD.RandRange(0, pool.Length - 1)];

            if (chosenId == "staph")
            {
                // Cluster spawn (3 cocci)
                Vector2 center = GetSpawnPoint(player.GlobalPosition, arenaSize, (float)GD.RandRange(450, 950));
                for (int s = 0; s < 3; s++)
                {
                    var staph = new StaphEnemy
                    {
                        GlobalPosition = center + new Vector2((float)GD.RandRange(-30, 30), (float)GD.RandRange(-30, 30)),
                        FibrinShield = 1
                    };
                    enemyContainer.AddChild(staph);
                }
            }
            else if (chosenId == "norovirus")
            {
                // Micro-swarm (10 units)
                Vector2 center = GetSpawnPoint(player.GlobalPosition, arenaSize, (float)GD.RandRange(450, 950));
                for (int n = 0; n < 10; n++)
                {
                    var noro = new NorovirusEnemy
                    {
                        GlobalPosition = center + new Vector2((float)GD.RandRange(-50, 50), (float)GD.RandRange(-50, 50))
                    };
                    enemyContainer.AddChild(noro);
                }
            }
            else
            {
                var enemy = CreatePathogen(chosenId);
                if (enemy != null)
                {
                    enemy.GlobalPosition = GetSpawnPoint(player.GlobalPosition, arenaSize, (float)GD.RandRange(450, 1000));
                    enemyContainer.AddChild(enemy);
                }
            }
        }
    }

    private static Vector2 GetSpawnPoint(Vector2 playerPos, Vector2 arenaSize, float dist)
    {
        float angle = GD.Randf() * Mathf.Tau;
        Vector2 pos = playerPos + Vector2.FromAngle(angle) * dist;

        float halfW = (arenaSize.X * 0.5f) - 80.0f;
        float halfH = (arenaSize.Y * 0.5f) - 80.0f;
        pos.X = Mathf.Clamp(pos.X, -halfW, halfW);
        pos.Y = Mathf.Clamp(pos.Y, -halfH, halfH);
        return pos;
    }
}

using Godot;
using Phagocyte.Combat;
using Phagocyte.Enemies;

namespace Phagocyte.Directors;

/// <summary>
/// Drifting neutral matter (senescent RBC cover + dormant toxin mines) plus
/// host ulceration ambient acid mist (docs/map.md §4.3). Extracted from Main.
/// Neutrals are not <see cref="BaseEnemy"/> nodes, so they never consume
/// screen-cap slots.
/// </summary>
public partial class NeutralMatterManager : Node
{
    public const int MaxSenescentRbc = 12;
    public const int MaxToxinVesicles = 10;
    public const float NeutralSpawnInterval = 6.0f;

    /// <summary>Run context (Main). Must be assigned before use.</summary>
    public IRunContext? Context { get; set; }

    public int UlcerationPulses => HostUlceration.Pulses;

    private float _neutralSpawnTimer = NeutralSpawnInterval;
    private float _ulcerHazardTimer = 12.0f;

    /// <summary>Opening population: 4 RBCs + 3 toxin vesicles. Call once from _Ready.</summary>
    public void SeedInitialPopulation()
    {
        for (int i = 0; i < 4; i++)
            SpawnSenescentRbc();
        for (int i = 0; i < 3; i++)
            SpawnToxinVesicle();
    }

    /// <summary>
    /// Maintains the drifting neutral matter population (cover RBCs + toxin mines).
    /// </summary>
    public void PhysicsTick(float dt)
    {
        ProcessNeutralMatter(dt);
        ProcessHostUlceration(dt);
    }

    private void ProcessNeutralMatter(float dt)
    {
        var ctx = Context;
        if (ctx?.EnemyContainer == null || ctx.Player == null)
            return;

        _neutralSpawnTimer -= dt;
        if (_neutralSpawnTimer > 0.0f)
            return;

        _neutralSpawnTimer = NeutralSpawnInterval;

        if (CountGroup("senescent_rbc") < MaxSenescentRbc)
            SpawnSenescentRbc();
        if (CountGroup("toxin_vesicle") < MaxToxinVesicles)
            SpawnToxinVesicle();
    }

    public void SpawnSenescentRbc()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        if (container == null || ctx?.Player == null)
            return;

        var rbc = new SenescentRBC
        {
            GlobalPosition = GetNeutralSpawnPoint()
        };
        container.AddChild(rbc);
    }

    public void SpawnToxinVesicle()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        if (container == null || ctx?.Player == null)
            return;

        var vesicle = new DormantToxinVesicle
        {
            GlobalPosition = GetNeutralSpawnPoint()
        };
        container.AddChild(vesicle);
    }

    private Vector2 GetNeutralSpawnPoint()
    {
        var ctx = Context;
        if (ctx?.Player == null)
            return Vector2.Zero;

        float margin = (float)GD.RandRange(100.0, 220.0);
        return PathogenSpawner.GetOffscreenSpawnPoint(ctx.Player.GlobalPosition, ctx.ArenaSize, GetVisibleWorldSize(), margin);
    }

    private Vector2 GetVisibleWorldSize()
    {
        var viewport = GetViewport();
        if (viewport == null)
            return Vector2.Zero;

        Vector2 size = viewport.GetVisibleRect().Size;
        var camera = Context?.MainCamera;
        if (camera != null && camera.Zoom.X > 0.0f && camera.Zoom.Y > 0.0f)
        {
            size.X /= camera.Zoom.X;
            size.Y /= camera.Zoom.Y;
        }
        return size;
    }

    /// <summary>
    /// Host ulceration (docs/map.md): accumulated invader acid degrades the arena by
    /// spawning ambient acid mist near the battle as the ulceration level rises.
    /// </summary>
    private void ProcessHostUlceration(float dt)
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null)
            return;

        if (HostUlceration.Pulses < HostUlceration.EnvironmentThreshold)
            return;

        _ulcerHazardTimer -= dt;
        if (_ulcerHazardTimer > 0.0f)
            return;

        _ulcerHazardTimer = HostUlceration.Pulses >= HostUlceration.SevereThreshold ? 6.0f : 12.0f;

        Vector2 offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(160.0, 360.0);
        var mist = new BioHazardArea
        {
            GlobalPosition = player.GlobalPosition + offset,
            Duration = 6.0f,
            Radius = 70.0f,
            Damage = 5.0f,
            TickInterval = 0.6f,
            SlowFactor = 0.7f,
            SlowsTarget = true,
            DealsDamage = true,
            CoreColor = new Color(0.45f, 0.22f, 0.12f, 0.30f),
            RimColor = new Color(0.85f, 0.45f, 0.20f, 0.60f)
        };
        container.AddChild(mist);
    }

    private int CountGroup(string group)
    {
        return GetTree().GetNodesInGroup(group).Count;
    }
}
